using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Payments.Dtos;
using MOD.Training.Training.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace MOD.Training.Training.Payments;

/// <summary>
/// Read access + Status transition (Pending → Approved) for auto-generated reallocations.
/// No Create / Update / Delete endpoints exist by design — rows can only enter via
/// <c>BudgetReallocationGenerator</c>, called from <c>CoursePaymentAppService.ConfirmAsync</c>.
/// </summary>
[Authorize(TrainingPaymentsPermissions.Reallocations.View)]
public class BudgetReallocationAppService(
    IRepository<BudgetReallocation, Guid> repository,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    IRepository<IdentityUser, Guid> userRepo,
    CourseNameResolver courseNameResolver,
    BudgetReallocationToDtoMapper toDtoMapper)
    : ApplicationService, IBudgetReallocationAppService
{
    public async Task<BudgetReallocationDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        return await BuildDtoAsync(entity);
    }

    public async Task<PagedResultDto<BudgetReallocationDto>> GetListAsync(BudgetReallocationGetListInput input)
    {
        var queryable = await repository.GetQueryableAsync();

        if (input.CasualCourseId.HasValue)
            queryable = queryable.Where(x => x.CasualCourseId == input.CasualCourseId.Value);
        if (input.CoursePaymentId.HasValue)
            queryable = queryable.Where(x => x.CoursePaymentId == input.CoursePaymentId.Value);
        if (input.ToFinancialItemId.HasValue)
            queryable = queryable.Where(x => x.ToFinancialItemId == input.ToFinancialItemId.Value);
        if (!string.IsNullOrWhiteSpace(input.FundingSourceVoteCode))
            queryable = queryable.Where(x => x.FundingSourceVoteCode == input.FundingSourceVoteCode);
        if (input.Status.HasValue)
            queryable = queryable.Where(x => x.Status == input.Status.Value);
        if (input.CreatedFrom.HasValue)
            queryable = queryable.Where(x => x.CreationTime >= input.CreatedFrom.Value);
        if (input.CreatedTo.HasValue)
            queryable = queryable.Where(x => x.CreationTime <= input.CreatedTo.Value);

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        queryable = queryable.OrderByDescending(x => x.CreationTime).PageBy(input);
        var entities = await AsyncExecuter.ToListAsync(queryable);

        var dtos = await BuildDtosAsync(entities);
        return new PagedResultDto<BudgetReallocationDto>(totalCount, dtos);
    }

    [Authorize(TrainingPaymentsPermissions.Reallocations.MarkApproved)]
    public async Task<BudgetReallocationDto> MarkApprovedAsync(Guid id, MarkReallocationApprovedDto input)
    {
        var entity = await repository.GetAsync(id);

        if (entity.Status != ReallocationStatus.Pending)
            throw new BusinessException("Training:Reallocation:InvalidStatusTransition");

        if (CurrentUser.Id == null)
            throw new AbpAuthorizationException();

        entity.Status = ReallocationStatus.Approved;
        entity.ApprovedAt = Clock.Now;
        entity.ApprovedById = CurrentUser.Id;
        entity.ApprovalNote = input.ApprovalNote;

        await repository.UpdateAsync(entity, autoSave: true);
        return await BuildDtoAsync(entity);
    }

    // ── DTO enrichment ─────────────────────────────────────────────────

    private async Task<BudgetReallocationDto> BuildDtoAsync(BudgetReallocation entity)
        => (await BuildDtosAsync(new[] { entity }))[0];

    private async Task<List<BudgetReallocationDto>> BuildDtosAsync(IReadOnlyList<BudgetReallocation> entities)
    {
        var dtos = entities.Select(e => toDtoMapper.Map(e)).ToList();
        if (dtos.Count == 0) return dtos;

        var casualIds = entities.Select(e => e.CasualCourseId).Distinct().ToList();
        var casualLookup = await BatchByIdsAsync(casualCourseRepo, casualIds);
        var tenantCourseIds = casualLookup.Values.Select(c => c.TenantCourseId).Distinct().ToList();
        var nameLookup = await courseNameResolver.BatchResolveAsync(tenantCourseIds);

        var financialItemIds = entities.Select(e => e.ToFinancialItemId).Distinct().ToList();
        var fiLookup = await BatchByIdsAsync(financialItemRepo, financialItemIds);

        var approverIds = entities.Where(e => e.ApprovedById.HasValue)
            .Select(e => e.ApprovedById!.Value).Distinct().ToList();
        var approverLookup = await BatchByIdsAsync(userRepo, approverIds);

        for (var i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            var ent = entities[i];

            if (casualLookup.TryGetValue(ent.CasualCourseId, out var casual))
            {
                dto.FundingScenario = casual.FundingScenario;
                dto.FundingSourceName = casual.FundingSourceName;
                if (nameLookup.TryGetValue(casual.TenantCourseId, out var courseName))
                    dto.CasualCourseNameAr = courseName.NameAr;
            }

            if (fiLookup.TryGetValue(ent.ToFinancialItemId, out var fi))
                dto.ToFinancialItemNameAr = fi.NameAr;

            if (ent.ApprovedById.HasValue
                && approverLookup.TryGetValue(ent.ApprovedById.Value, out var approver))
            {
                dto.ApprovedByName = approver.UserName;
            }
        }

        return dtos;
    }

    private async Task<Dictionary<Guid, T>> BatchByIdsAsync<T>(IRepository<T, Guid> repo, IEnumerable<Guid> ids)
        where T : class, IEntity<Guid>
    {
        var idList = ids.Where(g => g != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<Guid, T>();
        var queryable = await repo.GetQueryableAsync();
        var rows = await AsyncExecuter.ToListAsync(queryable.Where(x => idList.Contains(x.Id)));
        return rows.ToDictionary(x => x.Id);
    }
}
