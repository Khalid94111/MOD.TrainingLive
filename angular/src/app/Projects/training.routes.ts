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

      // Phase 4 (future)
      // { path: 'casual-courses', loadComponent: () => import(...) },
      // { path: 'casual-courses/new', loadComponent: () => import(...) },
      // { path: 'casual-courses/:id/review', loadComponent: () => import(...) },
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
