using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Hr;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Payments;
using MOD.Training.Training.Plans;
using MOD.Training.Training.Travel.Integration;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Travel;

/// <summary>
/// Imports Travel's completed per-employee calculation as confirmed Finance records.
/// Travel is the source of truth for these amounts, so Training must not require a
/// second approval. Re-running the import upgrades existing drafts while confirmed
/// rows stay immutable for audit purposes.
/// </summary>
[RemoteService(IsEnabled = false)]
public class SessionTravelPaymentSynchronizer(
    IRepository<TravelAllowancePayment, Guid> paymentRepo,
    IRepository<SessionNomination, Guid> nominationRepo,
    IRepository<CasualCourseNomination, Guid> casualNominationRepo,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<Employee, Guid> employeeRepo,
    IRepository<Rank, Guid> rankRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    IRepository<TrainingExpenseRecovery, Guid> recoveryRepo,
    IRepository<TrainingExpenseRecoveryItem, Guid> recoveryItemRepo)
    : ApplicationService
{
    public async Task SyncAsync(Guid sessionId, TrainingTravelGatewayResult result)
    {
        if (!result.IsCompleted || !result.TravelRequestId.HasValue || result.Employees.Count == 0)
        {
            return;
        }

        var nominationQuery = await nominationRepo.GetQueryableAsync();
        var nominations = await AsyncExecuter.ToListAsync(
            nominationQuery.Where(x => x.SessionId == sessionId));

        await SyncAsync(
            sessionId,
            casualCourseId: null,
            nominations.Select(x => new NominationLink(x.Id, x.EmployeeId)).ToList(),
            result);
    }

    public async Task SyncCasualCourseAsync(Guid casualCourseId, TrainingTravelGatewayResult result)
    {
        if (!result.IsCompleted || !result.TravelRequestId.HasValue || result.Employees.Count == 0)
        {
            return;
        }

        var nominationQuery = await casualNominationRepo.GetQueryableAsync();
        var nominations = await AsyncExecuter.ToListAsync(
            nominationQuery.Where(x => x.CasualCourseId == casualCourseId));

        await SyncAsync(
            sessionId: null,
            casualCourseId,
            nominations.Select(x => new NominationLink(x.Id, x.EmployeeId)).ToList(),
            result);

        await SyncExpenseRecoveryAsync(casualCourseId, result);
    }

    private async Task SyncExpenseRecoveryAsync(
        Guid casualCourseId,
        TrainingTravelGatewayResult result)
    {
        var course = await casualCourseRepo.GetAsync(casualCourseId);
        if (course.FundingScenario != FundingScenario.FundingSourceCoversCourse
            || !result.TravelRequestId.HasValue)
        {
            return;
        }

        var importedItems = result.Employees
            .SelectMany(x => x.Payments)
            .Where(x => x.Amount != 0m && !string.IsNullOrWhiteSpace(x.FundingSourceVoteCode))
            .Select(x => new
            {
                TypeCode = NormalizeExpenseType(x.TypeCode),
                VoteCode = x.FundingSourceVoteCode.Trim(),
                x.Amount
            })
            .GroupBy(x => new
            {
                TypeCode = x.TypeCode.ToUpperInvariant(),
                VoteCode = x.VoteCode.ToUpperInvariant()
            })
            .Select(group => new ImportedExpenseItem(
                group.First().TypeCode,
                group.First().VoteCode,
                group.Sum(x => x.Amount)))
            .Where(x => x.AmountOMR != 0m)
            .OrderBy(x => x.TypeCode)
            .ToList();

        if (importedItems.Count == 0)
        {
            return;
        }

        var recoveryQuery = await recoveryRepo.WithDetailsAsync(x => x.Items);
        var recovery = await AsyncExecuter.FirstOrDefaultAsync(recoveryQuery.Where(x =>
            x.CasualCourseId == casualCourseId
            && x.TravelRequestId == result.TravelRequestId.Value));

        if (recovery?.Status is TrainingExpenseRecoveryStatus.PartiallySettled
            or TrainingExpenseRecoveryStatus.Settled)
        {
            return;
        }

        var financialItems = await financialItemRepo.GetListAsync(x => x.IsActive);
        var financialItemsByVoteCode = financialItems
            .Where(x => !string.IsNullOrWhiteSpace(x.VoteCode))
            .GroupBy(x => x.VoteCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);

        var valuesChanged = recovery != null && !ExpenseItemsMatch(recovery.Items, importedItems);
        if (recovery != null && !valuesChanged)
        {
            recovery.ExpenseDate = result.CompletedAt ?? recovery.ExpenseDate;
            recovery.Currency = string.IsNullOrWhiteSpace(result.Currency) ? recovery.Currency : result.Currency.Trim();
            recovery.TotalAmountOMR = importedItems.Sum(x => x.AmountOMR);
            return;
        }

        if (recovery == null)
        {
            recovery = new TrainingExpenseRecovery(GuidGenerator.Create())
            {
                TenantId = CurrentTenant.Id,
                CasualCourseId = casualCourseId,
                TravelRequestId = result.TravelRequestId.Value
            };
            await recoveryRepo.InsertAsync(recovery);
        }
        else
        {
            foreach (var oldItem in recovery.Items.ToList())
            {
                await recoveryItemRepo.DeleteAsync(oldItem);
            }

            if (valuesChanged && recovery.Status == TrainingExpenseRecoveryStatus.Reviewed)
            {
                recovery.Status = TrainingExpenseRecoveryStatus.PendingReview;
                recovery.ReviewedAt = null;
                recovery.ReviewedById = null;
                recovery.ReviewNote = null;
            }
        }

        recovery.ExpenseDate = result.CompletedAt ?? Clock.Now;
        recovery.Currency = string.IsNullOrWhiteSpace(result.Currency) ? "OMR" : result.Currency.Trim();
        recovery.TotalAmountOMR = importedItems.Sum(x => x.AmountOMR);

        foreach (var importedItem in importedItems)
        {
            var item = new TrainingExpenseRecoveryItem(GuidGenerator.Create())
            {
                TenantId = CurrentTenant.Id,
                TrainingExpenseRecoveryId = recovery.Id,
                FinancialItemId = financialItemsByVoteCode.TryGetValue(importedItem.VoteCode, out var financialItem)
                    ? financialItem.Id
                    : null,
                ExpenseTypeCode = importedItem.TypeCode,
                FundingSourceVoteCode = importedItem.VoteCode,
                AmountOMR = importedItem.AmountOMR
            };
            await recoveryItemRepo.InsertAsync(item);
        }

    }

    private static string NormalizeExpenseType(string typeCode)
        => string.Equals(typeCode, "Deduction", StringComparison.OrdinalIgnoreCase)
            ? "DailyAllowance"
            : typeCode.Trim();

    private static bool ExpenseItemsMatch(
        IEnumerable<TrainingExpenseRecoveryItem> existing,
        IReadOnlyCollection<ImportedExpenseItem> imported)
    {
        var existingValues = existing.ToDictionary(
            x => $"{x.ExpenseTypeCode}|{x.FundingSourceVoteCode}",
            x => x.AmountOMR,
            StringComparer.OrdinalIgnoreCase);
        var importedValues = imported.ToDictionary(
            x => $"{x.TypeCode}|{x.VoteCode}",
            x => x.AmountOMR,
            StringComparer.OrdinalIgnoreCase);
        return existingValues.Count == importedValues.Count
            && existingValues.All(x => importedValues.TryGetValue(x.Key, out var amount) && amount == x.Value);
    }

    private async Task SyncAsync(
        Guid? sessionId,
        Guid? casualCourseId,
        IReadOnlyList<NominationLink> nominations,
        TrainingTravelGatewayResult result)
    {
        if (nominations.Count == 0 || !result.TravelRequestId.HasValue)
        {
            return;
        }
        var externalRequestId = result.TravelRequestId.Value;

        var employeeIds = nominations.Select(x => x.EmployeeId).Distinct().ToList();
        var employeeQuery = await employeeRepo.GetQueryableAsync();
        var employees = await AsyncExecuter.ToListAsync(
            employeeQuery.Where(x => employeeIds.Contains(x.Id)));
        var employeesByNumber = employees
            .Where(x => !string.IsNullOrWhiteSpace(x.ServiceNumber))
            .GroupBy(x => x.ServiceNumber.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);

        var rankIds = employees.Select(x => x.RankId).Distinct().ToList();
        var rankQuery = await rankRepo.GetQueryableAsync();
        var ranks = await AsyncExecuter.ToListAsync(rankQuery.Where(x => rankIds.Contains(x.Id)));
        var ranksById = ranks.ToDictionary(x => x.Id);

        var paymentQuery = await paymentRepo.GetQueryableAsync();
        var existing = sessionId.HasValue
            ? await AsyncExecuter.ToListAsync(paymentQuery.Where(x => x.SessionId == sessionId.Value))
            : await AsyncExecuter.ToListAsync(paymentQuery.Where(x => x.CasualCourseId == casualCourseId));
        var paymentsByNomination = existing.ToDictionary(x => x.NominationId);
        var existingIds = existing.Select(x => x.Id).ToHashSet();

        foreach (var employeeResult in result.Employees)
        {
            var employeeNumber = employeeResult.EmployeeNumber?.Trim();
            if (string.IsNullOrWhiteSpace(employeeNumber)
                || !employeesByNumber.TryGetValue(employeeNumber, out var employee))
            {
                continue;
            }

            var nomination = nominations.FirstOrDefault(x => x.EmployeeId == employee.Id);
            if (nomination == null)
            {
                continue;
            }

            if (!paymentsByNomination.TryGetValue(nomination.Id, out var payment))
            {
                payment = new TravelAllowancePayment(GuidGenerator.Create())
                {
                    TenantId = CurrentTenant.Id,
                    SessionId = sessionId,
                    CasualCourseId = casualCourseId,
                    NominationId = nomination.Id
                };
                paymentsByNomination[nomination.Id] = payment;
            }
            else if (payment.Status != PaymentStatus.Draft)
            {
                continue;
            }

            payment.PersonnelType = ranksById.TryGetValue(employee.RankId, out var rank)
                && string.Equals(rank.PersonnelType, "Officer", StringComparison.OrdinalIgnoreCase)
                    ? PersonnelType.Officer
                    : PersonnelType.Enlisted;
            payment.TicketAmountOMR = employeeResult.TicketAmount;
            payment.VisaFeesOMR = employeeResult.VisaAmount;
            payment.InsuranceOMR = employeeResult.HealthInsuranceAmount;
            payment.TravelAllowanceOMR = Math.Max(
                0m,
                employeeResult.TravelAllowanceAmount - employeeResult.DeductionAmount);
            payment.ClothingAllowanceOMR = employeeResult.ClothingAllowanceAmount;
            payment.TotalOMR = payment.TicketAmountOMR
                + payment.VisaFeesOMR
                + payment.InsuranceOMR
                + payment.TravelAllowanceOMR
                + payment.ClothingAllowanceOMR;
            payment.ExternalRequestId = externalRequestId.ToString();
            payment.ExternalStatus = result.StatusCode;
            payment.ExternalResponseAt = Clock.Now;
            payment.Status = PaymentStatus.Confirmed;
            payment.ConfirmedAt = result.CompletedAt ?? Clock.Now;
            payment.ConfirmedById = null;

            if (!existingIds.Contains(payment.Id))
            {
                await paymentRepo.InsertAsync(payment);
            }
            else
            {
                await paymentRepo.UpdateAsync(payment);
            }
        }
    }

    private sealed record NominationLink(Guid Id, Guid EmployeeId);
    private sealed record ImportedExpenseItem(string TypeCode, string VoteCode, decimal AmountOMR);
}
