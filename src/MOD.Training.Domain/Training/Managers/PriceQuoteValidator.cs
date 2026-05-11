using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace MOD.Training.Training.Managers;

/// <summary>
/// Guards the casual-course PriceQuote selection flow:
/// course must be THApproved and the quote must point at the same course.
/// Used by <c>CasualCourseAppService.SelectPriceQuoteAsync</c>.
/// </summary>
public class PriceQuoteValidator(
    IRepository<CasualCourse, Guid> courseRepo,
    IRepository<PriceQuote, Guid> quoteRepo)
    : DomainService
{
    public async Task ValidateForSelectionAsync(Guid casualCourseId, Guid quoteId)
    {
        var course = await courseRepo.GetAsync(casualCourseId);
        if (course.Status != CasualCourseStatus.THApproved)
            throw new BusinessException("Training:PriceQuote:CourseNotApproved");

        var quote = await quoteRepo.GetAsync(quoteId);
        if (quote.CasualCourseId != casualCourseId)
            throw new BusinessException("Training:PriceQuote:WrongCourse");
    }
}
