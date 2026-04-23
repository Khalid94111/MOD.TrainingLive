import { Routes } from '@angular/router';

/**
 * Training module routes — Angular 21 standalone pattern.
 *
 * Register in your app routing:
 *
 *   // app.routes.ts
 *   {
 *     path: 'training',
 *     loadChildren: () => import('./training/training.routes').then(m => m.TRAINING_ROUTES),
 *   }
 */
export const TRAINING_ROUTES: Routes = [
  {
    path: '',
    children: [
      // Phase 1: Catalog
      {
        path: 'catalog',
        loadComponent: () =>
          import('./catalog/course-catalog/course-catalog.component').then(
            m => m.CourseCatalogComponent
          ),
      },
      {
        path: 'catalog/fields',
        loadComponent: () =>
          import('./catalog/course-fields/course-fields.component').then(
            m => m.CourseFieldsComponent
          ),
      },
      {
        path: 'catalog/proposals',
        loadComponent: () =>
          import('./catalog/course-proposals/course-proposals.component').then(
            m => m.CourseProposalsComponent
          ),
      },

      // Phase 1: Tenant Courses
      {
        path: 'tenant-courses',
        loadComponent: () =>
          import('./tenant-courses/tenant-courses-list/tenant-courses-list.component').then(
            m => m.TenantCoursesListComponent
          ),
      },

      // Finance routes
{
  path: 'finance/financial-items',
  loadComponent: () =>
    import('./finance/financial-items/financial-items.component').then(
      m => m.FinancialItemsComponent
    ),
},
{
  path: 'finance/defaults',
  loadComponent: () =>
    import('./finance/financial-item-defaults/financial-item-defaults.component').then(
      m => m.FinancialItemDefaultsComponent
    ),
},
{
  path: 'finance/exchange-rates',
  loadComponent: () =>
    import('./finance/exchange-rates/exchange-rates.component').then(
      m => m.ExchangeRatesComponent
    ),
},
{
  path: 'finance/budgets',
  loadComponent: () =>
    import('./finance/training-budgets/training-budgets.component').then(
      m => m.TrainingBudgetsComponent
    ),
},
// ─── Phase 2B: Centers (ADD THESE) ───
  {
    path: 'centers',
    loadComponent: () =>
      import('./centers/training-centers/training-centers.component').then(
        (m) => m.TrainingCentersComponent
      ),
  },
  {
    path: 'centers/plans',
    loadComponent: () =>
      import('./centers/center-plans/center-plans.component').then(
        (m) => m.CenterPlansComponent
      ),
  },

  // ===== Phase 3: Annual Plans =====
  {
    path: 'plans',
    loadComponent: () =>
      import('./plans/annual-plan-list/annual-plan-list.component').then(m => m.AnnualPlanListComponent),
  },
  {
    path: 'plans/:planId/entry',
    loadComponent: () =>
      import('./plans/plan-entry/plan-entry.component').then(m => m.PlanEntryComponent),
  },
  {
    path: 'plans/:planId/review',
    loadComponent: () =>
      import('./plans/plan-review/plan-review.component').then(m => m.PlanReviewComponent),
  },
  {
    path: 'plans/:planId/approve',
    loadComponent: () =>
      import('./plans/plan-approval/plan-approval.component').then(m => m.PlanApprovalComponent),
  },

  // ===== Phase 3: Nominations =====
  {
    path: 'nominations',
    loadComponent: () =>
      import('./nominations/nomination-list/nomination-list.component').then(m => m.NominationListComponent),
  },

  // ===== Phase 3: Price Quotes =====
  {
    path: 'price-quotes',
    loadComponent: () =>
      import('./price-quotes/price-quote-list/price-quote-list.component').then(m => m.PriceQuoteListComponent),
  },

  // ===== Phase 3: Training Providers =====
  {
    path: 'providers',
    loadComponent: () =>
      import('./providers/provider-list/provider-list.component').then(m => m.ProviderListComponent),
  },
      // Phase 2 (future)
      // { path: 'finance/items', loadComponent: () => import(...) },
      // { path: 'finance/defaults', loadComponent: () => import(...) },
      // { path: 'finance/exchange-rates', loadComponent: () => import(...) },
      // { path: 'finance/budgets', loadComponent: () => import(...) },
      // { path: 'centers', loadComponent: () => import(...) },
      // { path: 'centers/:centerId/plans', loadComponent: () => import(...) },
      // { path: 'centers/:centerId/sessions', loadComponent: () => import(...) },

      // Phase 3 (future)
      // { path: 'plans', loadComponent: () => import(...) },
      // { path: 'plans/:planId/entry', loadComponent: () => import(...) },
      // { path: 'plans/:planId/review', loadComponent: () => import(...) },
      // { path: 'plans/:planId/approval', loadComponent: () => import(...) },
      // { path: 'nominations', loadComponent: () => import(...) },
      // { path: 'price-quotes', loadComponent: () => import(...) },

      // ===== Phase 4A: Casual Courses =====
      {
        path: 'casual-courses',
        loadComponent: () =>
          import('./casual-courses/casual-courses-list/casual-courses-list.component')
            .then(m => m.CasualCoursesListComponent),
      },
      {
        path: 'casual-courses/new',
        loadComponent: () =>
          import('./casual-courses/casual-course-request/casual-course-request.component')
            .then(m => m.CasualCourseRequestComponent),
      },
      {
        path: 'casual-courses/:id/edit',
        loadComponent: () =>
          import('./casual-courses/casual-course-request/casual-course-request.component')
            .then(m => m.CasualCourseRequestComponent),
      },
      {
        path: 'casual-courses/:id/review',
        loadComponent: () =>
          import('./casual-courses/casual-course-review/casual-course-review.component')
            .then(m => m.CasualCourseReviewComponent),
      },
      {
        path: 'casual-courses/:id/approve',
        loadComponent: () =>
          import('./casual-courses/casual-course-approval/casual-course-approval.component')
            .then(m => m.CasualCourseApprovalComponent),
      },

      // Phase 4 (future)
      // { path: 'payments/travel', loadComponent: () => import(...) },
      // { path: 'payments/course', loadComponent: () => import(...) },
      // { path: 'payments/reallocations', loadComponent: () => import(...) },
      // { path: 'shared-requests', loadComponent: () => import(...) },

      // Phase 5 (future)
      // { path: 'post-course/results', loadComponent: () => import(...) },
      // { path: 'post-course/certificates', loadComponent: () => import(...) },
      // { path: 'post-course/evaluation', loadComponent: () => import(...) },
      // { path: 'reports/financial-items', loadComponent: () => import(...) },
      // { path: 'reports/reallocations', loadComponent: () => import(...) },
      // { path: 'reports/plan-progress', loadComponent: () => import(...) },
      // { path: 'reports/plan-table', loadComponent: () => import(...) },
      // { path: 'reports/plan-gantt', loadComponent: () => import(...) },

      // Default
      { path: '', redirectTo: 'catalog', pathMatch: 'full' },
    ],
  },
];
