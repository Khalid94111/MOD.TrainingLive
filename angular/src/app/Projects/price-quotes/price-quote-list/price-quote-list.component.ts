import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { DxButtonModule, DxDataGridModule, DxPopupModule, DxSelectBoxModule, DxNumberBoxModule, DxRadioGroupModule, DxTextAreaModule } from 'devextreme-angular';
import { ToolbarItem } from 'devextreme/ui/popup';
import { PriceQuoteService, TrainingProviderService } from 'src/app/proxy/training/finance';
import { PriceQuoteDto, TrainingProviderDto, CreateUpdatePriceQuoteDto } from 'src/app/proxy/training/finance/dtos';
import { CourseSessionService } from 'src/app/proxy/training/plans';
import { CourseSessionDto } from 'src/app/proxy/training/plans/dtos';
import { TrainingLocalizationHelper } from '../../shared';
 
 

@Component({
  standalone: true,
  selector: 'app-price-quote-list',
  templateUrl: './price-quote-list.component.html',
  styleUrl: './price-quote-list.component.scss',
  imports: [
    CommonModule, LocalizationPipe,
    DxDataGridModule, DxPopupModule, DxSelectBoxModule,
    DxNumberBoxModule, DxRadioGroupModule, DxTextAreaModule,
    DxButtonModule,
  ],
})
export class PriceQuoteListComponent implements OnInit {
  private quoteService = inject(PriceQuoteService);
  private providerService = inject(TrainingProviderService);
  private sessionService = inject(CourseSessionService);
  private permissionService = inject(PermissionService);
    private l = inject(TrainingLocalizationHelper);


  quotes = signal<PriceQuoteDto[]>([]);
  providers = signal<TrainingProviderDto[]>([]);
  sessions = signal<CourseSessionDto[]>([]);

  isDialogVisible = signal(false);
  isEditMode = signal(false);
  selectedQuoteId = signal<string | null>(null);

  formData = signal<CreateUpdatePriceQuoteDto>({
    sessionId: '',
    providerId: '',
    pricingType: 0,
    quotedPrice: 0,
    participantsCount: 1,
  });

  pricingTypes = [
    { value: 0, text: '' },
    { value: 1, text: '' },
  ];

  canCreate = false;
  dialogToolbarItems: ToolbarItem[] | undefined;

  get dialogTitle(): string {
    return this.isEditMode() ? this.l.t('::Training.PriceQuote') : this.l.t('::Training.CreatePriceQuote');
  }

  ngOnInit(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('Training.PriceQuote.Create');

    this.pricingTypes = [
      { value: 0, text: this.l.t('::Training.PricingType.PerPerson') },
      { value: 1, text: this.l.t('::Training.PricingType.Total') },
    ];

    this.dialogToolbarItems = [
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Save'), type: 'default', onClick: () => this.onSave() },
      },
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Cancel'), onClick: () => this.isDialogVisible.set(false) },
      },
    ];

    this.loadQuotes();
    this.loadProviders();
    this.loadSessions();
  }

  async loadQuotes(): Promise<void> {
    const result = await firstValueFrom(this.quoteService.getList({ maxResultCount: 100 }));
    this.quotes.set(result.items ?? []);
  }

  async loadProviders(): Promise<void> {
    const result = await firstValueFrom(this.providerService.getAllActive());
    this.providers.set(result);
  }

  async loadSessions(): Promise<void> {
    const result = await firstValueFrom(this.sessionService.getList({ maxResultCount: 200 }));
    this.sessions.set(result.items ?? []);
  }

  onAdd(): void {
    this.isEditMode.set(false);
    this.selectedQuoteId.set(null);
    this.formData.set({ sessionId: '', providerId: '', pricingType: 0, quotedPrice: 0, participantsCount: 1 });
    this.isDialogVisible.set(true);
  }

  onEdit(quote: PriceQuoteDto): void {
    this.isEditMode.set(true);
    this.selectedQuoteId.set(quote.id);
    this.formData.set({
      sessionId: quote.sessionId,
      providerId: quote.providerId,
      pricingType: quote.pricingType,
      quotedPrice: quote.quotedPrice,
      participantsCount: quote.participantsCount,
      notes: quote.notes,
    });
    this.isDialogVisible.set(true);
  }

  async onSave(): Promise<void> {
    const data = this.formData();
    if (this.isEditMode() && this.selectedQuoteId()) {
      await firstValueFrom(this.quoteService.update(this.selectedQuoteId()!, data));
    } else {
      await firstValueFrom(this.quoteService.create(data));
    }
    this.isDialogVisible.set(false);
    await this.loadQuotes();
  }

  async onDelete(id: string): Promise<void> {
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

  updateSessionId(value: string): void {
    // Phase 4C-α (v4.10.0): CourseSession no longer carries MaxSeats — sessions use a
    // fixed nominee snapshot taken at creation (Q-D). Participants count is now entered
    // manually by Staff.
    this.formData.update(f => ({ ...f, sessionId: value }));
  }

  updateProviderId(value: string): void {
    this.formData.update(f => ({ ...f, providerId: value }));
  }

  updatePricingType(value: number): void {
    this.formData.update(f => ({ ...f, pricingType: value }));
  }

  updateQuotedPrice(value: number): void {
    this.formData.update(f => ({ ...f, quotedPrice: value }));
  }

  updateParticipantsCount(value: number): void {
    this.formData.update(f => ({ ...f, participantsCount: value }));
  }

  updateNotes(value: string): void {
    this.formData.update(f => ({ ...f, notes: value }));
  }
}
