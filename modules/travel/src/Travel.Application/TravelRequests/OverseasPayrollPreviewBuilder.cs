using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Travel.Allowances;

namespace Travel.TravelRequests;

internal static class OverseasPayrollPreviewBuilder
{
    public static OverseasPayrollPreviewDto Build(TravelRequest request)
    {
        var totalDays = request.GetDurationInDays();
        var requiresPayrollElementSelection = totalDays > TravelRequestConsts.MaxTravelAllowanceDays;
        var extraDays = Math.Max(0, totalDays - TravelRequestConsts.MaxTravelAllowanceDays);
        var allowanceStartDate = request.GetAllowanceStartDate();
        var allowanceEndDate = request.GetAllowanceEndDate();
        var extraPeriodStartDate = allowanceStartDate.AddDays(TravelRequestConsts.MaxTravelAllowanceDays);

        var preview = new OverseasPayrollPreviewDto
        {
            TotalDays = totalDays,
            ExtraDays = extraDays,
            ExtraPeriodStartDate = extraPeriodStartDate,
            ExtraPeriodEndDate = allowanceEndDate,
            RequiresPayrollElementSelection = requiresPayrollElementSelection,
            Employees = new List<OverseasPayrollEmployeePreviewDto>()
        };

        if (!requiresPayrollElementSelection)
        {
            return preview;
        }

        foreach (var detail in request.AllowanceDetails)
        {
            var lastPercentage = GetLastPercentage(detail);
            var dailyAmount = detail.DailyRate * (lastPercentage / 100m);
            var totalAmount = dailyAmount * extraDays;

            preview.Employees.Add(new OverseasPayrollEmployeePreviewDto
            {
                EmployeeId = detail.EmployeeId,
                EmployeeName = detail.EmployeeName,
                EmployeeNumber = detail.EmployeeNumber,
                RankName = detail.RankName,
                DailyRate = detail.DailyRate,
                Percentage = lastPercentage,
                AccommodationPaymentPercentage = detail.AccommodationPaymentPercentage,
                DailyAmount = dailyAmount,
                TotalAmount = totalAmount,
                ExtraDays = extraDays
            });
        }

        return preview;
    }

    private static decimal GetLastPercentage(TravelRequestAllowanceDetail detail)
    {
        if (string.IsNullOrWhiteSpace(detail.SegmentsJson))
        {
            return 100m;
        }

        try
        {
            var segments = JsonSerializer.Deserialize<List<AllowanceCalculationSegment>>(
                detail.SegmentsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (segments == null || segments.Count == 0)
            {
                return 100m;
            }

            return segments.OrderByDescending(s => s.ToDay).First().Percentage;
        }
        catch
        {
            return 100m;
        }
    }
}
