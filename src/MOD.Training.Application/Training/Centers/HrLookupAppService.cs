using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.Centers.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Centers;

/// <summary>
/// HR Lookup service — reads from shared HR DbContext (read-only, ExcludeFromMigrations).
/// Provides employee and position lookups scoped to a specific OrgUnit.
///
/// NOTE: Replace commented-out HR entity references with actual HR module types.
/// </summary>
[Authorize]
public class HrLookupAppService(
    /* IRepository<HrEmployee, Guid> employeeRepository, */
    /* IRepository<HrPosition, Guid> positionRepository */
    ) : ApplicationService, IHrLookupAppService
{
    //public async Task<List<HrEmployeeLookupDto>> GetEmployeesAsync(
    //    string serviceNumber, Guid orgUnitId)
    //{
    //    // TODO: Replace with actual HR module query
    //    // var query = await employeeRepository.GetQueryableAsync();
    //    // var employees = await AsyncExecuter.ToListAsync(
    //    //     query.Where(e => e.ServiceNumber.Contains(serviceNumber)
    //    //                   && e.OrgUnitId == orgUnitId)
    //    //          .Select(e => new HrEmployeeLookupDto
    //    //          {
    //    //              EmployeeId = e.Id,
    //    //              ServiceNumber = e.ServiceNumber,
    //    //              FullNameAr = e.FullNameAr,
    //    //              FullNameEn = e.FullNameEn,
    //    //              RankName = e.Rank != null ? e.Rank.NameAr : null,
    //    //              PositionName = e.Position != null ? e.Position.NameAr : null
    //    //          })
    //    //          .Take(20));
    //    // return employees;

    //    await Task.CompletedTask;
    //    return [];
    //}
    public async Task<List<HrEmployeeLookupDto>> GetEmployeesAsync(
    string serviceNumber, Guid orgUnitId)
    {
        await Task.CompletedTask;

        var dummyEmployees = new List<HrEmployeeLookupDto>
    {
        new HrEmployeeLookupDto
        {
            EmployeeId = Guid.NewGuid(),
            ServiceNumber = "D1-7443",
            FullNameAr = "أحمد محمد العامري",
            FullNameEn = "Ahmed Mohammed Al Amri",
            RankName = "نقيب",
            PositionName = "مدير إدارة الموارد البشرية"
        },
        new HrEmployeeLookupDto
        {
            EmployeeId = Guid.NewGuid(),
            ServiceNumber = "D1-7446",
            FullNameAr = "سالم سعيد البلوشي",
            FullNameEn = "Salem Saeed Al Balushi",
            RankName = "نقيب",
            PositionName = "مدير مركز تدريب صحار"
        },
        new HrEmployeeLookupDto
        {
            EmployeeId = Guid.NewGuid(),
            ServiceNumber = "D1-7445",
            FullNameAr = "خالد عبدالله الحارثي",
            FullNameEn = "Khalid Abdullah Al Harthi",
            RankName = "ملازم",
            PositionName = "مهندس تقنية معلومات"
        },
        new HrEmployeeLookupDto
        {
            EmployeeId = Guid.NewGuid(),
            ServiceNumber = "D1-7447",
            FullNameAr = "يوسف ناصر الريامي",
            FullNameEn = "Yousuf Nasser Al Riyami",
            RankName = "نقيب",
            PositionName = "ركن/2 دراسات"
        },
        new HrEmployeeLookupDto
        {
            EmployeeId = Guid.NewGuid(),
            ServiceNumber = "D1-7448",
            FullNameAr = "محمد علي الكندي",
            FullNameEn = "Mohammed Ali Al Kindi",
            RankName = "رائد",
            PositionName = "مسؤول شؤون إدارية"
        }
    };

        // Filter by serviceNumber if provided
        if (!string.IsNullOrWhiteSpace(serviceNumber))
        {
            dummyEmployees = dummyEmployees
                .Where(e => e.ServiceNumber.Contains(serviceNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return dummyEmployees.Take(20).ToList();
    }
    //public async Task<List<HrPositionLookupDto>> GetPositionsAsync(
    //    string name, Guid orgUnitId)
    //{
    //    // TODO: Replace with actual HR module query
    //    // var query = await positionRepository.GetQueryableAsync();
    //    // var positions = await AsyncExecuter.ToListAsync(
    //    //     query.Where(p => (p.NameAr.Contains(name) || p.NameEn.Contains(name))
    //    //                   && p.OrgUnitId == orgUnitId)
    //    //          .Select(p => new HrPositionLookupDto
    //    //          {
    //    //              PositionId = p.Id,
    //    //              PositionNameAr = p.NameAr,
    //    //              PositionNameEn = p.NameEn
    //    //          })
    //    //          .Take(20));
    //    // return positions;

    //    await Task.CompletedTask;
    //    return [];
    //}
    public async Task<List<HrPositionLookupDto>> GetPositionsAsync(
    string name, Guid orgUnitId)
    {
        await Task.CompletedTask;

        var dummyPositions = new List<HrPositionLookupDto>
    {
        new HrPositionLookupDto
        {
            PositionId = Guid.NewGuid(),
            PositionNameAr = "مدير إدارة الموارد البشرية",
            PositionNameEn = "HR Department Manager"
        },
        new HrPositionLookupDto
        {
            PositionId = Guid.NewGuid(),
            PositionNameAr = "مدير مركز تدريب صحار",
            PositionNameEn = "TCM Sohar"
        },
        new HrPositionLookupDto
        {
            PositionId = Guid.NewGuid(),
            PositionNameAr = "مهندس تقنية معلومات",
            PositionNameEn = "IT Engineer"
        },
        new HrPositionLookupDto
        {
            PositionId = Guid.NewGuid(),
            PositionNameAr = "ركن/2 دراسات",
            PositionNameEn = "TCM SOhar"
        },
        new HrPositionLookupDto
        {
            PositionId = Guid.NewGuid(),
            PositionNameAr = "مسؤول شؤون إدارية",
            PositionNameEn = "Administrative Affairs Officer"
        }
    };

        // Filter by name if provided
        if (!string.IsNullOrWhiteSpace(name))
        {
            dummyPositions = dummyPositions
                .Where(p => p.PositionNameAr.Contains(name, StringComparison.OrdinalIgnoreCase)
                         || p.PositionNameEn.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return dummyPositions.Take(20).ToList();
    }
}
