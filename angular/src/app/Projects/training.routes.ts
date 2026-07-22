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
  {
    path: 'centers/nominations',
    loadComponent: () =>
      import('./centers/center-nominations/center-nominations.component').then(
        (m) => m.CenterNominationsComponent
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
      // Creation is a focused full-page form. Once saved, the request enters the
      // normal stage-based detail shell under /:id.
      {
        path: 'casual-courses/new',
        loadComponent: () =>
          import('./casual-courses/casual-course-request/casual-course-request.component')
            .then(m => m.CasualCourseRequestComponent),
      },
      // ===== Phase 4B-α: Casual Course Detail (stage-based progressive disclosure) =====
      // Single-page layout: sticky header + status pipeline + 4 accordion sections.
      // Authoritative spec: docs/GTMS-Phase4B-Alpha-Frontend-Layout.md
      // `data.embedded` is inherited by inner components rendered inline by the shell
      // (request/approval/review) so they suppress their own page-toolbar.
      {
        path: 'casual-courses/:id',
        data: { embedded: true },
        loadComponent: () =>
          import('./casual-courses/casual-course-detail/casual-course-detail.component')
            .then(m => m.CasualCourseDetailComponent),
      },
      // Backward-compat redirects from the old tabbed/flat URLs to the single shell.
      // The shell reads the URL hash to auto-expand the matching section.
      { path: 'casual-courses/:id/details',             redirectTo: 'casual-courses/:id', pathMatch: 'full' },
      { path: 'casual-courses/:id/financials',          redirectTo: 'casual-courses/:id', pathMatch: 'full' },
      { path: 'casual-courses/:id/quotes',              redirectTo: 'casual-courses/:id', pathMatch: 'full' },
      { path: 'casual-courses/:id/travel',              redirectTo: 'casual-courses/:id', pathMatch: 'full' },
      { path: 'casual-courses/:id/edit',                redirectTo: 'casual-courses/:id', pathMatch: 'full' },
      { path: 'casual-courses/:id/review',              redirectTo: 'casual-courses/:id', pathMatch: 'full' },
      { path: 'casual-courses/:id/approve',             redirectTo: 'casual-courses/:id', pathMatch: 'full' },
      { path: 'casual-courses/:id/price-quotes',        redirectTo: 'casual-courses/:id', pathMatch: 'full' },
      // Standalone price-quote route kept for backward links.
      {
        path: 'sessions/:id/price-quotes',
        data: { parentArm: 'session' },
        loadComponent: () =>
          import('./execution/price-quotes/price-quotes.component')
            .then(m => m.PriceQuotesComponent),
      },

      // ===== Phase 4C-α: Annual Plan Sessions =====
      {
        path: 'annual-plan/sessions-queue',
        loadComponent: () =>
          import('./annual-plan-sessions/sessions-queue/sessions-queue.component')
            .then(m => m.SessionsQueueComponent),
      },
      {
        path: 'annual-plan/create-session/internal/:planItemId',
        data: { mode: 'internal' },
        loadComponent: () =>
          import('./annual-plan-sessions/create-session/create-session.component')
            .then(m => m.CreateSessionComponent),
      },
      {
        path: 'annual-plan/create-session/external/:planItemId',
        data: { mode: 'external' },
        loadComponent: () =>
          import('./annual-plan-sessions/create-session/create-session.component')
            .then(m => m.CreateSessionComponent),
      },
      {
        path: 'sessions',
        loadComponent: () =>
          import('./sessions/sessions-list/sessions-list.component')
            .then(m => m.SessionsListComponent),
      },
      {
        path: 'sessions/:id',
        data: { parentArm: 'session', embedded: true },
        loadComponent: () =>
          import('./sessions/session-detail/session-detail.component')
            .then(m => m.SessionDetailComponent),
      },
      {
        path: 'annual-plan/dashboard',
        loadComponent: () =>
          import('./annual-plan-sessions/dashboard/dashboard.component')
            .then(m => m.AnnualPlanDashboardComponent),
      },

      // ===== Phase 4B-β: Payments + Auto-Reallocation =====
      {
        path: 'payments/travel-allowances',
        loadComponent: () =>
          import('./payments/travel-allowance-payments/travel-allowance-payments.component')
            .then(m => m.TravelAllowancePaymentsComponent),
      },
      {
        path: 'payments/courses',
        loadComponent: () =>
          import('./payments/course-payments/course-payments.component')
            .then(m => m.CoursePaymentsComponent),
      },
      {
        path: 'payments/reallocations',
        loadComponent: () =>
          import('./payments/budget-reallocations/budget-reallocations.component')
            .then(m => m.BudgetReallocationsComponent),
      },

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
