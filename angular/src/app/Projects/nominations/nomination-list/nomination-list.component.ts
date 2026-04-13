import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { DxButtonModule, DxDataGridModule, DxPopupModule, DxSelectBoxModule, DxTagBoxModule } from 'devextreme-angular';
import { ToolbarItem } from 'devextreme/ui/popup';
import { NominationService } from 'src/app/proxy/training/nominations';
import { NominationDto, NominationApprovalDto, CreateNominationDto } from 'src/app/proxy/training/nominations/dtos';
import { CourseSessionService } from 'src/app/proxy/training/plans';
import { CourseSessionDto } from 'src/app/proxy/training/plans/dtos';
import { NominationStatus } from '../../shared';
 import { TrainingLocalizationHelper } from '../../shared';


@Component({
  standalone: true,
  selector: 'app-nomination-list',
  templateUrl: './nomination-list.component.html',
  styleUrl: './nomination-list.component.scss',
  imports: [CommonModule, LocalizationPipe, DxDataGridModule, DxPopupModule, DxSelectBoxModule, DxTagBoxModule, DxButtonModule],
})
export class NominationListComponent implements OnInit {
  private nominationService = inject(NominationService);
  private sessionService = inject(CourseSessionService);
  private permissionService = inject(PermissionService);
    private l = inject(TrainingLocalizationHelper);


  nominations = signal<NominationDto[]>([]);
  totalCount = signal(0);
  sessions = signal<CourseSessionDto[]>([]);
  approvalChainMap = signal(new Map<string, NominationApprovalDto[]>());

  isCreateDialogVisible = signal(false);
  isApprovalDialogVisible = signal(false);
  selectedNominationId = signal<string | null>(null);
  approvalNotes = signal('');

  formData = signal<CreateNominationDto>({
    sessionId: '',
    employeeIds: [],
  });

  canCreate = false;
  canApproveUGM = false;
  canApproveTD = false;

  createDialogToolbarItems: ToolbarItem[] | undefined;

  ngOnInit(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('Training.Nomination.Create');
    this.canApproveUGM = this.permissionService.getGrantedPolicy('Training.Nomination.ApproveUGM');
    this.canApproveTD = this.permissionService.getGrantedPolicy('Training.Nomination.ApproveTD');

    this.createDialogToolbarItems = [
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: {
          text: this.l.t('::Training.NominateEmployees'),
          type: 'default',
          onClick: () => this.onSubmitNominations(),
        },
      },
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Cancel'), onClick: () => this.isCreateDialogVisible.set(false) },
      },
    ];

    this.loadNominations();
    this.loadSessions();
  }

  async loadNominations(): Promise<void> {
    const result = await firstValueFrom(
      this.nominationService.getList({ maxResultCount: 100 })
    );
    this.nominations.set(result.items ?? []);
    this.totalCount.set(result.totalCount);
  }

  async loadSessions(): Promise<void> {
    const result = await firstValueFrom(
      this.sessionService.getList({ maxResultCount: 200 })
    );
    this.sessions.set(result.items ?? []);
  }

  onAdd(): void {
    this.formData.set({ sessionId: '', employeeIds: [] });
    this.isCreateDialogVisible.set(true);
  }

  async onSubmitNominations(): Promise<void> {
    const data = this.formData();
    if (!data.sessionId || data.employeeIds.length === 0) return;

    await firstValueFrom(this.nominationService.createBatch(data));
    this.isCreateDialogVisible.set(false);
    await this.loadNominations();
  }

  async onRowExpanding(e: any): Promise<void> {
    const nominationId = e.key as string;
    if (!this.approvalChainMap().has(nominationId)) {
      const chain = await firstValueFrom(
        this.nominationService.getApprovalChain(nominationId)
      );
      this.approvalChainMap.update(m => {
        const newMap = new Map(m);
        newMap.set(nominationId, chain);
        return newMap;
      });
    }
  }

  getApprovalChain(nominationId: string): NominationApprovalDto[] {
    return this.approvalChainMap().get(nominationId) ?? [];
  }

  async onApprove(nominationId: string): Promise<void> {
    await firstValueFrom(
      this.nominationService.approve(nominationId, { notes: this.approvalNotes() })
    );
    this.approvalChainMap.update(m => {
      const newMap = new Map(m);
      newMap.delete(nominationId);
      return newMap;
    });
    await this.loadNominations();
  }

  async onReject(nominationId: string): Promise<void> {
    await firstValueFrom(
      this.nominationService.reject(nominationId, { notes: this.approvalNotes() })
    );
    this.approvalChainMap.update(m => {
      const newMap = new Map(m);
      newMap.delete(nominationId);
      return newMap;
    });
    await this.loadNominations();
  }

  canApproveNomination(nomination: NominationDto): boolean {
    if (nomination.status === NominationStatus.UTMApproved && this.canApproveUGM) return true;
    if (nomination.status === NominationStatus.UGMApproved && this.canApproveTD) return true;
    return false;
  }

  getStatusBadgeClass(status: NominationStatus): string {
    const map: Record<number, string> = {
      [NominationStatus.Nominated]: 'badge-nominated',
      [NominationStatus.UTMApproved]: 'badge-utm',
      [NominationStatus.UGMApproved]: 'badge-ugm',
      [NominationStatus.TDApproved]: 'badge-td',
      [NominationStatus.Rejected]: 'badge-rejected',
    };
    return map[status] ?? 'badge-nominated';
  }

  updateSessionId(value: string): void {
    this.formData.update(f => ({ ...f, sessionId: value }));
  }

  updateEmployeeIds(value: string[]): void {
    this.formData.update(f => ({ ...f, employeeIds: value }));
  }

  updateApprovalNotes(value: string): void {
    this.approvalNotes.set(value);
  }
}
