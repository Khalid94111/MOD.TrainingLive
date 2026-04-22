import { Component, OnInit, computed, effect, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { PlanNoteService } from 'src/app/proxy/training/plans';
import { PlanNoteDto } from 'src/app/proxy/training/plans/dtos';
import { PlanNoteAuthorRole } from 'src/app/proxy/training/enums/plan-note-author-role.enum';
import { PlanNoteEntityType } from 'src/app/proxy/training/enums/plan-note-entity-type.enum';

@Component({
  standalone: true,
  selector: 'gtms-notes-drawer',
  templateUrl: './notes-drawer.component.html',
  styleUrls: ['./notes-drawer.component.scss', '../../gtms-design.scss'],
  imports: [CommonModule],
})
export class NotesDrawerComponent implements OnInit {
  private noteService = inject(PlanNoteService);

  isOpen = input.required<boolean>();
  entityType = input.required<PlanNoteEntityType>();
  entityId = input.required<string>();
  title = input<string>('');
  readOnly = input<boolean>(false);

  closeDrawer = output<void>();
  noteAdded = output<PlanNoteDto>();

  PlanNoteEntityType = PlanNoteEntityType;

  notes = signal<PlanNoteDto[]>([]);
  loading = signal(false);
  newNote = signal('');
  sending = signal(false);
  activeFilter = signal<'all' | 'return' | 'reply'>('all');

  filteredNotes = computed(() => {
    const all = this.notes();
    const f = this.activeFilter();
    if (f === 'return') return all.filter(n => n.isReturnReason);
    if (f === 'reply') return all.filter(n => !n.isReturnReason);
    return all;
  });

  returnReasonCount = computed(() => this.notes().filter(n => n.isReturnReason).length);

  constructor() {
    effect(() => {
      if (this.isOpen() && this.entityId()) {
        this.loadNotes();
      }
    });
  }

  ngOnInit(): void {}

  async loadNotes(): Promise<void> {
    this.loading.set(true);
    try {
      const list = await firstValueFrom(
        this.noteService.getList({
          entityType: this.entityType(),
          entityId: this.entityId(),
          maxResultCount: 200,
          sorting: 'creationTime asc',
        }),
      );
      this.notes.set(list ?? []);
    } finally {
      this.loading.set(false);
    }
  }

  async onSend(): Promise<void> {
    const text = this.newNote().trim();
    if (!text || this.sending() || this.readOnly()) return;
    this.sending.set(true);
    try {
      const created = await firstValueFrom(
        this.noteService.create({
          entityType: this.entityType(),
          entityId: this.entityId(),
          note: text,
          isReturnReason: false,
        }),
      );
      this.notes.update(list => [...list, created]);
      this.newNote.set('');
      this.noteAdded.emit(created);
    } finally {
      this.sending.set(false);
    }
  }

  onClose(): void {
    this.closeDrawer.emit();
  }

  onBackdropClick(event: Event): void {
    if (event.target === event.currentTarget) this.onClose();
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') this.onClose();
  }

  getRoleLabel(role?: PlanNoteAuthorRole): string {
    return ({
      [PlanNoteAuthorRole.UTM]: 'مسؤول تدريب الوحدة',
      [PlanNoteAuthorRole.Staff]: 'الموظف الفني',
      [PlanNoteAuthorRole.TD]: 'مدير التدريب',
      [PlanNoteAuthorRole.TH]: 'رئيس التدريب',
      [PlanNoteAuthorRole.UGM]: 'مدير الوحدة',
    } as Record<number, string>)[role as number] ?? '—';
  }

  getRoleBadgeClass(role?: PlanNoteAuthorRole): string {
    return ({
      [PlanNoteAuthorRole.UTM]: 'role-utm',
      [PlanNoteAuthorRole.Staff]: 'role-staff',
      [PlanNoteAuthorRole.TD]: 'role-td',
      [PlanNoteAuthorRole.TH]: 'role-th',
      [PlanNoteAuthorRole.UGM]: 'role-ugm',
    } as Record<number, string>)[role as number] ?? '';
  }

  formatTime(d?: string): string {
    if (!d) return '';
    const date = new Date(d);
    return date.toLocaleString('ar-OM', { dateStyle: 'short', timeStyle: 'short' });
  }

  trackById(_: number, n: PlanNoteDto): string { return n.id ?? ''; }

  setFilter(f: 'all' | 'return' | 'reply'): void { this.activeFilter.set(f); }

  onNoteInput(event: Event): void { this.newNote.set((event.target as HTMLTextAreaElement).value); }

  onTextareaKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      this.onSend();
    }
  }
}
