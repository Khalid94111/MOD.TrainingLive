using System;
using System.Threading;
using System.Threading.Tasks;
using MOD.Training.Training.Centers;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace MOD.Training.Training.Managers;

public class TrainingCenterPlanReservationManager : DomainService
{
    private readonly IRepository<TrainingCenterPlanItem, Guid> _repository;

    public TrainingCenterPlanReservationManager(IRepository<TrainingCenterPlanItem, Guid> repository)
    {
        _repository = repository;
    }

    public async Task ReserveSeatsAsync(
        Guid centerPlanItemId,
        int seats,
        CancellationToken cancellationToken = default)
    {
        if (seats <= 0) return;

        await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var item = await _repository.GetAsync(centerPlanItemId, cancellationToken: cancellationToken);

            if (item.ReservedSeats + seats > item.Capacity)
            {
                throw new BusinessException("Training:CenterPlanItem:NoCapacity")
                    .WithData("Requested", seats)
                    .WithData("Remaining", item.Capacity - item.ReservedSeats);
            }

            item.ReservedSeats += seats;
            await _repository.UpdateAsync(item, autoSave: true, cancellationToken);
        });
    }

    public async Task ReleaseSeatsAsync(
        Guid centerPlanItemId,
        int seats,
        CancellationToken cancellationToken = default)
    {
        if (seats <= 0) return;

        await ExecuteWithConcurrencyRetryAsync(async () =>
        {
            var item = await _repository.GetAsync(centerPlanItemId, cancellationToken: cancellationToken);
            item.ReservedSeats = Math.Max(0, item.ReservedSeats - seats);
            await _repository.UpdateAsync(item, autoSave: true, cancellationToken);
        });
    }

    private static async Task ExecuteWithConcurrencyRetryAsync(Func<Task> action, int maxAttempts = 3)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex) when (IsConcurrencyException(ex))
            {
                if (attempt == maxAttempts)
                {
                    throw new BusinessException("Training:CenterPlanItem:ConcurrencyCapacity");
                }
            }
        }
    }

    private static bool IsConcurrencyException(Exception ex)
    {
        // Detect EF Core concurrency exception without taking a compile-time dependency on EF Core in the domain layer.
        const string efConcurrencyType = "Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException";
        return ex.GetType().FullName == efConcurrencyType
               || ex.InnerException?.GetType().FullName == efConcurrencyType;
    }
}
