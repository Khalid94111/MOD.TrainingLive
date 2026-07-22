import { Component, inject, OnInit, TemplateRef, ViewChild, ChangeDetectionStrategy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModal, NgbModalModule } from '@ng-bootstrap/ng-bootstrap';
import { LocalizationModule, LocalizationService, PermissionDirective } from '@abp/ng.core';
import { ConfirmationService, ToasterService } from '@abp/ng.theme.shared';
import { AccommodationRuleDto, AccommodationRuleService, CreateUpdateAccommodationRuleDto } from '../../services/accommodation-rule.service';
import { AllowanceRuleDto, AllowanceRuleService } from '../../services/allowance-rule.service';

@Component({
  selector: 'app-accommodation-rules',
  standalone: true,
  imports: [CommonModule, FormsModule, NgbModalModule, LocalizationModule, PermissionDirective],
  templateUrl: './accommodation-rules.component.html',
  styleUrls: ['./accommodation-rules.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccommodationRulesComponent implements OnInit {
  private readonly service = inject(AccommodationRuleService);
  private readonly allowanceRuleService = inject(AllowanceRuleService);
  private readonly modalService = inject(NgbModal);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);
  private readonly localizationService = inject(LocalizationService);

  readonly rules = signal<AccommodationRuleDto[]>([]);
  readonly allowanceRules = signal<AllowanceRuleDto[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly editingRule = signal<AccommodationRuleDto | null>(null);

  form: CreateUpdateAccommodationRuleDto = {
    name: '',
    paymentPercentage: 100,
    priority: 0,
    isActive: true,
    allowanceRuleIds: [],
  };

  @ViewChild('ruleModal', { static: false }) ruleModalRef!: TemplateRef<any>;

  readonly activeRulesCount = computed(() =>
    this.rules().filter(r => r.isActive).length
  );

  readonly averagePercentage = computed(() => {
    const items = this.rules();
    if (items.length === 0) return 0;
    return items.reduce((sum, r) => sum + (r.paymentPercentage || 0), 0) / items.length;
  });

  ngOnInit(): void {
    this.loadRules();
    this.loadAllowanceRules();
  }

  loadRules(): void {
    this.isLoading.set(true);
    this.service.getList({ maxResultCount: 100 }).subscribe({
      next: (res) => {
        this.rules.set(res.items);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  loadAllowanceRules(): void {
    this.allowanceRuleService.getList({ maxResultCount: 100 }).subscribe({
      next: (res) => {
        this.allowanceRules.set(res.items);
      },
    });
  }

  openModal(rule?: AccommodationRuleDto): void {
    this.editingRule.set(rule || null);
    this.form = rule
      ? {
          name: rule.name,
          paymentPercentage: rule.paymentPercentage,
          priority: rule.priority,
          isActive: rule.isActive,
          allowanceRuleIds: [...(rule.allowanceRuleIds || [])],
        }
      : {
          name: '',
          paymentPercentage: 100,
          priority: 0,
          isActive: true,
          allowanceRuleIds: [],
        };

    this.modalService.open(this.ruleModalRef, {
      backdrop: 'static',
      centered: true,
      size: 'lg',
      windowClass: 'accommodation-modal-window',
    });
  }

  isAllowanceRuleSelected(id: string): boolean {
    return this.form.allowanceRuleIds.includes(id);
  }

  toggleAllowanceRule(id: string, checked: boolean): void {
    this.form.allowanceRuleIds = checked
      ? [...new Set([...this.form.allowanceRuleIds, id])]
      : this.form.allowanceRuleIds.filter(x => x !== id);
  }

  save(): void {
    const action = this.editingRule()
      ? this.service.update(this.editingRule()!.id, this.form)
      : this.service.create(this.form);

    action.subscribe({
      next: () => {
        this.toaster.success(this.localizationService.instant('Travel::SavedSuccessfully'));
        this.loadRules();
      },
      error: () => {
        this.toaster.error(this.localizationService.instant('Travel::SaveFailed'));
      },
    });
  }

  deleteRule(id: string): void {
    this.confirmationService
      .warn(
        this.localizationService.instant('Travel::DeleteRuleConfirmation'),
        this.localizationService.instant('Travel::AreYouSure')
      )
      .subscribe((status) => {
        if (status !== 'confirm') return;

        this.service.delete(id).subscribe({
          next: () => {
            this.toaster.success(this.localizationService.instant('Travel::DeletedSuccessfully'));
            this.loadRules();
          },
          error: () => {
            this.toaster.error(this.localizationService.instant('Travel::DeleteFailed'));
          },
        });
      });
  }
}
