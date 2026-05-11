using MOD.Training.Training.Execution.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Execution;

/// <summary>
/// Pre-execution travel instruction CRUD + state transitions. One row per polymorphic parent
/// (casual course OR session). Edits allowed only while <c>Status = Draft</c>.
/// </summary>
public interface ITravelInstructionAppService : IApplicationService
{
    /// <summary>Upsert — creates if no row exists for the parent, otherwise updates.</summary>
    Task<TravelInstructionDto> CreateOrUpdateAsync(CreateUpdateTravelInstructionDto input);

    /// <summary>Returns the single instruction for a casual course or session (null if not yet created).</summary>
    Task<TravelInstructionDto?> GetByParentAsync(Guid? casualCourseId, Guid? sessionId);

    /// <summary>Status: Draft → Issued. Validates prerequisites (tickets booked, visa/insurance fields if applicable).</summary>
    Task<TravelInstructionDto> IssueAsync(Guid id);

    /// <summary>Status → Cancelled.</summary>
    Task<TravelInstructionDto> CancelAsync(Guid id);
}
