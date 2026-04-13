using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Catalog;
using MOD.Training.Training.CourseProposals.Dtos;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
 
namespace MOD.Training.Training.CourseProposals;

[Authorize(TrainingPermissions.CourseProposals.Default)]
public class CourseProposalAppService(
    IRepository<CourseProposal, Guid> proposalRepo,
    IRepository<CourseCatalog, Guid> catalogRepo)
    : ApplicationService, ICourseProposalAppService
{
    public async Task<CourseProposalDto> GetAsync(Guid id)
    {
        var queryable = await proposalRepo.WithDetailsAsync(x => x.Field!);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(queryable.Where(x => x.Id == id))
            ?? throw new BusinessException("Training:CourseProposal:NotFound");

        return MapToDto(entity);
    }

    public async Task<PagedResultDto<CourseProposalDto>> GetListAsync(CourseProposalGetListInput input)
    {
        var queryable = await proposalRepo.WithDetailsAsync(x => x.Field!);

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter!.Trim();
            queryable = queryable.Where(x =>
                x.CourseNameAr.Contains(filter) || x.CourseNameEn.Contains(filter));
        }
        if (input.Status.HasValue)
            queryable = queryable.Where(x => x.Status == input.Status.Value);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        var items = await AsyncExecuter.ToListAsync(
            queryable
                .OrderBy(input.Sorting.IsNullOrWhiteSpace()
                    ? $"{nameof(CourseProposal.CreationTime)} desc"
                    : input.Sorting)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

        return new PagedResultDto<CourseProposalDto>(totalCount, items.Select(MapToDto).ToList());
    }

    /// <summary>
    /// UTM submits a proposal.
    /// POST /api/app/course-proposal
    /// </summary>
    [Authorize(TrainingPermissions.CourseProposals.Create)]
    public async Task<CourseProposalDto> CreateAsync(CreateCourseProposalDto input)
    {
        var entity = new CourseProposal
        {
            CourseNameAr = input.CourseNameAr,
            CourseNameEn = input.CourseNameEn,
            Category = input.Category,
            Nature = input.Nature,
            FieldId = input.FieldId,
            Status = ProposalStatus.Pending,
            ProposedById = CurrentUser.GetId(),
        };

        entity = await proposalRepo.InsertAsync(entity, autoSave: true);
        return await GetAsync(entity.Id);
    }

    /// <summary>
    /// System Admin reviews. Approve → auto-creates CourseCatalog.
    /// PUT /api/app/course-proposal/{id}/review
    /// </summary>
    [Authorize(TrainingPermissions.CourseProposals.Review)]
    public async Task<CourseProposalDto> ReviewAsync(Guid id, ReviewCourseProposalDto input)
    {
        var proposal = await proposalRepo.GetAsync(id);

        if (proposal.Status != ProposalStatus.Pending)
            throw new BusinessException("Training:CourseProposal:AlreadyReviewed");

        proposal.Status = input.Decision;
        proposal.ReviewedById = CurrentUser.GetId();
        proposal.ReviewedAt = Clock.Now;

        if (input.Decision == ProposalStatus.Rejected)
        {
            proposal.RejectionReason = input.RejectionReason;
        }
        else if (input.Decision == ProposalStatus.Approved)
        {
            await ValidateCatalogNameAsync(proposal.CourseNameAr, proposal.CourseNameEn);

            var catalogCourse = new CourseCatalog
            {
                CourseNameAr = proposal.CourseNameAr,
                CourseNameEn = proposal.CourseNameEn,
                Category = proposal.Category,
                Nature = proposal.Nature,
                FieldId = proposal.FieldId,
                ResultType = ResultType.AttendanceOnly,
                IsActive = true,
            };
            await catalogRepo.InsertAsync(catalogCourse, autoSave: true);
        }

        await proposalRepo.UpdateAsync(proposal, autoSave: true);
        return await GetAsync(id);
    }

    // ── Private ──

    private static CourseProposalDto MapToDto(CourseProposal entity)
    {
        var dto = entity.ToDto();
        dto.FieldNameAr = entity.Field?.FieldNameAr;
        return dto;
    }

    private async Task ValidateCatalogNameAsync(string nameAr, string nameEn)
    {
        if (await catalogRepo.AnyAsync(x => x.CourseNameAr == nameAr))
            throw new BusinessException("Training:CourseCatalog:DuplicateNameAr").WithData("name", nameAr);

        if (await catalogRepo.AnyAsync(x => x.CourseNameEn == nameEn))
            throw new BusinessException("Training:CourseCatalog:DuplicateNameEn").WithData("name", nameEn);
    }
}
