import { Component, inject, OnInit, OnDestroy, signal, computed, ViewChild, ChangeDetectionStrategy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModal, NgbModalModule } from '@ng-bootstrap/ng-bootstrap';
import { LocalizationModule, LocalizationService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';
import { ConfirmationService } from '@abp/ng.theme.shared';
import { PermissionDirective } from '@abp/ng.core';
import {
  ClothingAllowanceRuleDto,
  CreateUpdateClothingAllowanceRuleDto,
  ClothingAllowanceRuleService,
} from '../../services/clothing-allowance-rule.service';
import { RankLookupDto, RankLookupService } from '../../services/rank-lookup.service';

@Component({
  selector: 'lib-clothing-allowance-rules',
  standalone: true,
  imports: [CommonModule, FormsModule, NgbModalModule, LocalizationModule, PermissionDirective],
  templateUrl: './clothing-allowance-rules.component.html',
  styleUrls: ['./clothing-allowance-rules.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClothingAllowanceRulesComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();
  private readonly ruleService = inject(ClothingAllowanceRuleService);
  private readonly rankService = inject(RankLookupService);
  private readonly modalService = inject(NgbModal);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);
  private readonly localizationService = inject(LocalizationService);

  @ViewChild('ruleModal') ruleModal: any;

  // Signals
  readonly rules = signal<ClothingAllowanceRuleDto[]>([]);
  readonly ranks = signal<RankLookupDto[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly searchText = signal<string>('');
  readonly editingRule = signal<ClothingAllowanceRuleDto | null>(null);

  // Computed
  readonly filteredRules = computed(() => {
    const search = this.searchText().trim().toLowerCase();
    const list = this.rules();
    if (!search) return list;
    return list.filter(
      (rule) =>
        rule.name?.toLowerCase().includes(search) ||
        rule.rankNames?.some((r: string) => r.toLowerCase().includes(search))
    );
  });

  readonly activeRulesCount = computed(() => this.rules().filter((r) => r.isActive).length);

  readonly averageFullAmount = computed(() => {
    const list = this.rules();
    if (list.length === 0) return 0;
    return list.reduce((sum, r) => sum + (r.fullAmount || 0), 0) / list.length;
  });

  readonly canSave = computed(() => {
    const f = this.form;
    return (
      !!f.name?.trim() &&
      f.rankIds?.length > 0 &&
      f.fullAmount >= 0 &&
      f.annualPartialAmount >= 0 &&
      f.fullPaymentPeriodYears > 0
    );
  });

  // Form
  form: CreateUpdateClothingAllowanceRuleDto = this.getDefaultForm();

  ngOnInit(): void {
    this.loadRules();
    this.loadRanks();
  }

  loadRules(): void {
    this.isLoading.set(true);
    this.ruleService.getList().pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        this.rules.set(result.items || []);
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

  loadRanks(): void {
    this.rankService.getRanks().pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        this.ranks.set(result || []);
      },
      error: () => {
        this.toaster.error(
          this.localizationService.instant('Travel::FailedToLoadRanks'),
          this.localizationService.instant('Travel::Error')
        );
      },
    });
  }

  onSearch(): void {
    // searchText is two-way bound, computed auto-updates
  }

  clearSearch(): void {
    this.searchText.set('');
  }

  openModal(rule?: ClothingAllowanceRuleDto): void {
    if (rule) {
      this.editingRule.set(rule);
      this.form = {
        name: rule.name,
        rankIds: [...(rule.rankIds || [])],
        fullAmount: rule.fullAmount,
        annualPartialAmount: rule.annualPartialAmount,
        fullPaymentPeriodYears: rule.fullPaymentPeriodYears,
        priority: rule.priority,
        isActive: rule.isActive,
      };
    } else {
      this.editingRule.set(null);
      this.form = this.getDefaultForm();
    }
    this.modalService.open(this.ruleModal, { size: 'lg', backdrop: 'static' });
  }

  saveRule(): void {
    if (!this.canSave()) return;

    const editing = this.editingRule();
    const dto = { ...this.form };

    const action$ = editing ? this.ruleService.update(editing.id, dto) : this.ruleService.create(dto);

    action$.pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toaster.success(
          this.localizationService.instant(editing ? 'Travel::RuleUpdated' : 'Travel::RuleCreated')
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

  isRankSelected(rankId: string): boolean {
    return this.form.rankIds?.includes(rankId) ?? false;
  }

  toggleRank(rankId: string, checked: boolean): void {
    const ids = this.form.rankIds || [];
    if (checked) {
      if (!ids.includes(rankId)) {
        this.form = { ...this.form, rankIds: [...ids, rankId] };
      }
    } else {
      this.form = { ...this.form, rankIds: ids.filter((id) => id !== rankId) };
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private getDefaultForm(): CreateUpdateClothingAllowanceRuleDto {
    return {
      name: '',
      rankIds: [],
      fullAmount: 0,
      annualPartialAmount: 0,
      fullPaymentPeriodYears: 1,
      priority: 0,
      isActive: true,
    };
  }
}
