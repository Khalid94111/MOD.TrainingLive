using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Plans;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace MOD.Training.Training.Managers;

/// <summary>
/// Guards PriceQuote selection on both polymorphic arms:
///   Casual-course arm — course must be THApproved and the quote must point at the course.
///   Session arm       — session must be in Planned status and the quote must point at the session.
/// Used by <c>CasualCourseAppService.SelectPriceQuoteAsync</c> (4B-α) and
/// <c>CourseSessionAppService.SelectPriceQuoteAsync</c> (4C-α).
/// </summary>
public class PriceQuoteValidator(
    IRepository<CasualCourse, Guid> courseRepo,
    IRepository<CourseSession, Guid> sessionRepo,
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

    public async Task ValidateForSessionSelectionAsync(Guid sessionId, Guid quoteId)
    {
        var session = await sessionRepo.GetAsync(sessionId);
        if (session.Status != SessionStatus.Planned)
            throw new BusinessException("Training:Session:NotInPlannedStatus");

        var quote = await quoteRepo.GetAsync(quoteId);
        if (quote.SessionId != sessionId)
            throw new BusinessException("Training:PriceQuote:WrongSession");
    }
}
