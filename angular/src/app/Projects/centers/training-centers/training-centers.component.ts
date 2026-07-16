import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';
import { DxDropDownBoxModule, DxTreeViewModule } from 'devextreme-angular';

import { TrainingCenterService } from 'src/app/proxy/training/centers';
import {
  CenterRoleAssignmentInputDto,
  CenterRoleAssignmentDto,
  CreateUpdateTrainingCenterDto,
  TrainingCenterDto,
} from 'src/app/proxy/training/centers/dtos';
import { CenterAssignmentType, CenterRoleType } from 'src/app/proxy/training/enums';
import { OrganizationUnitService } from '@volo/abp.ng.identity/proxy';
import { HrLookupService } from 'src/app/proxy/training/hr-integration';
import { EmployeeLookupDto } from 'src/app/proxy/training/hr-integration/models';
import { TrainingLocalizationHelper, ConfirmDialogComponent } from '../../shared';

interface OrgUnitNode {
  id: string;
  parentId?: string | null;
  displayName: string;
  code?: string;
}

@Component({
  selector: 'app-training-centers',
  standalone: true,
  imports: [
    CommonModule,
    LocalizationPipe,
    DxDropDownBoxModule,
    DxTreeViewModule,
    ConfirmDialogComponent,
  ],
  templateUrl: './training-centers.component.html',
  styleUrl: './training-centers.component.scss',
})
export class TrainingCentersComponent implements OnInit {
  private readonly centerService = inject(TrainingCenterService);
  private readonly orgUnitService = inject(OrganizationUnitService);
  private readonly hrLookupService = inject(HrLookupService);
  private readonly permissionService = inject(PermissionService);
  private readonly toaster = inject(ToasterService);
  private readonly l = inject(TrainingLocalizationHelper);

  // ── Data ──
  centers = signal<TrainingCenterDto[]>([]);
  orgUnits = signal<OrgUnitNode[]>([]);
  isLoading = signal(false);

  // ── Filters ──
  searchText = signal('');
  filterStatus = signal<boolean | null>(null);

  // ── Dialog ──
  isDialogVisible = signal(false);
  isEditMode = signal(false);
  editingId = signal<string | null>(null);
  isOrgUnitDropDownOpen = signal(false);
  isLoadingRoles = signal(false);
  isSaving = signal(false);

  formData = signal<CreateUpdateTrainingCenterDto>({
    orgUnitId: '',
    centerNameAr: '',
    centerNameEn: '',
    location: '',
    isActive: true,
  });

  tcmAssignment = signal<CenterRoleAssignmentInputDto>(this.createEmptyTcmAssignment());
  tcoAssignments = signal<CenterRoleAssignmentInputDto[]>([]);
  tcmSearchResult = signal<EmployeeLookupDto | null>(null);
  tcoSearchResults = signal<Map<number, EmployeeLookupDto>>(new Map());

  // ── Delete dialog ──
  isDeleteDialogVisible = signal(false);
  centerToDelete = signal<TrainingCenterDto | null>(null);

  // ── Permissions ──
  canCreate = computed(() => this.permissionService.getGrantedPolicy('Training.Centers.Create'));
  canUpdate = computed(() => this.permissionService.getGrantedPolicy('Training.Centers.Edit'));
  canDelete = computed(() => this.permissionService.getGrantedPolicy('Training.Centers.Delete'));
  canManageRoles = computed(() => this.permissionService.getGrantedPolicy('Training.Centers.ManageRoles'));
  canEditBasicInfo = computed(() => (this.isEditMode() ? this.canUpdate() : this.canCreate()));
  canOpenDialog = computed(() => this.canEditBasicInfo() || this.canManageRoles());

  // ── Computed stats / filtered list ──
  filteredCenters = computed(() => {
    let list = this.centers();
    const term = this.searchText().trim().toLowerCase();
    if (term) {
      list = list.filter(
        c =>
          (c.centerNameAr?.toLowerCase().includes(term) ?? false) ||
          (c.centerNameEn?.toLowerCase().includes(term) ?? false) ||
          (c.orgUnitName?.toLowerCase().includes(term) ?? false) ||
          (c.location?.toLowerCase().includes(term) ?? false)
      );
    }
    if (this.filterStatus() !== null) {
      list = list.filter(c => c.isActive === this.filterStatus());
    }
    return list;
  });

  totalCount = computed(() => this.filteredCenters().length);
  activeCount = computed(() => this.filteredCenters().filter(c => c.isActive).length);
  inactiveCount = computed(() => this.filteredCenters().filter(c => !c.isActive).length);

  dialogTitle = computed(() =>
    this.isEditMode() ? this.l.t('::Training.EditCenter') : this.l.t('::Training.AddCenter')
  );

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadOrgUnits(), this.loadCenters()]);
  }

  async loadCenters(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(
        this.centerService.getList({ maxResultCount: 1000, skipCount: 0 })
      );
      this.centers.set(result.items ?? []);
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async loadOrgUnits(): Promise<void> {
    try {
      const result = await firstValueFrom(this.orgUnitService.getList({ maxResultCount: 1000 }));
      this.orgUnits.set((result.items ?? []) as OrgUnitNode[]);
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    }
  }

  // ── CRUD ──

  onAdd(): void {
    this.isEditMode.set(false);
    this.editingId.set(null);
    this.resetForm();
    this.isDialogVisible.set(true);
  }

  async onEdit(center: TrainingCenterDto): Promise<void> {
    this.isEditMode.set(true);
    this.editingId.set(center.id);
    this.resetForm();
    this.isDialogVisible.set(true);
    this.isLoadingRoles.set(true);

    try {
      const [fullCenter, assignments] = await Promise.all([
        firstValueFrom(this.centerService.get(center.id)),
        firstValueFrom(this.centerService.getRoleAssignments(center.id)),
      ]);

      this.formData.set({
        orgUnitId: fullCenter.orgUnitId ?? '',
        centerNameAr: fullCenter.centerNameAr ?? '',
        centerNameEn: fullCenter.centerNameEn ?? '',
        location: fullCenter.location ?? '',
        isActive: fullCenter.isActive ?? true,
      });

      await this.applyRoleAssignments(assignments);
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
      this.isDialogVisible.set(false);
    } finally {
      this.isLoadingRoles.set(false);
    }
  }

  onDelete(center: TrainingCenterDto): void {
    this.centerToDelete.set(center);
    this.isDeleteDialogVisible.set(true);
  }

  async onConfirmDelete(): Promise<void> {
    const center = this.centerToDelete();
    if (!center?.id) return;
    try {
      await firstValueFrom(this.centerService.delete(center.id));
      this.toaster.success(this.l.t('::Training.Common.Delete'));
      this.isDeleteDialogVisible.set(false);
      this.centerToDelete.set(null);
      await this.loadCenters();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    }
  }

  onCancelDelete(): void {
    this.isDeleteDialogVisible.set(false);
    this.centerToDelete.set(null);
  }

  async onSave(): Promise<void> {
    const canEditBasic = this.canEditBasicInfo();
    const canEditRoles = this.canManageRoles();

    if (!canEditBasic && !canEditRoles) {
      this.toaster.error(this.l.t('::Training.Errors.Unauthorized'));
      return;
    }

    if (!this.validateForm()) return;

    this.isSaving.set(true);
    const form = this.formData();

    try {
      if (canEditBasic) {
        if (this.isEditMode() && this.editingId()) {
          await firstValueFrom(this.centerService.update(this.editingId()!, form));
        } else {
          const created = await firstValueFrom(this.centerService.create(form));
          this.editingId.set(created.id ?? null);
        }
      }

      if (canEditRoles) {
        const centerId = this.editingId()!;
        const assignments = this.buildValidAssignments();
        await firstValueFrom(this.centerService.setRoleAssignments(centerId, { assignments }));
      }

      this.toaster.success(this.l.t('::Training.Common.Save'));
      this.isDialogVisible.set(false);
      await this.loadCenters();
    } catch (err: any) {
      const message = err?.error?.error?.message || this.l.t('::Training.Errors.Generic');
      this.toaster.error(message);
    } finally {
      this.isSaving.set(false);
    }
  }

  deleteTargetLabel(): string {
    const c = this.centerToDelete();
    if (!c) return '';
    return c.centerNameAr || c.centerNameEn || c.orgUnitName || '';
  }

  // ── Form field update methods ──

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
    this.tcmAssignment.update(a => ({
      ...a,
      serviceNumber: value,
      employeeId: undefined,
    }));
    this.tcmSearchResult.set(null);
  }

  updateTcoServiceNumber(index: number, value: string): void {
    this.tcoAssignments.update(list => {
      const u = [...list];
      u[index] = { ...u[index], serviceNumber: value, employeeId: undefined };
      return u;
    });
    this.tcoSearchResults.update(m => {
      const updated = new Map(m);
      updated.delete(index);
      return updated;
    });
  }

  // ── OrgUnit ──

  onOrgUnitSelected(e: any): void {
    const node = e.itemData as OrgUnitNode | undefined;
    if (!node) return;
    this.formData.update(f => ({
      ...f,
      orgUnitId: node.id,
      centerNameAr: node.displayName,
      centerNameEn: node.displayName,
    }));
    this.isOrgUnitDropDownOpen.set(false);
  }

  // ── HR Search ──

  async searchTcmEmployee(): Promise<void> {
    const serviceNumber = this.tcmAssignment().serviceNumber;
    const emp = await this.findEmployee(serviceNumber);
    if (emp) {
      this.setTcmEmployee(emp);
    } else {
      this.clearTcmEmployee();
    }
  }

  async searchTcoEmployee(index: number): Promise<void> {
    const serviceNumber = this.tcoAssignments()[index]?.serviceNumber;
    const emp = await this.findEmployee(serviceNumber);
    if (emp) {
      this.setTcoEmployee(index, emp);
    } else {
      this.clearTcoEmployee(index);
    }
  }

  // ── Role Assignment Management ──

  addTcoAssignment(): void {
    this.tcoAssignments.update(list => [...list, this.createEmptyTcoAssignment()]);
  }

  removeTcoAssignment(index: number): void {
    this.tcoAssignments.update(list => list.filter((_, i) => i !== index));
    this.tcoSearchResults.update(m => {
      const updated = new Map<number, EmployeeLookupDto>();
      m.forEach((value, key) => {
        if (key < index) updated.set(key, value);
        if (key > index) updated.set(key - 1, value);
      });
      return updated;
    });
  }

  // ── Display helpers ──

  onSearch(): void {
    // filteredCenters is computed — searchText already bound via input event.
  }

  statusBadge(isActive?: boolean): { css: string; label: string } {
    return isActive
      ? { css: 'status-pill status-active', label: this.l.t('::Active') }
      : { css: 'status-pill status-inactive', label: this.l.t('::Inactive') };
  }

  // ── Private helpers ──

  private resetForm(): void {
    this.formData.set({
      orgUnitId: '',
      centerNameAr: '',
      centerNameEn: '',
      location: '',
      isActive: true,
    });
    this.tcmAssignment.set(this.createEmptyTcmAssignment());
    this.tcoAssignments.set([]);
    this.tcmSearchResult.set(null);
    this.tcoSearchResults.set(new Map());
    this.isLoadingRoles.set(false);
  }

  private createEmptyTcmAssignment(): CenterRoleAssignmentInputDto {
    return {
      roleType: CenterRoleType.TCM,
      assignmentType: CenterAssignmentType.Employee,
    };
  }

  private createEmptyTcoAssignment(): CenterRoleAssignmentInputDto {
    return {
      roleType: CenterRoleType.TCO,
      assignmentType: CenterAssignmentType.Employee,
    };
  }

  private async applyRoleAssignments(assignments: CenterRoleAssignmentDto[]): Promise<void> {
    const tcm = assignments.find(a => a.roleType === CenterRoleType.TCM);
    const tcos = assignments.filter(a => a.roleType === CenterRoleType.TCO);

    if (tcm) {
      this.tcmAssignment.set({
        roleType: CenterRoleType.TCM,
        assignmentType: CenterAssignmentType.Employee,
        employeeId: tcm.employeeId ?? undefined,
        serviceNumber: tcm.serviceNumber ?? undefined,
      });

      if (tcm.serviceNumber) {
        await this.loadTcmDetails();
      }
    }

    this.tcoAssignments.set(
      tcos.map(t => ({
        roleType: CenterRoleType.TCO,
        assignmentType: CenterAssignmentType.Employee,
        employeeId: t.employeeId ?? undefined,
        serviceNumber: t.serviceNumber ?? undefined,
      }))
    );

    this.tcoSearchResults.set(new Map());

    await Promise.all(
      tcos.map((t, i) => (t.serviceNumber ? this.loadTcoDetails(i, t.serviceNumber) : Promise.resolve()))
    );
  }

  private async loadTcmDetails(): Promise<void> {
    const emp = await this.findEmployee(this.tcmAssignment().serviceNumber);
    if (emp) {
      this.setTcmEmployee(emp);
    }
  }

  private async loadTcoDetails(index: number, serviceNumber: string): Promise<void> {
    const emp = await this.findEmployee(serviceNumber);
    if (emp) {
      this.setTcoEmployee(index, emp);
    }
  }

  private async findEmployee(serviceNumber?: string | null): Promise<EmployeeLookupDto | null> {
    const orgUnitId = this.formData().orgUnitId;
    if (!orgUnitId) {
      this.toaster.warn(this.l.t('::Training.SelectOrgUnitFirst'));
      return null;
    }
    if (!serviceNumber?.trim()) {
      return null;
    }

    try {
      return await firstValueFrom(this.hrLookupService.getByServiceNumber(serviceNumber.trim(), orgUnitId));
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
      return null;
    }
  }

  private setTcmEmployee(emp: EmployeeLookupDto): void {
    this.tcmSearchResult.set(emp);
    this.tcmAssignment.update(a => ({
      ...a,
      employeeId: emp.id,
      serviceNumber: emp.serviceNumber,
    }));
  }

  private clearTcmEmployee(): void {
    this.tcmSearchResult.set(null);
    this.tcmAssignment.update(a => ({ ...a, employeeId: undefined }));
    this.toaster.warn(this.l.t('::Training.EmployeeNotFound'));
  }

  private setTcoEmployee(index: number, emp: EmployeeLookupDto): void {
    this.tcoAssignments.update(list => {
      const u = [...list];
      u[index] = { ...u[index], employeeId: emp.id, serviceNumber: emp.serviceNumber };
      return u;
    });
    this.tcoSearchResults.update(m => {
      const updated = new Map(m);
      updated.set(index, emp);
      return updated;
    });
  }

  private clearTcoEmployee(index: number): void {
    this.tcoAssignments.update(list => {
      const u = [...list];
      u[index] = { ...u[index], employeeId: undefined };
      return u;
    });
    this.tcoSearchResults.update(m => {
      const updated = new Map(m);
      updated.delete(index);
      return updated;
    });
    this.toaster.warn(this.l.t('::Training.EmployeeNotFound'));
  }

  private buildValidAssignments(): CenterRoleAssignmentInputDto[] {
    const assignments: CenterRoleAssignmentInputDto[] = [];
    const tcm = this.tcmAssignment();
    if (tcm.employeeId) {
      assignments.push(tcm);
    }
    assignments.push(...this.tcoAssignments().filter(t => t.employeeId));
    return assignments;
  }

  private validateForm(): boolean {
    const form = this.formData();
    const canEditBasic = this.canEditBasicInfo();

    if (canEditBasic) {
      if (!form.orgUnitId?.trim()) {
        this.toaster.warn(this.l.t('::Training.Validation.OrgUnitRequired'));
        return false;
      }
      if (!form.centerNameAr?.trim()) {
        this.toaster.warn(this.l.t('::Training.Validation.CenterNameArRequired'));
        return false;
      }
      if (!form.centerNameEn?.trim()) {
        this.toaster.warn(this.l.t('::Training.Validation.CenterNameEnRequired'));
        return false;
      }
    }

    if (this.canManageRoles()) {
      const tcm = this.tcmAssignment();
      if (tcm.serviceNumber && !tcm.employeeId) {
        this.toaster.warn(this.l.t('::Training.Validation.TcmMustBeVerified'));
        return false;
      }

      for (let i = 0; i < this.tcoAssignments().length; i++) {
        const tco = this.tcoAssignments()[i];
        if (tco.serviceNumber && !tco.employeeId) {
          this.toaster.warn(this.l.t('::Training.Validation.TcoMustBeVerified', i + 1));
          return false;
        }
      }
    }

    return true;
  }
}
