import { AfterViewInit, Component, ViewChild, inject } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule } from '@angular/forms';
import {
  NgbDateAdapter,
  NgbDateNativeAdapter,
  NgbDatepickerModule,
} from '@ng-bootstrap/ng-bootstrap';
import { LocalizationPipe, PermissionDirective } from '@abp/ng.core';
import { PageComponent } from '@abp/ng.components/page';
import { DateRangePickerComponent } from '@volo/abp.commercial.ng.ui';
import { EditionsUsageWidgetComponent, LatestTenantsWidgetComponent } from '@volo/abp.ng.saas';

const now = new Date();
const oneMonthAgo = new Date(now.getFullYear(), now.getMonth() - 1, now.getDate());

@Component({
  selector: 'app-host-dashboard',
  templateUrl: './host-dashboard.component.html',
  styleUrls: ['./host-dashboard.component.scss'],
  imports: [
    EditionsUsageWidgetComponent,
    LatestTenantsWidgetComponent,
    FormsModule,
    ReactiveFormsModule,
    NgbDatepickerModule,
    PageComponent,
    DateRangePickerComponent,
    PermissionDirective,
    LocalizationPipe,
  ],
  providers: [{ provide: NgbDateAdapter, useClass: NgbDateNativeAdapter }],
})

export class HostDashboardComponent implements AfterViewInit {
  fb = inject(FormBuilder);

  @ViewChild('editionsUsageWidget', { static: false })
  editionsUsageWidget: EditionsUsageWidgetComponent;

  @ViewChild('latestTenantsWidget', { static: false })
  latestTenantsWidget: LatestTenantsWidgetComponent;

  toDate = now;
  fromDate = oneMonthAgo;

  formFilters = this.fb.group({
    times: [
      {
        fromDate: this.fromDate,
        toDate: this.toDate,
      },
    ],
  });
  
  ngAfterViewInit() {
    this.refresh();
  }

  refresh() {
    const { fromDate, toDate } = {
      ...this.formFilters.value.times,
    };

    this.editionsUsageWidget?.draw();
    this.latestTenantsWidget?.draw();
  }
  
  private convertToString(value: Date): string {
    return value ? value.toLocalISOString() : '';
  }
}
