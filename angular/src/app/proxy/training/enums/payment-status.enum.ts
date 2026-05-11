import { mapEnumToOptions } from '@abp/ng.core';

export enum PaymentStatus {
  Draft = 0,
  Confirmed = 1,
  Cancelled = 2,
}

export const paymentStatusOptions = mapEnumToOptions(PaymentStatus);
