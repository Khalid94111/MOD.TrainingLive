using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class CasualCourseGetListInput : PagedAndSortedResultRequestDto
{
    public int? Year { get; set; }
    public List<CasualCourseStatus>? Status { get; set; }
    public Guid? UnitId { get; set; }
    public bool OnlyMyRequests { get; set; }
    public bool IsReturnedOnly { get; set; }
    public string? Search { get; set; }
}
