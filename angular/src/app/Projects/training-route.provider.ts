import { eLayoutType, RoutesService } from '@abp/ng.core';
import { APP_INITIALIZER, inject } from '@angular/core';

/**
 * Registers Training module menu items with ABP's navigation system.
 *
 * Add to app.config.ts providers:
 *   TRAINING_ROUTE_PROVIDER
 */
export const TRAINING_ROUTE_PROVIDER = {
  provide: APP_INITIALIZER,
  multi: true,
  useFactory: () => {
    const routes = inject(RoutesService);

    return () => {
      routes.add([
        {
          path: '/training',
          name: '::Training.Menu.Training',
          iconClass: 'fas fa-graduation-cap',
          order: 30,
          layout: eLayoutType.application,
        },
        {
          path: '/training/catalog',
          name: '::Training.Menu.Catalog',
          parentName: '::Training.Menu.Training',
          iconClass: 'fas fa-book',
          order: 1,
          requiredPolicy: 'Training.CourseCatalog',
        },
        {
          path: '/training/catalog/fields',
          name: '::Training.Menu.Fields',
          parentName: '::Training.Menu.Training',
          iconClass: 'fas fa-tags',
          order: 2,
          requiredPolicy: 'Training.CourseFields',
        },
        {
          path: '/training/catalog/proposals',
          name: '::Training.Menu.Proposals',
          parentName: '::Training.Menu.Training',
          iconClass: 'fas fa-lightbulb',
          order: 3,
          requiredPolicy: 'Training.CourseProposals',
        },
        {
          path: '/training/tenant-courses',
          name: '::Training.Menu.TenantCourses',
          parentName: '::Training.Menu.Training',
          iconClass: 'fas fa-building',
          order: 4,
          requiredPolicy: 'Training.TenantCourses',
        },

        // ── Phase 2 (uncomment when ready) ──
        // {
        //   path: '/training/finance/items',
        //   name: '::Training.Menu.FinancialItems',
        //   parentName: '::Training.Menu.Training',
        //   iconClass: 'fas fa-money-bill',
        //   order: 5,
        //   requiredPolicy: 'Training.Finance',
        // },
        // {
        //   path: '/training/centers',
        //   name: '::Training.Menu.TrainingCenters',
        //   parentName: '::Training.Menu.Training',
        //   iconClass: 'fas fa-school',
        //   order: 6,
        //   requiredPolicy: 'Training.TrainingCenters',
        // },

        // ── Phase 3 ──
        // {
        //   path: '/training/plans',
        //   name: '::Training.Menu.AnnualPlans',
        //   parentName: '::Training.Menu.Training',
        //   iconClass: 'fas fa-calendar-alt',
        //   order: 7,
        //   requiredPolicy: 'Training.TrainingPlans',
        // },
        // {
        //   path: '/training/nominations',
        //   name: '::Training.Menu.Nominations',
        //   parentName: '::Training.Menu.Training',
        //   iconClass: 'fas fa-user-check',
        //   order: 8,
        //   requiredPolicy: 'Training.Nominations',
        // },

        // ── Phase 4 ──
        // {
        //   path: '/training/casual-courses',
        //   name: '::Training.Menu.CasualCourses',
        //   parentName: '::Training.Menu.Training',
        //   iconClass: 'fas fa-random',
        //   order: 9,
        //   requiredPolicy: 'Training.CasualCourses',
        // },

        // ── Phase 5 ──
        // {
        //   path: '/training/reports/plan-progress',
        //   name: '::Training.Menu.Reports',
        //   parentName: '::Training.Menu.Training',
        //   iconClass: 'fas fa-chart-bar',
        //   order: 10,
        //   requiredPolicy: 'Training.Reports',
        // },
      ]);
    };
  },
};
