import { eLayoutType, RoutesService } from '@abp/ng.core';
import {
  EnvironmentProviders,
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { eTravelRouteNames } from '../enums/route-names';

export const TRAVEL_ROUTE_PROVIDERS = [
  provideAppInitializer(() => {
    configureRoutes();
  }),
];

export function configureRoutes() {
  const routesService = inject(RoutesService);
  routesService.add([
    {
      path: '/travel',
      name: eTravelRouteNames.Travel,
      iconClass: 'fas fa-plane',
      layout: eLayoutType.application,
      order: 3,
    },
    {
      path: '/travel/requests',
      name: eTravelRouteNames.TravelRequests,
      iconClass: 'fas fa-list',
      layout: eLayoutType.application,
      parentName: eTravelRouteNames.Travel,
      order: 1,
    },
    {
      path: '/travel/funding-source-vote-rules',
      name: eTravelRouteNames.FundingSourceVoteRules,
      iconClass: 'fas fa-file-invoice-dollar',
      layout: eLayoutType.application,
      parentName: eTravelRouteNames.Travel,
      order: 20,
      requiredPolicy: 'TravelManagement.TravelRequests.ManageAllowances',
    },
  ]);
}

const TRAVEL_PROVIDERS: EnvironmentProviders[] = [...TRAVEL_ROUTE_PROVIDERS];

export function provideTravel() {
  return makeEnvironmentProviders(TRAVEL_PROVIDERS);
}
