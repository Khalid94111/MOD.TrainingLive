# GTMS Phase 4B-α — Implementation Prompt

**Version:** v1.0
**Target version:** v4.7.0 → v4.8.0
**Scope:** Post-approval execution layer for casual courses + annual plan sessions. Three new entities, one new domain service (auto-generator), four new pages, one entity extension (TrainingProviders), one file-storage integration. Excludes price quotes (deferred to Phase 4B-β).
**Base code:** Phase 4A v4.7.0 (Patches 1-5 all merged).
**Companion docs:**
- `docs/GTMS-BRD-v4_0.md` §17.2 (3 funding scenarios), §19A (BudgetReallocations)
- `docs/GTMS-Dev-Roadmap-v3.md` Phase 4 section (PAGE 4.5 – 4.7)
- `docs/GTMS-Project-Checkpoint-v4_7_0.md` — current state recovery

---

## 1. Why this phase exists

Once a casual course (or annual plan session) reaches `THApproved`, three execution tracks become possible:

- **Track A — Price Quotes** (external courses only) → **deferred to Phase 4B-β**
- **Track B — Travel Allowance Payments** (external courses only) → **this phase**
- **Track C — Course Payment** (all courses) → **this phase**, plus the **BudgetReallocation auto-generation** that fires on payment confirmation

Phase 4B-α delivers Tracks B + C, the `BudgetReallocationGenerator` domain service, and the prerequisite extensions to `TrainingProviders` + `GeographicalLocation` integration. This unblocks Finance Officers to start processing real payments against approved courses while Phase 4B-β (Price Quotes) is built in parallel.

### 1.1 Locked decisions (confirmed by OmanAI)

| # | Decision | Implementation |
|---|---|---|
| Q1 | Phase 4B split into 4B-α + 4B-β | This patch covers α only. Price quotes wait for β. |
| Q2 | Payment entities polymorphic from day one | Both `TravelAllowancePayment` and `CoursePayment` carry nullable `SessionId` + `CasualCourseId` with check constraint that exactly one is set. Annual plan sessions use the same entities + screens. |
| Q3 | File storage = `BlobStoring.FileSystem` with `BasePath` driven by ABP Setting Management | Setting key: `Gtms.Files.Path`. Read at module startup via `ISettingProvider`. |
| Q-A | TrainingProvider Nebras fields are sync-driven, never user-editable | `IsFromNebras` + `NebrasId` columns exist on the entity but are excluded from create/update DTOs. Staff cannot touch them. Reserved for future Nebras sync. |
| Q-B | TrainingProvider scope = enum, not bool | New `ProviderScope` enum: `Internal / Local / International`. Replaces ad-hoc "is external" thinking. |
| Q-C | PriceQuote location fields (Country/City) deferred to Phase 4B-β | Not in this patch. |
| — | TrainingProvider gets new `CountryId` field | FK to HR `GeographicalLocation` (top-level rows only). Required for `Local` and `International` providers; null for `Internal`. |
| — | `GeographicalLocation` is read-only HR reference | Single table, self-referencing parent. Country = `LocationParentId IS NULL`, City = `LocationParentId = country.Id`. GTMS adds FKs but never writes. |

---

## 2. Schema changes

### 2.1 New entity — `TrainingProvider` extension (already exists; columns added)

**Migration:** `v4_8_0_TrainingProviderExtension`

```csharp
migrationBuilder.AddColumn<bool>(
    name: "IsFromNebras",
    table: "TrnTrainingProviders",
    type: "bit",
    nullable: false,
    defaultValue: false);

migrationBuilder.AddColumn<string>(
    name: "NebrasId",
    table: "TrnTrainingProviders",
    type: "nvarchar(100)",
    maxLength: 100,
    nullable: true);

migrationBuilder.AddColumn<int>(
    name: "Scope",
    table: "TrnTrainingProviders",
    type: "int",
    nullable: false,
    defaultValue: 1);  // Local — safe default for existing rows

migrationBuilder.AddColumn<Guid>(
    name: "CountryId",
    table: "TrnTrainingProviders",
    type: "uniqueidentifier",
    nullable: true);

// FK to HR GeographicalLocation
migrationBuilder.CreateIndex(
    name: "IX_TrnTrainingProviders_CountryId",
    table: "TrnTrainingProviders",
    column: "CountryId");
```

**Foreign key to HR `GeographicalLocation`** — declared in EF config but **not enforced as DB FK** because the HR module owns that table (read-only reference per project pattern). Same approach used for `Employee` and `Rank` references.

**New enum — `ProviderScope`:**
```csharp
public enum ProviderScope
{
    Internal      = 0,  // Our own training center (within tenant org)
    Local         = 1,  // External Omani company
    International = 2   // Foreign company
}
```

**Updated entity:**
```csharp
public class TrainingProvider : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid TenantId { get; set; }
    public string ProviderNameAr { get; set; }
    public string ProviderNameEn { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal? AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    
    // NEW
    public bool IsFromNebras { get; set; }              // sync-driven — never edited via UI
    public string? NebrasId { get; set; }               // sync-driven — never edited via UI
    public ProviderScope Scope { get; set; }
    public Guid? CountryId { get; set; }                // FK to GeographicalLocation; required for Local + International
}
```

### 2.2 New entity — `TravelAllowancePayment`

**Migration:** `v4_8_0_TravelAllowancePayments` (within same migration file as 2.1)

```csharp
public class TravelAllowancePayment : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid TenantId { get; set; }
    
    // Polymorphic parent — exactly one set (DB check constraint)
    public Guid? SessionId { get; set; }
    public Guid? CasualCourseId { get; set; }
    
    public Guid NominationId { get; set; }              // FK — the nominee receiving payment
    public PersonnelType PersonnelType { get; set; }    // auto-filled from nomination
    
    // Five amount components — all in OMR
    public decimal TicketAmountOMR { get; set; }
    public decimal TravelAllowanceOMR { get; set; }
    public decimal ClothingAllowanceOMR { get; set; }
    public decimal InsuranceOMR { get; set; }
    public decimal VisaFeesOMR { get; set; }
    public decimal TotalOMR { get; set; }               // computed = sum of above; written by AppService
    
    public PaymentStatus Status { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedById { get; set; }
    
    // Future Nebras integration fields (read-only display, populated later)
    public string? ExternalRequestId { get; set; }
    public string? ExternalStatus { get; set; }
    public DateTime? ExternalResponseAt { get; set; }
    
    public string? Notes { get; set; }
}
```

**Indexes:** `(TenantId, NominationId)` unique, `(TenantId, SessionId)`, `(TenantId, CasualCourseId)`.

**Check constraint** in EF config:
```csharp
b.ToTable(t => t.HasCheckConstraint(
    "CK_TravelAllowancePayment_PolymorphicParent",
    "(SessionId IS NOT NULL AND CasualCourseId IS NULL) OR (SessionId IS NULL AND CasualCourseId IS NOT NULL)"
));
```

**`PaymentStatus` enum (new):**
```csharp
public enum PaymentStatus
{
    Draft     = 0,   // Finance entered amounts, not yet confirmed
    Confirmed = 1,   // Finance signed off — triggers reallocation generation (CoursePayment only)
    Cancelled = 2    // Reverted; not currently used but reserved
}
```

### 2.3 New entity — `CoursePayment`

```csharp
public class CoursePayment : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid TenantId { get; set; }
    
    // Polymorphic parent — exactly one set
    public Guid? SessionId { get; set; }
    public Guid? CasualCourseId { get; set; }
    
    public Guid TrainingProviderId { get; set; }        // FK
    
    public decimal InvoiceAmountOMR { get; set; }       // what provider charged
    public decimal NebrasAmountOMR { get; set; }        // what Nebras paid out
    public DateTime InvoiceDate { get; set; }
    
    // BlobStoring file refs
    public string? InvoiceBlobName { get; set; }        // file ID inside container
    public string? InvoiceOriginalFileName { get; set; }
    
    public PaymentStatus Status { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedById { get; set; }
    
    public string? Notes { get; set; }
}
```

**Indexes + check constraint:** same shape as `TravelAllowancePayment`.

### 2.4 New entity — `BudgetReallocation`

```csharp
public class BudgetReallocation : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid CasualCourseId { get; set; }            // FK — only generated for casual courses (Phase 4B-α)
    public Guid CoursePaymentId { get; set; }           // FK — the payment that triggered this reallocation
    
    public string FundingSourceVoteCode { get; set; }   // copied from CasualCourse at generation time (audit)
    public Guid ToFinancialItemId { get; set; }         // FK — destination financial item
    public decimal AmountOMR { get; set; }
    
    public ReallocationStatus Status { get; set; }      // Pending | Approved
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public string? ApprovalNote { get; set; }
}
```

**`ReallocationStatus` enum:**
```csharp
public enum ReallocationStatus
{
    Pending  = 0,   // Auto-generated, awaiting external TD signature + Staff status update
    Approved = 1
}
```

**Annual plan sessions don't currently generate reallocations** because annual plan items have a single funding budget by design. Casual courses are the only ones with the 3-scenario funding split that triggers reallocation. Phase 4B-α reflects this — `BudgetReallocation.CasualCourseId` is required (not nullable). If annual plan parity is needed later, we'd add `SessionId?` and a check constraint.

### 2.5 GeographicalLocation — read-only access

No schema change to the HR table. We add it to GTMS's read access by:
- Configuring it in `TrainingDbContext` with `ExcludeFromMigrations = true` (same pattern as `Employee`/`Rank`)
- Adding a small read-only repository wrapper in GTMS Application layer

```csharp
// In TrainingDbContext
public DbSet<GeographicalLocation> GeographicalLocations { get; set; }

// In GeographicalLocationConfiguration
b.ToTable("HrGeographicalLocations", excludeFromMigrations: true);
b.HasKey(x => x.Id);
// Don't define columns — HR owns the schema
```

### 2.6 Cleanup — none needed

No prior data to clean. These are net-new entities + the TrainingProvider extension's safe defaults handle existing rows.

---

## 3. Domain layer

### 3.1 New domain service — `BudgetReallocationGenerator`

**File:** `src/YourApp.Domain/Training/Managers/BudgetReallocationGenerator.cs`

This is the heart of Phase 4B-α. Walks the casual course's financial items + scenario rules and produces reallocation rows.

```csharp
public class BudgetReallocationGenerator : IDomainService
{
    private readonly IRepository<CasualCourse, Guid> _courseRepo;
    private readonly IRepository<CasualCourseFinancialItem, Guid> _itemRepo;
    private readonly IRepository<BudgetReallocation, Guid> _reallocRepo;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// Called by CoursePaymentAppService.ConfirmAsync after a payment transitions to Confirmed.
    /// For scenarios 2 and 3, generates one BudgetReallocation row per relevant financial item.
    /// Idempotent — does nothing if reallocations already exist for this payment.
    /// </summary>
    public async Task GenerateForCasualCoursePaymentAsync(
        Guid coursePaymentId,
        Guid casualCourseId)
    {
        // Idempotency guard
        var alreadyGenerated = await _reallocRepo.AnyAsync(
            r => r.CoursePaymentId == coursePaymentId);
        if (alreadyGenerated) return;

        var course = await _courseRepo.GetAsync(casualCourseId);
        
        // Scenario 1 — FundingSource covers everything → no reallocation needed
        if (course.FundingScenario == FundingScenario.FundingSourceCoversAll)
            return;
        
        if (course.FundingScenario == null)
            throw new BusinessException("Training:Reallocation:ScenarioNotSet");

        if (string.IsNullOrEmpty(course.FundingSourceVoteCode))
            throw new BusinessException("Training:Reallocation:VoteCodeRequired");

        var items = await _itemRepo.GetListAsync(
            i => i.CasualCourseId == casualCourseId,
            includeDetails: true);

        foreach (var item in items)
        {
            // Scenario 2 — only items with Source=FinancialItem reallocate
            //              (CourseCost stays on FundingSource)
            // Scenario 3 — every item reallocates (FinancialItem covers all)
            if (course.FundingScenario == FundingScenario.FundingSourceCoversCourse
                && item.Source == FinancialAmountSource.FundingSource)
            {
                continue;  // skip — this item is paid from FundingSource directly
            }

            // Sum across all rank rows for this item
            var amount = item.Ranks.Sum(r => r.SubtotalOMR);
            if (amount <= 0) continue;  // empty rows skipped

            var realloc = new BudgetReallocation
            {
                Id = _guidGenerator.Create(),
                TenantId = _currentTenant.Id ?? Guid.Empty,
                CasualCourseId = casualCourseId,
                CoursePaymentId = coursePaymentId,
                FundingSourceVoteCode = course.FundingSourceVoteCode,
                ToFinancialItemId = item.FinancialItemId,
                AmountOMR = amount,
                Status = ReallocationStatus.Pending
            };
            await _reallocRepo.InsertAsync(realloc);
        }
    }
}
```

**Properties:**
- **Idempotent** — re-running for the same `coursePaymentId` does nothing. Important if Finance accidentally clicks Confirm twice.
- **Scenario-aware** — uses the existing `FundingScenario` enum and `FinancialAmountSource` from Patch 3 work.
- **Audit-friendly** — `FundingSourceVoteCode` is copied (not joined) so reallocations are stable even if the casual course's funding source value changes later.
- **Uses sums, not individual rank rows** — one reallocation per `FinancialItem`, not per rank. The rank breakdown lives in `CasualCourseFinancialItemRanks`; reallocation is at the item level.

### 3.2 Reuse existing domain services

- `FundingScenarioSourceResolver` (Patch 3) — not directly called by 4B-α but the data it produced (the `Source` column) drives reallocation logic.
- `FinancialItemDefaultResolver` — unused in 4B-α.
- `EmployeeResolver` — used to resolve nominees during travel payment entry.

### 3.3 No validator changes for casual courses

Phase 4B-α doesn't change the Phase 4A casual-course state machine. All payment + reallocation activity happens **after** `THApproved` and doesn't transition the casual course's status. The casual course stays at `THApproved` permanently.

---

## 4. Application layer

### 4.1 New AppService — `TrainingProviderAppService` (extension)

The existing AppService gets new behaviors for the extended fields. Critical rule: **Nebras fields are not in the create/update DTO at all**, so frontend cannot send them.

```csharp
public class CreateUpdateTrainingProviderDto
{
    public string ProviderNameAr { get; set; }
    public string ProviderNameEn { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    
    // NEW
    public ProviderScope Scope { get; set; }
    public Guid? CountryId { get; set; }
    
    // INTENTIONALLY ABSENT:
    //   IsFromNebras, NebrasId, IsApproved, AverageRating, TotalRatings
}
```

**Validation in AppService.CreateAsync / UpdateAsync:**
```csharp
if (input.Scope != ProviderScope.Internal && input.CountryId == null)
    throw new BusinessException("Training:TrainingProvider:CountryRequiredForExternal");
```

**Read DTOs** include all fields including `IsFromNebras` and `NebrasId` (read-only display).

### 4.2 New AppService — `GeographicalLocationAppService` (read-only)

Small lookup service for the cascading dropdowns.

| Endpoint | Method | Route | Purpose | Permission |
|---|---|---|---|---|
| `GetCountriesAsync` | `GET` | `/api/app/geographical-locations/countries` | Returns all rows where `LocationParentId IS NULL` | Authenticated |
| `GetCitiesByCountryAsync` | `GET` | `/api/app/geographical-locations/cities/{countryId}` | Returns rows where `LocationParentId = countryId` | Authenticated |

DTO:
```csharp
public class GeographicalLocationDto : EntityDto<Guid>
{
    public string ArabicName { get; set; }
    public string EnglishName { get; set; }
    public Guid? LocationParentId { get; set; }
}
```

No create/update/delete endpoints — HR owns the table.

### 4.3 New AppService — `TravelAllowancePaymentAppService`

| Endpoint | Method | Route | Purpose | Role |
|---|---|---|---|---|
| Create | `POST` | `/api/app/travel-allowance-payments` | Finance creates a draft payment row | Finance |
| GetList | `GET` | `/api/app/travel-allowance-payments` | Filtered list | Finance, Staff (read) |
| Get | `GET` | `/api/app/travel-allowance-payments/{id}` | Detail | Finance, Staff |
| Update | `PUT` | `/api/app/travel-allowance-payments/{id}` | Edit while Draft | Finance |
| Delete | `DELETE` | `/api/app/travel-allowance-payments/{id}` | Remove while Draft | Finance |
| Confirm | `POST` | `/api/app/travel-allowance-payments/{id}/confirm` | Finalize → Status=Confirmed | Finance |

**State guards:**
- Update + Delete only allowed when `Status == Draft`
- Confirm only allowed when `Status == Draft`
- Once Confirmed, payment is immutable (financial audit requirement)

**Server-computed fields:**
- `TotalOMR = TicketAmountOMR + TravelAllowanceOMR + ClothingAllowanceOMR + InsuranceOMR + VisaFeesOMR` — computed on every save, never trusted from client.
- `PersonnelType` derived from `Nomination.Employee.Rank.IsOfficer` — auto-populated, not editable.

**Filter input shape:**
```csharp
public class TravelAllowancePaymentGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? SessionId { get; set; }
    public Guid? CasualCourseId { get; set; }
    public PaymentStatus? Status { get; set; }
    public Guid? NominationId { get; set; }
}
```

### 4.4 New AppService — `CoursePaymentAppService`

| Endpoint | Method | Route | Purpose | Role |
|---|---|---|---|---|
| Create | `POST` | `/api/app/course-payments` | Finance creates draft | Finance |
| GetList | `GET` | `/api/app/course-payments` | Filtered list | Finance, Staff |
| Get | `GET` | `/api/app/course-payments/{id}` | Detail | Finance, Staff |
| Update | `PUT` | `/api/app/course-payments/{id}` | Edit while Draft | Finance |
| Delete | `DELETE` | `/api/app/course-payments/{id}` | Remove while Draft | Finance |
| Confirm | `POST` | `/api/app/course-payments/{id}/confirm` | Finalize → triggers reallocation | Finance |
| UploadInvoice | `POST` | `/api/app/course-payments/{id}/upload-invoice` (multipart) | Upload PDF to BlobStoring | Finance |
| DownloadInvoice | `GET` | `/api/app/course-payments/{id}/download-invoice` | Stream the file back | Finance, Staff |

**Confirm flow (the important one):**
```csharp
public async Task<CoursePaymentDto> ConfirmAsync(Guid id)
{
    var payment = await _repo.GetAsync(id);
    
    if (payment.Status != PaymentStatus.Draft)
        throw new BusinessException("Training:CoursePayment:InvalidStatusTransition");
    
    if (string.IsNullOrEmpty(payment.InvoiceBlobName))
        throw new BusinessException("Training:CoursePayment:InvoiceRequired");
    
    payment.Status = PaymentStatus.Confirmed;
    payment.ConfirmedAt = Clock.Now;
    payment.ConfirmedById = CurrentUser.Id;
    
    // FIRE THE REALLOCATION GENERATOR — ONLY FOR CASUAL COURSES
    if (payment.CasualCourseId.HasValue)
    {
        await _reallocGenerator.GenerateForCasualCoursePaymentAsync(
            coursePaymentId: payment.Id,
            casualCourseId: payment.CasualCourseId.Value);
    }
    
    return ObjectMapper.Map<CoursePayment, CoursePaymentDto>(payment);
}
```

**Annual plan sessions don't trigger reallocation** — see §2.4 reasoning.

### 4.5 New AppService — `BudgetReallocationAppService`

Read + status-update only. Records are auto-generated; users never create them manually.

| Endpoint | Method | Route | Purpose | Role |
|---|---|---|---|---|
| GetList | `GET` | `/api/app/budget-reallocations` | Filtered list | Staff, TD |
| Get | `GET` | `/api/app/budget-reallocations/{id}` | Detail | Staff, TD |
| MarkApproved | `POST` | `/api/app/budget-reallocations/{id}/mark-approved` | Staff updates status to Approved (TD has externally signed paperwork) | Staff |

**No Create / Update / Delete endpoints** — manual creation is forbidden.

**MarkApproved DTO:**
```csharp
public class MarkReallocationApprovedDto
{
    public Guid? ApprovedById { get; set; }     // The TD who signed externally — Staff selects from user list
    public string? ApprovalNote { get; set; }
}
```

State guard: only allowed when `Status == Pending`. Once Approved, immutable.

### 4.6 File storage configuration

**File:** `src/YourApp.Domain/Training/Storage/CourseInvoiceContainer.cs`

```csharp
[BlobContainerName("training-course-invoices")]
public class CourseInvoiceContainer { }
```

**Wire in module — `YourAppDomainModule.cs` or `YourAppApplicationModule.cs`:**

```csharp
public override async Task ConfigureServicesAsync(ServiceConfigurationContext context)
{
    // Read setting at startup. Falls back to a default if not configured.
    var settingProvider = context.Services
        .BuildServiceProviderFromFactory()
        .GetRequiredService<ISettingProvider>();
    
    var basePath = await settingProvider.GetOrNullAsync("Gtms.Files.Path") 
                   ?? Path.Combine(Path.GetTempPath(), "gtms-files");
    
    Configure<AbpBlobStoringOptions>(options =>
    {
        options.Containers.Configure<CourseInvoiceContainer>(c =>
        {
            c.UseFileSystem(fs =>
            {
                fs.BasePath = basePath;
            });
        });
    });
}
```

**Setting registration — `GtmsSettingDefinitionProvider.cs`:**

```csharp
public class GtmsSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(new SettingDefinition(
            name: "Gtms.Files.Path",
            defaultValue: Path.Combine(Path.GetTempPath(), "gtms-files"),
            displayName: L("Setting:Gtms.Files.Path"),
            description: L("Setting:Gtms.Files.Path:Description"),
            isVisibleToClients: false  // server-only setting
        ));
    }
}
```

**Per-tenant isolation is automatic** — `BlobStoring.FileSystem` writes to `{basePath}/{tenantId}/{containerName}/{blobName}`. ABP handles the tenant subfolder.

**Important:** the BasePath directory must exist and be writable by the IIS/Kestrel process. Document this in deployment notes.

---

## 5. Frontend

### 5.1 PAGE 4.5 — Travel Allowance Payments

**Route:** `/training/payments/travel-allowances`
**Role:** Finance Officer (write), Staff (read)
**Component:** `dx-data-grid`

Grid columns from roadmap section 4.5:

| # | Column | Field | Width |
|---|---|---|---|
| 1 | Employee | NominationId → Employee.NameAr | 180px |
| 2 | Course | Session.Course.NameAr OR CasualCourse.TenantCourse.NameAr | 180px |
| 3 | Type | PersonnelType (Officer/Enlisted badge) | 80px |
| 4 | Tickets | TicketAmountOMR | 100px |
| 5 | Travel Allowance | TravelAllowanceOMR | 100px |
| 6 | Clothing | ClothingAllowanceOMR | 100px |
| 7 | Insurance | InsuranceOMR | 100px |
| 8 | Visa | VisaFeesOMR | 100px |
| 9 | Total | TotalOMR (calculated, server-side) | 100px |
| 10 | Status | PaymentStatus badge | 100px |

**Top filter bar:**
- Course type (Session / CasualCourse / Both)
- Status (Draft / Confirmed)
- Specific course picker (optional)

**Payment dialog (create/edit):**
- NominationId — `dx-select-box`, populated from approved nominations of the chosen course
- PersonnelType — auto-filled, read-only
- Five amount inputs — `dx-number-box`, each shows OMR + USD (USD = OMR / ExchangeRate, display only)
- Notes — `dx-text-area`

**Action buttons per row:** Edit, Delete (Draft only); Confirm (Draft only); View (always)

### 5.2 PAGE 4.6 — Course Payments

**Route:** `/training/payments/courses`
**Role:** Finance Officer
**Component:** `dx-data-grid`

Grid columns:

| # | Column | Field | Width |
|---|---|---|---|
| 1 | Course | (joined name) | 200px |
| 2 | Provider | TrainingProvider.ProviderNameAr | 150px |
| 3 | Invoice Amount | InvoiceAmountOMR | 120px |
| 4 | Nebras Amount | NebrasAmountOMR | 120px |
| 5 | Invoice Date | InvoiceDate | 100px |
| 6 | File | InvoiceBlobName (icon if uploaded, with download link) | 60px |
| 7 | Status | PaymentStatus | 100px |

**Payment dialog:**
- Course picker (Session or CasualCourse) — `dx-select-box`
- TrainingProvider — `dx-select-box`
- Invoice amount — `dx-number-box` (OMR + USD)
- Nebras amount — `dx-number-box` (OMR + USD)
- Invoice date — `dx-date-box`
- Invoice file — drag-drop upload; calls `UploadInvoiceAsync` on file selection; shows progress + filename after upload
- Notes

**Confirm button:** disabled until `InvoiceBlobName` is set (server enforces; client shows tooltip).

**On Confirm:** UI shows toast "Payment confirmed — budget reallocations generated for X items" if scenario 2 or 3.

### 5.3 PAGE 4.7 — Budget Reallocations

**Route:** `/training/payments/reallocations`
**Role:** Staff (write status), TD (view)
**Component:** `dx-data-grid`

Grid columns from roadmap:

| # | Column | Field | Width |
|---|---|---|---|
| 1 | Casual Course | CasualCourseId → display name | 200px |
| 2 | From | FundingSourceVoteCode | 120px |
| 3 | To | ToFinancialItemId → FinancialItem.NameAr | 150px |
| 4 | Amount | AmountOMR | 120px |
| 5 | Status | Pending / Approved badge | 100px |
| 6 | Created | CreatedAt | 120px |
| 7 | Approved by | ApprovedById → user.UserName | 120px |

**Filter:** by Status, by CasualCourse (search), by FinancialItem (search), by date range.

**Action button per Pending row:** "Mark Approved" — opens dialog, Staff selects the TD user + adds approval note + confirms.

**Read-only banner at top of page:**
> "Reallocations are auto-generated when a course payment is confirmed for casual courses with funding scenarios 2 or 3. They cannot be created manually. After TD signs the paperwork externally, click 'Mark Approved' to update status."

### 5.4 PAGE 4.4-extended — TrainingProviders Management

Existing page from Phase 3 needs minor updates:

- New form fields: `Scope` dropdown (3 options), `CountryId` cascading dropdown
- Country dropdown calls `GetCountriesAsync` on page load
- City dropdown — **defer to Phase 4B-β** (only PriceQuote uses cities; provider has country only)
- New read-only badge if `IsFromNebras` is true: "From Nebras" pill, with NebrasId visible in tooltip

---

## 6. Localization

Add to both `ar.json` + `en.json`:

```json
{
  "Training:TravelAllowancePayment:InvalidStatusTransition": "...",
  "Training:CoursePayment:InvalidStatusTransition": "...",
  "Training:CoursePayment:InvoiceRequired": "...",
  "Training:Reallocation:ScenarioNotSet": "...",
  "Training:Reallocation:VoteCodeRequired": "...",
  "Training:TrainingProvider:CountryRequiredForExternal": "...",
  "Training.ProviderScope.Internal": "...",
  "Training.ProviderScope.Local": "...",
  "Training.ProviderScope.International": "...",
  "Training.PaymentStatus.Draft": "...",
  "Training.PaymentStatus.Confirmed": "...",
  "Training.ReallocationStatus.Pending": "...",
  "Training.ReallocationStatus.Approved": "...",
  "Setting:Gtms.Files.Path": "...",
  "Setting:Gtms.Files.Path:Description": "..."
}
```

---

## 7. Permissions

New permission group `TrainingPayments`:

```csharp
public static class TrainingPaymentsPermissions
{
    public const string GroupName = "TrainingPayments";
    
    public const string TravelAllowance = GroupName + ".TravelAllowance";
    public const string TravelAllowance_Create = TravelAllowance + ".Create";
    public const string TravelAllowance_Update = TravelAllowance + ".Update";
    public const string TravelAllowance_Delete = TravelAllowance + ".Delete";
    public const string TravelAllowance_Confirm = TravelAllowance + ".Confirm";
    
    public const string CoursePayments = GroupName + ".CoursePayments";
    public const string CoursePayments_Create = CoursePayments + ".Create";
    public const string CoursePayments_Update = CoursePayments + ".Update";
    public const string CoursePayments_Delete = CoursePayments + ".Delete";
    public const string CoursePayments_Confirm = CoursePayments + ".Confirm";
    public const string CoursePayments_Upload = CoursePayments + ".UploadInvoice";
    public const string CoursePayments_Download = CoursePayments + ".DownloadInvoice";
    
    public const string Reallocations = GroupName + ".Reallocations";
    public const string Reallocations_View = Reallocations + ".View";
    public const string Reallocations_MarkApproved = Reallocations + ".MarkApproved";
}
```

Default grants:
- Finance role → all `TravelAllowance.*`, all `CoursePayments.*`
- Staff role → all `Reallocations.*`, `TravelAllowance` read, `CoursePayments` read
- TD role → `Reallocations.View` only

---

## 8. Verification checklist

**Schema & migrations**
- [ ] Migration v4_8_0 runs cleanly on all 3 demo tenants
- [ ] `IsFromNebras`, `NebrasId`, `Scope`, `CountryId` columns added to `TrnTrainingProviders`
- [ ] Existing TrainingProvider rows default to `Scope = Local` (safe), `IsFromNebras = false`, `NebrasId = null`, `CountryId = null`
- [ ] `TrnTravelAllowancePayments` table created with check constraint on polymorphic parent
- [ ] `TrnCoursePayments` table created with same check constraint
- [ ] `TrnBudgetReallocations` table created
- [ ] `HrGeographicalLocations` configured with `excludeFromMigrations: true` — no migration touches it
- [ ] `PaymentStatus` and `ReallocationStatus` and `ProviderScope` enums all exist

**Domain**
- [ ] `BudgetReallocationGenerator` registered as IDomainService
- [ ] Generator is idempotent (re-run for same payment generates nothing extra)
- [ ] Generator skips scenario 1 entirely
- [ ] Generator skips items where `Source = FundingSource` under scenario 2
- [ ] Generator processes all items under scenario 3
- [ ] Generator copies `FundingSourceVoteCode` (not joins) for audit trail
- [ ] `FundingSourceVoteCode` validation throws when null

**File storage**
- [ ] `Gtms.Files.Path` setting registered with default value
- [ ] `BlobStoring.FileSystem` configured with BasePath from setting
- [ ] `CourseInvoiceContainer` defined with proper attribute
- [ ] Invoice upload writes to `{path}/{tenantId}/training-course-invoices/{blobName}`
- [ ] Invoice download streams correctly
- [ ] Multi-tenant isolation verified — tenant A cannot read tenant B's blobs

**AppServices**
- [ ] `TrainingProviderAppService` accepts `Scope` + `CountryId` in DTOs
- [ ] `IsFromNebras` and `NebrasId` are NOT in create/update DTOs (read-only)
- [ ] Validation throws when `Scope != Internal` and `CountryId == null`
- [ ] `GeographicalLocationAppService` returns countries (parent IS NULL)
- [ ] `GeographicalLocationAppService` returns cities for given country
- [ ] `TravelAllowancePaymentAppService` computes `TotalOMR` server-side
- [ ] `TravelAllowancePaymentAppService.PersonnelType` auto-populated from nomination
- [ ] State guards on Update / Delete (Draft only)
- [ ] State guard on Confirm (Draft only)
- [ ] `CoursePaymentAppService.ConfirmAsync` rejects when no invoice uploaded
- [ ] `CoursePaymentAppService.ConfirmAsync` triggers `BudgetReallocationGenerator` for casual courses only
- [ ] `BudgetReallocationAppService` has no Create/Update/Delete endpoints
- [ ] `MarkApprovedAsync` rejects when not Pending

**Frontend**
- [ ] Proxy regenerated — all new types visible
- [ ] PAGE 4.5 grid renders with 10 columns; payment dialog works
- [ ] OMR + USD dual display works in number inputs
- [ ] PAGE 4.6 grid renders; invoice upload works; download works
- [ ] PAGE 4.6 Confirm button disabled until invoice uploaded
- [ ] PAGE 4.6 toast on confirm shows reallocation count for casual courses
- [ ] PAGE 4.7 grid renders; "Mark Approved" dialog works
- [ ] PAGE 4.7 read-only banner explains auto-generation
- [ ] TrainingProvider page shows Scope dropdown + Country cascade
- [ ] TrainingProvider page shows "From Nebras" badge when applicable

**Cross-cutting**
- [ ] Phase 4A casual course workflow not regressed
- [ ] Annual plan workflow untouched
- [ ] Settings UI (if exposed in admin) shows the new path setting
- [ ] Smoke test: create casual course → approve → confirm course payment for scenario 2 → verify reallocations created (one per non-CourseCost financial item)
- [ ] Smoke test: same for scenario 3 → verify reallocations created (one per ALL financial items)
- [ ] Smoke test: scenario 1 → verify NO reallocations created

---

## 9. Non-regression

- **Do not modify Phase 4A casual course state machine.** Payments + reallocations are post-`THApproved` activities, no transitions involved.
- **Do not modify `FundingScenario` enum or `FundingScenarioSourceResolver`.** Reused.
- **Do not write directly to HR `GeographicalLocations`, `Employee`, or `Rank` tables.** Read-only.
- **Do not let frontend send `IsFromNebras` or `NebrasId`** — these are server-side fields populated by future sync logic.
- **Do not add price quote functionality.** Phase 4B-β scope.
- **Do not add session-level BudgetReallocation generation.** Annual plan sessions use single-budget funding; reallocation is casual-course-specific.
- **Do not couple TravelAllowancePayment to CoursePayment.** They're independent — Finance can confirm one without the other.

---

## 10. Commit / checkpoint

Suggested commit message:
```
feat(payments): TravelAllowancePayment + CoursePayment + BudgetReallocation auto-gen

Phase 4B-α — post-approval execution layer.

- Three new entities: TravelAllowancePayment, CoursePayment, BudgetReallocation
- Both payment entities polymorphic (SessionId? + CasualCourseId?, check constraint)
- BudgetReallocationGenerator domain service — auto-fires on CoursePayment.Confirm
  Scenario 1: no reallocations
  Scenario 2: reallocates non-CourseCost items only
  Scenario 3: reallocates all items
- TrainingProvider extended: IsFromNebras, NebrasId (sync-driven, hidden from UI),
  ProviderScope enum (Internal/Local/International), CountryId (FK to HR)
- GeographicalLocationAppService for country/city dropdowns (read-only)
- BlobStoring.FileSystem invoice upload, BasePath from Gtms.Files.Path setting
- 4 new AppServices, ~24 endpoints, 4 frontend pages
- Annual plan sessions reuse the same payment screens (polymorphic from day 1)

Locked decisions:
  Q1 → split into 4B-α (this) and 4B-β (price quotes)
  Q2 → polymorphic payment entities from day one
  Q3 → BlobStoring.FileSystem driven by ABP Setting Management
  Q-A → Nebras fields sync-driven only, never user-editable
  Q-B → ProviderScope = Internal/Local/International enum
  Q-C → PriceQuote location fields deferred to 4B-β

Closes #<issue>
```

Checkpoint v4.7.0 → v4.8.0 under "Scope Evolution":

| # | What Changed | Notes |
|---|---|---|
| S11 | Post-approval execution available | Finance Officers can now process payments against approved courses |
| S12 | Auto-reallocation working | Casual courses with scenarios 2 or 3 generate audit-trail reallocation records when course payment confirms |
| S13 | Polymorphic payments | Same TravelAllowancePayment + CoursePayment entities serve both annual plan sessions and casual courses |
| S14 | TrainingProvider becomes Nebras-aware | Sync fields ready for future integration; user-facing scope + country additions live now |
| S15 | File storage live | Course invoices stored via BlobStoring.FileSystem with setting-driven base path |

---

## 11. Migration risk callout

**Highest-risk items** in deployment order:

1. **The migration adds 4 columns to `TrnTrainingProviders`.** Existing rows get safe defaults (`Scope = Local`, `IsFromNebras = false`). If your existing providers should actually be `International`, that's a manual data update. Worth checking before running.

2. **`Gtms.Files.Path` setting must be configured pre-deployment.** If unset, the default falls to `%TEMP%/gtms-files` which is per-machine and won't persist invoices reliably. Document the setting in deployment runbook.

3. **`BasePath` directory must be writable by the app process.** Failure mode is silent on first request — check write permissions during smoke test.

4. **Polymorphic check constraint** on payment tables — both payments enforce "exactly one of SessionId / CasualCourseId is set." If anyone manually inserts data without going through AppService, this constraint catches violations. Tests should cover both arms.

5. **Idempotency of reallocation generator** — verified by `AnyAsync` check at start. Stress-test by clicking Confirm twice rapidly on a payment to ensure no duplicates.

---

## 12. Phase 4B-β preview (for context, not in scope)

After 4B-α is stable, 4B-β delivers:
- `PriceQuote.CasualCourseId` arm of polymorphic relationship (already declared, never used)
- `PriceQuote.CountryId` + `CityId` columns
- PAGE 4.8 — quote comparison UI, side-by-side cards
- `CasualCourse.SelectedPriceQuoteId` becomes writable from PAGE 4.8
- City cascade dropdown wired into provider editing (Phase 4B-α only does country)

Estimated 4B-β: ~1.5 weeks. Independent of 4B-α once both are merged.

---

*End of Phase 4B-α Implementation Prompt — v1.0*
*Estimated effort: ~6 days Claude Code work. Single biggest risk: file storage configuration on the deployment server.*
*Target file count: 1 migration, ~16 backend files (3 new entities, 1 entity extension, 4 AppServices, 1 domain service, EF configs, mappers, settings/permissions/localization), ~10 frontend files (3 new pages + provider page edits + cascade service + proxy regen).*
