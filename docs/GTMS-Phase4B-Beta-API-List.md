# GTMS Phase 4B-β — API List (Gate 2)

**Version:** v1.0
**Source:** Implementation Prompt v1.0 + Mockup v1.0
**Status:** Awaiting OmanAI approval
**Total endpoints:** 17 new + 0 modified across 3 AppServices + 1 domain service

---

## Summary

| AppService / Component | New endpoints | Purpose |
|---|---|---|
| `TravelAllowancePaymentAppService` | 6 | Per-nominee travel cost CRUD + confirm |
| `CoursePaymentAppService` | 8 | Course invoice CRUD + upload + download + confirm (triggers reallocation) |
| `BudgetReallocationAppService` | 3 | Read-only list/get + status update (no manual create) |
| `BudgetReallocationGenerator` (domain service) | — | Auto-fired internally on CoursePayment.Confirm |
| **Total new** | **17** | — |

**Cumulative endpoint count after Phase 4B-β:** 32 (Phase 4B-α) + 17 = **49 casual-course-related endpoints**.

---

## 1. `TravelAllowancePaymentAppService` — 6 NEW endpoints

| # | Method | Route | Purpose | Role | Request DTO | Response DTO | Permission |
|---|---|---|---|---|---|---|---|
| 1 | `POST` | `/api/app/travel-allowance-payments` | Create draft for a nomination | Finance | `CreateUpdateTravelAllowancePaymentDto` | `TravelAllowancePaymentDto` | `TrainingPayments.TravelAllowance.Create` |
| 2 | `GET` | `/api/app/travel-allowance-payments` | Filtered list | Finance, Staff | `TravelAllowancePaymentGetListInput` | `PagedResultDto<TravelAllowancePaymentDto>` | `TrainingPayments.TravelAllowance.Default` |
| 3 | `GET` | `/api/app/travel-allowance-payments/{id}` | Detail | Finance, Staff | — | `TravelAllowancePaymentDto` | `TrainingPayments.TravelAllowance.Default` |
| 4 | `PUT` | `/api/app/travel-allowance-payments/{id}` | Edit while Draft only | Finance | `CreateUpdateTravelAllowancePaymentDto` | `TravelAllowancePaymentDto` | `TrainingPayments.TravelAllowance.Update` |
| 5 | `DELETE` | `/api/app/travel-allowance-payments/{id}` | Delete while Draft only | Finance | — | — | `TrainingPayments.TravelAllowance.Delete` |
| 6 | `POST` | `/api/app/travel-allowance-payments/{id}/confirm` | Status `Draft → Confirmed`, locks immutable | Finance | — | `TravelAllowancePaymentDto` | `TrainingPayments.TravelAllowance.Confirm` |

**State machine:**
```
Draft ──confirm──▶ Confirmed (terminal — immutable)
```

Once `Confirmed`, no further changes allowed (financial audit requirement). Cancellation is reserved as enum value but no endpoint exposes it in 4B-β.

**Server-computed fields:**
- `TotalOMR` = `TicketAmountOMR + TravelAllowanceOMR + ClothingAllowanceOMR + InsuranceOMR + VisaFeesOMR` — recomputed on every save
- `PersonnelType` derived from `Nomination.Employee.Rank.IsOfficer` — auto-populated, not editable

**Validation rules:**
- Exactly one of `SessionId` / `CasualCourseId` set
- `NominationId` must belong to the specified course/session
- Course/session must be `THApproved` (not earlier)
- TravelInstruction for the parent course must exist with `Status = Issued`
- All five amounts ≥ 0
- Update + Delete + Confirm only when `Status = Draft`

**Default values pre-populated on create (server-side enrichment from TravelInstruction + rank rates):**
- `TravelAllowanceOMR` = `EffectiveTravelDays × DailyRateForRank` (rank-based defaults)
- `InsuranceOMR` = derived from TravelInstruction if `InsuranceArranged = true`
- `VisaFeesOMR` = derived from TravelInstruction if `VisaRequired = true`
- `TicketAmountOMR`, `ClothingAllowanceOMR` = 0 (Finance enters)

These defaults are **suggestions only** — Finance can override before confirming.

**DTOs:**
```csharp
public class CreateUpdateTravelAllowancePaymentDto
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid NominationId { get; set; }
    
    public decimal TicketAmountOMR { get; set; }
    public decimal TravelAllowanceOMR { get; set; }
    public decimal ClothingAllowanceOMR { get; set; }
    public decimal InsuranceOMR { get; set; }
    public decimal VisaFeesOMR { get; set; }
    
    public string? Notes { get; set; }
    
    // INTENTIONALLY ABSENT (server-controlled):
    //   TotalOMR, PersonnelType, Status, ConfirmedAt, ConfirmedById,
    //   ExternalRequestId, ExternalStatus, ExternalResponseAt
}

public class TravelAllowancePaymentDto : EntityDto<Guid>
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid NominationId { get; set; }
    
    // Joined fields for display
    public string EmployeeNameAr { get; set; }
    public string RankNameAr { get; set; }
    public PersonnelType PersonnelType { get; set; }
    public string CourseNameAr { get; set; }              // joined from Session.Course or CasualCourse.TenantCourse
    public int EffectiveTravelDays { get; set; }          // joined from TravelInstruction
    
    public decimal TicketAmountOMR { get; set; }
    public decimal TravelAllowanceOMR { get; set; }
    public decimal ClothingAllowanceOMR { get; set; }
    public decimal InsuranceOMR { get; set; }
    public decimal VisaFeesOMR { get; set; }
    public decimal TotalOMR { get; set; }
    
    public PaymentStatus Status { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedById { get; set; }
    public string? ConfirmedByName { get; set; }          // joined
    
    public string? ExternalRequestId { get; set; }
    public string? ExternalStatus { get; set; }
    public DateTime? ExternalResponseAt { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreationTime { get; set; }
}

public class TravelAllowancePaymentGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? NominationId { get; set; }
    public PaymentStatus? Status { get; set; }
    public PersonnelType? PersonnelType { get; set; }
    public string? Search { get; set; }    // employee name
}
```

---

## 2. `CoursePaymentAppService` — 8 NEW endpoints

| # | Method | Route | Purpose | Role | Request DTO | Response DTO | Permission |
|---|---|---|---|---|---|---|---|
| 7 | `POST` | `/api/app/course-payments` | Create draft | Finance | `CreateUpdateCoursePaymentDto` | `CoursePaymentDto` | `TrainingPayments.CoursePayments.Create` |
| 8 | `GET` | `/api/app/course-payments` | Filtered list | Finance, Staff | `CoursePaymentGetListInput` | `PagedResultDto<CoursePaymentDto>` | `TrainingPayments.CoursePayments.Default` |
| 9 | `GET` | `/api/app/course-payments/{id}` | Detail | Finance, Staff | — | `CoursePaymentDto` | `TrainingPayments.CoursePayments.Default` |
| 10 | `PUT` | `/api/app/course-payments/{id}` | Edit while Draft | Finance | `CreateUpdateCoursePaymentDto` | `CoursePaymentDto` | `TrainingPayments.CoursePayments.Update` |
| 11 | `DELETE` | `/api/app/course-payments/{id}` | Delete while Draft | Finance | — | — | `TrainingPayments.CoursePayments.Delete` |
| 12 | `POST` | `/api/app/course-payments/{id}/upload-invoice` | Upload PDF to BlobStoring | Finance | multipart/form-data with `IRemoteStreamContent` | `CoursePaymentDto` | `TrainingPayments.CoursePayments.UploadInvoice` |
| 13 | `GET` | `/api/app/course-payments/{id}/download-invoice` | Stream PDF back | Finance, Staff | — | `IRemoteStreamContent` | `TrainingPayments.CoursePayments.DownloadInvoice` |
| 14 | `POST` | `/api/app/course-payments/{id}/confirm` | Status `Draft → Confirmed`, **triggers BudgetReallocationGenerator for casual courses** | Finance | — | `CoursePaymentConfirmResultDto` | `TrainingPayments.CoursePayments.Confirm` |

**State machine:** same as TravelAllowancePayment — `Draft → Confirmed (terminal)`.

**Confirm endpoint side effects (atomic — single transaction):**

```csharp
public async Task<CoursePaymentConfirmResultDto> ConfirmAsync(Guid id)
{
    var payment = await _repo.GetAsync(id);
    
    // Pre-conditions
    if (payment.Status != PaymentStatus.Draft)
        throw new BusinessException("Training:CoursePayment:InvalidStatusTransition");
    if (string.IsNullOrEmpty(payment.InvoiceBlobName))
        throw new BusinessException("Training:CoursePayment:InvoiceRequired");
    
    // Mutate
    payment.Status = PaymentStatus.Confirmed;
    payment.ConfirmedAt = Clock.Now;
    payment.ConfirmedById = CurrentUser.Id;
    
    // FIRE THE REALLOCATION GENERATOR — casual courses only
    var generatedCount = 0;
    if (payment.CasualCourseId.HasValue)
    {
        generatedCount = await _reallocGenerator.GenerateForCasualCoursePaymentAsync(
            coursePaymentId: payment.Id,
            casualCourseId: payment.CasualCourseId.Value);
    }
    
    return new CoursePaymentConfirmResultDto
    {
        Payment = ObjectMapper.Map<CoursePayment, CoursePaymentDto>(payment),
        GeneratedReallocationsCount = generatedCount
    };
}
```

The result DTO returns the count of generated reallocations so the frontend can show a toast like "Confirmed — 4 reallocations generated."

**Validation rules:**
- Exactly one of `SessionId` / `CasualCourseId` set
- For casual courses: must reach `THApproved`, must have `SelectedPriceQuoteId` set
- `TrainingProviderId` must match `SelectedPriceQuote.TrainingProviderId` (server enforces)
- `InvoiceAmountOMR` ≥ 0, `NebrasAmountOMR` ≥ 0
- `InvoiceBlobName` required before confirm
- Update + Delete + Confirm only when `Status = Draft`

**File upload behavior:**
- `UploadInvoiceAsync` accepts only PDF (`Content-Type: application/pdf`)
- Max file size 10 MB (configurable via ABP setting)
- Generates unique `BlobName` like `invoice-{paymentId}-{Guid}.pdf`
- Replaces existing blob if re-uploaded (old blob deleted)
- Stores `OriginalFileName` for download UX

**File download behavior:**
- Returns `IRemoteStreamContent` with `Content-Disposition: attachment; filename={OriginalFileName}`
- Permission `DownloadInvoice` granted to Finance + Staff (Staff needs read access for context)

**DTOs:**
```csharp
public class CreateUpdateCoursePaymentDto
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid TrainingProviderId { get; set; }
    
    public decimal InvoiceAmountOMR { get; set; }
    public decimal NebrasAmountOMR { get; set; }
    public DateTime InvoiceDate { get; set; }
    
    public string? Notes { get; set; }
    
    // INTENTIONALLY ABSENT:
    //   InvoiceBlobName, InvoiceOriginalFileName (set via UploadInvoiceAsync)
    //   Status, ConfirmedAt, ConfirmedById
}

public class CoursePaymentDto : EntityDto<Guid>
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid TrainingProviderId { get; set; }
    public string TrainingProviderName { get; set; }       // joined
    public string? CourseNameAr { get; set; }              // joined
    public string? FundingScenarioLabel { get; set; }      // joined for display: "Scenario 2"
    
    public decimal InvoiceAmountOMR { get; set; }
    public decimal NebrasAmountOMR { get; set; }
    public decimal VarianceOMR { get; set; }               // computed: NebrasAmountOMR - InvoiceAmountOMR
    public DateTime InvoiceDate { get; set; }
    
    public string? InvoiceBlobName { get; set; }
    public string? InvoiceOriginalFileName { get; set; }
    public bool HasInvoice => !string.IsNullOrEmpty(InvoiceBlobName);
    
    public PaymentStatus Status { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedById { get; set; }
    public string? ConfirmedByName { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreationTime { get; set; }
}

public class CoursePaymentConfirmResultDto
{
    public CoursePaymentDto Payment { get; set; }
    public int GeneratedReallocationsCount { get; set; }
}

public class CoursePaymentGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? TrainingProviderId { get; set; }
    public PaymentStatus? Status { get; set; }
    public DateTime? InvoiceDateFrom { get; set; }
    public DateTime? InvoiceDateTo { get; set; }
}
```

---

## 3. `BudgetReallocationAppService` — 3 NEW endpoints

Reallocations are **auto-generated only**. No `Create` endpoint exists. Read access for Staff/TD; Status update for Staff.

| # | Method | Route | Purpose | Role | Request DTO | Response DTO | Permission |
|---|---|---|---|---|---|---|---|
| 15 | `GET` | `/api/app/budget-reallocations` | Filtered list | Staff, TD | `BudgetReallocationGetListInput` | `PagedResultDto<BudgetReallocationDto>` | `TrainingPayments.Reallocations.View` |
| 16 | `GET` | `/api/app/budget-reallocations/{id}` | Detail | Staff, TD | — | `BudgetReallocationDto` | `TrainingPayments.Reallocations.View` |
| 17 | `POST` | `/api/app/budget-reallocations/{id}/mark-approved` | Status `Pending → Approved` (records TD signature) | Staff | `MarkReallocationApprovedDto` | `BudgetReallocationDto` | `TrainingPayments.Reallocations.MarkApproved` |

**State machine:**
```
Pending ──mark-approved──▶ Approved (terminal — immutable)
```

**Validation:**
- `MarkApproved` only allowed when `Status = Pending`
- `ApprovedById` must reference a user with TD role (server enforces)

**No Create / Update / Delete endpoints.** Records can only enter the system via `BudgetReallocationGenerator` (called from `CoursePaymentAppService.ConfirmAsync`).

**DTOs:**
```csharp
public class BudgetReallocationDto : EntityDto<Guid>
{
    public Guid CasualCourseId { get; set; }
    public string CasualCourseNameAr { get; set; }       // joined
    public FundingScenario FundingScenario { get; set; } // joined from CasualCourse
    public Guid CoursePaymentId { get; set; }
    
    public string FundingSourceVoteCode { get; set; }
    public string? FundingSourceName { get; set; }       // joined from CasualCourse at gen time
    
    public Guid ToFinancialItemId { get; set; }
    public string ToFinancialItemNameAr { get; set; }    // joined
    
    public decimal AmountOMR { get; set; }
    
    public ReallocationStatus Status { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }          // joined
    public string? ApprovalNote { get; set; }
    
    public DateTime CreationTime { get; set; }            // when generator fired
}

public class MarkReallocationApprovedDto
{
    public Guid ApprovedById { get; set; }       // TD user who signed externally
    public string? ApprovalNote { get; set; }    // optional reference, paper signature date, etc.
}

public class BudgetReallocationGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? CasualCourseId { get; set; }
    public Guid? CoursePaymentId { get; set; }
    public Guid? ToFinancialItemId { get; set; }
    public string? FundingSourceVoteCode { get; set; }
    public ReallocationStatus? Status { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
}
```

---

## 4. `BudgetReallocationGenerator` — internal domain service

Not an HTTP endpoint. Called only from `CoursePaymentAppService.ConfirmAsync` after a payment transitions to Confirmed.

```csharp
public class BudgetReallocationGenerator : IDomainService
{
    public async Task<int> GenerateForCasualCoursePaymentAsync(
        Guid coursePaymentId,
        Guid casualCourseId)
    {
        // Idempotency guard
        if (await _reallocRepo.AnyAsync(r => r.CoursePaymentId == coursePaymentId))
            return 0;
        
        var course = await _courseRepo.GetAsync(casualCourseId);
        
        // Scenario 1: no reallocations needed
        if (course.FundingScenario == FundingScenario.FundingSourceCoversAll)
            return 0;
        
        if (course.FundingScenario == null)
            throw new BusinessException("Training:Reallocation:ScenarioNotSet");
        if (string.IsNullOrEmpty(course.FundingSourceVoteCode))
            throw new BusinessException("Training:Reallocation:VoteCodeRequired");
        
        var items = await _itemRepo.GetListAsync(
            i => i.CasualCourseId == casualCourseId,
            includeDetails: true);
        
        var generated = 0;
        foreach (var item in items)
        {
            // Scenario 2: skip items with Source = FundingSource
            if (course.FundingScenario == FundingScenario.FundingSourceCoversCourse
                && item.Source == FinancialAmountSource.FundingSource)
                continue;
            
            var amount = item.Ranks.Sum(r => r.SubtotalOMR);
            if (amount <= 0) continue;
            
            await _reallocRepo.InsertAsync(new BudgetReallocation
            {
                Id = _guidGenerator.Create(),
                TenantId = _currentTenant.Id ?? Guid.Empty,
                CasualCourseId = casualCourseId,
                CoursePaymentId = coursePaymentId,
                FundingSourceVoteCode = course.FundingSourceVoteCode,
                ToFinancialItemId = item.FinancialItemId,
                AmountOMR = amount,
                Status = ReallocationStatus.Pending
            });
            generated++;
        }
        
        return generated;
    }
}
```

**Properties:**
- **Idempotent** — re-running for same `coursePaymentId` returns 0 (does nothing)
- **Scenario-aware** — uses existing `FundingScenario` enum and `FinancialAmountSource` from Patch 3
- **Audit-friendly** — copies `FundingSourceVoteCode` (not joins), so reallocations stable even if course funding source changes later
- **Sums per item** — one reallocation per `FinancialItem`, not per rank

---

## 5. Cross-cutting concerns

### Polymorphic parent rule

`TravelAllowancePayment` and `CoursePayment` both enforce server-side:
> Exactly one of `SessionId` / `CasualCourseId` must be set; the other must be null.

DB check constraint as defense-in-depth.

### Tenant isolation

All endpoints respect `IMultiTenant`. ABP filter applies automatically.

### File storage

All invoice blobs stored via `BlobStoring.FileSystem` with `BasePath` from ABP setting `Gtms.Files.Path`. Per-tenant subfolders auto-created by ABP. Container name: `training-course-invoices`.

### Validation error codes

| Key | Trigger |
|---|---|
| `Training:TravelAllowancePayment:InvalidStatusTransition` | Edit/Delete/Confirm on non-Draft |
| `Training:TravelAllowancePayment:CourseNotApproved` | Course not at THApproved |
| `Training:TravelAllowancePayment:TravelInstructionNotIssued` | TravelInstruction missing or not Issued |
| `Training:TravelAllowancePayment:NominationNotInCourse` | NominationId doesn't belong to specified course |
| `Training:TravelAllowancePayment:DuplicateForNomination` | Already exists for this NominationId |
| `Training:CoursePayment:InvalidStatusTransition` | Edit/Delete/Confirm on non-Draft |
| `Training:CoursePayment:InvoiceRequired` | Confirm attempted without uploaded invoice |
| `Training:CoursePayment:ProviderMismatch` | TrainingProviderId != SelectedPriceQuote.TrainingProviderId |
| `Training:CoursePayment:CourseNotReady` | Casual course missing SelectedPriceQuoteId |
| `Training:CoursePayment:InvalidFileType` | Upload non-PDF |
| `Training:CoursePayment:FileTooLarge` | Upload exceeds size limit |
| `Training:Reallocation:ScenarioNotSet` | Generator called for course without scenario |
| `Training:Reallocation:VoteCodeRequired` | FundingSourceVoteCode null |
| `Training:Reallocation:InvalidStatusTransition` | MarkApproved on non-Pending |
| `Training:Reallocation:ApproverNotTD` | ApprovedById doesn't have TD role |

All keys also exist in `ar.json` and `en.json`.

---

## 6. Permission group

```csharp
public static class TrainingPaymentsPermissions
{
    public const string GroupName = "TrainingPayments";
    
    // TravelAllowance
    public const string TravelAllowance = GroupName + ".TravelAllowance";
    public const string TravelAllowance_Default = TravelAllowance + ".Default";
    public const string TravelAllowance_Create = TravelAllowance + ".Create";
    public const string TravelAllowance_Update = TravelAllowance + ".Update";
    public const string TravelAllowance_Delete = TravelAllowance + ".Delete";
    public const string TravelAllowance_Confirm = TravelAllowance + ".Confirm";
    
    // CoursePayments
    public const string CoursePayments = GroupName + ".CoursePayments";
    public const string CoursePayments_Default = CoursePayments + ".Default";
    public const string CoursePayments_Create = CoursePayments + ".Create";
    public const string CoursePayments_Update = CoursePayments + ".Update";
    public const string CoursePayments_Delete = CoursePayments + ".Delete";
    public const string CoursePayments_Confirm = CoursePayments + ".Confirm";
    public const string CoursePayments_UploadInvoice = CoursePayments + ".UploadInvoice";
    public const string CoursePayments_DownloadInvoice = CoursePayments + ".DownloadInvoice";
    
    // Reallocations
    public const string Reallocations = GroupName + ".Reallocations";
    public const string Reallocations_View = Reallocations + ".View";
    public const string Reallocations_MarkApproved = Reallocations + ".MarkApproved";
}
```

**Default grants:**

| Role | TravelAllowance | CoursePayments | Reallocations |
|---|---|---|---|
| Finance | All (Create/Update/Delete/Confirm/Upload/Download) | All | View only |
| Staff | View + Download (read-only) | View + Download (read-only) | View + MarkApproved |
| TD | View only | View only | View only |
| TH | View only | View only | View only |
| UTM, UGM, DM | None | None | None |

---

## 7. Endpoint count derivation

| Source | Count |
|---|---|
| Phase 4A | 25 |
| Phase 4B-α (new) | 7 |
| Phase 4B-β (new — this) | 17 |
| **Cumulative casual-course-touched endpoints** | **49** |

---

## 8. Approval checklist

- [ ] All 17 endpoints (6 Travel + 8 Course + 3 Reallocation) reviewed
- [ ] Polymorphic parent rule clear on both payment entities
- [ ] Server-controlled fields verified: `TotalOMR`, `Status`, `PersonnelType`, `InvoiceBlobName`
- [ ] Auto-generation logic in `BudgetReallocationGenerator` understood (idempotent, scenario-aware)
- [ ] File upload constraints reasonable (PDF only, 10 MB)
- [ ] Validation error keys match Phase 4A/4B-α naming pattern
- [ ] Permission group structure aligned with existing GTMS conventions
- [ ] Default role grants reasonable
- [ ] Confirm result DTO returns reallocation count for UI feedback
- [ ] No endpoint exposes sensitive data inappropriately

---

*End of Phase 4B-β API List — v1.0*
*Awaiting OmanAI approval to proceed to backend implementation*
