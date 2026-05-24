import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import {  PermissionService } from '@abp/ng.core';
import { PriceQuoteService, TrainingProviderService } from 'src/app/proxy/training/finance';
import { PriceQuoteDto, TrainingProviderDto, CreateUpdatePriceQuoteDto } from 'src/app/proxy/training/finance/dtos';
import { CourseSessionService } from 'src/app/proxy/training/plans';
import { CourseSessionDto } from 'src/app/proxy/training/plans/dtos';
import { TrainingLocalizationHelper } from '../shared';

@Component({
  standalone: true,
  selector: 'app-price-quote-list',
  templateUrl: './price-quote-list.component.html',
  styleUrls: ['./price-quote-list.component.scss', '../shared/gtms-design.scss'],
  imports: [CommonModule],
})
export class PriceQuoteListComponent implements OnInit {
  private quoteService = inject(PriceQuoteService);
  private providerService = inject(TrainingProviderService);
  private sessionService = inject(CourseSessionService);
  private permissionService = inject(PermissionService);
  l = inject(TrainingLocalizationHelper);

  quotes = signal<PriceQuoteDto[]>([]);
  providers = signal<TrainingProviderDto[]>([]);
  sessions = signal<CourseSessionDto[]>([]);

  isDialogOpen = signal(false);
  isEditMode = signal(false);
  editId = signal<string | null>(null);

  fSessionId = signal('');
  fProviderId = signal('');
  fPricingType = signal(0);
  fQuotedPrice = signal(0);
  fParticipantsCount = signal(1);
  fNotes = signal('');

  canCreate = false;

  get dialogTitle(): string {
    return this.isEditMode() ? '✏️ تعديل عرض سعر' : '➕ إنشاء عرض سعر';
  }

  ngOnInit(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('Training.PriceQuote.Create');
    this.loadQuotes();
    this.loadProviders();
    this.loadSessions();
  }

  async loadQuotes(): Promise<void> {
    const r = await firstValueFrom(this.quoteService.getList({ maxResultCount: 200 }));
    this.quotes.set(r.items ?? []);
  }

  async loadProviders(): Promise<void> {
    this.providers.set(await firstValueFrom(this.providerService.getAllActive()));
  }

  async loadSessions(): Promise<void> {
    const r = await firstValueFrom(this.sessionService.getList({ maxResultCount: 200 }));
    console.log('sessions', r);
    this.sessions.set(r.items ?? []);
  }

  openAddDialog(): void {
    this.isEditMode.set(false);
    this.editId.set(null);
    this.fSessionId.set('');
    this.fProviderId.set('');
    this.fPricingType.set(0);
    this.fQuotedPrice.set(0);
    this.fParticipantsCount.set(1);
    this.fNotes.set('');
    this.isDialogOpen.set(true);
  }

  openEditDialog(q: PriceQuoteDto): void {
    this.isEditMode.set(true);
    this.editId.set(q.id);
    this.fSessionId.set(q.sessionId);
    this.fProviderId.set(q.providerId);
    this.fPricingType.set(q.pricingType);
    this.fQuotedPrice.set(q.quotedPrice);
    this.fParticipantsCount.set(q.participantsCount);
    this.fNotes.set(q.notes ?? '');
    this.isDialogOpen.set(true);
  }

  async onSave(): Promise<void> {
    const data: CreateUpdatePriceQuoteDto = {
      sessionId: this.fSessionId(),
      providerId: this.fProviderId(),
      pricingType: this.fPricingType(),
      quotedPrice: this.fQuotedPrice(),
      participantsCount: this.fParticipantsCount(),
      notes: this.fNotes() || undefined,
    };

    if (this.isEditMode() && this.editId()) {
      await firstValueFrom(this.quoteService.update(this.editId()!, data));
    } else {
      await firstValueFrom(this.quoteService.create(data));
    }
    this.isDialogOpen.set(false);
    await this.loadQuotes();
  }

  async onDelete(id: string): Promise<void> {
    if (!confirm('هل أنت متأكد؟')) return;
    await firstValueFrom(this.quoteService.delete(id));
    await this.loadQuotes();
  }

  async onApprove(id: string): Promise<void> {
    await firstValueFrom(this.quoteService.approve(id));
    await this.loadQuotes();
  }

  async onReject(id: string): Promise<void> {
    await firstValueFrom(this.quoteService.reject(id));
    await this.loadQuotes();
  }

  onSessionSelected(): void {
    // Phase 4C-α (v4.10.0): CourseSession no longer carries MaxSeats — sessions use a
    // fixed nominee snapshot taken at creation (Q-D). Participants count is entered manually.
  }

  getStatusClass(s: number): string {
    return ({ 0: 'badge-pending', 1: 'badge-approved', 2: 'badge-rejected' } as Record<number, string>)[s] ?? '';
  }

  getStatusText(s: number): string {
    return ({ 0: 'قيد الانتظار', 1: 'معتمد', 2: 'مرفوض' } as Record<number, string>)[s] ?? '';
  }

  formatCost(n?: number): string {
    if (!n) return '—';
    return n.toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
  }
}
