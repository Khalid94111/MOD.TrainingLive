import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, ChangeDetectionStrategy, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocalizationModule, LocalizationService, PermissionDirective } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';
import {
  FundingSourceVoteRuleService,
  TravelFundingSourceVoteRuleDto,
} from '../../services/funding-source-vote-rule.service';

@Component({
  selector: 'app-funding-source-vote-rules',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationModule, PermissionDirective],
  templateUrl: './funding-source-vote-rules.component.html',
  styleUrls: ['./funding-source-vote-rules.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FundingSourceVoteRulesComponent implements OnInit {
  private readonly service = inject(FundingSourceVoteRuleService);
  private readonly toaster = inject(ToasterService);
  private readonly localizationService = inject(LocalizationService);

  readonly rules = signal<TravelFundingSourceVoteRuleDto[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly isSaving = signal<boolean>(false);
  readonly savingRuleId = signal<string>('');
  readonly canEdit = true;

  readonly activeRulesCount = computed(() =>
    this.rules().filter(r => r.isActive).length
  );

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.service.getList().subscribe({
      next: rules => {
        this.rules.set(rules);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  save(rule: TravelFundingSourceVoteRuleDto): void {
    this.isSaving.set(true);
    this.savingRuleId.set(rule.id);
    this.service
      .update(rule.id, {
        fundingSourceVoteCode: rule.fundingSourceVoteCode,
        isActive: rule.isActive,
      })
      .subscribe({
        next: updatedRule => {
          this.rules.update(current =>
            current.map(r => (r.id === updatedRule.id ? updatedRule : r))
          );
          this.toaster.success(this.localizationService.instant('Travel::SavedSuccessfully'));
          this.isSaving.set(false);
          this.savingRuleId.set('');
        },
        error: () => {
          this.isSaving.set(false);
          this.savingRuleId.set('');
        },
      });
  }

  onToggleChanged(rule: TravelFundingSourceVoteRuleDto): void {
    if (rule.fundingSourceVoteCode?.trim()) {
      this.save(rule);
    }
  }

  getPaymentTypeLabel(paymentType: string): string {
    return `Travel::FundingSourceVotePaymentType.${paymentType}`;
  }

  getPaymentTypeIcon(paymentType: string): string {
    switch (paymentType.toLowerCase()) {
      case 'ticket':
        return 'fa-ticket';
      case 'visa':
        return 'fa-passport';
      case 'healthinsurance':
        return 'fa-heart-pulse';
      case 'dailyallowance':
        return 'fa-coins';
      case 'clothingallowance':
        return 'fa-shirt';
      default:
        return 'fa-file-invoice-dollar';
    }
  }
}
