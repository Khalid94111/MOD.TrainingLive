using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Travel.Allowances;
using Travel.TravelRequests.Events;
using Travel.TravelTypes;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.EventBus.Local;

namespace Travel.TravelRequests;

public class TravelRequestManager : DomainService
{
    private readonly ILocalEventBus _localEventBus;

    public TravelRequestManager(ILocalEventBus localEventBus)
    {
        _localEventBus = localEventBus;
    }

    public virtual async Task SubmitAsync(TravelRequest request)
    {
        await ChangeStatusAsync(request, RequestStatus.AtTravelOffice);
    }

    public virtual async Task ApproveAsync(TravelRequest request)
    {
        await ChangeStatusAsync(request, RequestStatus.Approved);
    }

    public virtual async Task ReturnAsync(TravelRequest request, string reason)
    {
        await ChangeStatusAsync(request, RequestStatus.Returned, reason);
    }

    public virtual async Task RejectAsync(TravelRequest request, string reason)
    {
        await ChangeStatusAsync(request, RequestStatus.Rejected, reason);
    }

    public virtual async Task SendToTravelOfficeAsync(TravelRequest request)
    {
        await ChangeStatusAsync(request, RequestStatus.AtTravelOffice);
    }

    public virtual async Task BookTicketsAsync(TravelRequest request, List<FlightOfferDto> offers)
    {
        await ChangeStatusAsync(request, RequestStatus.TicketsBooked);
    }

    public virtual async Task ConfirmByEmployeeAsync(TravelRequest request, Guid employeeId)
    {
        await ChangeStatusAsync(request, RequestStatus.Confirmed);
    }

    public virtual async Task CompleteAsync(TravelRequest request, AllowanceSnapshot allowances)
    {
        ValidateTransition(request.Status, RequestStatus.Completed);
        request.SetAllowanceSnapshot(allowances);
        await ChangeStatusAsync(request, RequestStatus.Completed);
    }

    public virtual async Task ChangeStatusAsync(TravelRequest request, RequestStatus target, string? reason = null)
    {
        var current = request.Status;
        ValidateTransition(current, target);

        request.ChangeStatus(target, reason);

        await _localEventBus.PublishAsync(
            new TravelRequestStatusChangedEvent(request, current, target, reason)
        );
    }

    protected virtual void ValidateTransition(RequestStatus current, RequestStatus target)
    {
        bool valid = (current, target) switch
        {
            // === المسار المختصر الرئيسي ===
            (RequestStatus.Draft, RequestStatus.AtTravelOffice) => true,
            (RequestStatus.AtTravelOffice, RequestStatus.TicketsBooked) => true,
            (RequestStatus.TicketsBooked, RequestStatus.CalculatingAllowances) => true,
            (RequestStatus.CalculatingAllowances, RequestStatus.Completed) => true,

            // === إلغاء من المراحل الرئيسية ===
            (RequestStatus.Draft, RequestStatus.Cancelled) => true,
            (RequestStatus.AtTravelOffice, RequestStatus.Cancelled) => true,
            (RequestStatus.TicketsBooked, RequestStatus.Cancelled) => true,

            // === إعادة للمسودة ===
            (RequestStatus.Returned, RequestStatus.Draft) => true,
            (RequestStatus.Returned, RequestStatus.AtTravelOffice) => true,

            // === رفض ===
            (RequestStatus.Draft, RequestStatus.Rejected) => true,
            (RequestStatus.AtTravelOffice, RequestStatus.Rejected) => true,
            (RequestStatus.TicketsBooked, RequestStatus.Rejected) => true,

            // === توافق: تطبيع بيانات قديمة (الحالات الداخلية القديمة) ===
            (RequestStatus.PendingApproval, RequestStatus.AtTravelOffice) => true,
            (RequestStatus.PendingApproval, RequestStatus.Cancelled) => true,
            (RequestStatus.Approved, RequestStatus.AtTravelOffice) => true,
            (RequestStatus.Approved, RequestStatus.Cancelled) => true,
            (RequestStatus.FlightSelection, RequestStatus.AtTravelOffice) => true,
            (RequestStatus.FlightSelection, RequestStatus.TicketsBooked) => true,
            (RequestStatus.VisaCheck, RequestStatus.AtTravelOffice) => true,
            (RequestStatus.VisaCheck, RequestStatus.TicketsBooked) => true,
            (RequestStatus.InsuranceCheck, RequestStatus.AtTravelOffice) => true,
            (RequestStatus.InsuranceCheck, RequestStatus.TicketsBooked) => true,
            (RequestStatus.DocsUploaded, RequestStatus.CalculatingAllowances) => true,
            (RequestStatus.PendingEmployeeConfirm, RequestStatus.CalculatingAllowances) => true,
            (RequestStatus.Confirmed, RequestStatus.CalculatingAllowances) => true,

            _ => false
        };

        if (!valid)
        {
            throw new BusinessException(TravelErrorCodes.InvalidStatusTransition)
                .WithData("Current", current)
                .WithData("Target", target);
        }
    }
}
