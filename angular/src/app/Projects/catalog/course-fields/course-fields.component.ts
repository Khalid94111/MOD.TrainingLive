import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { DxDataGridModule } from 'devextreme-angular';
import { CourseFieldService, createAbpStore } from '../../shared';
import type { CourseFieldDto } from '../../shared';

@Component({
  selector: 'app-course-fields',
  standalone: true,
  imports: [CommonModule, LocalizationPipe, DxDataGridModule],
  templateUrl: './course-fields.component.html',
})
export class CourseFieldsComponent implements OnInit {
  private readonly fieldService = inject(CourseFieldService);

  dataSource!: ReturnType<typeof createAbpStore<CourseFieldDto>>;

  ngOnInit(): void {
    this.dataSource = createAbpStore<CourseFieldDto>({
      loadFn: params => this.fieldService.getList(params),
      insertFn: values => this.fieldService.create(values as any),
      updateFn: (key, values) => this.fieldService.update(key, values as any),
      removeFn: key => this.fieldService.delete(key),
    });
  }
}
