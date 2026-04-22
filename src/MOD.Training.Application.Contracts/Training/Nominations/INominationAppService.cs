using MOD.Training.Training.Nominations.Dtos;
using MOD.Training.Training.Plans.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Nominations;

public interface INominationAppService : IApplicationService
{
    Task<NominationDto> GetAsync(Guid id);
    Task<PagedResultDto<NominationDto>> GetListAsync(NominationGetListInput input);

    // Legacy — kept for backward compat, throws BusinessException
    [Obsolete("Moved to TrainingPlanItemAppService.CreateAsync")]
    Task<List<NominationDto>> CreateBatchAsync(CreateNominationDto input);

    Task ApproveAsync(Guid id, ApproveRejectNominationDto input);
    Task RejectAsync(Guid id, ApproveRejectNominationDto input);
    Task<List<NominationApprovalDto>> GetApprovalChainAsync(Guid nominationId);

    // CHG-05: Return + Replace
    Task ReturnAsync(Guid id, ReturnReasonDto input);
    Task<NominationDto> ReplaceAsync(Guid id, ReplaceNominationDto input);
}
