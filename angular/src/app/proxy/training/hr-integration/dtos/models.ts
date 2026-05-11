import type { EntityDto } from '@abp/ng.core';

export interface GeographicalLocationDto extends EntityDto<string> {
  arabicName?: string;
  englishName?: string;
  locationParentId?: string | null;
}
