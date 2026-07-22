import { Component, inject, OnInit, OnDestroy, signal, computed, ViewChild, ChangeDetectionStrategy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModal, NgbModalModule } from '@ng-bootstrap/ng-bootstrap';
import { LocalizationModule, LocalizationService, PermissionDirective } from '@abp/ng.core';
import { ConfirmationService, ToasterService } from '@abp/ng.theme.shared';
import {
  AllowanceRuleService,
  AllowanceRuleDto,
  CreateUpdateAllowanceRuleDto,
  AllowanceRuleTestResultDto,
} from '../../services/allowance-rule.service';
import {
  TravelTypeDefinitionService,
  TravelTypeDefinitionDto,
} from '../../services/travel-type-definition.service';

@Component({
  selector: 'app-allowance-rules',
  standalone: true,
  imports: [CommonModule, FormsModule, NgbModalModule, LocalizationModule, PermissionDirective],
  templateUrl: './allowance-rules.component.html',
  styleUrls: ['./allowance-rules.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AllowanceRulesComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();
  private readonly ruleService = inject(AllowanceRuleService);
  private readonly travelTypeService = inject(TravelTypeDefinitionService);
  private readonly modalService = inject(NgbModal);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);
  private readonly localizationService = inject(LocalizationService);

  @ViewChild('ruleModal') ruleModal: any;

  // Signals
  readonly rules = signal<AllowanceRuleDto[]>([]);
  readonly travelTypes = signal<TravelTypeDefinitionDto[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly searchText = signal<string>('');
  readonly editingRule = signal<AllowanceRuleDto | null>(null);
  readonly ruleTestResult = signal<AllowanceRuleTestResultDto | null>(null);

  // Computed
  readonly filteredRules = computed(() => {
    const search = this.searchText().trim().toLowerCase();
    const list = this.rules();
    if (!search) return list;
    return list.filter(
      (rule) =>
        rule.name?.toLowerCase().includes(search) ||
        this.getTravelTypeLabel(rule.travelTypeDefinitionId)?.toLowerCase().includes(search)
    );
  });

  readonly activeRulesCount = computed(() => this.rules().filter((r) => r.isActive).length);
  readonly totalSegmentsCount = computed(() =>
    this.rules().reduce((sum, r) => sum + (r.segments?.length || 0), 0)
  );

  canSave(): boolean {
    const f = this.ruleForm;
    return (
      !!f.name?.trim() &&
      f.segments?.length > 0 &&
      f.segments.every(
        (s) =>
          s.fromDay >= 1 && s.toDay >= s.fromDay && s.percentage >= 0 && s.percentage <= 100
      )
    );
  }

  // Form
  ruleForm: CreateUpdateAllowanceRuleDto = this.getDefaultForm();

  tester = {
    days: 21,
    dailyRate: 45,
    employeeCount: 1,
    accommodationPaymentPercentage: 100,
  };

  ngOnInit(): void {
    this.loadRules();
    this.loadTravelTypes();
  }

  loadRules(): void {
    this.isLoading.set(true);
    this.ruleService.getList({ allowanceType: 1, maxResultCount: 100 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
      next: (res) => {
        this.rules.set(res.items || []);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.toaster.error(
          this.localizationService.instant('Travel::FailedToLoadRules'),
          this.localizationService.instant('Travel::Error')
        );
      },
    });
  }

  loadTravelTypes(): void {
    this.travelTypeService.getActiveTypes()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items) => {
          this.travelTypes.set(items);
        },
        error: () => {
          this.toaster.error(
            this.localizationService.instant('Travel::FailedToLoadTravelTypes'),
            this.localizationService.instant('Travel::Error')
          );
        },
      });
  }

  onSearch(): void {
    // searchText is two-way bound; computed auto-updates
  }

  clearSearch(): void {
    this.searchText.set('');
  }

  getTravelTypeLabel(travelTypeDefinitionId?: string | null): string {
    if (!travelTypeDefinitionId) return this.localizationService.instant('Travel::All');
    return this.travelTypes().find((x) => x.id === travelTypeDefinitionId)?.name || travelTypeDefinitionId;
  }

  getCategoryLabel(category?: number | null): string {
    if (!category) return this.localizationService.instant('Travel::All');
    return this.localizationService.instant(
      'Travel::AllowanceCategory.' + (category === 1 ? 'A' : 'B')
    );
  }

  getSegmentConditionLabel(seg: {
    appliesWhenTotalDaysFrom?: number;
    appliesWhenTotalDaysTo?: number;
  }): string {
    if (!seg.appliesWhenTotalDaysFrom && !seg.appliesWhenTotalDaysTo) return '';
    const from = seg.appliesWhenTotalDaysFrom || 1;
    const to = seg.appliesWhenTotalDaysTo || '\u221E';
    return `${from} - ${to}`;
  }

  getSegmentColor(index: number): string {
    const colors = [
      '#2563eb', '#059669', '#d97706', '#dc2626',
      '#7c3aed', '#0891b2', '#ec4899', '#f43f5e',
    ];
    return colors[index % colors.length];
  }

  openModal(rule?: AllowanceRuleDto): void {
    this.ruleTestResult.set(null);
    if (rule) {
      this.editingRule.set(rule);
      this.ruleForm = {
        name: rule.name,
        allowanceType: 1,
        travelTypeDefinitionId: rule.travelTypeDefinitionId,
        category: rule.category,
        appliesWhenAccommodationIncluded: null,
        accommodationMultiplier: null,
        priority: rule.priority,
        isActive: rule.isActive,
        segments: rule.segments.map((s) => ({
          fromDay: s.fromDay,
          toDay: s.toDay,
          percentage: s.percentage,
          appliesWhenTotalDaysFrom: s.appliesWhenTotalDaysFrom,
          appliesWhenTotalDaysTo: s.appliesWhenTotalDaysTo,
        })),
      };
    } else {
      this.editingRule.set(null);
      this.ruleForm = this.getDefaultForm();
    }
    this.modalService.open(this.ruleModal, { backdrop: 'static', size: 'xl' });
  }

  copyRule(rule: AllowanceRuleDto): void {
    this.editingRule.set(null);
    this.ruleTestResult.set(null);
    this.ruleForm = {
      name: `${rule.name} - ${this.localizationService.instant('Travel::Copy')}`,
      allowanceType: 1,
      travelTypeDefinitionId: rule.travelTypeDefinitionId,
      category: rule.category,
      appliesWhenAccommodationIncluded: null,
      accommodationMultiplier: null,
      priority: rule.priority,
      isActive: false,
      segments: rule.segments.map((s) => ({
        fromDay: s.fromDay,
        toDay: s.toDay,
        percentage: s.percentage,
        appliesWhenTotalDaysFrom: s.appliesWhenTotalDaysFrom,
        appliesWhenTotalDaysTo: s.appliesWhenTotalDaysTo,
      })),
    };
    this.modalService.open(this.ruleModal, { backdrop: 'static', size: 'xl' });
  }

  addSegment(): void {
    this.ruleForm.segments.push({ fromDay: 1, toDay: 7, percentage: 100 });
    this.ruleTestResult.set(null);
  }

  removeSegment(index: number): void {
    this.ruleForm.segments.splice(index, 1);
    this.ruleTestResult.set(null);
  }

  testRule(): void {
    const editing = this.editingRule();
    if (!editing) return;
    this.ruleService
      .test(editing.id, {
        days: Number(this.tester.days || 1),
        dailyRate: Number(this.tester.dailyRate || 0),
        employeeCount: Number(this.tester.employeeCount || 1),
        accommodationPaymentPercentage: Number(this.tester.accommodationPaymentPercentage || 100),
      })
      .subscribe({
        next: (result) => this.ruleTestResult.set(result),
        error: () => {
          this.toaster.error(
            this.localizationService.instant('Travel::TestRuleFailed'),
            this.localizationService.instant('Travel::Error')
          );
        },
      });
  }

  saveRule(): void {
    const input = this.normalizeRuleForm();
    const obs = this.editingRule()
      ? this.ruleService.update(this.editingRule()!.id, input)
      : this.ruleService.create(input);
    obs.pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toaster.success(
          this.localizationService.instant(
            this.editingRule() ? 'Travel::RuleUpdated' : 'Travel::RuleCreated'
          )
        );
        this.loadRules();
      },
      error: (err) => {
        const message =
          err?.error?.error?.message || this.localizationService.instant('Travel::SaveFailed');
        this.toaster.error(message, this.localizationService.instant('Travel::Error'));
      },
    });
  }

  private normalizeRuleForm(): CreateUpdateAllowanceRuleDto {
    return {
      ...this.ruleForm,
      allowanceType: 1,
      segments: this.ruleForm.segments.map((segment) => ({
        fromDay: Number(segment.fromDay),
        toDay: Number(segment.toDay),
        percentage: Number(segment.percentage),
        appliesWhenTotalDaysFrom: segment.appliesWhenTotalDaysFrom
          ? Number(segment.appliesWhenTotalDaysFrom)
          : undefined,
        appliesWhenTotalDaysTo: segment.appliesWhenTotalDaysTo
          ? Number(segment.appliesWhenTotalDaysTo)
          : undefined,
      })),
    };
  }

  deleteRule(id: string): void {
    this.confirmationService
      .warn(
        this.localizationService.instant('Travel::DeleteRuleConfirmationMessage'),
        this.localizationService.instant('Travel::DeleteRuleConfirmationTitle')
      )
      .subscribe((status) => {
        if (status === 'confirm') {
          this.ruleService.delete(id).subscribe({
            next: () => {
              this.toaster.success(this.localizationService.instant('Travel::RuleDeleted'));
              this.loadRules();
            },
            error: () => {
              this.toaster.error(
                this.localizationService.instant('Travel::DeleteFailed'),
                this.localizationService.instant('Travel::Error')
              );
            },
          });
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private getDefaultForm(): CreateUpdateAllowanceRuleDto {
    return {
      name: '',
      allowanceType: 1,
      travelTypeDefinitionId: null,
      category: null,
      appliesWhenAccommodationIncluded: null,
      accommodationMultiplier: null,
      priority: 0,
      isActive: true,
      segments: [],
    };
  }
}
