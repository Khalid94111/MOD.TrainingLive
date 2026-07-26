using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Payments.Dtos;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Travel;
using MOD.Training.Training.Travel.Integration;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace MOD.Training.Training.Payments;

/// <summary>
/// Read and audit workflow for actual Training-funded expenses imported from Travel.
/// Records cannot be created, edited, or deleted manually.
/// </summary>
[Authorize(TrainingPaymentsPermissions.Reallocations.View)]
public class TrainingExpenseRecoveryAppService(
    IRepository<TrainingExpenseRecovery, Guid> repository,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    IRepository<IdentityUser, Guid> userRepo,
    CourseNameResolver courseNameResolver,
    ITrainingTravelGateway travelGateway,
    SessionTravelPaymentSynchronizer paymentSynchronizer)
    : ApplicationService, ITrainingExpenseRecoveryAppService
{
    public async Task<TrainingExpenseRecoveryDto> GetAsync(Guid id)
    {
        var query = await repository.WithDetailsAsync(x => x.Items);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id))
            ?? throw new EntityNotFoundException(typeof(TrainingExpenseRecovery), id);
        return (await BuildDtosAsync([entity]))[0];
    }

    public async Task<PagedResultDto<TrainingExpenseRecoveryDto>> GetListAsync(
        TrainingExpenseRecoveryGetListInput input)
    {
        var query = await repository.WithDetailsAsync(x => x.Items);
        if (input.CasualCourseId.HasValue)
        {
            query = query.Where(x => x.CasualCourseId == input.CasualCourseId.Value);
        }
        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        var entities = await AsyncExecuter.ToListAsync(query.OrderBy(x => x.Status).ThenByDescending(x => x.ExpenseDate));
        var dtos = await BuildDtosAsync(entities);

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim();
            dtos = dtos.Where(x =>
                    x.CasualCourseNameAr.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.TravelRequestId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.Items.Any(item =>
                        item.FundingSourceVoteCode.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || (item.FinancialItemNameAr?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)))
                .ToList();
        }

        var totalCount = dtos.Count;
        var page = dtos.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();
        return new PagedResultDto<TrainingExpenseRecoveryDto>(totalCount, page);
    }

    /// <summary>
    /// Recovery path for completed requests that predate this register or whose completion event
    /// could not be consumed. Normal records still arrive through the Travel completion event.
    /// </summary>
    public async Task<int> RefreshAsync()
    {
        var courseQuery = await casualCourseRepo.GetQueryableAsync();
        var courseIds = await AsyncExecuter.ToListAsync(courseQuery
            .Where(x =>
                x.CourseType == CourseType.ExternalInternational
                && x.FundingScenario == FundingScenario.FundingSourceCoversCourse)
            .Select(x => x.Id));

        var synchronized = 0;
        foreach (var courseId in courseIds)
        {
            var result = await travelGateway.GetByTrainingCourseAsync(courseId);
            if (!result.IsCompleted)
            {
                continue;
            }
            await paymentSynchronizer.SyncCasualCourseAsync(courseId, result);
            synchronized++;
        }
        return synchronized;
    }

    [Authorize(TrainingPaymentsPermissions.Reallocations.Review)]
    public async Task<TrainingExpenseRecoveryDto> MarkReviewedAsync(
        Guid id,
        MarkTrainingExpenseRecoveryReviewedDto input)
    {
        var entity = await repository.GetAsync(id);
        if (entity.Status != TrainingExpenseRecoveryStatus.PendingReview)
        {
            throw new BusinessException("Training:ExpenseRecovery:ReviewRequiresPending");
        }
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException();
        }

        entity.Status = TrainingExpenseRecoveryStatus.Reviewed;
        entity.ReviewedAt = Clock.Now;
        entity.ReviewedById = CurrentUser.Id;
        entity.ReviewNote = input.ReviewNote?.Trim();
        await repository.UpdateAsync(entity, autoSave: true);
        return await GetAsync(id);
    }

    [Authorize(TrainingPaymentsPermissions.Reallocations.MarkSettled)]
    public async Task<TrainingExpenseRecoveryDto> MarkItemSettledAsync(
        Guid id,
        Guid itemId,
        MarkTrainingExpenseRecoverySettledDto input)
    {
        var entity = await GetWithItemsAsync(id);
        EnsureCanSettle(entity);
        var item = entity.Items.FirstOrDefault(x => x.Id == itemId)
            ?? throw new EntityNotFoundException(typeof(TrainingExpenseRecoveryItem), itemId);
        if (item.IsSettled)
        {
            throw new BusinessException("Training:ExpenseRecovery:ItemAlreadySettled");
        }

        SettleItem(item, input);
        UpdateSettlementStatus(entity);
        await repository.UpdateAsync(entity, autoSave: true);
        return await GetAsync(id);
    }

    [Authorize(TrainingPaymentsPermissions.Reallocations.MarkSettled)]
    public async Task<TrainingExpenseRecoveryDto> MarkAllSettledAsync(
        Guid id,
        MarkTrainingExpenseRecoverySettledDto input)
    {
        var entity = await GetWithItemsAsync(id);
        EnsureCanSettle(entity);
        foreach (var item in entity.Items.Where(x => !x.IsSettled))
        {
            SettleItem(item, input);
        }

        UpdateSettlementStatus(entity);
        await repository.UpdateAsync(entity, autoSave: true);
        return await GetAsync(id);
    }

    private async Task<TrainingExpenseRecovery> GetWithItemsAsync(Guid id)
    {
        var query = await repository.WithDetailsAsync(x => x.Items);
        return await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id))
            ?? throw new EntityNotFoundException(typeof(TrainingExpenseRecovery), id);
    }

    private void EnsureCanSettle(TrainingExpenseRecovery entity)
    {
        if (entity.Status is not (TrainingExpenseRecoveryStatus.Reviewed
            or TrainingExpenseRecoveryStatus.PartiallySettled))
        {
            throw new BusinessException("Training:ExpenseRecovery:SettlementRequiresReview");
        }
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException();
        }
    }

    private void SettleItem(
        TrainingExpenseRecoveryItem item,
        MarkTrainingExpenseRecoverySettledDto input)
    {
        item.IsSettled = true;
        item.SettledAt = Clock.Now;
        item.SettledById = CurrentUser.Id;
        item.SettlementReference = input.SettlementReference.Trim();
        item.SettlementNote = input.SettlementNote?.Trim();
    }

    private static void UpdateSettlementStatus(TrainingExpenseRecovery entity)
    {
        entity.Status = entity.Items.All(x => x.IsSettled)
            ? TrainingExpenseRecoveryStatus.Settled
            : TrainingExpenseRecoveryStatus.PartiallySettled;
    }

    private async Task<List<TrainingExpenseRecoveryDto>> BuildDtosAsync(
        IReadOnlyList<TrainingExpenseRecovery> entities)
    {
        if (entities.Count == 0)
        {
            return [];
        }

        var courses = await BatchByIdsAsync(casualCourseRepo, entities.Select(x => x.CasualCourseId));
        var courseNames = await courseNameResolver.BatchResolveAsync(
            courses.Values.Select(x => x.TenantCourseId).Distinct().ToList());
        var itemIds = entities.SelectMany(x => x.Items)
            .Where(x => x.FinancialItemId.HasValue)
            .Select(x => x.FinancialItemId!.Value);
        var financialItems = await BatchByIdsAsync(financialItemRepo, itemIds);
        var userIds = entities.SelectMany(x =>
                new[] { x.ReviewedById }.Concat(x.Items.Select(item => item.SettledById)))
            .Where(x => x.HasValue)
            .Select(x => x!.Value);
        var users = await BatchByIdsAsync(userRepo, userIds);

        return entities.Select(entity =>
        {
            var dto = new TrainingExpenseRecoveryDto
            {
                Id = entity.Id,
                CasualCourseId = entity.CasualCourseId,
                TravelRequestId = entity.TravelRequestId,
                ExpenseDate = entity.ExpenseDate,
                Currency = entity.Currency,
                TotalAmountOMR = entity.TotalAmountOMR,
                SettledAmountOMR = entity.Items.Where(x => x.IsSettled).Sum(x => x.AmountOMR),
                RemainingAmountOMR = entity.Items.Where(x => !x.IsSettled).Sum(x => x.AmountOMR),
                Status = entity.Status,
                ReviewedAt = entity.ReviewedAt,
                ReviewedById = entity.ReviewedById,
                ReviewNote = entity.ReviewNote,
                CreationTime = entity.CreationTime,
                Items = entity.Items.OrderBy(x => x.ExpenseTypeCode).Select(item => new TrainingExpenseRecoveryItemDto
                {
                    Id = item.Id,
                    FinancialItemId = item.FinancialItemId,
                    FinancialItemNameAr = item.FinancialItemId.HasValue
                        && financialItems.TryGetValue(item.FinancialItemId.Value, out var financialItem)
                            ? financialItem.NameAr
                            : null,
                    ExpenseTypeCode = item.ExpenseTypeCode,
                    FundingSourceVoteCode = item.FundingSourceVoteCode,
                    AmountOMR = item.AmountOMR,
                    IsSettled = item.IsSettled,
                    SettledAmountOMR = item.IsSettled ? item.AmountOMR : 0m,
                    RemainingAmountOMR = item.IsSettled ? 0m : item.AmountOMR,
                    SettledAt = item.SettledAt,
                    SettledById = item.SettledById,
                    SettledByName = item.SettledById.HasValue
                        && users.TryGetValue(item.SettledById.Value, out var settler)
                            ? settler.Name.IsNullOrWhiteSpace() ? settler.UserName : settler.Name
                            : null,
                    SettlementReference = item.SettlementReference,
                    SettlementNote = item.SettlementNote
                }).ToList()
            };

            if (courses.TryGetValue(entity.CasualCourseId, out var course)
                && courseNames.TryGetValue(course.TenantCourseId, out var name))
            {
                dto.CasualCourseNameAr = name.NameAr;
            }
            if (entity.ReviewedById.HasValue && users.TryGetValue(entity.ReviewedById.Value, out var reviewer))
            {
                dto.ReviewedByName = reviewer.Name.IsNullOrWhiteSpace() ? reviewer.UserName : reviewer.Name;
            }
            return dto;
        }).ToList();
    }

    private async Task<Dictionary<Guid, T>> BatchByIdsAsync<T>(IRepository<T, Guid> repo, IEnumerable<Guid> ids)
        where T : class, IEntity<Guid>
    {
        var idList = ids.Where(x => x != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0)
        {
            return [];
        }
        var query = await repo.GetQueryableAsync();
        var rows = await AsyncExecuter.ToListAsync(query.Where(x => idList.Contains(x.Id)));
        return rows.ToDictionary(x => x.Id);
    }
}
