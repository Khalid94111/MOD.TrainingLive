# GTMS Phase 4B-α — API List (Gate 2)

**Version:** v1.0
**Source:** Implementation Prompt v1.1
**Status:** Awaiting OmanAI approval
**Total endpoints:** 7 new + 5 modified across 4 AppServices

---

## Summary

| AppService | New endpoints | Modified endpoints | Purpose |
|---|---|---|---|
| `TrainingProviderAppService` | 0 | DTO-shape only (5 endpoints) | Add Scope + CountryId support; expose Nebras read-only |
| `GeographicalLocationAppService` | 2 | — | Read-only HR cascade for Country + City dropdowns |
| `PriceQuoteAppService` | 0 | 5 (polymorphic + location) | Activate `CasualCourseId` arm; add Country/City |
| `TravelInstructionAppService` | 4 | — | New entity CRUD + state transitions |
| `CasualCourseAppService` | 1 (new) | 0 | `SelectPriceQuoteAsync` writes winner + actual dates |
| **Total** | **7 new** | **10 modified** | — |

**Cumulative endpoint count after Phase 4B-α:** 25 (Phase 4A) + 7 = **32 casual-course-related endpoints**.

---

## 1. `TrainingProviderAppService` — DTO shape changes (5 endpoints)

The existing AppService keeps its endpoints. Only DTO contracts change.

| # | Method | Route | Purpose | Role | Request DTO | Response DTO | Permission |
|---|---|---|---|---|---|---|---|
| 1 | `POST` | `/api/app/training-providers` | Create provider; **NO Nebras fields accepted** | Staff | `CreateUpdateTrainingProviderDto` (extended: `Scope`, `CountryId`) | `TrainingProviderDto` (extended) | `Training.TrainingProviders.Create` |
| 2 | `GET` | `/api/app/training-providers` | Paged list; filter by `Scope?`, `CountryId?`, `IsFromNebras?`, `IsActive?` | all | `TrainingProviderGetListInput` (extended) | `PagedResultDto<TrainingProviderListItemDto>` | `Training.TrainingProviders.Default` |
| 3 | `GET` | `/api/app/training-providers/{id}` | Detail | all | — | `TrainingProviderDto` (includes Nebras read-only) | `Training.TrainingProviders.Default` |
| 4 | `PUT` | `/api/app/training-providers/{id}` | Update; **Nebras fields rejected** if sent | Staff | `CreateUpdateTrainingProviderDto` | `TrainingProviderDto` | `Training.TrainingProviders.Edit` |
| 5 | `DELETE` | `/api/app/training-providers/{id}` | Delete (only if no quotes/payments reference it) | Staff | — | — | `Training.TrainingProviders.Delete` |

**DTO shapes:**

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
    
    // INTENTIONALLY ABSENT (server-controlled or sync-driven):
    //   IsFromNebras, NebrasId, IsApproved, AverageRating, TotalRatings
}

public class TrainingProviderDto : EntityDto<Guid>
{
    public string ProviderNameAr { get; set; }
    public string ProviderNameEn { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal? AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    
    // NEW (read-only display)
    public bool IsFromNebras { get; set; }
    public string? NebrasId { get; set; }
    public ProviderScope Scope { get; set; }
    public Guid? CountryId { get; set; }
    public string? CountryNameAr { get; set; }    // joined from GeographicalLocation
    public string? CountryNameEn { get; set; }
}

public class TrainingProviderGetListInput : PagedAndSortedResultRequestDto
{
    public ProviderScope? Scope { get; set; }
    public Guid? CountryId { get; set; }
    public bool? IsFromNebras { get; set; }
    public bool? IsActive { get; set; }
    public string? Search { get; set; }
}
```

---

## 2. `GeographicalLocationAppService` — 2 NEW endpoints

Read-only access to HR's `Mst_GeographicalLocations` table. Powers Country + City cascade dropdowns.

| # | Method | Route | Purpose | Role | Request DTO | Response DTO | Permission |
|---|---|---|---|---|---|---|---|
| 6 | `GET` | `/api/app/geographical-locations/countries` | All rows where `LocationParentId IS NULL`. Cached server-side per request — small dataset. | Authenticated | — | `ListResultDto<GeographicalLocationDto>` | (auth only) |
| 7 | `GET` | `/api/app/geographical-locations/cities/{countryId}` | All rows where `LocationParentId = countryId` | Authenticated | — | `ListResultDto<GeographicalLocationDto>` | (auth only) |

**No create / update / delete** — HR module owns the table.

**DTO:**
```csharp
public class GeographicalLocationDto : EntityDto<Guid>
{
    public string ArabicName { get; set; }
    public string EnglishName { get; set; }
    public Guid? LocationParentId { get; set; }
}
```

---

## 3. `PriceQuoteAppService` — 5 endpoints (modified for polymorphic + location)

Existing endpoints kept; DTO + validation extended for casual-course arm and location.

| # | Method | Route | Purpose | Role | Request DTO | Response DTO | Permission |
|---|---|---|---|---|---|---|---|
| 8 | `POST` | `/api/app/price-quotes` | Create quote (with `CasualCourseId` OR `SessionId`, exactly one) | Staff | `CreateUpdatePriceQuoteDto` | `PriceQuoteDto` | `TrainingExecution.PriceQuotes.Create` |
| 9 | `GET` | `/api/app/price-quotes` | Filter by `CasualCourseId?` or `SessionId?` (mutually exclusive) | Staff, TD, TH | `PriceQuoteGetListInput` | `ListResultDto<PriceQuoteDto>` | (auth only) |
| 10 | `GET` | `/api/app/price-quotes/{id}` | Detail with provider + country/city joined | Staff, TD, TH | — | `PriceQuoteDto` | (auth only) |
| 11 | `PUT` | `/api/app/price-quotes/{id}` | Edit when `IsSelected = false` | Staff | `CreateUpdatePriceQuoteDto` | `PriceQuoteDto` | `TrainingExecution.PriceQuotes.Edit` |
| 12 | `DELETE` | `/api/app/price-quotes/{id}` | Delete when `IsSelected = false` | Staff | — | — | `TrainingExecution.PriceQuotes.Delete` |

**Key validation rules in AppService:**

- Exactly one of `CasualCourseId` / `SessionId` set (server-side, not client-trusted)
- If `CityId` set, must be a child of `CountryId` (server validates via DB lookup)
- `CountryId` referenced in `Mst_GeographicalLocations`
- `IsSelected` is server-controlled — silently ignored if frontend sends it; only set via `SelectPriceQuoteAsync` (endpoint #16 below)
- `Update` and `Delete` rejected when quote is `IsSelected = true` (Staff must un-select first by picking a different winner)

**DTOs:**
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

public class PriceQuoteDto : EntityDto<Guid>
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid TrainingProviderId { get; set; }
    public string TrainingProviderName { get; set; }     // joined
    public ProviderScope ProviderScope { get; set; }     // joined
    public decimal QuotedPriceOMR { get; set; }
    public string? Notes { get; set; }
    public Guid? CountryId { get; set; }
    public string? CountryNameAr { get; set; }            // joined
    public Guid? CityId { get; set; }
    public string? CityNameAr { get; set; }               // joined
    public bool IsSelected { get; set; }
    public DateTime CreationTime { get; set; }
}

public class PriceQuoteGetListInput
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
}
```

---

## 4. `TravelInstructionAppService` — 4 NEW endpoints

| # | Method | Route | Purpose | Role | Request DTO | Response DTO | Permission |
|---|---|---|---|---|---|---|---|
| 13 | `PUT` | `/api/app/travel-instructions` | **Upsert** — one per course. Creates if no row exists for the polymorphic parent; updates otherwise. | Staff | `CreateUpdateTravelInstructionDto` | `TravelInstructionDto` | `TrainingExecution.TravelInstructions.Edit` |
| 14 | `GET` | `/api/app/travel-instructions/by-course?casualCourseId={id}` OR `?sessionId={id}` | Get the single instruction (one of the two query params required) | Staff, Finance, TD, TH | — | `TravelInstructionDto?` (null if not yet created) | (auth only) |
| 15 | `POST` | `/api/app/travel-instructions/{id}/issue` | `Status: Draft → Issued`. Validates prerequisites: dates valid, `TicketsBooked = true`, visa fields if `VisaRequired`, insurance fields if `InsuranceArranged` | Staff | — | `TravelInstructionDto` | `TrainingExecution.TravelInstructions.Issue` |
| 16 | `POST` | `/api/app/travel-instructions/{id}/cancel` | `Status → Cancelled` | Staff | — | `TravelInstructionDto` | `TrainingExecution.TravelInstructions.Cancel` |

**State machine:**
```
Draft   ──issue──▶ Issued   ──cancel──▶ Cancelled (terminal)
  │
  └─── cancel ────▶ Cancelled (terminal)

Edit allowed only when Status == Draft.
```

**Server logic in upsert:**

- If existing row found by `(CasualCourseId | SessionId)` and its status is not `Draft`, throw `Training:TravelInstruction:CannotEditAfterIssued`
- Validate date order: `DepartureDate ≤ ArrivalDate ≤ ReturnDate ≤ ArrivalBackDate`
- Recompute `CalculatedTravelDays = (ArrivalBackDate.Date - DepartureDate.Date).TotalDays + 1` server-side (never trust client)
- `OverrideTravelDays` accepted as-is from input; server doesn't validate range (Staff knows their context)

**DTOs:**
```csharp
public class CreateUpdateTravelInstructionDto
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    
    public DateTime DepartureDate { get; set; }
    public DateTime ArrivalDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public DateTime ArrivalBackDate { get; set; }
    
    public bool VisaRequired { get; set; }
    public string? VisaNotes { get; set; }
    public bool InsuranceArranged { get; set; }
    public string? InsuranceProvider { get; set; }
    public bool TicketsBooked { get; set; }
    public string? TicketReference { get; set; }
    
    public int? OverrideTravelDays { get; set; }
}

public class TravelInstructionDto : EntityDto<Guid>
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    
    public DateTime DepartureDate { get; set; }
    public DateTime ArrivalDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public DateTime ArrivalBackDate { get; set; }
    
    public bool VisaRequired { get; set; }
    public string? VisaNotes { get; set; }
    public bool InsuranceArranged { get; set; }
    public string? InsuranceProvider { get; set; }
    public bool TicketsBooked { get; set; }
    public string? TicketReference { get; set; }
    
    public int CalculatedTravelDays { get; set; }
    public int? OverrideTravelDays { get; set; }
    public int EffectiveTravelDays { get; set; }   // computed in mapper: OverrideTravelDays ?? CalculatedTravelDays
    
    public TravelInstructionStatus Status { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
}
```

---

## 5. `CasualCourseAppService.SelectPriceQuoteAsync` — 1 NEW endpoint

| # | Method | Route | Purpose | Role | Request DTO | Response DTO | Permission |
|---|---|---|---|---|---|---|---|
| 17 | `POST` | `/api/app/casual-courses/{id}/select-price-quote` | Atomic operation: set winning quote + write `ActualStartDate` + `ActualEndDate`. Flips `IsSelected` on previous winner if any. | Staff | `SelectPriceQuoteDto` | `CasualCourseDto` | `TrainingExecution.PriceQuotes.Select` |

**State guards:**
- Course must be `THApproved`
- Quote must reference this casual course (`PriceQuote.CasualCourseId == courseId`)
- `ActualEndDate ≥ ActualStartDate`

**Side effects (atomic — single transaction):**
1. Old `SelectedPriceQuoteId` (if any) → its quote's `IsSelected = false`
2. New quote's `IsSelected = true`
3. `course.SelectedPriceQuoteId = input.PriceQuoteId`
4. `course.ActualStartDate = input.ActualStartDate`
5. `course.ActualEndDate = input.ActualEndDate`

**No state transition** — course stays at `THApproved`.

**DTO:**
```csharp
public class SelectPriceQuoteDto
{
    public Guid PriceQuoteId { get; set; }
    public DateTime ActualStartDate { get; set; }
    public DateTime ActualEndDate { get; set; }
}
```

---

## Cross-cutting concerns

### Polymorphic parent rule

Every endpoint that operates on a polymorphic-parented entity (`PriceQuote`, `TravelInstruction`) enforces the rule **server-side**:
> Exactly one of `CasualCourseId` / `SessionId` must be set; the other must be null.

Frontend cannot bypass this — server validates on every Create/Update + enforced by DB check constraint as defense-in-depth.

### Tenant isolation

All endpoints respect `IMultiTenant`. ABP filter applies automatically. No custom code needed.

### Cross-module FK behavior

References to `Mst_GeographicalLocations` (HR-owned) are **declared in EF config only**, no DB-level FK. Same approach as `Employee` and `Rank`. Migration does not generate these constraints.

### Validation error codes (all keys)

| Key | Trigger |
|---|---|
| `Training:TrainingProvider:CountryRequiredForExternal` | `Scope ≠ Internal` and `CountryId == null` |
| `Training:PriceQuote:OnePolymorphicParentRequired` | Both `CasualCourseId` + `SessionId` set, or both null |
| `Training:PriceQuote:CityNotInCountry` | `CityId.LocationParentId != CountryId` |
| `Training:PriceQuote:CourseNotApproved` | Trying to select a quote on a course not at `THApproved` |
| `Training:PriceQuote:WrongCourse` | Quote's `CasualCourseId` doesn't match the course in route |
| `Training:PriceQuote:CannotEditSelected` | Update/Delete on a row where `IsSelected = true` |
| `Training:TravelInstruction:OnePolymorphicParentRequired` | Both polymorphic IDs set, or both null |
| `Training:TravelInstruction:InvalidDateOrder` | Dates not in chronological order |
| `Training:TravelInstruction:CannotEditAfterIssued` | Edit attempted when `Status ≠ Draft` |
| `Training:TravelInstruction:PrerequisitesNotMet` | Issue attempted with missing tickets / visa / insurance fields |
| `Training:CasualCourse:NotApprovedYet` | Selecting quote on a non-`THApproved` course |
| `Training:CasualCourse:InvalidActualDates` | `ActualEndDate < ActualStartDate` |

All keys also exist in `ar.json` and `en.json` per Phase 4A localization conventions.

---

## Permission group

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

**Default grants:**

| Role | TrainingProviders | PriceQuotes | TravelInstructions |
|---|---|---|---|
| Staff | Create / Edit / Delete | All (Create / Edit / Delete / Select) | All (Edit / Issue / Cancel) |
| TD | View only | View only | View only |
| TH | View only | View only | View only |
| Finance | View only | View only (read for context) | **View only** (read for Phase 4B-β allowance computation) |
| UTM, UGM, DM | View only | None | None |

`GeographicalLocationAppService` requires authentication only — no permission group (lookup data).

---

## Endpoint count derivation

| Source | Count |
|---|---|
| Phase 4A endpoints (after Patch 5) | 25 |
| New in Phase 4B-α | 7 |
| Modified in Phase 4B-α | 10 |
| **Total casual-course-touched endpoints after 4B-α** | **32** |

Annual plan + sessions endpoints: untouched count carries over.

---

## Approval checklist

- [ ] All 17 endpoints (5 modified + 7 new + 5 modified DTO-shape) reviewed
- [ ] Polymorphic parent rule clear on PriceQuote + TravelInstruction
- [ ] Server-controlled fields verified: `IsSelected`, `IsFromNebras`, `NebrasId`, `CalculatedTravelDays`
- [ ] Validation error keys match Phase 4A naming pattern (`Training:Entity:Error`)
- [ ] Permission group structure aligned with existing GTMS conventions
- [ ] Default role grants reasonable for each role
- [ ] No endpoint exposes sensitive data inappropriately

---

*End of Phase 4B-α API List — v1.0*
*Awaiting OmanAI approval to proceed to Gate 3 (Mockups)*
