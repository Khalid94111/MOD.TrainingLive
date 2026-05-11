import { mapEnumToOptions } from '@abp/ng.core';

export enum ProviderScope {
  Internal = 0,
  Local = 1,
  International = 2,
}

export const providerScopeOptions = mapEnumToOptions(ProviderScope);
