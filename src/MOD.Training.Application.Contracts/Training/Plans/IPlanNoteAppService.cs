using MOD.Training.Training.Plans.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Plans;

public interface IPlanNoteAppService : IApplicationService
{
    Task<PlanNoteDto> CreateAsync(CreatePlanNoteDto input);
    Task<List<PlanNoteDto>> GetListAsync(PlanNoteGetListInput input);
}
