import { mapEnumToOptions } from '@abp/ng.core';

export enum ProposalStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
}

export const proposalStatusOptions = mapEnumToOptions(ProposalStatus);
