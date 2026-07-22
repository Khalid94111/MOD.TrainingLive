import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from '@abp/ng.core';

export const TRAVEL_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'requests',
  },
  {
    path: 'requests',
    loadComponent: () =>
      import('./components/travel-requests/travel-requests.component').then((c) => c.TravelRequestsComponent),
  },
  {
    path: 'requests/:id/employees/:employeeId',
    canActivate: [authGuard, permissionGuard],
    data: { requiredPolicy: 'TravelManagement.TravelRequests' },
    loadComponent: () =>
      import('./components/travel-request-employee-detail/travel-request-employee-detail.component').then((c) => c.TravelRequestEmployeeDetailComponent),
  },
  {
    path: 'requests/:id',
    canActivate: [authGuard, permissionGuard],
    data: { requiredPolicy: 'TravelManagement.TravelRequests' },
    loadComponent: () =>
      import('./components/travel-request-detail/travel-request-detail.component').then((c) => c.TravelRequestDetailComponent),
  },
  {
    path: 'allowance-rules',
    canActivate: [authGuard, permissionGuard],
    data: { requiredPolicy: 'TravelManagement.TravelRequests' },
    loadComponent: () =>
      import('./components/allowance-rules/allowance-rules.component').then((c) => c.AllowanceRulesComponent),
  },
  {
    path: 'allowance-rates',
    canActivate: [authGuard, permissionGuard],
    data: { requiredPolicy: 'TravelManagement.TravelRequests' },
    loadComponent: () =>
      import('./components/allowance-rates/allowance-rates.component').then((c) => c.AllowanceRatesComponent),
  },
  {
    path: 'clothing-allowance-rules',
    canActivate: [authGuard, permissionGuard],
    data: { requiredPolicy: 'TravelManagement.TravelRequests' },
    loadComponent: () =>
      import('./components/clothing-allowance-rules/clothing-allowance-rules.component').then((c) => c.ClothingAllowanceRulesComponent),
  },
  {
    path: 'travel-types',
    canActivate: [authGuard, permissionGuard],
    data: { requiredPolicy: 'TravelManagement.TravelRequests' },
    loadComponent: () =>
      import('./components/travel-types/travel-types.component').then((c) => c.TravelTypesComponent),
  },
  {
    path: 'accommodation-rules',
    canActivate: [authGuard, permissionGuard],
    data: { requiredPolicy: 'TravelManagement.TravelRequests' },
    loadComponent: () =>
      import('./components/accommodation-rules/accommodation-rules.component').then((c) => c.AccommodationRulesComponent),
  },
  {
    path: 'funding-source-vote-rules',
    canActivate: [authGuard, permissionGuard],
    data: { requiredPolicy: 'TravelManagement.TravelRequests' },
    loadComponent: () =>
      import('./components/funding-source-vote-rules/funding-source-vote-rules.component').then((c) => c.FundingSourceVoteRulesComponent),
  },
];
