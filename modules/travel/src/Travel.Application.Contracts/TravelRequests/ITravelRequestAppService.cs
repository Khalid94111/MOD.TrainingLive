using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Travel.Allowances;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Travel.TravelRequests;

public interface ITravelRequestAppService : ICrudAppService<
    TravelRequestDto,
    Guid,
    GetTravelRequestListInput,
    CreateUpdateTravelRequestDto>
{
    Task<PagedResultDto<TravelRequestDto>> GetListByTypeAsync(
        Guid travelTypeDefinitionId,
        PagedAndSortedResultRequestDto input);

    /// <summary>
    /// Creates a draft travel request from a training course integration payload.
    /// </summary>
    /// <param name="input">Training course travel details.</param>
    /// <returns>The created or existing draft travel request reference.</returns>
    Task<CreateTravelRequestFromTrainingResultDto> CreateFromTrainingAsync(CreateTravelRequestFromTrainingDto input);

    /// <summary>
    /// Gets sample training courses that mimic the training system payload for local testing.
    /// </summary>
    /// <returns>Sample training course travel payloads.</returns>
    Task<List<TrainingCourseLookupDto>> GetTrainingCourseLookupAsync();

    /// <summary>
    /// Gets the travel completion and payment result for a training course integration.
    /// </summary>
    /// <param name="trainingCourseId">Training course identifier.</param>
    /// <returns>The travel request status and employee payment details when completed.</returns>
    Task<TrainingTravelResultDto> GetTrainingResultAsync(Guid trainingCourseId);

    /// <summary>
    /// Changes the workflow status of a travel request.
    /// </summary>
    /// <param name="id">Travel request identifier.</param>
    /// <param name="input">Target status and optional reason.</param>
    /// <returns>The updated travel request.</returns>
    Task<TravelRequestDto> ChangeStatusAsync(Guid id, ChangeStatusDto input);

    /// <summary>
    /// Saves visa, insurance, and ticket details handled by the travel office, then marks tickets as booked.
    /// </summary>
    /// <param name="id">Travel request identifier.</param>
    /// <param name="input">Travel office procedure details.</param>
    /// <returns>The updated travel request.</returns>
    Task<TravelRequestDto> SaveTravelOfficeDetailsAsync(Guid id, TravelOfficeDetailsDto input);

    /// <summary>
    /// Selects the departure and return flight offers approved by management for all request employees.
    /// </summary>
    /// <param name="id">Travel request identifier.</param>
    /// <param name="input">Selected departure and return flight offer identifiers.</param>
    /// <returns>The updated travel request.</returns>
    Task<TravelRequestDto> SelectFlightOffersAsync(Guid id, SelectFlightOffersDto input);

    /// <summary>
    /// Confirms final ticket booking after management selected the required offers.
    /// </summary>
    /// <param name="id">Travel request identifier.</param>
    /// <returns>The updated travel request.</returns>
    Task<TravelRequestDto> ConfirmTicketBookingAsync(Guid id, ConfirmTicketBookingDto input);

    /// <summary>
    /// Calculates the current allowance summary for a travel request.
    /// </summary>
    /// <param name="id">Travel request identifier.</param>
    /// <param name="includeClothingAllowance">Whether to include clothing allowance in calculation.</param>
    /// <returns>The calculated allowance summary.</returns>
    Task<AllowanceSummaryDto> CalculateAllowancesAsync(Guid id, bool includeClothingAllowance = false);

    /// <summary>
    /// Previews allowance calculation before a travel request is saved.
    /// </summary>
    /// <param name="input">Draft travel request details.</param>
    /// <returns>The calculated allowance preview.</returns>
    Task<AllowanceSummaryDto> PreviewAllowancesAsync(CreateUpdateTravelRequestDto input);

    /// <summary>
    /// Gets a unified travel cost summary for tickets, visa, insurance, and allowances.
    /// </summary>
    /// <param name="id">Travel request identifier.</param>
    /// <returns>The current cost summary.</returns>
    Task<TravelCostSummaryDto> GetCostSummaryAsync(Guid id);

    /// <summary>
    /// Confirms a travel request on behalf of an assigned employee.
    /// </summary>
    /// <param name="id">Travel request identifier.</param>
    /// <param name="employeeId">Employee identifier.</param>
    Task ConfirmByEmployeeAsync(Guid id, Guid employeeId);

    /// <summary>
    /// Gets payroll elements available for overseas payment tasks when travel exceeds 60 days.
    /// </summary>
    /// <returns>List of active payroll elements with segment code 4206.</returns>
    Task<List<PayrollElementLookupDto>> GetPayrollElementLookupAsync();

    /// <summary>
    /// Previews the overseas payroll details that will be sent to HR when completing a long travel request.
    /// </summary>
    /// <param name="id">Travel request identifier.</param>
    /// <returns>Preview of extra days, amounts, and affected employees.</returns>
    Task<OverseasPayrollPreviewDto> GetOverseasPayrollPreviewAsync(Guid id);

    /// <summary>
    /// Uploads a travel document file (ticket, visa, or insurance) to blob storage.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <returns>The uploaded file metadata.</returns>
    Task<TravelFileUploadResultDto> UploadFileAsync(byte[] file, string fileName);

    /// <summary>
    /// Downloads a travel document file from blob storage by its blob name.
    /// </summary>
    /// <param name="blobName">The blob name of the file.</param>
    /// <returns>The file content as bytes.</returns>
    Task<byte[]> DownloadFileAsync(string blobName);
}
