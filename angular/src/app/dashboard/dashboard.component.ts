import { Component } from '@angular/core';
import { PermissionDirective } from '@abp/ng.core';
import { HostDashboardComponent } from './host-dashboard/host-dashboard.component';
import { TenantDashboardComponent } from './tenant-dashboard/tenant-dashboard.component';

@Component({
  selector: 'app-dashboard',
  template: `
    <app-host-dashboard *abpPermission="'Training.Dashboard.Host'" />
    <app-tenant-dashboard *abpPermission="'Training.Dashboard.Tenant'" />
  `,
  imports: [HostDashboardComponent,TenantDashboardComponent, PermissionDirective]
})
export class DashboardComponent {}
