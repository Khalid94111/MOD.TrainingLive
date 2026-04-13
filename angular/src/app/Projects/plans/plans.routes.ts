import { Routes } from '@angular/router';

export const PLANS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./annual-plan-list/annual-plan-list.component').then(m => m.AnnualPlanListComponent),
  },
  {
    path: ':planId/entry',
    loadComponent: () =>
      import('./plan-entry/plan-entry.component').then(m => m.PlanEntryComponent),
  },
  {
    path: ':planId/review',
    loadComponent: () =>
      import('./plan-review/plan-review.component').then(m => m.PlanReviewComponent),
  },
  {
    path: ':planId/approve',
    loadComponent: () =>
      import('./plan-approval/plan-approval.component').then(m => m.PlanApprovalComponent),
  },
];

export const NOMINATIONS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('../nominations/nomination-list/nomination-list.component').then(m => m.NominationListComponent),
  },
];

export const PRICE_QUOTES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('../price-quotes/price-quote-list/price-quote-list.component').then(m => m.PriceQuoteListComponent),
  },
];

export const PROVIDERS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('../providers/provider-list/provider-list.component').then(m => m.ProviderListComponent),
  },
];
