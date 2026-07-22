using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Hr;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Payments.Dtos;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans;
using MOD.Training.Training.Travel.Integration;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Payments;

/// <summary>
/// Read-only register of per-employee travel costs imported from completed Travel requests.
/// Creation and confirmation are intentionally owned by <see cref="SessionTravelPaymentSynchronizer"/>.
/// </summary>
[Authorize(TrainingPaymentsPermissions.TravelAllowance.Default)]
public class TravelAllowancePaymentAppService(
    IRepository<TravelAllowancePayment, Guid> repository,
    IRepository<CasualCourseNomination, Guid> casualNominationRepo,
    IRepository<SessionNomination, Guid> sessionNominationRepo,
    IRepository<Employee, Guid> employeeRepo,
    IRepository<Rank, Guid> rankRepo,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<CourseSession, Guid> sessionRepo,
    CourseNameResolver courseNameResolver,
    TravelAllowancePaymentToDtoMapper toDtoMapper,
    ITrainingTravelGateway travelGateway)
    : ApplicationService, ITravelAllowancePaymentAppService
{
    public async Task<TravelAllowancePaymentDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        return (await BuildDtosAsync(new[] { entity }))[0];
    }

    public async Task<PagedResultDto<TravelAllowancePaymentDto>> GetListAsync(
        TravelAllowancePaymentGetListInput input)
    {
        var queryable = await repository.GetQueryableAsync();

        if (input.CasualCourseId.HasValue)
            queryable = queryable.Where(x => x.CasualCourseId == input.CasualCourseId.Value);
        if (input.SessionId.HasValue)
            queryable = queryable.Where(x => x.SessionId == input.SessionId.Value);
        if (input.NominationId.HasValue)
            queryable = queryable.Where(x => x.NominationId == input.NominationId.Value);
        if (input.Status.HasValue)
            queryable = queryable.Where(x => x.Status == input.Status.Value);
        if (input.PersonnelType.HasValue)
            queryable = queryable.Where(x => x.PersonnelType == input.PersonnelType.Value);

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        var entities = await AsyncExecuter.ToListAsync(
            queryable.OrderByDescending(x => x.CreationTime).PageBy(input));
        var dtos = await BuildDtosAsync(entities);

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var needle = input.Search.Trim();
            dtos = dtos.Where(x => x.EmployeeNameAr?.Contains(needle) == true).ToList();
            totalCount = dtos.Count;
        }

        return new PagedResultDto<TravelAllowancePaymentDto>(totalCount, dtos);
    }

    private async Task<List<TravelAllowancePaymentDto>> BuildDtosAsync(
        IReadOnlyList<TravelAllowancePayment> entities)
    {
        var dtos = entities.Select(toDtoMapper.Map).ToList();
        if (dtos.Count == 0) return dtos;

        var casualNominationIds = entities
            .Where(x => x.CasualCourseId.HasValue)
            .Select(x => x.NominationId)
            .Distinct()
            .ToList();
        var sessionNominationIds = entities
            .Where(x => x.SessionId.HasValue)
            .Select(x => x.NominationId)
            .Distinct()
            .ToList();
        var casualNominations = await BatchByIdsAsync(casualNominationRepo, casualNominationIds);
        var sessionNominations = await BatchByIdsAsync(sessionNominationRepo, sessionNominationIds);

        var employeeIds = casualNominations.Values.Select(x => x.EmployeeId)
            .Concat(sessionNominations.Values.Select(x => x.EmployeeId))
            .Distinct()
            .ToList();
        var employees = await BatchByIdsAsync(employeeRepo, employeeIds);
        var ranks = await BatchByIdsAsync(rankRepo, employees.Values.Select(x => x.RankId));

        var casualCourseIds = entities
            .Where(x => x.CasualCourseId.HasValue)
            .Select(x => x.CasualCourseId!.Value)
            .Distinct()
            .ToList();
        var sessionIds = entities
            .Where(x => x.SessionId.HasValue)
            .Select(x => x.SessionId!.Value)
            .Distinct()
            .ToList();
        var casualCourses = await BatchByIdsAsync(casualCourseRepo, casualCourseIds);
        var sessions = await BatchByIdsAsync(sessionRepo, sessionIds);

        var tenantCourseIds = casualCourses.Values.Select(x => x.TenantCourseId)
            .Concat(sessions.Values.Select(x => x.TenantCourseId))
            .Distinct()
            .ToList();
        var courseNames = await courseNameResolver.BatchResolveAsync(tenantCourseIds);

        var travelDaysByCourse = new Dictionary<Guid, int>();
        foreach (var trainingCourseId in casualCourseIds.Concat(sessionIds).Distinct())
        {
            try
            {
                var result = await travelGateway.GetByTrainingCourseAsync(trainingCourseId);
                if (result.IsCompleted)
                    travelDaysByCourse[trainingCourseId] = result.CalculatedDays;
            }
            catch
            {
                // Stored payment history remains readable while Travel is unavailable.
            }
        }

        for (var index = 0; index < dtos.Count; index++)
        {
            var entity = entities[index];
            var dto = dtos[index];

            Employee? employee = null;
            if (entity.CasualCourseId.HasValue
                && casualNominations.TryGetValue(entity.NominationId, out var casualNomination))
            {
                employees.TryGetValue(casualNomination.EmployeeId, out employee);
            }
            else if (entity.SessionId.HasValue
                     && sessionNominations.TryGetValue(entity.NominationId, out var sessionNomination))
            {
                employees.TryGetValue(sessionNomination.EmployeeId, out employee);
            }

            if (employee != null)
            {
                dto.EmployeeNameAr = employee.FullNameAr;
                if (ranks.TryGetValue(employee.RankId, out var rank))
                    dto.RankNameAr = rank.NameAr;
            }

            Guid? trainingCourseId = entity.CasualCourseId ?? entity.SessionId;
            Guid? tenantCourseId = null;
            if (entity.CasualCourseId.HasValue
                && casualCourses.TryGetValue(entity.CasualCourseId.Value, out var casualCourse))
            {
                tenantCourseId = casualCourse.TenantCourseId;
            }
            else if (entity.SessionId.HasValue
                     && sessions.TryGetValue(entity.SessionId.Value, out var session))
            {
                tenantCourseId = session.TenantCourseId;
            }

            if (tenantCourseId.HasValue && courseNames.TryGetValue(tenantCourseId.Value, out var courseName))
                dto.CourseNameAr = courseName.NameAr;
            if (trainingCourseId.HasValue && travelDaysByCourse.TryGetValue(trainingCourseId.Value, out var days))
                dto.EffectiveTravelDays = days;
        }

        return dtos;
    }

    private async Task<Dictionary<Guid, T>> BatchByIdsAsync<T>(
        IRepository<T, Guid> repository,
        IEnumerable<Guid> ids)
        where T : class, IEntity<Guid>
    {
        var idList = ids.Where(x => x != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<Guid, T>();

        var queryable = await repository.GetQueryableAsync();
        var entities = await AsyncExecuter.ToListAsync(queryable.Where(x => idList.Contains(x.Id)));
        return entities.ToDictionary(x => x.Id);
    }
}
