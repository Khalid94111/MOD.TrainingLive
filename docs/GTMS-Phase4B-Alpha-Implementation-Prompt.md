# GTMS Phase 4B-α — Implementation Prompt (Pre-Execution Preparation)

**Version:** v1.1 — supersedes v1.0. Three corrections from OmanAI feedback:
- Adds `IsSelected` column to PriceQuote (was assumed to exist from Phase 3 — it doesn't)
- HR table renamed `HrGeographicalLocations` → `Mst_GeographicalLocations`
- Non-regression note added re HR table naming consistency
**Target version:** v4.7.0 → v4.8.0
**Scope:** Pre-execution preparation layer for casual courses + annual plan sessions. New entities, AppServices, and pages enabling Staff to select winning price quotes, confirm actual dates with the provider, and issue travel instructions. **Excludes payments and reallocations** — those are Phase 4B-β.
**Base code:** Phase 4A v4.7.0 (Patches 1–5 all merged).
**Companion docs:**
- `docs/GTMS-BRD-v4_0.md` — base business rules
- `docs/GTMS-Dev-Roadmap-v3.md` — Phase 4 section
- `docs/GTMS-Phase4B-Beta-Implementation-Prompt.md` — Phase 4B-β (downstream consumer)
- `docs/GTMS-Project-Checkpoint-v4_7_0.md` — current state recovery

---

## 1. Why this phase exists

After `THApproved`, three execution tracks fire — but they have a **strict ordering** that the original 4B-α scope ignored:

```
THApproved
   ↓
1. Staff collects price quotes        (THIS PHASE — 4B-α)
2. Staff picks winner                  (THIS PHASE)
3. Staff contacts provider, sets       (THIS PHASE)
   ActualStartDate + ActualEndDate    
4. Staff issues TravelInstruction      (THIS PHASE)
   based on confirmed dates           
   ↓
5. Finance disburses travel allowance  (Phase 4B-β — depends on TravelInstruction)
6. Course runs (no system event)      
7. Finance pays course invoice         (Phase 4B-β — depends on SelectedPriceQuoteId)
8. BudgetReallocation auto-generates   (Phase 4B-β)
```

Phase 4B-α delivers steps 1–4 — everything Staff does between TH approval and the moment Finance starts moving money. Without 4B-α complete, Finance has no quote winner, no provider, no travel dates, and no visa/insurance state to read from. **4B-α is the prerequisite for 4B-β.**

### 1.1 Locked decisions

| # | Decision | Implementation |
|---|---|---|
| Q1 | Phase 4B split, ordered by dependency | 4B-α (this) ships first, then 4B-β |
| Q2 | Cross-course entities are polymorphic | PriceQuote and TravelInstruction both have `CasualCourseId? + SessionId?` with check constraints |
| Q-A | TrainingProvider Nebras fields are sync-driven | `IsFromNebras` + `NebrasId` exist on entity but never in create/update DTOs |
| Q-B | TrainingProvider scope = enum | `ProviderScope` enum: `Internal / Local / International` |
| Q-C | PriceQuote location fields | `CountryId` + `CityId` columns, populated by Staff at quote entry |
| — | TravelInstruction is one-per-course (not per-nominee) | All nominees on a course travel together — single shared instruction |
| — | `ActualStartDate` + `ActualEndDate` written by Staff at quote-winner selection | Not after course runs — set when provider confirms dates |
| — | `OverrideTravelDays` lives on TravelInstruction | Staff edits; Finance reads only |
| — | Calculated travel days default = `ArrivalBackDate - DepartureDate + 1` | Server computes; Staff overrides if needed |
| — | `GeographicalLocation` is read-only HR reference | Same pattern as Employee/Rank |

---

## 2. Schema changes

### 2.1 Extend `TrainingProvider`

**Migration:** `v4_8_0_PreExecutionLayer` (single migration covering all schema changes in this phase)

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

migrationBuilder.CreateIndex(
    name: "IX_TrnTrainingProviders_CountryId",
    table: "TrnTrainingProviders",
    column: "CountryId");
```

**`ProviderScope` enum (new):**
```csharp
public enum ProviderScope
{
    Internal      = 0,
    Local         = 1,
    International = 2
}
```

**Updated entity properties:**
- `IsFromNebras: bool` — sync-driven, never user-editable
- `NebrasId: string?` — sync-driven, never user-editable
- `Scope: ProviderScope` — Staff sets at create time
- `CountryId: Guid?` — FK to HR GeographicalLocation; required when Scope ≠ Internal

### 2.2 Add columns to `CasualCourses`

```csharp
migrationBuilder.AddColumn<DateTime>(
    name: "ActualStartDate",
    table: "TrnCasualCourses",
    type: "datetime2",
    nullable: true);

migrationBuilder.AddColumn<DateTime>(
    name: "ActualEndDate",
    table: "TrnCasualCourses",
    type: "datetime2",
    nullable: true);
```

Both nullable — populated only when Staff selects the winning quote (after confirming dates with provider).

`SelectedPriceQuoteId` was declared in Patch 1 (Phase 4A) as nullable FK; this phase makes it writable via the new `SelectPriceQuoteAsync` endpoint.

### 2.3 Extend `PriceQuotes` for casual course arm + location + IsSelected

```csharp
// CasualCourseId nullable FK was added in Patch 1; activated here

migrationBuilder.AddColumn<bool>(
    name: "IsSelected",
    table: "TrnPriceQuotes",
    type: "bit",
    nullable: false,
    defaultValue: false);
// Was assumed to exist from Phase 3 but doesn't — added now.
// Default false: existing session quotes have no winner picked yet.

migrationBuilder.AddColumn<Guid>(
    name: "CountryId",
    table: "TrnPriceQuotes",
    type: "uniqueidentifier",
    nullable: true);

migrationBuilder.AddColumn<Guid>(
    name: "CityId",
    table: "TrnPriceQuotes",
    type: "uniqueidentifier",
    nullable: true);

migrationBuilder.CreateIndex(
    name: "IX_TrnPriceQuotes_CountryId",
    table: "TrnPriceQuotes",
    column: "CountryId");
migrationBuilder.CreateIndex(
    name: "IX_TrnPriceQuotes_CityId",
    table: "TrnPriceQuotes",
    column: "CityId");

// Polymorphic check constraint
migrationBuilder.Sql(@"
    ALTER TABLE TrnPriceQuotes ADD CONSTRAINT CK_PriceQuote_PolymorphicParent
    CHECK (
        (SessionId IS NOT NULL AND CasualCourseId IS NULL) OR 
        (SessionId IS NULL AND CasualCourseId IS NOT NULL)
    )
");
```

`IsSelected` is server-controlled — only set via `CasualCourseAppService.SelectPriceQuoteAsync` (or its session-equivalent if/when added). Frontend cannot write it directly.

`CountryId` nullable, `CityId` nullable. FKs to GeographicalLocation declared in EF config but not enforced as DB-level FK (HR owns the table).

### 2.4 New entity — `TravelInstructions`

```csharp
public class TravelInstruction : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid TenantId { get; set; }
    
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    
    public DateTime DepartureDate { get; set; }       // Leaving Oman
    public DateTime ArrivalDate { get; set; }         // Arriving at host country
    public DateTime ReturnDate { get; set; }          // Leaving host country
    public DateTime ArrivalBackDate { get; set; }     // Arriving back in Oman
    
    public bool VisaRequired { get; set; }
    public string? VisaNotes { get; set; }
    public bool InsuranceArranged { get; set; }
    public string? InsuranceProvider { get; set; }
    public bool TicketsBooked { get; set; }
    public string? TicketReference { get; set; }
    
    public int CalculatedTravelDays { get; set; }    // Server-computed
    public int? OverrideTravelDays { get; set; }     // Staff override
    
    public TravelInstructionStatus Status { get; set; }
}

public enum TravelInstructionStatus
{
    Draft     = 0,
    Issued    = 1,
    Cancelled = 2
}
```

Migration:
```csharp
migrationBuilder.CreateTable(
    name: "TrnTravelInstructions",
    columns: table => new
    {
        Id = table.Column<Guid>(nullable: false),
        TenantId = table.Column<Guid>(nullable: false),
        CasualCourseId = table.Column<Guid>(nullable: true),
        SessionId = table.Column<Guid>(nullable: true),
        DepartureDate = table.Column<DateTime>(nullable: false),
        ArrivalDate = table.Column<DateTime>(nullable: false),
        ReturnDate = table.Column<DateTime>(nullable: false),
        ArrivalBackDate = table.Column<DateTime>(nullable: false),
        VisaRequired = table.Column<bool>(nullable: false, defaultValue: false),
        VisaNotes = table.Column<string>(maxLength: 500, nullable: true),
        InsuranceArranged = table.Column<bool>(nullable: false, defaultValue: false),
        InsuranceProvider = table.Column<string>(maxLength: 200, nullable: true),
        TicketsBooked = table.Column<bool>(nullable: false, defaultValue: false),
        TicketReference = table.Column<string>(maxLength: 100, nullable: true),
        CalculatedTravelDays = table.Column<int>(nullable: false),
        OverrideTravelDays = table.Column<int>(nullable: true),
        Status = table.Column<int>(nullable: false, defaultValue: 0),
        // ... audit columns from FullAuditedAggregateRoot
    });

migrationBuilder.CreateIndex(
    name: "IX_TrnTravelInstructions_CasualCourseId",
    table: "TrnTravelInstructions",
    column: "CasualCourseId",
    unique: true,
    filter: "[CasualCourseId] IS NOT NULL");

migrationBuilder.CreateIndex(
    name: "IX_TrnTravelInstructions_SessionId",
    table: "TrnTravelInstructions",
    column: "SessionId",
    unique: true,
    filter: "[SessionId] IS NOT NULL");

migrationBuilder.Sql(@"
    ALTER TABLE TrnTravelInstructions ADD CONSTRAINT CK_TravelInstruction_PolymorphicParent
    CHECK (
        (CasualCourseId IS NOT NULL AND SessionId IS NULL) OR 
        (CasualCourseId IS NULL AND SessionId IS NOT NULL)
    )
");
```

### 2.5 GeographicalLocation — read-only HR reference

```csharp
// In TrainingDbContext
public DbSet<GeographicalLocation> GeographicalLocations { get; set; }

// Configuration
b.ToTable("Mst_GeographicalLocations", excludeFromMigrations: true);
b.HasKey(x => x.Id);
// Don't define columns — HR module owns the schema
```

```csharp
public class GeographicalLocation : Entity<Guid>
{
    public string ArabicName { get; set; }
    public string EnglishName { get; set; }
    public Guid? LocationParentId { get; set; }
}
```

The `Mst_` prefix follows the HR module's master-data table naming convention. Verify other HR-referenced tables (`Employee`, `Rank`, `OrgUnit` etc.) use consistent naming when reviewing.

---

## 3. Domain layer

### 3.1 New domain service — `TravelDayCalculator`

```csharp
public class TravelDayCalculator : IDomainService
{
    /// <summary>
    /// Default = inclusive day count from departure (leaving Oman) to arrival back.
    /// Staff can override via TravelInstruction.OverrideTravelDays.
    /// </summary>
    public int CalculateDays(DateTime departureDate, DateTime arrivalBackDate)
    {
        if (arrivalBackDate < departureDate)
            throw new BusinessException("Training:TravelInstruction:InvalidDateOrder");
            
        return (int)(arrivalBackDate.Date - departureDate.Date).TotalDays + 1;
    }
    
    /// <summary>
    /// Returns days actually used for allowance calculation.
    /// Override wins if set, otherwise computed default.
    /// </summary>
    public int GetEffectiveTravelDays(TravelInstruction instruction)
    {
        return instruction.OverrideTravelDays ?? instruction.CalculatedTravelDays;
    }
}
```

Used at:
- `TravelInstructionAppService.CreateOrUpdateAsync` — recomputes when dates change
- (Phase 4B-β) `TravelAllowancePaymentAppService` — reads `GetEffectiveTravelDays`

### 3.2 New domain service — `PriceQuoteValidator`

```csharp
public class PriceQuoteValidator : IDomainService
{
    public async Task ValidateForSelectionAsync(Guid casualCourseId, Guid quoteId)
    {
        var course = await _courseRepo.GetAsync(casualCourseId);
        if (course.Status != CasualCourseStatus.THApproved)
            throw new BusinessException("Training:PriceQuote:CourseNotApproved");
            
        var quote = await _quoteRepo.GetAsync(quoteId);
        if (quote.CasualCourseId != casualCourseId)
            throw new BusinessException("Training:PriceQuote:WrongCourse");
    }
}
```

### 3.3 No state machine changes

Casual course stays at `THApproved`. Quote selection, date confirmation, and travel instruction issuance are post-approval activities that don't transition the course's status.

---

## 4. Application layer

### 4.1 `TrainingProviderAppService` — extend

**Critical: Nebras fields excluded from create/update DTOs.**

```csharp
public class CreateUpdateTrainingProviderDto
{
    public string ProviderNameAr { get; set; }
    public string ProviderNameEn { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    
    public ProviderScope Scope { get; set; }
    public Guid? CountryId { get; set; }
    
    // INTENTIONALLY ABSENT: IsFromNebras, NebrasId, IsApproved, AverageRating, TotalRatings
}

public class TrainingProviderDto : EntityDto<Guid>
{
    // ... existing fields ...
    public bool IsFromNebras { get; set; }       // read-only
    public string? NebrasId { get; set; }        // read-only
    public ProviderScope Scope { get; set; }
    public Guid? CountryId { get; set; }
    public string? CountryNameAr { get; set; }   // joined from GeographicalLocation
    public string? CountryNameEn { get; set; }
}
```

**Validation:**
```csharp
if (input.Scope != ProviderScope.Internal && !input.CountryId.HasValue)
    throw new BusinessException("Training:TrainingProvider:CountryRequiredForExternal");
```

### 4.2 New AppService — `GeographicalLocationAppService`

| # | Method | Route | Purpose |
|---|---|---|---|
| 1 | `GET` | `/api/app/geographical-locations/countries` | Rows where `LocationParentId IS NULL` |
| 2 | `GET` | `/api/app/geographical-locations/cities/{countryId}` | Rows where `LocationParentId = countryId` |

```csharp
public class GeographicalLocationDto : EntityDto<Guid>
{
    public string ArabicName { get; set; }
    public string EnglishName { get; set; }
    public Guid? LocationParentId { get; set; }
}
```

No create/update/delete — HR owns the table.

### 4.3 `PriceQuoteAppService` — extend for casual courses

| # | Method | Route | Purpose | Role |
|---|---|---|---|---|
| 1 | `POST` | `/api/app/price-quotes` | Create (with `CasualCourseId` OR `SessionId`) | Staff |
| 2 | `GET` | `/api/app/price-quotes` | Filtered list | Staff, TD, TH |
| 3 | `GET` | `/api/app/price-quotes/{id}` | Detail | Staff, TD, TH |
| 4 | `PUT` | `/api/app/price-quotes/{id}` | Edit (when not yet selected) | Staff |
| 5 | `DELETE` | `/api/app/price-quotes/{id}` | Remove (when not yet selected) | Staff |

```csharp
public class CreateUpdatePriceQuoteDto
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid TrainingProviderId { get; set; }
    public decimal QuotedPriceOMR { get; set; }
    public string? Notes { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? CityId { get; set; }
}
```

**Validation:**
- Exactly one of `CasualCourseId`/`SessionId` set
- If `CityId` set, must be a child of `CountryId`
- `IsSelected` is server-controlled — only set via `SelectPriceQuoteAsync`

### 4.4 New AppService — `TravelInstructionAppService`

| # | Method | Route | Purpose | Role |
|---|---|---|---|---|
| 1 | `PUT` | `/api/app/travel-instructions` | Upsert (one per course) | Staff |
| 2 | `GET` | `/api/app/travel-instructions/by-course?casualCourseId=...` | Get the single instruction | Staff, Finance, TD, TH |
| 3 | `POST` | `/api/app/travel-instructions/{id}/issue` | Status `Draft → Issued` | Staff |
| 4 | `POST` | `/api/app/travel-instructions/{id}/cancel` | Status → `Cancelled` | Staff |

**`CreateOrUpdateAsync` body:**
```csharp
public async Task<TravelInstructionDto> CreateOrUpdateAsync(
    CreateUpdateTravelInstructionDto input)
{
    if ((input.CasualCourseId.HasValue) == (input.SessionId.HasValue))
        throw new BusinessException("Training:TravelInstruction:OnePolymorphicParentRequired");

    var existing = await _repo.FirstOrDefaultAsync(i =>
        (input.CasualCourseId != null && i.CasualCourseId == input.CasualCourseId) ||
        (input.SessionId != null && i.SessionId == input.SessionId));

    var instruction = existing ?? new TravelInstruction
    {
        Id = GuidGenerator.Create(),
        TenantId = CurrentTenant.Id ?? Guid.Empty,
        CasualCourseId = input.CasualCourseId,
        SessionId = input.SessionId,
        Status = TravelInstructionStatus.Draft
    };

    if (existing != null && existing.Status != TravelInstructionStatus.Draft)
        throw new BusinessException("Training:TravelInstruction:CannotEditAfterIssued");

    if (!(input.DepartureDate <= input.ArrivalDate &&
          input.ArrivalDate <= input.ReturnDate &&
          input.ReturnDate <= input.ArrivalBackDate))
    {
        throw new BusinessException("Training:TravelInstruction:InvalidDateOrder");
    }

    instruction.DepartureDate = input.DepartureDate;
    instruction.ArrivalDate = input.ArrivalDate;
    instruction.ReturnDate = input.ReturnDate;
    instruction.ArrivalBackDate = input.ArrivalBackDate;
    instruction.VisaRequired = input.VisaRequired;
    instruction.VisaNotes = input.VisaNotes;
    instruction.InsuranceArranged = input.InsuranceArranged;
    instruction.InsuranceProvider = input.InsuranceProvider;
    instruction.TicketsBooked = input.TicketsBooked;
    instruction.TicketReference = input.TicketReference;
    instruction.OverrideTravelDays = input.OverrideTravelDays;

    instruction.CalculatedTravelDays = _travelDayCalculator.CalculateDays(
        input.DepartureDate, input.ArrivalBackDate);

    if (existing == null)
        await _repo.InsertAsync(instruction);

    return ObjectMapper.Map<TravelInstruction, TravelInstructionDto>(instruction);
}
```

**`IssueAsync` validation:** `Status = Draft`, dates valid, `TicketsBooked = true`, visa fields complete if `VisaRequired`, insurance fields complete if `InsuranceArranged`.

### 4.5 New endpoint on `CasualCourseAppService` — `SelectPriceQuoteAsync`

| Method | Route |
|---|---|
| `POST` | `/api/app/casual-courses/{id}/select-price-quote` |

```csharp
public class SelectPriceQuoteDto
{
    public Guid PriceQuoteId { get; set; }
    public DateTime ActualStartDate { get; set; }
    public DateTime ActualEndDate { get; set; }
}

public async Task<CasualCourseDto> SelectPriceQuoteAsync(
    Guid casualCourseId, SelectPriceQuoteDto input)
{
    var course = await _repo.GetAsync(casualCourseId);
    
    if (course.Status != CasualCourseStatus.THApproved)
        throw new BusinessException("Training:CasualCourse:NotApprovedYet");
    
    if (input.ActualEndDate < input.ActualStartDate)
        throw new BusinessException("Training:CasualCourse:InvalidActualDates");

    await _quoteValidator.ValidateForSelectionAsync(casualCourseId, input.PriceQuoteId);
    
    if (course.SelectedPriceQuoteId.HasValue)
    {
        var oldQuote = await _quoteRepo.GetAsync(course.SelectedPriceQuoteId.Value);
        oldQuote.IsSelected = false;
    }
    
    var newQuote = await _quoteRepo.GetAsync(input.PriceQuoteId);
    newQuote.IsSelected = true;
    
    course.SelectedPriceQuoteId = input.PriceQuoteId;
    course.ActualStartDate = input.ActualStartDate;
    course.ActualEndDate = input.ActualEndDate;
    
    return ObjectMapper.Map<CasualCourse, CasualCourseDto>(course);
}
```

### 4.6 Endpoint count for Phase 4B-α

| AppService | New | Modified |
|---|---|---|
| `TrainingProviderAppService` | 0 | DTO-shape only |
| `GeographicalLocationAppService` | 2 | — |
| `PriceQuoteAppService` | 0 | 5 (DTO + casual arm) |
| `TravelInstructionAppService` | 4 | — |
| `CasualCourseAppService.SelectPriceQuoteAsync` | 1 | — |
| **Total new endpoints** | **7** | **5 modified** |

---

## 5. Frontend

### 5.1 Updated page — `TrainingProviders` management

- New form fields: `Scope` dropdown (3 options), cascading `Country` dropdown
- Country dropdown calls `GetCountriesAsync` on page load
- City dropdown — defer to Phase 4B-β
- Read-only badge "From Nebras" displayed when `IsFromNebras = true`, `NebrasId` in tooltip
- `Scope` chip (gray Internal / blue Local / amber International)

### 5.2 New page — `Price Quotes` (PAGE 4.8)

**Routes:**
- `/training/casual-courses/:id/price-quotes`
- `/training/sessions/:id/price-quotes`

**Layout:**

```
┌──────────────────────────────────────────────────────────────┐
│ Casual Course: [name]                          [Status: TH ✓] │
│ Estimated Total: 5,000 OMR                                   │
└──────────────────────────────────────────────────────────────┘

┌── Price Quotes ──────────────────────────────────────────────┐
│ [+ Add Quote]                                                │
│                                                               │
│ ┌─────────────────────────────────────────────────────────┐  │
│ │ Provider              Country   City    Price    Action │  │
│ ├─────────────────────────────────────────────────────────┤  │
│ │ ABC Training (Local)  Oman     Muscat   4,800   ✓ Win   │  │
│ │ XYZ Institute (Intl)  UAE      Dubai    5,200   [Pick]  │  │
│ │ DEF Center (Intl)     UK       London   6,000   [Pick]  │  │
│ └─────────────────────────────────────────────────────────┘  │
│                                                               │
│ [Pick Winner: ABC] → opens dialog for ActualStart/EndDate    │
└──────────────────────────────────────────────────────────────┘
```

**Quote dialog (Add/Edit):**
- TrainingProvider — `dx-select-box`, filtered by `IsActive = true`
- Country — cascading dropdown
- City — cascading dropdown (filtered by country)
- Quoted Price OMR — number input
- Notes — textarea

**Pick Winner dialog:**
- Selected quote summary (read-only)
- ActualStartDate — `dx-date-box`, required
- ActualEndDate — `dx-date-box`, required, must be ≥ ActualStartDate
- Confirmation note: "This will lock the price quote selection. Travel instructions can now be issued."

**Variance indicator:** when picking, if `quote.QuotedPriceOMR > 1.10 × estimated`, show amber warning chip. Not blocking — visual only.

### 5.3 New page — `Travel Instructions`

**Routes:**
- `/training/casual-courses/:id/travel-instructions`
- `/training/sessions/:id/travel-instructions`

```
┌──────────────────────────────────────────────────────────────┐
│ Travel Instructions — [Course Name]            [Status: Draft]│
└──────────────────────────────────────────────────────────────┘

┌── Travel Dates ──────────────────────────────────────────────┐
│  Departure (leaving Oman):     [📅 ___]                      │
│  Arrival (at host country):    [📅 ___]                      │
│  Return (leaving host):        [📅 ___]                      │
│  Arrival back (in Oman):       [📅 ___]                      │
│                                                               │
│  ⓘ Calculated travel days: 8                                 │
│  Override if needed: [____]  (leave blank to use calculated) │
└──────────────────────────────────────────────────────────────┘

┌── Visa ──────────────────────────────────────────────────────┐
│  ☐ Visa required                                              │
│  Notes: [________________________]                           │
└──────────────────────────────────────────────────────────────┘

┌── Insurance ─────────────────────────────────────────────────┐
│  ☐ Insurance arranged                                         │
│  Provider: [________________________]                        │
└──────────────────────────────────────────────────────────────┘

┌── Tickets ───────────────────────────────────────────────────┐
│  ☐ Tickets booked                                             │
│  Reference: [________________________]                       │
└──────────────────────────────────────────────────────────────┘

[💾 Save Draft]    [📤 Issue]    [⛔ Cancel]
```

**Behavior:**
- "Save Draft" persists with `Status = Draft` — Staff returns later
- "Issue" requires all four dates valid, `TicketsBooked = true`, visa+insurance fields complete if applicable. Sets `Status = Issued`. Locks editing
- `CalculatedTravelDays` displayed as live badge
- Override field shows chip when populated ("Override: 9 days · default 8")

### 5.4 Updated — Casual Course detail panel

```
┌── Execution Status ──────────────────────────────────────────┐
│ Selected Quote: ABC Training (4,800 OMR)         [View]      │
│ Actual Dates:   2026-04-15 → 2026-04-22                     │
│                                                               │
│ Variance: −200 OMR vs estimated (under budget) ✓             │
│                                                               │
│ Travel Instruction: Issued ✓                     [View]      │
└──────────────────────────────────────────────────────────────┘
```

Renders only after a quote is selected. Read-only.

---

## 6. Localization

Add to `ar.json` + `en.json`:

```json
{
  "Training:TrainingProvider:CountryRequiredForExternal": "...",
  "Training:PriceQuote:CourseNotApproved": "...",
  "Training:PriceQuote:WrongCourse": "...",
  "Training:PriceQuote:CityNotInCountry": "...",
  "Training:TravelInstruction:OnePolymorphicParentRequired": "...",
  "Training:TravelInstruction:InvalidDateOrder": "...",
  "Training:TravelInstruction:CannotEditAfterIssued": "...",
  "Training:CasualCourse:NotApprovedYet": "...",
  "Training:CasualCourse:InvalidActualDates": "...",
  
  "Training.ProviderScope.Internal": "...",
  "Training.ProviderScope.Local": "...",
  "Training.ProviderScope.International": "...",
  
  "Training.TravelInstructionStatus.Draft": "...",
  "Training.TravelInstructionStatus.Issued": "...",
  "Training.TravelInstructionStatus.Cancelled": "..."
}
```

---

## 7. Permissions

```csharp
public static class TrainingExecutionPermissions
{
    public const string GroupName = "TrainingExecution";
    
    public const string PriceQuotes = GroupName + ".PriceQuotes";
    public const string PriceQuotes_Create = PriceQuotes + ".Create";
    public const string PriceQuotes_Edit = PriceQuotes + ".Edit";
    public const string PriceQuotes_Delete = PriceQuotes + ".Delete";
    public const string PriceQuotes_Select = PriceQuotes + ".Select";
    
    public const string TravelInstructions = GroupName + ".TravelInstructions";
    public const string TravelInstructions_Edit = TravelInstructions + ".Edit";
    public const string TravelInstructions_Issue = TravelInstructions + ".Issue";
    public const string TravelInstructions_Cancel = TravelInstructions + ".Cancel";
}
```

Default grants:
- Staff → all permissions
- TD, TH → read-only access (handled at AppService layer, not separate permissions)
- Finance → read access on TravelInstructions only (for Phase 4B-β)

---

## 8. Verification checklist

**Schema & migration**
- [ ] Migration v4_8_0 runs cleanly on all 3 demo tenants
- [ ] TrainingProvider gets 4 new columns; existing rows default sanely
- [ ] CasualCourse gets ActualStartDate + ActualEndDate (nullable)
- [ ] PriceQuote gets CountryId + CityId + check constraint
- [ ] TrnTravelInstructions table created with unique indexes per polymorphic arm + check constraint
- [ ] Mst_GeographicalLocations configured with `excludeFromMigrations: true`
- [ ] PriceQuote.IsSelected column added with default false

**Domain**
- [ ] `TravelDayCalculator` registered; calculates inclusive day count
- [ ] `PriceQuoteValidator` blocks selection when course not THApproved

**AppServices**
- [ ] `TrainingProviderAppService.CreateAsync` rejects non-Internal scope without CountryId
- [ ] `TrainingProviderDto` includes Nebras fields read-only; excludes them from create/update
- [ ] `GeographicalLocationAppService` returns countries (parent IS NULL) and cities by parent
- [ ] `PriceQuoteAppService` accepts both polymorphic arms
- [ ] `PriceQuoteAppService` validates `CityId` is a child of `CountryId`
- [ ] `TravelInstructionAppService.CreateOrUpdateAsync` is upsert (one per course)
- [ ] `TravelInstructionAppService.CreateOrUpdateAsync` rejects edits after Issued
- [ ] `IssueAsync` blocks when prerequisites unmet (tickets, visa, insurance)
- [ ] `SelectPriceQuoteAsync` validates and persists ActualStart + ActualEnd
- [ ] `SelectPriceQuoteAsync` flips IsSelected on previous winner if any

**Frontend**
- [ ] Proxy regenerated — all new types visible
- [ ] TrainingProviders page shows Scope chip + Country cascade
- [ ] Price Quotes page allows add/edit/delete + winner selection
- [ ] Pick Winner dialog requires actual dates
- [ ] Travel Instructions page renders dates section + 3 requirement sections
- [ ] CalculatedTravelDays auto-recomputes on date change
- [ ] Override field labeled clearly with default fallback shown
- [ ] Issue button blocked until prerequisites met
- [ ] CasualCourse detail page shows Execution Status section after quote selected

**Cross-cutting**
- [ ] Phase 4A casual course workflow not regressed
- [ ] Annual plan workflow not regressed
- [ ] Smoke test: TH-approve → add 3 quotes → pick winner → set actual dates → issue travel instruction → verify all statuses
- [ ] Smoke test: pick a different quote (switching winners) — old IsSelected flips false, new flips true

---

## 9. Non-regression

- **Do not modify casual course state machine** — quote selection and travel instruction issuance are post-`THApproved` activities.
- **Do not let frontend send `IsFromNebras` or `NebrasId`** — server-side fields only.
- **Do not skip `excludeFromMigrations: true`** on `Mst_GeographicalLocations` — HR module owns the table.
- **Do not enforce DB FKs on cross-module references.** Use `HasOne(...).WithMany().HasForeignKey(...)` without DB-level constraint.
- **Verify HR table naming convention before running migration.** All HR cross-module references (Employee, Rank, OrgUnit, GeographicalLocation) should use `Mst_` prefix per the established HR pattern. If any prior phase used a different prefix, raise it before running this migration.
- **Do not couple TravelInstruction to TravelAllowancePayment yet.** Phase 4B-β consumes; this phase only writes.
- **Do not change cost gate location at TD/TH** — Phase 4A locked decision Q4 stands.
- **Do not modify `FundingScenarioSourceResolver`** — reused unchanged.

---

## 10. Commit / checkpoint

Suggested commit message:
```
feat(execution-prep): price quotes + actual dates + travel instructions (Phase 4B-α)

Pre-execution preparation layer for the post-TH-approval workflow.

Changes:
- TrainingProvider: IsFromNebras + NebrasId (sync-driven), Scope enum,
  CountryId (FK to HR GeographicalLocation)
- CasualCourse: ActualStartDate + ActualEndDate (writable via SelectPriceQuoteAsync)
- PriceQuote: CasualCourseId arm activated, CountryId + CityId columns,
  polymorphic check constraint
- TravelInstructions: new polymorphic entity (one per casual course or session),
  4 dates + visa + insurance + tickets + travel days
- TravelDayCalculator domain service: server-side day computation
- PriceQuoteValidator domain service: blocks selection when not THApproved
- New endpoints: GeographicalLocation read-only, TravelInstruction CRUD,
  SelectPriceQuoteAsync on CasualCourse
- 3 new pages: extended TrainingProviders, Price Quotes (PAGE 4.8),
  Travel Instructions (new)

Locked decisions:
  Q1: 4B split with α first (preparation) and β next (payments)
  Q2: polymorphic from day one (PriceQuote, TravelInstruction)
  Q-A: Nebras fields sync-driven only
  Q-B: ProviderScope enum (Internal/Local/International)
  Q-C: Country/City on PriceQuote
  Q-extra: TravelInstruction is one-per-course (group travel)

Closes #<issue>
```

Checkpoint v4.7.0 → v4.8.0 entries under "Scope Evolution":

| # | Item | Note |
|---|---|---|
| S11 | Pre-execution layer shipped | Quote selection + actual dates + travel instructions |
| S12 | TravelInstruction = single per course | All nominees travel together — confirmed by OmanAI |
| S13 | TrainingProvider becomes Nebras-aware | Sync fields ready; user-facing Scope + Country live now |
| S14 | PriceQuote polymorphic arm activated | `CasualCourseId` declared in Patch 1, now usable |
| S15 | ActualStartDate + ActualEndDate written at quote selection | Not after course runs — set when provider confirms dates |

---

## 11. Phase 4B-β preview (downstream)

After 4B-α stable, Phase 4B-β delivers (prompt at `docs/GTMS-Phase4B-Beta-Implementation-Prompt.md`):
- TravelAllowancePayment — consumes `TravelInstruction.GetEffectiveTravelDays`
- CoursePayment — consumes `SelectedPriceQuoteId` for provider info
- BudgetReallocation + Generator
- BlobStoring.FileSystem invoice upload (path from `Gtms.Files.Path` setting)
- 3 new pages (4.5, 4.6, 4.7)

Estimated 4B-β: ~6 days. Cannot start until 4B-α is merged.

---

*End of Phase 4B-α Implementation Prompt — v1.1*
*Supersedes v1.0 — adds IsSelected column to PriceQuote, corrects HR table name to Mst_GeographicalLocations*
*Estimated effort: ~6 days Claude Code work. Lower risk than 4B-β because no auto-generation logic and no file storage. Single biggest item: Travel Instructions page UX.*
*Target file count: 1 migration, ~12 backend files (1 new entity, 2 new domain services, 4 AppServices, mappers, EF configs, permissions, localization), ~6 frontend files (3 new pages + provider page edits + cascade service + proxy regen).*
