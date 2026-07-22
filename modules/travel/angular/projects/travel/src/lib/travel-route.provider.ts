import { APP_INITIALIZER, Provider } from '@angular/core';
import { eLayoutType, RoutesService } from '@abp/ng.core';

const TRAVEL_ROUTE_NAME = 'Travel::Menu:Travel';

export function configureRoutes(routesService: RoutesService) {
  return () => {
    routesService.add([
      {
        path: '/travel',
        name: TRAVEL_ROUTE_NAME,
        iconClass: 'fas fa-plane',
        layout: eLayoutType.application,
        order: 5,
      },
      {
        path: '/travel/requests',
        name: 'Travel::Menu:TravelRequests',
        parentName: TRAVEL_ROUTE_NAME,
        iconClass: 'fas fa-file-alt',
        layout: eLayoutType.application,
        order: 1,
        requiredPolicy: 'TravelManagement.TravelRequests',
      },
      {
        path: '/travel/allowance-rules',
        name: 'Travel::Menu:AllowanceRules',
        parentName: TRAVEL_ROUTE_NAME,
        iconClass: 'fas fa-calculator',
        layout: eLayoutType.application,
        order: 2,
        requiredPolicy: 'TravelManagement.TravelRequests',
      },
      {
        path: '/travel/allowance-rates',
        name: 'Travel::Menu:AllowanceRates',
        parentName: TRAVEL_ROUTE_NAME,
        iconClass: 'fas fa-money-bill',
        layout: eLayoutType.application,
        order: 3,
        requiredPolicy: 'TravelManagement.TravelRequests',
      },
      {
        path: '/travel/clothing-allowance-rules',
        name: 'Travel::Menu:ClothingAllowanceRules',
        parentName: TRAVEL_ROUTE_NAME,
        iconClass: 'fas fa-tshirt',
        layout: eLayoutType.application,
        order: 4,
        requiredPolicy: 'TravelManagement.TravelRequests',
      },
      {
        path: '/travel/accommodation-rules',
        name: 'Travel::Menu:AccommodationRules',
        parentName: TRAVEL_ROUTE_NAME,
        iconClass: 'fas fa-hotel',
        layout: eLayoutType.application,
        order: 5,
        requiredPolicy: 'TravelManagement.TravelRequests',
      },
      {
        path: '/travel/travel-types',
        name: 'Travel::Menu:TravelTypes',
        parentName: TRAVEL_ROUTE_NAME,
        iconClass: 'fas fa-tags',
        layout: eLayoutType.application,
        order: 6,
        requiredPolicy: 'TravelManagement.TravelRequests',
      },
      {
        path: '/travel/funding-source-vote-rules',
        name: 'Travel::Menu:FundingSourceVoteRules',
        parentName: TRAVEL_ROUTE_NAME,
        iconClass: 'fas fa-vote-yea',
        layout: eLayoutType.application,
        order: 7,
        requiredPolicy: 'TravelManagement.TravelRequests',
      },
    ]);
  };
}

export const TRAVEL_ROUTE_PROVIDERS: Provider[] = [
  {
    provide: APP_INITIALIZER,
    useFactory: configureRoutes,
    deps: [RoutesService],
    multi: true,
  },
];
