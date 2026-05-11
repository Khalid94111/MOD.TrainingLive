# GTMS Phase 4B-β — Patch 1: TD Approves Reallocations Directly

**Version:** v1.0
**Target:** v4.9.0 → v4.9.1 (patch bump)
**Scope:** Reverse the "Staff records TD's external signature" model. TD now approves reallocations directly in the system. Affects both backend and frontend.
**Reason:** OmanAI clarified that there is no external paper signature step. TDs approve in-system, not via paperwork.

---

## What's changing

The current implementation (just shipped in v4.9.0) has Staff acting as a proxy — Staff opens PAGE 4.7, selects which TD signed externally from a dropdown, and clicks confirm to record the approval.

The correct model: **TD logs in, opens PAGE 4.7, reviews each Pending reallocation, and clicks Approve directly.** Staff is removed from the approval workflow entirely (still has read access for context).

Three real changes flow from this:

1. **Permission shift** — `Reallocations.MarkApproved` moves from Staff role to TD role
2. **DTO simplification** — `MarkReallocationApprovedDto.ApprovedById` field removed; server derives it from `CurrentUser.Id`
3. **Validation removed** — `Training:Reallocation:ApproverNotTD` error code is no longer needed (permission gate enforces this)

---

## Backend changes

### B1 — `MarkReallocationApprovedDto`

**File:** `src/YourApp.Application.Contracts/Training/BudgetReallocations/MarkReallocationApprovedDto.cs`

Remove the `ApprovedById` field:

```csharp
// BEFORE
public class MarkReallocationApprovedDto
{
    public Guid ApprovedById { get; set; }
    public string? ApprovalNote { get; set; }
}

// AFTER
public class MarkReallocationApprovedDto
{
    public string? ApprovalNote { get; set; }
}
```

### B2 — `BudgetReallocationAppService.MarkApprovedAsync`

**File:** `src/YourApp.Application/Training/BudgetReallocations/BudgetReallocationAppService.cs`

Update method body — derive ApprovedById from CurrentUser, remove role validation:

```csharp
// BEFORE
public async Task<BudgetReallocationDto> MarkApprovedAsync(
    Guid id, MarkReallocationApprovedDto input)
{
    var realloc = await _repo.GetAsync(id);
    
    if (realloc.Status != ReallocationStatus.Pending)
        throw new BusinessException("Training:Reallocation:InvalidStatusTransition");
    
    // Validate ApprovedById has TD role
    var approver = await _userRepo.GetAsync(input.ApprovedById);
    var hasTDRole = await _userManager.IsInRoleAsync(approver, "TrainingDirector");
    if (!hasTDRole)
        throw new BusinessException("Training:Reallocation:ApproverNotTD");
    
    realloc.Status = ReallocationStatus.Approved;
    realloc.ApprovedAt = Clock.Now;
    realloc.ApprovedById = input.ApprovedById;
    realloc.ApprovalNote = input.ApprovalNote;
    
    return ObjectMapper.Map<BudgetReallocation, BudgetReallocationDto>(realloc);
}

// AFTER
public async Task<BudgetReallocationDto> MarkApprovedAsync(
    Guid id, MarkReallocationApprovedDto input)
{
    var realloc = await _repo.GetAsync(id);
    
    if (realloc.Status != ReallocationStatus.Pending)
        throw new BusinessException("Training:Reallocation:InvalidStatusTransition");
    
    if (CurrentUser.Id == null)
        throw new AbpAuthorizationException();
    
    realloc.Status = ReallocationStatus.Approved;
    realloc.ApprovedAt = Clock.Now;
    realloc.ApprovedById = CurrentUser.Id;
    realloc.ApprovalNote = input.ApprovalNote;
    // Permission system enforces TD role — no manual role check needed
    
    return ObjectMapper.Map<BudgetReallocation, BudgetReallocationDto>(realloc);
}
```

Also remove unused dependencies if `_userRepo` and `_userManager` were injected only for the role check — clean them out of the constructor.

### B3 — Permission grants

**File:** `src/YourApp.Application/Training/Permissions/TrainingPaymentsPermissionDataSeedContributor.cs` (or wherever default role-permission grants are seeded)

Move `TrainingPayments.Reallocations.MarkApproved` from Staff role to TD role:

```csharp
// BEFORE
context.Grant(StaffRoleName, TrainingPaymentsPermissions.Reallocations_View);
context.Grant(StaffRoleName, TrainingPaymentsPermissions.Reallocations_MarkApproved);
context.Grant(TDRoleName, TrainingPaymentsPermissions.Reallocations_View);

// AFTER
context.Grant(StaffRoleName, TrainingPaymentsPermissions.Reallocations_View);
context.Grant(TDRoleName, TrainingPaymentsPermissions.Reallocations_View);
context.Grant(TDRoleName, TrainingPaymentsPermissions.Reallocations_MarkApproved);
```

If the seeder has already run on existing tenants, also write a one-time migration or seed update that revokes `MarkApproved` from Staff role and grants it to TD role for all existing tenants.

### B4 — Localization

**Files:** `src/YourApp.Domain.Shared/Localization/YourApp/ar.json` + `en.json`

Remove the unused error key:

```json
// REMOVE from both files:
"Training:Reallocation:ApproverNotTD": "..."
```

### B5 — Backend verification

After applying B1–B4:

```bash
# Verify build
dotnet build

# Run any existing tests on BudgetReallocationAppService
dotnet test --filter "FullyQualifiedName~BudgetReallocation"
```

Manual test via Swagger:
- Login as Staff → POST `/api/app/budget-reallocations/{id}/mark-approved` → expect 403 Forbidden
- Login as TD → same call → expect success, response shows `ApprovedById = TD's user id`

---

## Frontend changes

### F1 — `MarkReallocationApprovedDto` proxy

**File:** Auto-generated from backend.

After B1 lands, regenerate the proxy:
```bash
abp generate-proxy -t ng
```

Verify `MarkReallocationApprovedDto` interface no longer has `approvedById` field.

### F2 — PAGE 4.7 approval dialog

**File:** `src/app/training/payments/budget-reallocations/budget-reallocations.component.html` (or the dialog component file)

Remove the TD selector dropdown. Dialog now has only the approval note + confirm button:

```html
<!-- BEFORE -->
<dx-popup [(visible)]="dialogVisible">
  <div class="dialog-content">
    <!-- Reallocation summary -->
    <div class="summary">...</div>
    
    <!-- TD selector — REMOVE THIS BLOCK -->
    <div class="form-group">
      <label>{{ '::Training.Reallocation.SelectApprover' | abpLocalization }}</label>
      <dx-select-box
        [items]="tdUsers()"
        displayExpr="userName"
        valueExpr="id"
        [(value)]="selectedApproverId">
      </dx-select-box>
    </div>
    
    <!-- Approval note -->
    <div class="form-group">
      <label>{{ '::Training.Reallocation.ApprovalNote' | abpLocalization }}</label>
      <dx-text-area [(value)]="approvalNote"></dx-text-area>
    </div>
  </div>
</dx-popup>

<!-- AFTER -->
<dx-popup [(visible)]="dialogVisible">
  <div class="dialog-content">
    <div class="summary">...</div>
    
    <div class="form-group">
      <label>{{ '::Training.Reallocation.ApprovalNote' | abpLocalization }}</label>
      <dx-text-area [(value)]="approvalNote"></dx-text-area>
    </div>
  </div>
</dx-popup>
```

### F3 — PAGE 4.7 component logic

**File:** `src/app/training/payments/budget-reallocations/budget-reallocations.component.ts`

Remove:
- `tdUsers` signal and the call that loads them (was probably `loadTDUsers()` calling some user API)
- `selectedApproverId` signal
- Any imports for the user proxy if they were only used for loading TD list

Update the approval method:

```typescript
// BEFORE
async onConfirmApproval() {
  const dto: MarkReallocationApprovedDto = {
    approvedById: this.selectedApproverId(),
    approvalNote: this.approvalNote()
  };
  await firstValueFrom(this.service.markApproved(this.selectedRow().id, dto));
  this.dialogVisible.set(false);
  this.reload();
}

// AFTER
async onConfirmApproval() {
  const dto: MarkReallocationApprovedDto = {
    approvalNote: this.approvalNote()
  };
  await firstValueFrom(this.service.markApproved(this.selectedRow().id, dto));
  this.dialogVisible.set(false);
  this.reload();
}
```

### F4 — Banner text update

**File:** PAGE 4.7 component template + the JSON files for localization

Update the read-only banner message:

```html
<!-- BEFORE -->
<div class="info-banner">
  {{ '::Training.Reallocation.AutoGenBannerStaff' | abpLocalization }}
</div>

<!-- AFTER -->
<div class="info-banner">
  {{ '::Training.Reallocation.AutoGenBannerTD' | abpLocalization }}
</div>
```

Add the new localization key, drop the old one:

```json
// ar.json — REPLACE
// REMOVE: "Training.Reallocation.AutoGenBannerStaff"
// ADD:
"Training.Reallocation.AutoGenBannerTD": "القيود تُولَّد تلقائياً عند تأكيد دفعة الدورة. مدير التدريب يراجع كل قيد معلق ويعتمده مباشرة."

// en.json — same pattern
"Training.Reallocation.AutoGenBannerTD": "Reallocations are auto-generated when a course payment is confirmed. TD reviews each pending reallocation and approves directly."
```

Also remove the now-unused localization key:
```json
// REMOVE from both ar.json and en.json:
"Training.Reallocation.SelectApprover": "..."
```

### F5 — PAGE 4.7 visibility for Staff vs TD

The page should still be visible to Staff (they have View permission), but the "Approve" button column should be hidden for them. TD sees the button and can click.

**File:** Component template

```html
<!-- Action column in the grid -->
<dxi-column cellTemplate="actionsTpl"></dxi-column>

<div *dxTemplate="let cell of 'actionsTpl'">
  @if (cell.data.status === ReallocationStatus.Pending && canApprove()) {
    <button class="btn-success" (click)="openApprovalDialog(cell.data)">
      ✓ {{ '::Training.Reallocation.Approve' | abpLocalization }}
    </button>
  }
  <button class="btn-ghost" (click)="viewDetails(cell.data)">👁️</button>
</div>
```

Add `canApprove` computed in the component:

```typescript
import { PermissionService } from '@abp/ng.core';

private permissionService = inject(PermissionService);

canApprove = computed(() => 
  this.permissionService.getGrantedPolicy('TrainingPayments.Reallocations.MarkApproved')
);
```

### F6 — Section 5 in casual course detail page

**File:** Section 5 component (the Reallocation card portion of the casual course detail shell)

The shortcut button in the reallocation card was likely labeled for Staff workflow. Update if needed:

```html
<!-- BEFORE -->
<button>→ {{ '::Training.OpenForApproval' | abpLocalization }}</button>

<!-- AFTER (more neutral) -->
<button>→ {{ '::Training.OpenReallocations' | abpLocalization }}</button>
```

### F7 — Frontend verification

After applying F1–F6:

```bash
# Rebuild
ng build

# Manual test:
# 1. Login as Staff → open PAGE 4.7 → see grid but no "Approve" button on rows
# 2. Login as TD → open PAGE 4.7 → "Approve" button visible on Pending rows
# 3. TD clicks Approve → dialog opens with only note field (no TD dropdown)
# 4. TD clicks Confirm → row updates, ApprovedById field shows TD's name
# 5. Login as Staff → see the just-approved row showing TD as approver in audit column
```

---

## Cross-cutting

### Existing Pending reallocations in the database

Any reallocations created during v4.9.0 testing that were "approved by Staff selecting a TD" are in a quasi-valid state — their `ApprovedById` references a TD, but the audit shows Staff was the actor (via `LastModifiedById` on the FullAudited base).

**No data migration needed.** Those rows are valid going forward. The change only affects how new approvals are recorded.

### Mockup file update

Update `docs/GTMS-Phase4B-Beta-Mockup.html` Screen 3 (PAGE 4.7):
- Remove the TD selector field from the Approval Dialog
- Update the role pill from "Staff (يحدّث الحالة)" to "TD (يعتمد)"
- Update the read-only banner text
- Update Screen 4 Section 5 reallocation card if the approval flow text was Staff-centric

This is for documentation accuracy; the mockup is reference material, not running code.

---

## Verification checklist

- [ ] Backend `MarkReallocationApprovedDto` no longer has `ApprovedById`
- [ ] Backend `MarkApprovedAsync` derives ApprovedById from CurrentUser
- [ ] Backend role validation removed; constructor cleaned of unused dependencies
- [ ] Permission grants updated: Staff loses MarkApproved, TD gains it
- [ ] Existing tenant data: revoke + grant migration runs
- [ ] `Training:Reallocation:ApproverNotTD` error key removed from both ar.json and en.json
- [ ] Backend builds clean, existing tests pass
- [ ] Swagger test: Staff gets 403, TD succeeds
- [ ] Proxy regenerated, frontend types match new DTO
- [ ] PAGE 4.7 dialog no longer shows TD selector
- [ ] PAGE 4.7 component imports/signals cleaned of TD-loading logic
- [ ] PAGE 4.7 banner text updated
- [ ] PAGE 4.7 "Approve" button hidden for users without MarkApproved permission
- [ ] Mockup HTML updated to match
- [ ] Manual test: Staff sees grid, no Approve buttons; TD sees grid + Approve buttons
- [ ] Manual test: TD approves a reallocation, ApprovedById = TD's user id

---

## Commit message suggestion

```
fix(reallocations): TD approves directly, removing Staff proxy step

OmanAI clarified there is no external paper signature for TD approvals —
TDs approve reallocations directly in the system, not via Staff recording
an external decision.

Changes:
- MarkReallocationApprovedDto: drop ApprovedById field
- MarkApprovedAsync: derive ApprovedById from CurrentUser, remove role check
- Permissions: Reallocations.MarkApproved moves from Staff to TD
- Localization: drop Training:Reallocation:ApproverNotTD (permission gate
  enforces it now)
- PAGE 4.7: remove TD selector from dialog, update banner text, gate
  Approve button by canApprove permission
- Mockup HTML updated to reflect new model

No data migration — existing reallocations remain valid.

Bumps v4.9.0 → v4.9.1
```

---

*End of Phase 4B-β Patch 1 — v1.0*
*Estimated effort: ~1 hour Claude Code work. Backend and frontend changes are mechanical, no architectural risk.*
