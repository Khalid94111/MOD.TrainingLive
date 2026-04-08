import { authGuard, permissionGuard } from '@abp/ng.core';
import { Routes } from '@angular/router';
import { GDPR_COOKIE_CONSENT_ROUTES } from './gdpr-cookie-consent/gdpr-cookie-consent.routes';
// import { ORANGE_ROUTES } from './oranges/orange/orange-routes';
 // old name AppRoutingModule is not used anymore since we are using standalone components and loadComponent method for lazy loading. We can directly export the routes as APP_ROUTES and use it in our main module or wherever needed.
export const APP_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./home/home.component').then(c => c.HomeComponent),
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/dashboard.component').then(c => c.DashboardComponent),
    canActivate: [authGuard, permissionGuard],
  },
  {
    path: 'account',
    loadChildren: () => import('@volo/abp.ng.account/public').then(c => c.createRoutes()),
  },
  {
    path: 'gdpr',
    loadChildren: () => import('@volo/abp.ng.gdpr').then(c => c.createRoutes()),
  },
  {
    path: 'identity',
    loadChildren: () => import('@volo/abp.ng.identity').then(c => c.createRoutes()),
  },
  {
    path: 'language-management',
    loadChildren: () => import('@volo/abp.ng.language-management').then(c => c.createRoutes()),
  },
  {
    path: 'saas',
    loadChildren: () => import('@volo/abp.ng.saas').then(c => c.createRoutes()),
  },
  {
    path: 'openiddict',
    loadChildren: () => import('@volo/abp.ng.openiddictpro').then(c => c.createRoutes()),
  },
  {
    path: 'text-template-management',
    loadChildren: () => import('@volo/abp.ng.text-template-management').then(c => c.createRoutes()),
  },
  {
    path: 'file-management',
    loadChildren: () => import('@volo/abp.ng.file-management').then(c => c.createRoutes()),
  },
  {
    path: 'gdpr-cookie-consent',
    children: GDPR_COOKIE_CONSENT_ROUTES,
  },
  {
    path: 'setting-management',
    loadChildren: () => import('@abp/ng.setting-management').then(c => c.createRoutes()),
  },
  // {
  //   path: 'books',
  //   loadComponent: () => import('./book/book.component').then(c => c.BookComponent),
  //   canActivate: [authGuard, permissionGuard],
  // },
  // { path: 'oranges', children: ORANGE_ROUTES },
    // { path: 'trainings', children: TRAINING_ROUTES },
      {
    path: 'training',
    loadChildren: () =>
      import('./Projects/training.routes').then(m => m.TRAINING_ROUTES),
  },

];
