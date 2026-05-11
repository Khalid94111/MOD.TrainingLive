using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Execution.Dtos;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Permissions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Execution;

[Authorize(TrainingExecutionPermissions.TravelInstructions.Default)]
public class TravelInstructionAppService(
    IRepository<TravelInstruction, Guid> repository,
    TravelDayCalculator travelDayCalculator,
    TravelInstructionToDtoMapper toDtoMapper,
    CreateUpdateTravelInstructionToEntityMapper toEntityMapper)
    : ApplicationService, ITravelInstructionAppService
{
    [Authorize(TrainingExecutionPermissions.TravelInstructions.Edit)]
    public async Task<TravelInstructionDto> CreateOrUpdateAsync(CreateUpdateTravelInstructionDto input)
    {
        ValidatePolymorphicParent(input);
        ValidateDateOrder(input);

        var queryable = await repository.GetQueryableAsync();
        var existing = input.CasualCourseId.HasValue
            ? await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.CasualCourseId == input.CasualCourseId.Value))
            : await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.SessionId == input.SessionId!.Value));

        TravelInstruction entity;
        if (existing != null)
        {
            if (existing.Status != TravelInstructionStatus.Draft)
                throw new BusinessException("Training:TravelInstruction:CannotEditAfterIssued");

            // Polymorphic parent is immutable on update.
            var casualOriginal = existing.CasualCourseId;
            var sessionOriginal = existing.SessionId;
            var statusOriginal = existing.Status;

            toEntityMapper.Map(input, existing);

            existing.CasualCourseId = casualOriginal;
            existing.SessionId = sessionOriginal;
            existing.Status = statusOriginal;
            existing.CalculatedTravelDays =
                travelDayCalculator.CalculateDays(input.DepartureDate, input.ArrivalBackDate);

            await repository.UpdateAsync(existing, autoSave: true);
            entity = existing;
        }
        else
        {
            entity = toEntityMapper.Map(input);
            entity.CalculatedTravelDays =
                travelDayCalculator.CalculateDays(input.DepartureDate, input.ArrivalBackDate);
            entity.Status = TravelInstructionStatus.Draft;
            await repository.InsertAsync(entity, autoSave: true);
        }

        return BuildDto(entity);
    }

    public async Task<TravelInstructionDto?> GetByParentAsync(Guid? casualCourseId, Guid? sessionId)
    {
        if (casualCourseId.HasValue == sessionId.HasValue)
            throw new BusinessException("Training:TravelInstruction:OnePolymorphicParentRequired");

        var queryable = await repository.GetQueryableAsync();
        var entity = casualCourseId.HasValue
            ? await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.CasualCourseId == casualCourseId.Value))
            : await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.SessionId == sessionId!.Value));

        return entity == null ? null : BuildDto(entity);
    }

    [Authorize(TrainingExecutionPermissions.TravelInstructions.Issue)]
    public async Task<TravelInstructionDto> IssueAsync(Guid id)
    {
        var entity = await repository.FindAsync(id)
            ?? throw new BusinessException("Training:TravelInstruction:NotFound");

        if (entity.Status != TravelInstructionStatus.Draft)
            throw new BusinessException("Training:TravelInstruction:CannotEditAfterIssued");

        ValidateIssuePrerequisites(entity);

        entity.Status = TravelInstructionStatus.Issued;
        await repository.UpdateAsync(entity, autoSave: true);

        return BuildDto(entity);
    }

    [Authorize(TrainingExecutionPermissions.TravelInstructions.Cancel)]
    public async Task<TravelInstructionDto> CancelAsync(Guid id)
    {
        var entity = await repository.FindAsync(id)
            ?? throw new BusinessException("Training:TravelInstruction:NotFound");

        if (entity.Status == TravelInstructionStatus.Cancelled)
            throw new BusinessException("Training:TravelInstruction:AlreadyCancelled");

        entity.Status = TravelInstructionStatus.Cancelled;
        await repository.UpdateAsync(entity, autoSave: true);

        return BuildDto(entity);
    }

    // ─── Helpers ────────────────────────────────────────────────────────

    private TravelInstructionDto BuildDto(TravelInstruction entity)
    {
        var dto = toDtoMapper.Map(entity);
        dto.EffectiveTravelDays = travelDayCalculator.GetEffectiveTravelDays(entity);
        return dto;
    }

    private static void ValidatePolymorphicParent(CreateUpdateTravelInstructionDto input)
    {
        if (input.CasualCourseId.HasValue == input.SessionId.HasValue)
            throw new BusinessException("Training:TravelInstruction:OnePolymorphicParentRequired");
    }

    private static void ValidateDateOrder(CreateUpdateTravelInstructionDto input)
    {
        if (!(input.DepartureDate <= input.ArrivalDate &&
              input.ArrivalDate <= input.ReturnDate &&
              input.ReturnDate <= input.ArrivalBackDate))
        {
            throw new BusinessException("Training:TravelInstruction:InvalidDateOrder");
        }
    }

    private static void ValidateIssuePrerequisites(TravelInstruction entity)
    {
        if (!entity.TicketsBooked || string.IsNullOrWhiteSpace(entity.TicketReference))
            throw new BusinessException("Training:TravelInstruction:PrerequisitesNotMet");

        if (entity.VisaRequired && string.IsNullOrWhiteSpace(entity.VisaNotes))
            throw new BusinessException("Training:TravelInstruction:PrerequisitesNotMet");

        if (entity.InsuranceArranged && string.IsNullOrWhiteSpace(entity.InsuranceProvider))
            throw new BusinessException("Training:TravelInstruction:PrerequisitesNotMet");
    }
}
