using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MOD.Training.Training.Centers.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Centers;

public interface IHrLookupAppService : IApplicationService
{
    Task<List<HrEmployeeLookupDto>> GetEmployeesAsync(string serviceNumber, Guid orgUnitId);
    Task<List<HrPositionLookupDto>> GetPositionsAsync(string name, Guid orgUnitId);
}
