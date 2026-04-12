import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe } from '@abp/ng.core';
import {
  DxDataGridModule,
  DxPopupModule,
  DxTextBoxModule,
  DxSwitchModule,
  DxButtonModule,
  DxDropDownBoxModule,
  DxTreeViewModule,
  DxRadioGroupModule,
  DxDataGridComponent,
} from 'devextreme-angular';
import { ToolbarItem } from 'devextreme/ui/popup';
 
import { createAbpStore } from '../../shared/helpers/create-abp-store';
import { TrainingCenterService, HrLookupService } from 'src/app/proxy/training/centers';
import { CenterRoleAssignmentInputDto } from 'src/app/proxy/training/centers/dtos';
import { CenterRoleType, CenterAssignmentType } from 'src/app/proxy/training/enums';
import { TrainingLocalizationHelper } from '../../shared';
import { OrganizationUnitService } from '@volo/abp.ng.identity/proxy';

@Component({
  selector: 'app-training-centers',
  standalone: true,
  imports: [
    CommonModule,
    LocalizationPipe,
    DxDataGridModule,
    DxPopupModule,
    DxTextBoxModule,
    DxSwitchModule,
    DxButtonModule,
    DxDropDownBoxModule,
    DxTreeViewModule,
    DxRadioGroupModule,
  ],
  templateUrl: './training-centers.component.html',
  styleUrl: './training-centers.component.scss',
})
export class TrainingCentersComponent implements OnInit {
  private readonly centerService = inject(TrainingCenterService);
    private readonly orgUnitService = inject(OrganizationUnitService);

  private readonly hrLookupService = inject(HrLookupService);
   readonly l = inject(TrainingLocalizationHelper);
 
  readonly centersGrid = viewChild<DxDataGridComponent>('centersGrid');

  dataSource!: ReturnType<typeof createAbpStore>;
  isDialogVisible = signal(false);
  isEditMode = signal(false);
  editingId = signal<string | null>(null);

  formData = signal({
    orgUnitId: '' as string,
    centerNameAr: '',
    centerNameEn: '',
    location: '',
    isActive: true,
  });

  orgUnits = signal<any[]>([]);
  isOrgUnitDropDownOpen = signal(false);

  tcmAssignment = signal<CenterRoleAssignmentInputDto>({
    roleType: CenterRoleType.TCM,
    assignmentType: CenterAssignmentType.Employee,
  });
  tcoAssignments = signal<CenterRoleAssignmentInputDto[]>([]);
  tcmSearchResult = signal<any>(null);
tcoSearchResults = signal<Map<number, any>>(new Map());
  assignmentTypeItems: any[] = [];
  dialogToolbarItems: ToolbarItem[] | undefined;

  ngOnInit(): void {
    this.assignmentTypeItems = [
      { value: CenterAssignmentType.Employee, text: this.l.t('::Training.ByEmployee') },
      { value: CenterAssignmentType.Position, text: this.l.t('::Training.ByPosition') },
    ];

    this.dialogToolbarItems = [
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: {
          text: this.l.t('::Save'),
          type: 'default',
          stylingMode: 'contained',
          onClick: () => this.onSave(),
        },
      },
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: {
          text: this.l.t('::Cancel'),
          onClick: () => this.isDialogVisible.set(false),
        },
      },
    ];

    this.dataSource = createAbpStore({
      loadFn: (params) => firstValueFrom(this.centerService.getList(params)),
    });

    this.loadOrgUnits();
  }

  get dialogTitle(): string {
    return this.isEditMode()
      ? this.l.t('::Training.EditCenter')
      : this.l.t('::Training.AddCenter');
  }

  // --- Form field update methods (called from template) ---

  updateCenterNameAr(value: string): void {
    this.formData.update(f => ({ ...f, centerNameAr: value }));
  }

  updateCenterNameEn(value: string): void {
    this.formData.update(f => ({ ...f, centerNameEn: value }));
  }

  updateLocation(value: string): void {
    this.formData.update(f => ({ ...f, location: value }));
  }

  updateIsActive(value: boolean): void {
    this.formData.update(f => ({ ...f, isActive: value }));
  }

  updateTcmServiceNumber(value: string): void {
    this.tcmAssignment.update(a => ({ ...a, serviceNumber: value }));
  }

  // --- CRUD ---

  onAdd(): void {
    this.isEditMode.set(false);
    this.editingId.set(null);
    this.formData.set({ orgUnitId: '', centerNameAr: '', centerNameEn: '', location: '', isActive: true });
    this.tcmAssignment.set({ roleType: CenterRoleType.TCM, assignmentType: CenterAssignmentType.Employee });
    this.tcoAssignments.set([]);
    this.tcmSearchResult.set(null);
    this.isDialogVisible.set(true);
  }

  async onEdit(data: any): Promise<void> {
    this.isEditMode.set(true);
    this.editingId.set(data.id);

    const center = await firstValueFrom(this.centerService.get(data.id));
    this.formData.set({
      orgUnitId: center.orgUnitId,
      centerNameAr: center.centerNameAr,
      centerNameEn: center.centerNameEn,
      location: center.location || '',
      isActive: center.isActive,
    });

    const assignments = await firstValueFrom(this.centerService.getRoleAssignments(data.id));
    const tcm = assignments.find(a => a.roleType === CenterRoleType.TCM);
    const tcos = assignments.filter(a => a.roleType === CenterRoleType.TCO);

    if (tcm) {
      this.tcmAssignment.set({
        roleType: CenterRoleType.TCM,
        assignmentType: tcm.assignmentType,
        employeeId: tcm.employeeId,
        serviceNumber: tcm.serviceNumber,
        positionId: tcm.positionId,
      });
      if (tcm.employeeId) {
        this.tcmSearchResult.set({ fullNameAr: tcm.employeeNameAr, serviceNumber: tcm.serviceNumber, rankName: tcm.rankName });
      }
    }

    this.tcoAssignments.set(tcos.map(t => ({
      roleType: CenterRoleType.TCO,
      assignmentType: t.assignmentType,
      employeeId: t.employeeId,
      serviceNumber: t.serviceNumber,
      positionId: t.positionId,
    })));

    this.isDialogVisible.set(true);
  }

  async onDelete(data: any): Promise<void> {
    await firstValueFrom(this.centerService.delete(data.id));
    this.centersGrid()?.instance.refresh();
  }

  async onSave(): Promise<void> {
    const form = this.formData();
    try {
      if (this.isEditMode() && this.editingId()) {
        await firstValueFrom(this.centerService.update(this.editingId()!, form));
      } else {
        const created = await firstValueFrom(this.centerService.create(form));
        this.editingId.set(created.id);
      }

      const centerId = this.editingId()!;
      const assignments: CenterRoleAssignmentInputDto[] = [];
      const tcm = this.tcmAssignment();
      if (tcm.employeeId || tcm.positionId) {
        assignments.push(tcm);
      }
      assignments.push(...this.tcoAssignments());
      await firstValueFrom(this.centerService.setRoleAssignments(centerId, { assignments }));

      this.isDialogVisible.set(false);
      this.centersGrid()?.instance.refresh();
    } catch (e) { /* ABP interceptor */ }
  }

  // --- OrgUnit ---

  onOrgUnitSelected(e: any): void {
    const node = e.itemData;
    if (!node) return;
    this.formData.update(f => ({ ...f, orgUnitId: node.id, centerNameAr: node.displayName, centerNameEn: node.displayName }));
    this.isOrgUnitDropDownOpen.set(false);
  }

  // --- HR Search ---

  async searchTcmEmployee(serviceNumber: string): Promise<void> {
    const orgUnitId = this.formData().orgUnitId;
    if (!orgUnitId || !serviceNumber) return;
    const results = await firstValueFrom(this.hrLookupService.getEmployees(serviceNumber, orgUnitId));
    if (results.length > 0) {
      const emp = results[0];
      this.tcmSearchResult.set(emp);
      this.tcmAssignment.update(a => ({ ...a, employeeId: emp.employeeId, serviceNumber: emp.serviceNumber }));
    }
  }
async searchTcoEmployee(index: number, serviceNumber: string): Promise<void> {
  const orgUnitId = this.formData().orgUnitId;
  if (!orgUnitId || !serviceNumber) return;
  const results = await firstValueFrom(this.hrLookupService.getEmployees(serviceNumber, orgUnitId));
  if (results.length > 0) {
    const emp = results[0];
    this.tcoAssignments.update(list => {
      const u = [...list];
      u[index] = { ...u[index], employeeId: emp.employeeId, serviceNumber: emp.serviceNumber };
      return u;
    });
    this.tcoSearchResults.update(m => {
      const updated = new Map(m);
      updated.set(index, emp);
      return updated;
    });
  }
}

async searchTcoPosition(index: number, name: string): Promise<void> {
  const orgUnitId = this.formData().orgUnitId;
  if (!orgUnitId || !name) return;
  const results = await firstValueFrom(this.hrLookupService.getPositions(name, orgUnitId));
  if (results.length > 0) {
    const pos = results[0];
    this.tcoAssignments.update(list => {
      const u = [...list];
      u[index] = { ...u[index], positionId: pos.positionId };
      return u;
    });
    this.tcoSearchResults.update(m => {
      const updated = new Map(m);
      updated.set(index, pos);
      return updated;
    });
  }
}

  // --- Role Assignment Management ---

  addTcoAssignment(): void {
    this.tcoAssignments.update(list => [...list, { roleType: CenterRoleType.TCO, assignmentType: CenterAssignmentType.Employee }]);
  }

removeTcoAssignment(index: number): void {
  this.tcoAssignments.update(list => list.filter((_, i) => i !== index));
  this.tcoSearchResults.update(m => {
    const updated = new Map(m);
    updated.delete(index);
    return updated;
  });
}

  onTcmAssignmentTypeChanged(value: CenterAssignmentType): void {
    this.tcmAssignment.update(a => ({ ...a, assignmentType: value, employeeId: undefined, serviceNumber: undefined, positionId: undefined }));
    this.tcmSearchResult.set(null);
  }

  onTcoAssignmentTypeChanged(index: number, value: CenterAssignmentType): void {
  this.tcoAssignments.update(list => {
    const u = [...list];
    u[index] = { ...u[index], assignmentType: value, employeeId: undefined, serviceNumber: undefined, positionId: undefined };
    return u;
  });
  this.tcoSearchResults.update(m => {
    const updated = new Map(m);
    updated.delete(index);
    return updated;
  });
}

private async loadOrgUnits(): Promise<void> {
  const result = await firstValueFrom(this.orgUnitService.getList({ maxResultCount: 1000 }));
  this.orgUnits.set(result.items ?? []);
}
}
