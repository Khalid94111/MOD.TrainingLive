using MOD.Training.Training.CasualCourses.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.CasualCourses;

public interface ICasualCourseFinancialItemRankAppService : IApplicationService
{
    /// <summary>
    /// Inline edit of one rank row's rate. Recomputes its subtotal,
    /// parent TotalAmount, and the course-level EstimatedTotalCost.
    /// Returns the refreshed parent (with Ranks hydrated).
    /// </summary>
    Task<CasualCourseFinancialDto> UpdateRateAsync(Guid id, UpdateRankRateDto input);
}
