using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Hr;
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
    IRepository<Employee, Guid> employeeRepo,
    IRepository<Rank, Guid> rankRepo)
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
}
