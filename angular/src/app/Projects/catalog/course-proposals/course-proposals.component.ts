import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import {
  DxDataGridModule, DxButtonModule, DxTextBoxModule, DxSelectBoxModule,
  DxPopupModule, DxTextAreaModule,
} from 'devextreme-angular';
import { CourseProposalService, CourseFieldService, TrainingLocalizationHelper } from '../../shared';
import type { CourseFieldDto, CourseProposalDto, CreateCourseProposalDto } from '../../shared';
import { ProposalStatus } from '../../shared/models/training-enums';
import { ToolbarItem } from 'devextreme/ui/popup';

@Component({
  selector: 'app-course-proposals',
  standalone: true,
  imports: [
    CommonModule, FormsModule, LocalizationPipe,
    DxDataGridModule, DxButtonModule, DxTextBoxModule, DxSelectBoxModule,
    DxPopupModule, DxTextAreaModule,
  ],
  templateUrl: './course-proposals.component.html',
})
export class CourseProposalsComponent implements OnInit {
  private readonly proposalService = inject(CourseProposalService);
  private readonly fieldService = inject(CourseFieldService);
  readonly l = inject(TrainingLocalizationHelper);

  // Data
  allProposals = signal<CourseProposalDto[]>([]);
  courseFields = signal<CourseFieldDto[]>([]);
  searchText = signal('');
  filterStatus = signal<ProposalStatus | null>(null);
  dataSource: any;

  // Computed from allProposals
  pendingProposals = computed(() =>
    this.allProposals().filter(p => p.status === ProposalStatus.Pending));
  reviewedProposals = computed(() =>
    this.allProposals().filter(p => p.status !== ProposalStatus.Pending));
  pendingCount = computed(() => this.pendingProposals().length);
  approvedCount = computed(() =>
    this.allProposals().filter(p => p.status === ProposalStatus.Approved).length);
  rejectedCount = computed(() =>
    this.allProposals().filter(p => p.status === ProposalStatus.Rejected).length);
  totalCount = computed(() => this.allProposals().length);

  // Submit dialog
  isSubmitDialogVisible = signal(false);
  proposalForm = signal<CreateCourseProposalDto>({
    courseNameAr: '', courseNameEn: '', category: '', nature: '', fieldId: '',
  });

  // Review dialog
  isReviewDialogVisible = signal(false);
  selectedProposal = signal<CourseProposalDto | null>(null);
  reviewDecision = signal<ProposalStatus | null>(null);
  rejectionReason = signal('');

  categoryDataSource: any[] = [];
  natureDataSource: any[] = [];
  statusFilterDataSource: any[] = [];
  submitProposalPopupToolbarItems: ToolbarItem[] | undefined;
  reviewProposelPopupToolbarItems: ToolbarItem[] | undefined;

  ngOnInit(): void {
 this.submitProposalPopupToolbarItems= [
    {
      widget: 'dxButton',
      location: 'after',
      toolbar: 'bottom',
      options: {
        text: this.l.t('::Training.Submit'),
        icon: 'save',
        type: 'default',
        onClick: () => this.onSubmitProposal()
      }
    },
    {
      widget: 'dxButton',
      location: 'after',
      toolbar: 'bottom',
      options: {
        text: this.l.t('::Training.Cancel'),
        onClick: () => this.isSubmitDialogVisible.set(false)
      }
    }
  ];

 
  this.reviewProposelPopupToolbarItems= [
    {
      widget: 'dxButton',
      location: 'after',
      toolbar: 'bottom',
      options: {
        text: this.l.t('::Training.Confirm'),
        icon: 'save',
        type: 'default',
        onClick: () => this.onSubmitReview()
      }
    },
    {
      widget: 'dxButton',
      location: 'after',
      toolbar: 'bottom',
      options: {
        text: this.l.t('::Training.Cancel'),
        onClick: () => this.isReviewDialogVisible.set(false)
      }
    }
  ];


    this.categoryDataSource = this.l.categoryDataSource();
    this.natureDataSource = this.l.natureDataSource();
    this.statusFilterDataSource = [
      { value: null, text: this.l.t('::Training.All') },
      ...this.l.proposalStatusDataSource(),
    ];
    this.loadFields();
    this.loadProposals();
  }

  async loadProposals(): Promise<void> {
    const result = await this.proposalService.getList({
      filter: this.searchText() || undefined,
      status: this.filterStatus() ?? undefined,
      skipCount: 0,
      maxResultCount: 100,
    });
    this.allProposals.set(result.items ?? []);
  }

  private async loadFields(): Promise<void> {
    this.courseFields.set(await this.fieldService.getAllActive());
  }

  onSearch(): void { this.loadProposals(); }
  onFilterChange(): void { this.loadProposals(); }

  // ── Submit dialog ──

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
    await this.loadProposals();
  }

  // ── Review dialog ──

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
    await this.loadProposals();
  }

  // ── Helpers ──

  getCategoryText = (data: any): string => this.l.category(data.category);
  getNatureText = (data: any): string => this.l.nature(data.nature);

 
 
}
 