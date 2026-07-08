import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { CourseProposalService, CourseFieldService, TrainingLocalizationHelper } from '../../shared';
import type { CourseFieldDto, CourseProposalDto, CreateCourseProposalDto } from '../../shared';
import { ProposalStatus } from '../../shared/models/training-enums';
import { ConfirmationService, ToasterService } from '@abp/ng.theme.shared';

@Component({
  selector: 'app-course-proposals',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe],
  templateUrl: './course-proposals.component.html',
  styleUrl: './course-proposals.component.scss',
})
export class CourseProposalsComponent implements OnInit {
  private readonly proposalService = inject(CourseProposalService);
  private readonly fieldService = inject(CourseFieldService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);
  readonly l = inject(TrainingLocalizationHelper);

  readonly ProposalStatus = ProposalStatus;

  allProposals = signal<CourseProposalDto[]>([]);
  courseFields = signal<CourseFieldDto[]>([]);
  searchText = signal('');
  filterStatus = signal<ProposalStatus | null>(null);
  isLoading = signal(false);

  pendingProposals = computed(() => this.allProposals().filter(p => p.status === ProposalStatus.Pending));
  reviewedProposals = computed(() => this.allProposals().filter(p => p.status !== ProposalStatus.Pending));
  pendingCount = computed(() => this.pendingProposals().length);
  approvedCount = computed(() => this.allProposals().filter(p => p.status === ProposalStatus.Approved).length);
  rejectedCount = computed(() => this.allProposals().filter(p => p.status === ProposalStatus.Rejected).length);
  totalCount = computed(() => this.allProposals().length);

  // Submit dialog
  isSubmitDialogVisible = signal(false);
  proposalForm = signal<CreateCourseProposalDto>({ courseNameAr: '', courseNameEn: '', category: '', nature: '', fieldId: '' });
  submitValidationErrors = signal<string[]>([]);
  isSubmitting = signal(false);

  // Review dialog
  isReviewDialogVisible = signal(false);
  selectedProposal = signal<CourseProposalDto | null>(null);
  reviewDecision = signal<ProposalStatus | null>(null);
  rejectionReason = signal('');
  isReviewing = signal(false);
  reviewValidationErrors = signal<string[]>([]);

  categoryOptions = computed(() => this.l.categoryDataSource());
  natureOptions = computed(() => this.l.natureDataSource());
  statusFilterOptions = computed(() => [{ value: null, text: this.l.t('::Training.All') }, ...this.l.proposalStatusDataSource()]);

  ngOnInit(): void {
    this.loadFields();
    this.loadProposals();
  }

  async loadProposals(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await this.proposalService.getList({
        filter: this.searchText() || undefined,
        status: this.filterStatus() ?? undefined,
        skipCount: 0,
        maxResultCount: 100,
      });
      this.allProposals.set(result.items ?? []);
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async loadFields(): Promise<void> {
    try {
      this.courseFields.set(await this.fieldService.getAllActive());
    } catch {
      // silently fail — fields are optional for display
    }
  }

  onSearch(): void { this.loadProposals(); }
  onFilterChange(): void { this.loadProposals(); }

  onOpenSubmitDialog(): void {
    this.submitValidationErrors.set([]);
    this.proposalForm.set({ courseNameAr: '', courseNameEn: '', category: '', nature: '', fieldId: '' });
    this.isSubmitDialogVisible.set(true);
  }

  updateProposalField<K extends keyof CreateCourseProposalDto>(key: K, value: CreateCourseProposalDto[K]): void {
    this.proposalForm.update(f => ({ ...f, [key]: value }));
    if (this.submitValidationErrors().length > 0) {
      this.validateSubmitForm();
    }
  }

  validateSubmitForm(): boolean {
    const errors: string[] = [];
    const data = this.proposalForm();
    if (!data.courseNameAr || data.courseNameAr.trim().length < 2) {
      errors.push(this.l.t('::Training.CourseProposal.Validation.NameArRequired'));
    }
    if (!data.courseNameEn || data.courseNameEn.trim().length < 2) {
      errors.push(this.l.t('::Training.CourseProposal.Validation.NameEnRequired'));
    }
    if (!data.category) {
      errors.push(this.l.t('::Training.CourseProposal.Validation.CategoryRequired'));
    }
    if (!data.nature) {
      errors.push(this.l.t('::Training.CourseProposal.Validation.NatureRequired'));
    }
    if (!data.fieldId) {
      errors.push(this.l.t('::Training.CourseProposal.Validation.FieldRequired'));
    }
    this.submitValidationErrors.set(errors);
    return errors.length === 0;
  }

  async onSubmitProposal(): Promise<void> {
    if (!this.validateSubmitForm()) return;
    this.isSubmitting.set(true);
    try {
      await this.proposalService.create(this.proposalForm());
      this.toaster.success(this.l.t('::Training.CourseProposal.SubmitSuccess'));
      this.isSubmitDialogVisible.set(false);
      await this.loadProposals();
    } catch {
      this.toaster.error(this.l.t('::Training.Common.SaveError'));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  onOpenReviewDialog(proposal: CourseProposalDto): void {
    this.reviewValidationErrors.set([]);
    this.selectedProposal.set(proposal);
    this.reviewDecision.set(null);
    this.rejectionReason.set('');
    this.isReviewDialogVisible.set(true);
  }

  validateReviewForm(): boolean {
    const errors: string[] = [];
    if (this.reviewDecision() === null) {
      errors.push(this.l.t('::Training.CourseProposal.Validation.DecisionRequired'));
    }
    if (this.reviewDecision() === ProposalStatus.Rejected && (!this.rejectionReason() || this.rejectionReason().trim().length < 3)) {
      errors.push(this.l.t('::Training.CourseProposal.Validation.RejectionReasonRequired'));
    }
    this.reviewValidationErrors.set(errors);
    return errors.length === 0;
  }

  async onSubmitReview(): Promise<void> {
    if (!this.validateReviewForm()) return;
    const decision = this.reviewDecision();
    const proposal = this.selectedProposal();
    if (!decision || !proposal) return;

    this.isReviewing.set(true);
    try {
      await this.proposalService.review(proposal.id, {
        decision,
        rejectionReason: decision === ProposalStatus.Rejected ? this.rejectionReason() : undefined,
      });
      this.toaster.success(
        decision === ProposalStatus.Approved
          ? this.l.t('::Training.CourseProposal.ApproveSuccess')
          : this.l.t('::Training.CourseProposal.RejectSuccess')
      );
      this.isReviewDialogVisible.set(false);
      await this.loadProposals();
    } catch {
      this.toaster.error(this.l.t('::Training.Common.SaveError'));
    } finally {
      this.isReviewing.set(false);
    }
  }

  onCancelSubmit(): void {
    this.isSubmitDialogVisible.set(false);
    this.submitValidationErrors.set([]);
  }

  onCancelReview(): void {
    this.isReviewDialogVisible.set(false);
    this.reviewValidationErrors.set([]);
  }

  getCategoryText = (data: any): string => this.l.category(data.category);
  getNatureText = (data: any): string => this.l.nature(data.nature);
  getStatusLabel = (status: ProposalStatus): string => {
    const info = this.l.proposalStatus(status);
    return info.label;
  };
}
