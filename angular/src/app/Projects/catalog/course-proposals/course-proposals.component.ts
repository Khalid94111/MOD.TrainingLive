import { Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import {
  DxDataGridModule, DxButtonModule, DxTextBoxModule, DxSelectBoxModule,
  DxPopupModule, DxTextAreaModule, DxRadioGroupModule, DxDataGridComponent,
} from 'devextreme-angular';
import { CourseProposalService, CourseFieldService, TrainingLocalizationHelper, createAbpStore } from '../../shared';
import type { CourseFieldDto, CourseProposalDto, CreateCourseProposalDto } from '../../shared';
import { ProposalStatus } from '../../shared/models/training-enums';

@Component({
  selector: 'app-course-proposals',
  standalone: true,
  imports: [
    CommonModule, FormsModule, LocalizationPipe,
    DxDataGridModule, DxButtonModule, DxTextBoxModule, DxSelectBoxModule,
    DxPopupModule, DxTextAreaModule, DxRadioGroupModule,
  ],
  templateUrl: './course-proposals.component.html',
})
export class CourseProposalsComponent implements OnInit {
  private readonly proposalService = inject(CourseProposalService);
  private readonly fieldService = inject(CourseFieldService);
  readonly l = inject(TrainingLocalizationHelper);
  readonly grid = viewChild<DxDataGridComponent>('proposalGrid');

  dataSource!: ReturnType<typeof createAbpStore<CourseProposalDto>>;
  courseFields = signal<CourseFieldDto[]>([]);
  searchText = signal('');
  filterStatus = signal<ProposalStatus | null>(null);

  isSubmitDialogVisible = signal(false);
  proposalForm = signal<CreateCourseProposalDto>({
    courseNameAr: '', courseNameEn: '', category: '', nature: '', fieldId: '',
  });

  isReviewDialogVisible = signal(false);
  selectedProposal = signal<CourseProposalDto | null>(null);
  reviewDecision = signal<ProposalStatus | null>(null);
  rejectionReason = signal('');

  categoryDataSource: any[] = [];
  natureDataSource: any[] = [];
  statusFilterDataSource: any[] = [];
  decisionOptions: any[] = [];

  ngOnInit(): void {
    this.categoryDataSource = this.l.categoryDataSource();
    this.natureDataSource = this.l.natureDataSource();
    this.statusFilterDataSource = [
      { value: null, text: this.l.t('::Training.All') },
      ...this.l.proposalStatusDataSource(),
    ];
    this.decisionOptions = [
      { value: ProposalStatus.Approved, text: this.l.t('::Training.Approve') },
      { value: ProposalStatus.Rejected, text: this.l.t('::Training.Reject') },
    ];
    this.loadFields();
    this.initDataSource();
  }

  private initDataSource(): void {
    this.dataSource = createAbpStore<CourseProposalDto>({
      loadFn: params =>
        this.proposalService.getList({
          ...params,
          filter: this.searchText() || undefined,
          status: this.filterStatus() ?? undefined,
        }),
    });
  }

  private async loadFields(): Promise<void> {
    this.courseFields.set(await this.fieldService.getAllActive());
  }

  onSearch(): void { this.grid()?.instance.refresh(); }
  onFilterChange(): void { this.grid()?.instance.refresh(); }

  onOpenSubmitDialog(): void {
    this.proposalForm.set({ courseNameAr: '', courseNameEn: '', category: '', nature: '', fieldId: '' });
    this.isSubmitDialogVisible.set(true);
  }

  updateProposalField<K extends keyof CreateCourseProposalDto>(key: K, value: CreateCourseProposalDto[K]): void {
    this.proposalForm.update(f => ({ ...f, [key]: value }));
  }

  async onSubmitProposal(): Promise<void> {
    await this.proposalService.create(this.proposalForm());
    this.isSubmitDialogVisible.set(false);
    this.grid()?.instance.refresh();
  }

  onOpenReviewDialog(proposal: CourseProposalDto): void {
    this.selectedProposal.set(proposal);
    this.reviewDecision.set(null);
    this.rejectionReason.set('');
    this.isReviewDialogVisible.set(true);
  }

  async onSubmitReview(): Promise<void> {
    const decision = this.reviewDecision();
    const proposal = this.selectedProposal();
    if (!decision || !proposal) return;

    await this.proposalService.review(proposal.id, {
      decision,
      rejectionReason: decision === ProposalStatus.Rejected ? this.rejectionReason() : undefined,
    });
    this.isReviewDialogVisible.set(false);
    this.grid()?.instance.refresh();
  }

  getStatusBadge = (rowData: any): string => this.l.proposalStatus(rowData.status).label;
  getStatusCssClass(status: number): string { return this.l.proposalStatus(status).cssClass; }
}
