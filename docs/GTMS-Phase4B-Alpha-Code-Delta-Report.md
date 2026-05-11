# GTMS Phase 4B-α — Backend Code Delta Report

**Version:** v1.0
**Date:** April 28, 2026
**Repo:** `D:\Training\MOD.Training`
**Scope:** Backend only — Domain.Shared / Domain / Application.Contracts / Application / EntityFrameworkCore
**Companion docs:**
- `GTMS-Phase4B-Alpha-Implementation-Prompt.md` v1.1 (source of scope)
- `GTMS-Phase4B-Alpha-API-List.md` v1.0 (Gate 2)
- `GTMS-Phase4B-Alpha-Mockup.html` (frontend reference; not consumed in this delta)

---

## 0. Defaulted decisions

The implementation prompt v1.1 was written from a slightly different baseline than the actual v4.7.0 codebase. Four defaults were taken to align with the running repo:

| # | Question | Decision | Rationale |
|---|---|---|---|
| D-01 | Table prefix — prompt uses `Trn*`, repo uses `App*` | **Keep `App*`** | Locked in Phase 3 Code Delta Report; all 12 prior migrations use `App*`. `TrainingConsts.DbTablePrefix = "App"`. |
| D-02 | HR table naming — prompt uses `Mst_GeographicalLocations` excluded from migrations; repo HR tables use plain `Hr*` and live in this DbContext | **Use `HrGeographicalLocations`, owned by Training DB** (user-confirmed in Section 1) | Matches existing `HrEmployees` / `HrRanks` pattern. Avoids the brittle "wait for HR module to ship" coupling implied by `excludeFromMigrations:true`. |
| D-03 | `PriceQuote.CasualCourseId` — prompt assumes Patch 1 added it; in fact it was never added | **Add it now** in v4.8.0 migration alongside making `SessionId` nullable | Confirmed via git history; no Patch 1 migration touches `AppPriceQuotes`. |
| D-04 | `PriceQuote` legacy pricing fields — prompt's casual DTO drops `PricingType` / `PricePerPerson` / `TotalPrice` / `ParticipantsCount` | **Keep legacy fields** (user-confirmed in Section 1) | Phase 3 sessions arm still uses them; dropping would be destructive to existing rows and break the Phase 3 sessions UI. New canonical price `QuotedPriceOMR` lives alongside. |

`TenantId` is `Guid?` on `TravelInstruction` (matching repo convention `IMultiTenant.TenantId`); the prompt's `Guid TenantId` was treated as a typo.

---

## 1. Files DELETED — none

No physical deletions. The legacy `PriceQuote(Guid id, Guid sessionId, …)` constructor is retained for the Phase 3 sessions arm.

---

## 2. Files MODIFIED (12)

### 2.1 `Domain.Shared/Training/Enums/` — two new enums (3 files affected)
Added two enum files — see §3.

### 2.2 `Domain/Training/Finance/TrainingProvider.cs`
Added four properties:
```csharp
public bool   IsFromNebras { get; set; }   // sync-driven; never user-editable
public string? NebrasId    { get; set; }   // sync-driven; never user-editable
public ProviderScope Scope { get; set; }   // Staff sets at create time
public Guid?  CountryId    { get; set; }   // FK → HrGeographicalLocations
```
Constructor sets `Scope = ProviderScope.Local` as a safe default for new rows.

### 2.3 `Domain/Training/Finance/PriceQuote.cs`
Polymorphic + new fields:
```csharp
public Guid? SessionId      { get; set; }   // was required; now nullable
public Guid? CasualCourseId { get; set; }   // new (Phase 4B-α arm)
public decimal QuotedPriceOMR { get; set; } // new — canonical for casual arm
public bool IsSelected     { get; set; }    // server-controlled
public Guid? CountryId     { get; set; }    // soft FK
public Guid? CityId        { get; set; }    // soft FK (must be child of CountryId)
```
Added `public PriceQuote(Guid id) : base(id)` ctor for casual-arm construction.
Legacy session-arm ctor + `CalculatePrices()` retained.

### 2.4 `Domain/Training/CasualCourses/CasualCourse.cs`
```csharp
public DateTime? ActualStartDate { get; set; }  // written by SelectPriceQuoteAsync
public DateTime? ActualEndDate   { get; set; }
```

### 2.5 `Domain/Training/DataSeeder/SeedIds.cs`
Added `GeographicalLocationIds` static class — 10 country GUIDs + 15 city GUIDs.

### 2.6 `Domain/Training/DataSeeder/GtmsDataSeeder.cs`
Injected `IGeographicalLocationDataSeeder` and wired one `ExecuteInUowAsync("GeographicalLocations", …)` call. (Active — not commented out, since the cascade dropdowns are blocked without it.)

### 2.7 `EntityFrameworkCore/Training/Configurations/HrReadOnlyConfiguration.cs`
Added EF mapping for `GeographicalLocation` → `HrGeographicalLocations`. Self-referencing FK on `LocationParentId` with `OnDelete(NoAction)` (parent → children cascade not appropriate for a hierarchy of master rows). Index on `LocationParentId`.

### 2.8 `EntityFrameworkCore/Training/TrainingDbContextModelCreatingExtensions.cs`
Three changes:
- `PriceQuote` config — `SessionId` now optional; new properties (`QuotedPriceOMR`, `IsSelected` default false); indexes on `CasualCourseId`, `CountryId`, `CityId`.
- `TrainingProvider` config — `IsFromNebras` default false, `NebrasId` max-length 100, `Scope` default `Local`; indexes on `CountryId`, `IsFromNebras`, `Scope`.
- `CasualCourse` config — `ActualStartDate?` + `ActualEndDate?` declared optional.
- New extension `ConfigurePreExecutionPhase4BAlpha()` — wires `TravelInstruction` table + unique-per-arm filtered indexes on `CasualCourseId` and `SessionId`.

### 2.9 `EntityFrameworkCore/EntityFrameworkCore/TrainingDbContext.cs`
Added `DbSet<GeographicalLocation> HrGeographicalLocations` and `DbSet<TravelInstruction> TravelInstructions`.

### 2.10 `Application.Contracts/Training/Permissions/TrainingPermissions.cs`
Added a new top-level static class `TrainingExecutionPermissions` (separate from `TrainingPermissions` per prompt §7) with `PriceQuotes` (Create/Edit/Delete/Select) and `TravelInstructions` (Edit/Issue/Cancel).

### 2.11 `Application.Contracts/Training/Permissions/TrainingPermissionDefinitionProvider.cs`
Registered the new `TrainingExecution` group with all 4 + 3 sub-permissions and localization keys.

### 2.12 `Application.Contracts/Training/CasualCourses/Dtos/CasualCourseDto.cs`
Added `ActualStartDate?` + `ActualEndDate?`. The `CasualCourseToDtoMapper` (Mapperly) auto-maps these.

### 2.13 `Application.Contracts/Training/CasualCourses/ICasualCourseAppService.cs`
Added `Task<CasualCourseDto> SelectPriceQuoteAsync(Guid id, SelectPriceQuoteDto input)`.

### 2.14 `Application.Contracts/Training/Finance/Dtos/TrainingProviderDto.cs`
Added `IsFromNebras`, `NebrasId`, `Scope`, `CountryId`, `CountryNameAr`, `CountryNameEn`. Country names are joined by `TrainingProviderAppService`, not by Mapperly.

### 2.15 `Application.Contracts/Training/Finance/Dtos/CreateUpdateTrainingProviderDto.cs`
Added `Scope` + `CountryId`. **Nebras fields intentionally absent** — comment in code reinforces server-side-only contract.

### 2.16 `Application.Contracts/Training/Finance/Dtos/TrainingProviderGetListInput.cs`
Added `Scope?`, `CountryId?`, `IsFromNebras?` filters.

### 2.17 `Application.Contracts/Training/Finance/Dtos/PriceQuoteDto.cs`
Added polymorphic ID columns + `QuotedPriceOMR` + location fields + joined display fields (`ProviderScope`, `CountryNameAr/En`, `CityNameAr/En`).

### 2.18 `Application.Contracts/Training/Finance/Dtos/CreateUpdatePriceQuoteDto.cs`
Restructured: `SessionId?` + `CasualCourseId?` (polymorphic), legacy fields kept optional, new `QuotedPriceOMR` + `CountryId?` + `CityId?`.

### 2.19 `Application.Contracts/Training/Finance/Dtos/PriceQuoteGetListInput.cs`
Added `CasualCourseId?` and `IsSelected?`.

### 2.20 `Application.Contracts/Training/Finance/IPriceQuoteAppService.cs` — unchanged
The interface inherits `ICrudAppService<…>`; existing 5+2 endpoints continue, all 5 of the CRUD operations now polymorphic-aware.

### 2.21 `Application/Training/Mapper/CreateUpdatePriceQuoteToEntityMapper.cs`
Rewritten: uses new `PriceQuote(Guid id)` ctor; `QuotedPriceOMR` falls back to `QuotedPrice` if missing (handles legacy session-arm clients); `CalculatePrices()` only invoked when `SessionId.HasValue`. `[MapperIgnoreTarget]` annotations cover server-controlled and audit fields.

### 2.22 `Application/Training/Finance/TrainingProviderAppService.cs`
Three additions:
- `CreateFilteredQueryAsync` — added `Scope`, `CountryId`, `IsFromNebras` filters.
- `CreateAsync` / `UpdateAsync` — wrap `base.CreateAsync` / `base.UpdateAsync` with `ValidateScopeCountry` (throws `Training:TrainingProvider:CountryRequiredForExternal` when `Scope != Internal && CountryId is null`).
- `MapToEntity(updateInput, entity)` — preserves `IsFromNebras` + `NebrasId` defensively, even if a forged client sets them.
- `EnrichAsync` — batch-loads countries from `HrGeographicalLocations` and joins `CountryNameAr/En` onto each DTO.

### 2.23 `Application/Training/Finance/PriceQuoteAppService.cs`
Full rewrite. Headlines:
- `GetListAsync` — accepts `CasualCourseId?` + `IsSelected?` filters in addition to existing ones.
- `CreateAsync` — `ValidatePolymorphicParent` + `ValidateCityInCountryAsync`. Permission attribute switched from legacy `TrainingPermissions.PriceQuote.Create` to `TrainingExecutionPermissions.PriceQuotes.Create`.
- `UpdateAsync` — rejects when `entity.IsSelected` (`Training:PriceQuote:CannotEditSelected`); preserves polymorphic parent + status + `IsSelected` (parent is immutable post-create).
- `DeleteAsync` — same `IsSelected` guard.
- `EnrichAsync` — batch-loads providers, sessions, countries + cities; populates `ProviderScope`, joined name fields.
- `BatchFindAsync<T>` — small helper using generic `IRepository<T,Guid>` constraint.

### 2.24 `Application/Training/CasualCourses/CasualCourseAppService.cs`
- Constructor: injected `IRepository<PriceQuote, Guid> priceQuoteRepo` + `PriceQuoteValidator priceQuoteValidator`.
- New method `SelectPriceQuoteAsync(id, input)`:
  - Guards: `Status == THApproved`, `ActualEndDate >= ActualStartDate`, `PriceQuoteValidator.ValidateForSelectionAsync` (course is approved + quote belongs to course).
  - Atomic: flips old winner's `IsSelected = false`, sets new winner's `IsSelected = true`, writes `SelectedPriceQuoteId` + `ActualStartDate` + `ActualEndDate`.
  - No status transition — course stays at `THApproved`.

### 2.25 `Domain.Shared/Localization/Training/en.json` and `ar.json`
27 new keys per language:
- 13 error keys covering `Training:TrainingProvider:CountryRequiredForExternal`, all `Training:PriceQuote:*` (incl. `OnePolymorphicParentRequired`, `CityNotInCountry`, `CourseNotApproved`, `WrongCourse`, `CannotEditSelected`), all `Training:TravelInstruction:*` (incl. `OnePolymorphicParentRequired`, `InvalidDateOrder`, `CannotEditAfterIssued`, `PrerequisitesNotMet`, `NotFound`, `AlreadyCancelled`), `Training:CasualCourse:NotApprovedYet`, `Training:CasualCourse:InvalidActualDates`.
- 3 `Training.ProviderScope.*` (Internal / Local / International).
- 3 `Training.TravelInstructionStatus.*` (Draft / Issued / Cancelled).
- 11 `Permission:TrainingExecution.*` for the new permission group.

---

## 3. Files ADDED (15)

### Domain.Shared (2)
| File | Purpose |
|---|---|
| `Training/Enums/ProviderScope.cs` | `Internal=0 / Local=1 / International=2` |
| `Training/Enums/TravelInstructionStatus.cs` | `Draft=0 / Issued=1 / Cancelled=2` |

### Domain (4)
| File | Purpose |
|---|---|
| `Training/Hr/GeographicalLocation.cs` | Read-only HR master entity (countries + cities) |
| `Training/Execution/TravelInstruction.cs` | New polymorphic `FullAuditedAggregateRoot<Guid>` — one per casual course or session |
| `Training/Managers/TravelDayCalculator.cs` | Domain service: `CalculateDays`, `GetEffectiveTravelDays` |
| `Training/Managers/PriceQuoteValidator.cs` | Domain service: blocks selection when course not THApproved or quote/course mismatch |
| `Training/DataSeeder/GeographicalLocationDataSeeder.cs` | Seeds 10 countries + 15 cities (idempotent) |

### Application.Contracts (6)
| File | Purpose |
|---|---|
| `Training/HrIntegration/IGeographicalLocationAppService.cs` | `GetCountriesAsync` / `GetCitiesAsync(countryId)` |
| `Training/HrIntegration/Dtos/GeographicalLocationDto.cs` | `Id` + `ArabicName` + `EnglishName` + `LocationParentId?` |
| `Training/Execution/ITravelInstructionAppService.cs` | `CreateOrUpdateAsync` / `GetByParentAsync` / `IssueAsync` / `CancelAsync` |
| `Training/Execution/Dtos/TravelInstructionDto.cs` | Includes `EffectiveTravelDays` (computed by AppService) |
| `Training/Execution/Dtos/CreateUpdateTravelInstructionDto.cs` | Polymorphic input — exactly one parent |
| `Training/CasualCourses/Dtos/SelectPriceQuoteDto.cs` | `PriceQuoteId` + `ActualStartDate` + `ActualEndDate` |

### Application (5)
| File | Purpose |
|---|---|
| `Training/HrIntegration/GeographicalLocationAppService.cs` | Implements the read-only countries/cities endpoints |
| `Training/Execution/TravelInstructionAppService.cs` | Upsert + issue + cancel; per-method permission attributes; uses `TravelDayCalculator` |
| `Training/Mapper/TravelInstructionToDtoMapper.cs` | Mapperly with `EffectiveTravelDays` ignored |
| `Training/Mapper/CreateUpdateTravelInstructionToEntityMapper.cs` | Mapperly with comprehensive `[MapperIgnoreTarget]` for audit + status + calculated fields |
| `Training/Mapper/GeographicalLocationToDtoMapper.cs` | Mapperly trivial |

### EntityFrameworkCore (1)
| File | Purpose |
|---|---|
| `Migrations/20260428100000_v4_8_0_PreExecutionLayer.cs` | Hand-written `Up` + `Down` covering all schema changes (see §4 for regeneration workflow) |

---

## 4. Migration regeneration workflow

The migration `.cs` file was hand-written; EF needs a matching `Designer.cs` + an updated `TrainingDbContextModelSnapshot.cs`. **Recommended path:**

```bash
# 1. Stop MOD.Training.HttpApi.Host (it locks the EF assembly).
# 2. Delete the hand-written file.
del src\MOD.Training.EntityFrameworkCore\Migrations\20260428100000_v4_8_0_PreExecutionLayer.cs

# 3. Regenerate from the entity changes already in place.
dotnet ef migrations add v4_8_0_PreExecutionLayer ^
    --project src\MOD.Training.EntityFrameworkCore ^
    --startup-project src\MOD.Training.DbMigrator

# 4. Open the freshly generated .cs file and paste in the manual SQL bits
#    (EF cannot generate raw CHECK constraints / backfills):
#    – ALTER TABLE [AppPriceQuotes] ADD CONSTRAINT [CK_PriceQuote_PolymorphicParent] CHECK …
#    – UPDATE [AppPriceQuotes] SET [QuotedPriceOMR] = [QuotedPrice] WHERE …  (backfill)
#    – ALTER TABLE [AppTravelInstructions] ADD CONSTRAINT [CK_TravelInstruction_PolymorphicParent] CHECK …

# 5. Apply when ready (NOT done by Claude per project policy — user explicitly approves).
dotnet ef database update --project src\MOD.Training.EntityFrameworkCore --startup-project src\MOD.Training.DbMigrator
```

The SQL fragments to paste are visible in `20260428100000_v4_8_0_PreExecutionLayer.cs` lines 159-167 (PriceQuote backfill + CHECK) and 245-252 (TravelInstruction CHECK).

---

## 5. Build status

| Project | Status |
|---|---|
| `MOD.Training.Domain.Shared` | ✅ 0 errors |
| `MOD.Training.Domain` | ✅ 0 errors |
| `MOD.Training.Application.Contracts` | ✅ 0 errors |
| `MOD.Training.Application` | ✅ 0 errors |
| `MOD.Training.EntityFrameworkCore` | ✅ 0 errors |

A solution-wide build hits MSB3027 file-lock errors on `MOD.Training.Application.dll` and dependents because `MOD.Training.HttpApi.Host` (PID 37256) is running. Stop that process and re-run for a clean full build. **No new warnings introduced beyond `RMG012` Mapperly notices for AppService-enriched joined fields** (e.g. `CountryNameAr` on `TrainingProviderDto`) — those are by design.

---

## 6. Endpoint count

Per the Phase 4B-α API List:

| AppService | New | Modified |
|---|---|---|
| `TrainingProviderAppService` | 0 | 5 (DTO-shape) |
| `GeographicalLocationAppService` | 2 | — |
| `PriceQuoteAppService` | 0 | 5 (polymorphic + filters) |
| `TravelInstructionAppService` | 4 | — |
| `CasualCourseAppService.SelectPriceQuoteAsync` | 1 | — |
| **Totals** | **7 new** | **10 modified** |

Cumulative casual-course-related endpoints after this delta: **32** (25 from Phase 4A + 7 new in 4B-α).

---

## 7. Verification checklist (post-migration)

**Schema & migration**
- [ ] `dotnet ef migrations add v4_8_0_PreExecutionLayer` regenerates Designer + snapshot
- [ ] Manual SQL fragments pasted in (3 raw SQL calls — backfill + 2 CHECK constraints)
- [ ] Migration applied cleanly to all 3 demo tenants
- [ ] `AppTrainingProviders` has 4 new columns; existing rows default sanely (`Scope = Local`)
- [ ] `AppCasualCourses` has `ActualStartDate` + `ActualEndDate` (nullable)
- [ ] `AppPriceQuotes`: `SessionId` is now nullable; `CasualCourseId`, `QuotedPriceOMR`, `IsSelected`, `CountryId`, `CityId` columns exist; `CK_PriceQuote_PolymorphicParent` constraint exists
- [ ] `AppTravelInstructions` table created; unique-filtered indexes per arm; `CK_TravelInstruction_PolymorphicParent` exists
- [ ] `HrGeographicalLocations` table created; index on `LocationParentId`
- [ ] `GeographicalLocationDataSeeder` populated 10 countries + 15 cities

**Domain**
- [ ] `TravelDayCalculator.CalculateDays(2026-04-15, 2026-04-22)` returns 8 (inclusive)
- [ ] `TravelDayCalculator.GetEffectiveTravelDays` returns Override when set, otherwise Calculated
- [ ] `PriceQuoteValidator.ValidateForSelectionAsync` rejects course in `Submitted` status
- [ ] `PriceQuoteValidator.ValidateForSelectionAsync` rejects quote pointing at a different course

**AppServices**
- [ ] `TrainingProviderAppService.CreateAsync` rejects `Scope=Local` without `CountryId`
- [ ] `TrainingProviderAppService.GetAsync` exposes `CountryNameAr`/`CountryNameEn`
- [ ] `TrainingProviderAppService.UpdateAsync` does NOT change `IsFromNebras` / `NebrasId` even if the client sends them
- [ ] `GeographicalLocationAppService.GetCountriesAsync` returns 10 rows ordered by Arabic name
- [ ] `GeographicalLocationAppService.GetCitiesAsync(omanId)` returns Muscat + Salalah only
- [ ] `PriceQuoteAppService.CreateAsync` rejects when both `SessionId` + `CasualCourseId` set
- [ ] `PriceQuoteAppService.CreateAsync` rejects when neither is set
- [ ] `PriceQuoteAppService.CreateAsync` rejects when `CityId` belongs to a different country
- [ ] `PriceQuoteAppService.UpdateAsync` rejects when `IsSelected = true`
- [ ] `PriceQuoteAppService.DeleteAsync` rejects when `IsSelected = true`
- [ ] `TravelInstructionAppService.CreateOrUpdateAsync` is upsert (one per parent — second call updates)
- [ ] `TravelInstructionAppService.CreateOrUpdateAsync` rejects edits when `Status != Draft`
- [ ] `TravelInstructionAppService.IssueAsync` blocks when tickets / visa / insurance fields incomplete
- [ ] `TravelInstructionAppService.IssueAsync` succeeds when all prerequisites met → `Status = Issued`
- [ ] `CasualCourseAppService.SelectPriceQuoteAsync` requires `Status = THApproved`
- [ ] `CasualCourseAppService.SelectPriceQuoteAsync` flips `IsSelected` on previous winner when picking a different one
- [ ] `CasualCourseAppService.SelectPriceQuoteAsync` writes `ActualStartDate` + `ActualEndDate`

**Cross-cutting**
- [ ] Phase 4A casual-course workflow (Submit → UGM → Staff → TD → TH approve) still works
- [ ] Phase 3 annual-plan + sessions workflow still works (legacy PriceQuote arm)
- [ ] Frontend proxy regenerated to expose all new types (run `nswag run` or equivalent)

---

## 8. Locked decisions reference

| # | Decision | Origin |
|---|---|---|
| Q1 | Phase 4B split, ordered by dependency | Prompt §1.1 |
| Q2 | Polymorphic from day one (PriceQuote, TravelInstruction) | Prompt §1.1 |
| Q-A | Nebras fields sync-driven only | Prompt §1.1 |
| Q-B | ProviderScope enum (Internal / Local / International) | Prompt §1.1 |
| Q-C | Country/City on PriceQuote | Prompt §1.1 |
| Q-extra | TravelInstruction one-per-course (group travel) | Prompt §1.1 |
| D-01 | App* prefix kept | This delta |
| D-02 | HrGeographicalLocations owned by Training DB | This delta |
| D-03 | PriceQuote.CasualCourseId added now (was missing) | This delta |
| D-04 | Legacy PriceQuote pricing fields preserved | This delta |

---

## 9. Non-regression notes

- Casual-course state machine **untouched**. `SelectPriceQuoteAsync` is post-`THApproved`; no transition.
- Legacy `PriceQuote(Guid id, Guid sessionId, …)` ctor preserved so any Phase 3 sessions code using it still compiles. The mapper now uses the new Id-only ctor; legacy paths are unaffected.
- `CasualCourseToDtoMapper` and `TrainingProviderToDtoMapper` unchanged at the mapper level — new fields auto-map (DTO + entity property names match). The two `RMG012` warnings on `TrainingProviderToDtoMapper` (`CountryNameAr`/`En` not on source) are intentional — those fields are AppService-enriched.
- `HrEmployees` / `HrRanks` table naming convention unchanged. The `Mst_` prefix flagged by the prompt does **not** match this codebase; the user-confirmed answer was to follow the existing `Hr*` pattern.

---

## 10. Suggested commit

```
feat(execution-prep): pre-execution layer — quotes + actual dates + travel (Phase 4B-α)

Pre-execution preparation layer for the post-TH-approval workflow.
Introduces price-quote selection, actual-date confirmation, and travel
instructions. Phase 4B-β (payments + reallocations) consumes the outputs.

Schema (App* prefix; HR table follows Hr* convention):
- AppTrainingProviders: IsFromNebras, NebrasId (sync-driven), Scope enum,
  CountryId (soft FK to HrGeographicalLocations).
- AppCasualCourses: ActualStartDate, ActualEndDate (writable via
  SelectPriceQuoteAsync).
- AppPriceQuotes: CasualCourseId (polymorphic arm activated),
  SessionId now nullable, QuotedPriceOMR, IsSelected, CountryId, CityId,
  polymorphic CHECK constraint.
- AppTravelInstructions: new polymorphic table, four travel dates +
  visa + insurance + tickets + travel days, polymorphic CHECK constraint.
- HrGeographicalLocations: new master-data table (countries + cities).

Domain services:
- TravelDayCalculator (inclusive day count + override resolution).
- PriceQuoteValidator (gates quote selection on course state).

AppServices:
- New: GeographicalLocationAppService, TravelInstructionAppService.
- Extended: TrainingProviderAppService (Scope/Country), PriceQuoteAppService
  (polymorphic + IsSelected guards + city-in-country validation).
- New endpoint: CasualCourseAppService.SelectPriceQuoteAsync.

7 new endpoints + 10 modified DTO shapes. New TrainingExecution permission
group (PriceQuotes.* + TravelInstructions.*). 27 localization keys per
language (ar/en).

Closes #<issue>
```

---

*End of Phase 4B-α Code Delta Report — v1.0*
*Backend complete; frontend deferred. Next: regenerate migration designer/snapshot, apply DB, smoke-test the workflow end-to-end.*
