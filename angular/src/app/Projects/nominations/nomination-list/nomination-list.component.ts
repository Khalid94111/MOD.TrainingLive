import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import {   PermissionService } from '@abp/ng.core';
import { HrLookupService } from 'src/app/proxy/training/hr-integration';
import { NominationService } from 'src/app/proxy/training/nominations';
import { NominationDto, NominationApprovalDto } from 'src/app/proxy/training/nominations/dtos';
import { CourseSessionService } from 'src/app/proxy/training/plans';
import { CourseSessionDto } from 'src/app/proxy/training/plans/dtos';
import { NominationStatus, TrainingLocalizationHelper } from '../../shared';
 

@Component({
  standalone: true,
  selector: 'app-nomination-list',
  templateUrl: './nomination-list.component.html',
  styleUrls: ['./nomination-list.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule],
})
export class NominationListComponent implements OnInit {
  private nominationService = inject(NominationService);
  private sessionService = inject(CourseSessionService);
  private hrService = inject(HrLookupService);
  private permissionService = inject(PermissionService);
  l = inject(TrainingLocalizationHelper);

  nominations = signal<NominationDto[]>([]);
  sessions = signal<CourseSessionDto[]>([]);
  employees = signal<any[]>([]);
  expandedIds = signal(new Set<string>());
  approvalChains = signal(new Map<string, NominationApprovalDto[]>());

  // Create dialog
  isCreateOpen = signal(false);
  selectedSessionId = signal('');
  selectedEmployeeIds = signal<string[]>([]);

  // Reject dialog
  isRejectOpen = signal(false);
  rejectTargetId = signal('');
  rejectNotes = signal('');

  // Filter
  filterSessionId = signal('');
  filterStatus = signal<string>('');

  canCreate = false;
  canApproveUGM = false;
  canApproveTD = false;

  NominationStatus = NominationStatus;

  ngOnInit(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('Training.Nomination.Create');
    this.canApproveUGM = this.permissionService.getGrantedPolicy('Training.Nomination.ApproveUGM');
    this.canApproveTD = this.permissionService.getGrantedPolicy('Training.Nomination.ApproveTD');
    this.loadNominations();
    this.loadSessions();
  }

  async loadNominations(): Promise<void> {
    const params: any = { maxResultCount: 200 };
    if (this.filterSessionId()) params.sessionId = this.filterSessionId();
    if (this.filterStatus()) params.status = +this.filterStatus();

    const result = await firstValueFrom(this.nominationService.getList(params));
    this.nominations.set(result.items ?? []);
  }

  async loadSessions(): Promise<void> {
    const result = await firstValueFrom(this.sessionService.getList({ maxResultCount: 200 }));
    this.sessions.set(result.items ?? []);
  }

  async toggleExpand(id: string): Promise<void> {
    const expanded = new Set(this.expandedIds());
    if (expanded.has(id)) {
      expanded.delete(id);
    } else {
      expanded.add(id);
      if (!this.approvalChains().has(id)) {
        const chain = await firstValueFrom(this.nominationService.getApprovalChain(id));
        this.approvalChains.update(m => { const n = new Map(m); n.set(id, chain); return n; });
      }
    }
    this.expandedIds.set(expanded);
  }

  isExpanded(id: string): boolean {
    return this.expandedIds().has(id);
  }

  getChain(id: string): NominationApprovalDto[] {
    return this.approvalChains().get(id) ?? [];
  }

  // --- Create ---
  openCreateDialog(): void {
    this.selectedSessionId.set('');
    this.selectedEmployeeIds.set([]);
    this.employees.set([]);
    this.isCreateOpen.set(true);
  }

  async onSessionSelected(): Promise<void> {
    // Load employees from current user's unit
    const me = await firstValueFrom(this.hrService.getCurrentEmployee());
    if (me) {
      const emps = await firstValueFrom(this.hrService.getEmployeesByUnit(me.mainUnitId));
      this.employees.set(emps);
    }
  }

  toggleEmployee(empId: string): void {
    const current = [...this.selectedEmployeeIds()];
    const idx = current.indexOf(empId);
    if (idx >= 0) current.splice(idx, 1);
    else current.push(empId);
    this.selectedEmployeeIds.set(current);
  }

  isEmployeeSelected(empId: string): boolean {
    return this.selectedEmployeeIds().includes(empId);
  }

  async onSubmitNominations(): Promise<void> {
    if (!this.selectedSessionId() || this.selectedEmployeeIds().length === 0) return;
    try {
      await firstValueFrom(this.nominationService.createBatch({
        sessionId: this.selectedSessionId(),
        employeeIds: this.selectedEmployeeIds(),
      }));
      this.isCreateOpen.set(false);
      await this.loadNominations();
    } catch (err: any) {
      alert(err?.error?.message || 'خطأ في الترشيح');
    }
  }

  // --- Approve / Reject ---
  canActOn(nom: NominationDto): boolean {
    if (nom.status === NominationStatus.UTMApproved && this.canApproveUGM) return true;
    if (nom.status === NominationStatus.UGMApproved && this.canApproveTD) return true;
    return false;
  }

  async onApprove(id: string): Promise<void> {
    await firstValueFrom(this.nominationService.approve(id, {}));
    this.approvalChains.update(m => { const n = new Map(m); n.delete(id); return n; });
    await this.loadNominations();
  }

  openRejectDialog(id: string): void {
    this.rejectTargetId.set(id);
    this.rejectNotes.set('');
    this.isRejectOpen.set(true);
  }

  async onConfirmReject(): Promise<void> {
    await firstValueFrom(this.nominationService.reject(this.rejectTargetId(), { notes: this.rejectNotes() }));
    this.isRejectOpen.set(false);
    this.approvalChains.update(m => { const n = new Map(m); n.delete(this.rejectTargetId()); return n; });
    await this.loadNominations();
  }

  // --- Helpers ---
  getStatusClass(status: NominationStatus): string {
    return ({
      [NominationStatus.Nominated]: 'badge-pending',
      [NominationStatus.UTMApproved]: 'badge-open',
      [NominationStatus.UGMApproved]: 'badge-review',
      [NominationStatus.TDApproved]: 'badge-approved',
      [NominationStatus.Rejected]: 'badge-rejected',
    } as Record<number, string>)[status] ?? 'badge-draft';
  }

  getStatusText(status: NominationStatus): string {
    return ({
      [NominationStatus.Nominated]: 'مرشح',
      [NominationStatus.UTMApproved]: 'معتمد UTM',
      [NominationStatus.UGMApproved]: 'معتمد UGM',
      [NominationStatus.TDApproved]: 'معتمد TD',
      [NominationStatus.Rejected]: 'مرفوض',
    } as Record<number, string>)[status] ?? '';
  }

  getApprovalStepClass(status: number): string {
    return ({ 0: 'step-pending', 1: 'step-done', 2: 'step-rejected' } as Record<number, string>)[status] ?? 'step-pending';
  }

  getApprovalStepIcon(status: number): string {
    return ({ 0: '⏳', 1: '✓', 2: '✕' } as Record<number, string>)[status] ?? '?';
  }

  getSelectedSession(): CourseSessionDto | undefined {
    return this.sessions().find(s => s.id === this.selectedSessionId());
  }

  formatDate(date?: string): string {
    if (!date) return '—';
    return new Date(date).toLocaleDateString('ar-OM');
  }
}
