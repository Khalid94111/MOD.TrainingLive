using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Travel.Allowances;
using Travel.Permissions;
using Travel.TravelRequests;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace Travel.TravelRequests;

[Area(TravelRemoteServiceConsts.ModuleName)]
[RemoteService(Name = TravelRemoteServiceConsts.RemoteServiceName)]
[Route("api/travel/requests")]
[Authorize(TravelManagementPermissions.TravelRequests.Default)]
public class TravelRequestController : TravelController, ITravelRequestAppService
{
    private readonly ITravelRequestAppService _appService;

    public TravelRequestController(ITravelRequestAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    [Authorize(TravelManagementPermissions.TravelRequests.Default)]
    public virtual Task<PagedResultDto<TravelRequestDto>> GetListAsync(GetTravelRequestListInput input)
    {
        return _appService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    [Authorize(TravelManagementPermissions.TravelRequests.Default)]
    public virtual Task<TravelRequestDto> GetAsync(Guid id)
    {
        return _appService.GetAsync(id);
    }

    [HttpGet("by-type/{travelTypeDefinitionId}")]
    [Authorize(TravelManagementPermissions.TravelRequests.ViewByType)]
    public virtual Task<PagedResultDto<TravelRequestDto>> GetListByTypeAsync(
        Guid travelTypeDefinitionId,
        [FromQuery] PagedAndSortedResultRequestDto input)
    {
        return _appService.GetListByTypeAsync(travelTypeDefinitionId, input);
    }

    [HttpPost("from-training")]
    [Authorize(TravelManagementPermissions.TravelRequests.Create)]
    public virtual Task<CreateTravelRequestFromTrainingResultDto> CreateFromTrainingAsync(CreateTravelRequestFromTrainingDto input)
    {
        return _appService.CreateFromTrainingAsync(input);
    }

    [HttpGet("training-courses/lookup")]
    [Authorize(TravelManagementPermissions.TravelRequests.Default)]
    public virtual Task<List<TrainingCourseLookupDto>> GetTrainingCourseLookupAsync()
    {
        return _appService.GetTrainingCourseLookupAsync();
    }

    [HttpGet("training-result")]
    [Authorize(TravelManagementPermissions.TravelRequests.Default)]
    public virtual Task<TrainingTravelResultDto> GetTrainingResultAsync([FromQuery] Guid trainingCourseId)
    {
        return _appService.GetTrainingResultAsync(trainingCourseId);
    }

    [HttpPost]
    [Authorize(TravelManagementPermissions.TravelRequests.Create)]
    public virtual Task<TravelRequestDto> CreateAsync(CreateUpdateTravelRequestDto input)
    {
        return _appService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(TravelManagementPermissions.TravelRequests.Edit)]
    public virtual Task<TravelRequestDto> UpdateAsync(Guid id, CreateUpdateTravelRequestDto input)
    {
        return _appService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(TravelManagementPermissions.TravelRequests.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _appService.DeleteAsync(id);
    }

    [HttpPost("{id}/status")]
    [Authorize(TravelManagementPermissions.TravelRequests.Edit)]
    public virtual Task<TravelRequestDto> ChangeStatusAsync(Guid id, ChangeStatusDto input)
    {
        return _appService.ChangeStatusAsync(id, input);
    }

    [HttpPost("{id}/travel-office")]
    [Authorize(TravelManagementPermissions.TravelRequests.Edit)]
    public virtual Task<TravelRequestDto> SaveTravelOfficeDetailsAsync(Guid id, TravelOfficeDetailsDto input)
    {
        return _appService.SaveTravelOfficeDetailsAsync(id, input);
    }

    [HttpPost("{id}/flight-selection")]
    [Authorize(TravelManagementPermissions.TravelRequests.Edit)]
    public virtual Task<TravelRequestDto> SelectFlightOffersAsync(Guid id, SelectFlightOffersDto input)
    {
        return _appService.SelectFlightOffersAsync(id, input);
    }

    [HttpPost("{id}/ticket-booking")]
    [Authorize(TravelManagementPermissions.TravelRequests.Edit)]
    public virtual Task<TravelRequestDto> ConfirmTicketBookingAsync(Guid id, ConfirmTicketBookingDto input)
    {
        return _appService.ConfirmTicketBookingAsync(id, input);
    }

    [HttpGet("{id}/allowances")]
    [Authorize(TravelManagementPermissions.TravelRequests.Default)]
    public virtual Task<AllowanceSummaryDto> CalculateAllowancesAsync(Guid id, [FromQuery] bool includeClothingAllowance = false)
    {
        return _appService.CalculateAllowancesAsync(id, includeClothingAllowance);
    }

    [HttpPost("allowances/preview")]
    [Authorize(TravelManagementPermissions.TravelRequests.Default)]
    public virtual Task<AllowanceSummaryDto> PreviewAllowancesAsync(CreateUpdateTravelRequestDto input)
    {
        return _appService.PreviewAllowancesAsync(input);
    }

    [HttpGet("{id}/cost-summary")]
    [Authorize(TravelManagementPermissions.TravelRequests.Default)]
    public virtual Task<TravelCostSummaryDto> GetCostSummaryAsync(Guid id)
    {
        return _appService.GetCostSummaryAsync(id);
    }

    [HttpPost("{id}/employee-confirm")]
    [Authorize(TravelManagementPermissions.TravelRequests.Confirm)]
    public virtual Task ConfirmByEmployeeAsync(Guid id, [FromQuery] Guid employeeId)
    {
        return _appService.ConfirmByEmployeeAsync(id, employeeId);
    }

    [HttpGet("payroll-elements/lookup")]
    [Authorize(TravelManagementPermissions.TravelRequests.Default)]
    public virtual Task<List<PayrollElementLookupDto>> GetPayrollElementLookupAsync()
    {
        return _appService.GetPayrollElementLookupAsync();
    }

    [HttpGet("{id}/overseas-payroll-preview")]
    [Authorize(TravelManagementPermissions.TravelRequests.Default)]
    public virtual Task<OverseasPayrollPreviewDto> GetOverseasPayrollPreviewAsync(Guid id)
    {
        return _appService.GetOverseasPayrollPreviewAsync(id);
    }

    [HttpPost("upload-file")]
    [Authorize(TravelManagementPermissions.TravelRequests.Edit)]
    public virtual async Task<TravelFileUploadResultDto> UploadFileAsync(IFormFile file)
    {
        using var stream = new System.IO.MemoryStream();
        await file.CopyToAsync(stream);
        return await _appService.UploadFileAsync(stream.ToArray(), file.FileName);
    }

    [HttpGet("download-file/{blobName}")]
    [Authorize(TravelManagementPermissions.TravelRequests.Default)]
    public virtual async Task<IActionResult> DownloadFileAsync(string blobName)
    {
        var bytes = await _appService.DownloadFileAsync(blobName);
        return File(bytes, "application/octet-stream", blobName);
    }

    // Explicit interface implementations
    Task<TravelFileUploadResultDto> ITravelRequestAppService.UploadFileAsync(byte[] file, string fileName)
    {
        return _appService.UploadFileAsync(file, fileName);
    }

    Task<byte[]> ITravelRequestAppService.DownloadFileAsync(string blobName)
    {
        return _appService.DownloadFileAsync(blobName);
    }
}
