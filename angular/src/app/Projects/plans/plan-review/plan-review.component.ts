import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { TrainingPlanService, TrainingPlanItemService, PlanItemFinancialItemService } from 'src/app/proxy/training/plans';
import { TrainingPlanDto, TrainingPlanItemDto, PlanItemFinancialItemDto } from 'src/app/proxy/training/plans/dtos';
import { PlanStatus, TrainingLocalizationHelper } from '../../shared';



@Component({
  standalone: true,
  selector: 'app-plan-review',
  templateUrl: './plan-review.component.html',
  styleUrls: ['./plan-review.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule],
})
export class PlanReviewComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private planService = inject(TrainingPlanService);
  private itemService = inject(TrainingPlanItemService);
  private fiService = inject(PlanItemFinancialItemService);
  l = inject(TrainingLocalizationHelper);

  planId = '';
  plan = signal<TrainingPlanDto | null>(null);
  items = signal<TrainingPlanItemDto[]>([]);
  selectedItemId = signal<string | null>(null);
  financialItems = signal<PlanItemFinancialItemDto[]>([]);

  // Add financial item dialog
  isAddFiOpen = signal(false);
  fiFiId = signal('');
  fiAmountOMR = signal(0);
  fiAmountUSD = signal<number | undefined>(undefined);
  fiNotes = signal('');

  // Available financial items for dropdown (loaded from FinancialItemService)
  availableFinancialItems = signal<any[]>([]);

  PlanStatus = PlanStatus;

  get selectedItem(): TrainingPlanItemDto | undefined {
    return this.items().find(i => i.id === this.selectedItemId());
  }

  get itemsMissingCost(): number {
    return this.items().filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0)).length;
  }

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('planId') ?? '';
    this.loadPlan();
    this.loadItems();
  }

  async loadPlan(): Promise<void> {
    this.plan.set(await firstValueFrom(this.planService.get(this.planId)));
  }

  async loadItems(): Promise<void> {
    const r = await firstValueFrom(this.itemService.getList({ planId: this.planId, maxResultCount: 500 }));
    this.items.set(r.items ?? []);
  }

  async selectItem(itemId: string): Promise<void> {
    this.selectedItemId.set(itemId);
    await this.loadFinancialItems(itemId);
  }

  async loadFinancialItems(itemId: string): Promise<void> {
    const r = await firstValueFrom(this.fiService.getListByPlanItem(itemId));
    this.financialItems.set(r);
  }

  // --- Financial item actions ---
  openAddFiDialog(): void {
    this.fiFiId.set('');
    this.fiAmountOMR.set(0);
    this.fiAmountUSD.set(undefined);
    this.fiNotes.set('');
    this.isAddFiOpen.set(true);
  }

  async onSaveFi(): Promise<void> {
    const itemId = this.selectedItemId();
    if (!itemId) return;

    await firstValueFrom(this.fiService.create({
      planItemId: itemId,
      financialItemId: this.fiFiId(),
      estimatedAmountOMR: this.fiAmountOMR(),
      estimatedAmountUSD: this.fiAmountUSD(),
      notes: this.fiNotes() || undefined,
    }));

    this.isAddFiOpen.set(false);
    await this.loadFinancialItems(itemId);
    await this.loadItems(); // Refresh cost totals
  }

  async onDeleteFi(id: string): Promise<void> {
    await firstValueFrom(this.fiService.delete(id));
    if (this.selectedItemId()) {
      await this.loadFinancialItems(this.selectedItemId()!);
      await this.loadItems();
    }
  }

  async onAutoFill(): Promise<void> {
    const itemId = this.selectedItemId();
    if (!itemId) return;
    await firstValueFrom(this.fiService.autoFillFromDefaults(itemId));
    await this.loadFinancialItems(itemId);
    await this.loadItems();
  }

  // --- Workflow actions ---
  async onCloseWindow(): Promise<void> {
    await firstValueFrom(this.planService.closeSubmissionWindow(this.planId));
    await this.loadPlan();
  }

  async onSubmitForReview(): Promise<void> {
    await firstValueFrom(this.planService.submitForReview(this.planId));
    await this.loadPlan();
  }

  // --- Helpers ---
  isSelected(itemId: string): boolean {
    return this.selectedItemId() === itemId;
  }

  getCourseTypeBadge(t: number): string {
    return ({ 0: 'badge-internal', 1: 'badge-ext-local', 2: 'badge-ext-intl' } as Record<number, string>)[t] ?? '';
  }

  getCourseTypeText(t: number): string {
    return ({ 0: 'داخلية', 1: 'خارجية محلية', 2: 'خارجية دولية' } as Record<number, string>)[t] ?? '';
  }

  isMissingCost(item: TrainingPlanItemDto): boolean {
    return item.courseType !== 0 && (!item.estimatedCost || item.estimatedCost <= 0);
  }

  formatCost(n?: number): string {
    if (!n || n <= 0) return '';
    return n.toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
  }

  getFinancialTotal(): number {
    return this.financialItems().reduce((s, i) => s + i.estimatedAmountOMR, 0);
  }
}
