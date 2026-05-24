# GTMS — Project Checkpoint v4.9.2

**As of:** May 12, 2026
**Status:** Phase 4B-β complete + 2 patches shipped. Ready for Phase 4C-α (Annual Plan Sessions Execution).
**Repo:** https://github.com/Khalid94111/MOD.TrainingLive.git

---

## Quick TL;DR

The system covers casual courses end-to-end (creation → approval → execution → payments → reallocation). Annual plan and training center plan sessions have backend infrastructure (polymorphic entities ready) but **no frontend execution UI yet** — that's Phase 4C.

Current version on `main`: **v4.9.2**.

---

## Version history

| Version | Date | Scope | Status |
|---|---|---|---|
| v4.7.0 | Apr 4 | Phase 4A — Casual Courses planning + approval | ✅ |
| v4.7.1-v4.7.5 | Apr 5-12 | Phase 4A Patches 1-5 (rank preview, financial fixes, calculator mode) | ✅ |
| v4.8.0 | Apr 18 | Phase 4B-α — Pre-execution (price quotes, travel instructions) | ✅ |
| v4.9.0 | Apr 22 | Phase 4B-β — Payments + auto-reallocation | ✅ |
| v4.9.1 | May 10 | Phase 4B-β Patch 1 — TD approves reallocations directly (was: Staff records external signature) | ✅ |
| v4.9.2 | May 12 | Phase 4B-β Patch 2 — Execution stage column on casual course list | ✅ |
| **v4.10.0** | (planned) | **Phase 4C-α — Annual Plan Sessions** | ⏳ Next |

---

## What's currently in the system

### 1. Annual Plan (Phase 3)
- TrainingPlan + TrainingPlanItem entities
- Full approval workflow (UTM submits → UGM → TD → TH)
- Per-item conditions, financial estimates
- Plan approval = items become "tentative" records
- **No session creation UI yet** ← Phase 4C-α fills this

### 2. Training Center Plan (Phase 2B)
- TrainingCenterPlanItem entities for internal training programs
- TCO approval workflow
- **No session creation UI yet** ← Phase 4C-β fills this

### 3. Casual Courses (Phase 4A) — fully working
**Lifecycle:** Draft → Submitted → UGMApproved → UnderReview → StaffReviewed → TDApproved → THApproved

**Roles:**
- UTM creates draft + nominates
- UGM verifies conditions, approves
- Staff reviews scenario + financial items
- TD approves
- TH gives final approval

**Key entities:**
- `CasualCourse` with FundingScenario enum (3 scenarios)
- `CasualCourseFinancialItem` with FinancialAmountSource (FundingSource | FinancialItem)
- `CasualCourseFinancialItemRank` for per-rank breakdown
- `CasualCourseNomination` with rank snapshots

**Patch history:**
- Patch 1: Rank-aware preview during UTM nominations
- Patch 2: Staff review uses rank-aware calculator
- Patch 3: Funding scenario fixes (FinancialAmountSource introduced)
- Patch 4: Full financial details at course creation
- Patch 5: Calculator-mode toggle

### 4. Phase 4B-α — Pre-execution (post-TH-approval) — fully working
**Triggers when:** Casual course reaches THApproved

**Workflow:**
1. Staff adds 1-3 PriceQuotes (from different TrainingProviders)
2. Staff picks winner → atomic: PriceQuote.IsSelected + ActualStartDate + ActualEndDate
3. Staff issues TravelInstruction (one per course, group travel) — external courses only
4. TravelInstruction status: Draft → Issued → Cancelled

**Key entities:**
- `PriceQuote` — polymorphic (CasualCourseId OR SessionId) + Country + City + IsSelected + QuotedPriceOMR
- `TravelInstruction` — polymorphic, one per course, includes departure/arrival/return dates + visa/insurance/tickets
- `TrainingProvider` extended with Nebras flag, Scope, CountryId
- `Mst_GeographicalLocations` — HR-side read-only reference (countries + cities)
- `CasualCourse` extended with ActualStartDate + ActualEndDate (nullable)

**Patch history:** none (shipped clean)

**UI pattern locked in this phase:**
- Stage-based progressive disclosure on casual course detail page
- 4 accordion sections: Details / Financials / Quotes / Travel
- 8-node status pipeline
- Mode-driven content reuse (3 components × 3 modes = 9 valid combinations)
- 12 bugs documented in `docs/GTMS-Phase4B-Alpha-Frontend-Notes.md` with patterns

### 5. Phase 4B-β — Payments + Reallocation — fully working
**Triggers when:** PriceQuote winner selected (for external) OR THApproved (for internal)

**Workflow:**
1. Finance creates TravelAllowancePayment per nominee (auto-filled from TravelInstruction + rank rates)
2. Finance confirms each TravelAllowancePayment → status terminal
3. Finance creates CoursePayment with invoice PDF upload
4. Finance confirms CoursePayment → **atomic transaction**:
   - Status = Confirmed
   - BudgetReallocationGenerator fires (idempotent, scenario-aware)
5. For scenarios 2 & 3: Pending reallocations now exist in DB
6. TD opens PAGE 4.7, approves each Pending reallocation (per Patch 1 — TD does it directly)

**Key entities:**
- `TravelAllowancePayment` — polymorphic, 5 cost components + computed TotalOMR + Nebras external fields
- `CoursePayment` — polymorphic, InvoiceAmountOMR + NebrasAmountOMR + invoice file blob
- `BudgetReallocation` — auto-generated, FundingSourceVoteCode → ToFinancialItem mapping
- `PaymentStatus`, `ReallocationStatus` enums

**File storage:** BlobStoring.FileSystem with BasePath = ABP Setting "Gtms.Files.Path", 25 MB max for invoices.

**BudgetReallocationGenerator behavior:**
- Scenario 1 (FundingSource covers all) → 0 reallocations
- Scenario 2 (Partial) → skip items with Source = FundingSource, generate for the rest
- Scenario 3 (FinancialItems cover all) → generate for all items
- Idempotent on CoursePaymentId

### 6. Phase 4B-β Patch 1 — TD Approves Directly (v4.9.1)
**What changed:** Reversed "Staff records TD's paper signature" model. TD now approves reallocations directly in the system.

- `MarkReallocationApprovedDto`: removed `ApprovedById` field
- `MarkApprovedAsync`: derives ApprovedById from CurrentUser.Id
- Permission moved: `Reallocations.MarkApproved` from Staff role → TD role
- Removed validation `Training:Reallocation:ApproverNotTD` (permission gate handles it)
- PAGE 4.7 dialog: removed TD selector dropdown

### 7. Phase 4B-β Patch 2 — Execution Stage Column (v4.9.2)
**What changed:** Added "Execution Stage" column to casual course list.

- New enum `ExecutionStage` (6 values)
- Server-side computation in `CasualCourseAppService.GetListAsync`
- Frontend grid column with filter dropdown + color-coded badges
- Internal courses: 2-stage path
- External courses: 5-stage path

---

## Endpoint count

| Source | Count |
|---|---|
| Phase 3 (Annual Plans, Plan Items, Plans approval) | ~40 |
| Phase 4A (Casual Courses + patches) | 25 |
| Phase 4B-α (Pre-execution) | 7 |
| Phase 4B-β (Payments + reallocations + patches) | 17 |
| **Total active in v4.9.2** | **~89** |

---

## Database tables

| Phase | Tables added |
|---|---|
| Phase 3 | TrnTrainingPlans, TrnTrainingPlanItems, TrnPlanItemConditions, TrnPlanItemFinancialItems |
| Phase 4A | TrnCasualCourses, TrnCasualCourseFinancialItems, TrnCasualCourseFinancialItemRanks, TrnCasualCourseNominations |
| Phase 4B-α | (extended TrainingProvider, PriceQuote, TravelInstruction); Mst_GeographicalLocations (HR-side) |
| Phase 4B-β | TrnTravelAllowancePayments, TrnCoursePayments, TrnBudgetReallocations |
| **Total** | **57 tables** in TrainingModule schema |

---

## Patterns locked in (hard rules — apply to all future phases)

### Backend
- ABP CrudAppService, no HttpApi controllers (auto-generated)
- DTO naming: `XxxGetListInput` (not `GetXxxListDto`)
- Mapperly class-based mappers (no AutoMapper, no static extensions)
- Trn table prefix, permissions in Application.Contracts
- Primary constructors (.NET 10)
- ABP repo methods: WithDetailsAsync, AnyAsync, CountAsync, GetListAsync
- Polymorphic check constraints via ToTable(t => t.HasCheckConstraint(...))
- HR cross-module refs declared in EF config, no DB-level FK
- Atomic transactions for state-mutating endpoints (Confirm + ReallocationGenerator)

### Frontend (from Phase 4B-α Frontend Notes §5)
- Standalone components, NO NgModules
- Signals everywhere (`signal()`, `computed()`)
- `@if` / `@for` (not `*ngIf` / `*ngFor`)
- `firstValueFrom` (NOT `toPromise`)
- ABP proxies direct (no wrapper services)
- `takeUntilDestroyed(destroyRef)` on EVERY service subscription — HARD RULE
- `refreshShell.refresh()` after EVERY state-mutating action — HARD RULE
- Mode-aware HTML gating with `@if (showX())` blocks
- BehaviorSubject hygiene: clear on shell init when no id
- CSS specificity via :host for shared stylesheet conflicts
- $any($event.target).value for native inputs
- Individual signals per form field
- No inline arrow functions in templates, no inline styles

### Localization (HARD RULE — never change)
- HTML & TS: `'::Training.X'` (with `::` prefix, dot notation)
- JSON: `"Training.X"` (no `::` prefix)
- Error codes: `"Training:X:Y"` (single colons — never change)
- Permission keys: `"Permission:X"`

---

## Stage-based shell architecture (Phase 4B-α canonical pattern)

This pattern is now the standard for any entity with a multi-stage lifecycle. Extends to Phase 4C-α sessions.

```
[Shell component]
  • Owns the entity() signal — source of truth
  • Computes section states from (entity × user role × section conditions)
  • Hosts status pipeline + header action bar
  • Listens to refresh service for child-driven reloads
       ↓
[Section components]
  • Each section: collapsed-summary / expanded-active / locked-preview / hidden
  • Delegates to mode-driven inner components (request / approval / review)
  • Mode input controls which HTML blocks render
       ↓
[Action service]
  • Root-provided RxJS Subject
  • Inner components subscribe with status + mode filter
  • takeUntilDestroyed prevents subscription leaks
```

---

## Project files in repository

```
docs/
├── GTMS-BRD-v4_0.md                                  (BRD)
├── GTMS-BRD-v4_0.docx                                
├── GTMS-ERD-v4_0.html                                
├── GTMS-API-Docs-v4_0.html                           
├── GTMS-Workflows-v4_0.html                          
├── GTMS-Dev-Roadmap-v3.md                            
├── GTMS-Phase4A-Implementation-Prompt.md             
├── GTMS-Phase4A-API-List.md                          
├── GTMS-Phase4A-Mockup.html                          
├── GTMS-Phase4A-Patch[1-5]*.md                       
├── GTMS-Phase4B-Alpha-Implementation-Prompt.md       
├── GTMS-Phase4B-Alpha-API-List.md                    
├── GTMS-Phase4B-Alpha-Mockup.html                    
├── GTMS-Phase4B-Alpha-Frontend-Layout.md             
├── GTMS-Phase4B-Alpha-Frontend-Notes.md          ⭐ MUST READ FOR FRONTEND
├── GTMS-Phase4B-Beta-Implementation-Prompt.md        
├── GTMS-Phase4B-Beta-API-List.md                     
├── GTMS-Phase4B-Beta-Mockup.html                     
├── GTMS-Phase4B-Beta-Patch1-TDApprovesDirectly.md    
├── GTMS-Phase4B-Beta-Patch2-ExecutionStageColumn.md  
├── GTMS-Phase4C-Alpha-Implementation-Prompt.md   ⭐ NEXT PHASE
├── GTMS-Phase4C-Alpha-Mockup.html                ⭐ NEXT PHASE
└── GTMS-Project-Checkpoint-v4_9_2.md             ⭐ THIS FILE
```

---

## What's NOT built yet

### Annual Plan Sessions (Phase 4C-α — NEXT)
- ❌ Session creation UI from approved TrainingPlanItems
- ❌ Session detail page (stage-based shell for sessions)
- ❌ Annual plan progress dashboard
- ❌ Frontend consumption of polymorphic Session arm of PriceQuote/TravelInstruction/Payments

**Backend state:**
- ✅ CourseSession entity exists (Phase 3)
- ⚠ May need extensions: nullable ActualStartDate/EndDate, SessionStatus enum, cancellation fields
- ✅ Polymorphic SessionId arm exists in PriceQuote, TravelInstruction, payments

### Training Center Sessions (Phase 4C-β — AFTER 4C-α)
- ❌ TCO workflow for internal training program sessions
- ❌ Similar UI to 4C-α but simpler (internal only, no quote/travel)

### Phase 5 — Reports + Post-execution (FUTURE)
- ❌ Financial Items Budget Report (highest value — makes 4B reallocation work visible)
- ❌ Casual Cost Report
- ❌ ExecutionStatus enum + ActualStartDate/EndDate execution tracking
- ❌ Certificates entity
- ❌ CourseEvaluations + ProviderEvaluations
- ❌ Provider rating feedback loop

### Future streams (DEFERRED per Dev Roadmap)
- IMET stream
- Global/International stream  
- Academic Studies stream
- ~16 tables for these three streams

---

## Operational state

### Demo tenants (seeded)
1. **Ground Forces** (القوات البرية) — TenantId from seeder
2. **Air Forces** (القوات الجوية)
3. **Naval Forces** (القوات البحرية)

Each tenant has:
- ~50-100 employees with ranks
- ~20-30 TenantCourses
- ~30-40 approved TrainingPlanItems for 2026 plan year
- A handful of casual courses in various states (Draft → THApproved)
- 1-2 casual courses in full execution flow with payments + reallocations

### Test data caveats
- Mst_GeographicalLocations seeded with Oman + ~10 common countries
- TrainingProviders seeded with mix of Nebras and non-Nebras
- BlobStoring filesystem path needs to exist + be writable on the dev machine
- No annual plan sessions exist yet (Phase 4C-α will need to create them in seeder for smoke testing)

---

## Phase 4C-α — what to do next

### Immediate next steps (in order)

1. **Open Claude Code session** in MOD.TrainingLive repo
2. **Paste the Phase 4C-α handoff message** (from chat)
3. **First task for Claude Code:** verify polymorphism of 4B-α/β components before any new code
4. **Schema migration** v4_10_0 → review before running
5. **Backend section by section** → review each
6. **Frontend page by page** → review each
7. **Smoke test full lifecycle** (Internal + External separately)
8. **Bump to v4.10.0**, update this checkpoint

### Estimated effort
**~10-12 days** total. Largest single phase since 4A. Frontend is the bigger portion (~7 days).

### Risks to watch
- **Polymorphism gap:** 4B-α/β components may have hard-coded casualCourseId. Verify and refactor BEFORE building session detail page.
- **CourseSession schema drift:** Phase 3 may have built it with non-nullable dates. Migration needs to handle existing data.
- **Section state matrix complexity:** Internal courses skip Sections 3 & 4 (hidden, not locked). New state logic.

---

## Critical reminders for next session

1. **Read `docs/GTMS-Phase4B-Alpha-Frontend-Notes.md` §5 before touching any frontend code.** The 12 bugs documented there will recur in Phase 4C-α if patterns aren't followed.

2. **`SelectPriceQuoteAsync` for sessions is atomic.** Three operations in one transaction (PriceQuote.IsSelected + Session.ActualDates + Session.Status). Follow the exact pattern from 4B-α casual course implementation.

3. **`BudgetReallocationGenerator` does NOT fire for sessions.** Sessions don't have funding scenarios. Reallocation is casual-course-only.

4. **Localization format is HARD-LOCKED.** Three formats in three places. No deviations — see Patterns section above.

5. **No NgModules in any frontend code.** Standalone components everywhere.

---

*End of Project Checkpoint v4.9.2*
*Next checkpoint: v4.10.0 after Phase 4C-α completes*
