using MOD.Training.Training.Enums;
using System;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class CasualCourseListItemDto
{
    public Guid Id { get; set; }
    public string CourseNameAr { get; set; } = "";
    public string? FundingSourceName { get; set; }
    public string? FundingSourceVoteCode { get; set; }
    public string UnitName { get; set; } = "";
    public string RequesterName { get; set; } = "";
    public int NomineesCount { get; set; }
    public decimal? EstimatedTotalCost { get; set; }
    public CasualCourseStatus Status { get; set; }
    public bool IsReturned { get; set; }
    public string? LastReturnReason { get; set; }
}
