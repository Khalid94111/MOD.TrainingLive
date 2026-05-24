# GTMS Phase 4C-α — Implementation Prompt (Annual Plan Sessions Execution)

**Version:** v1.0
**Target version:** v4.9.2 → v4.10.0 (minor bump — new entity workflow + frontend)
**Scope:** Build the Annual Plan Session execution layer. Staff creates sessions from approved TrainingPlanItems based on priority + preferred quarter; sessions then go through the same post-approval execution as casual courses (quotes, travel, payments). Internal courses get dates at creation; external courses get dates at quote selection.
**Base code:** v4.9.2 (Phase 4B-β + Patch 1 + Patch 2 all merged)
**Companion docs:**
- `docs/GTMS-BRD-v4_0.md` §10.5 (annual plan session creation rules)
- `docs/GTMS-Phase4C-Alpha-Mockup.html` (5 screens v2 with Internal/External split)
- `docs/GTMS-Phase4B-Alpha-Frontend-Notes.md` (stage-based shell patterns — reused here)
- `docs/GTMS-Project-Checkpoint-v4_9_2.md` (current state)

---

## 1. Why this phase exists

Phase 3 shipped TrainingPlan + TrainingPlanItem entities. Plans get approved through TH approval. But after approval, the items become tentative records with no execution UI — Staff has no way to activate them as actual training events.

Phase 4B built the full execution workflow (quotes, travel, payments, reallocations) but only for casual courses. The polymorphic infrastructure was wired for sessions but no UI consumed it.

Phase 4C-α closes both gaps:
1. **Session creation workflow** — Staff sees prioritized queue of plan items, creates Sessions
2. **Session execution UI** — same stage-based shell as casual courses, reusing 4B-α/β components polymorphically

### 1.1 Locked decisions

| # | Decision | Implementation |
|---|---|---|
| Q1 | Phase 4C split into α + β | This patch is α (Annual Plan). β handles Training Center sessions later. |
| Q2 | 4C ships before Phase 5 | Reports postponed. |
| Q3 | One session per TrainingPlanItem | No batches. Strict 1:1. |
| Q-A | Sessions are manually created from approved plan items, not auto-created | Staff initiates via "Create Session" button. |
| Q-B | Staff is the creator | Not TCO (TCO creates Training Center sessions in 4C-β). |
| Q-C | Session cancellation allowed | Before InProgress. After InProgress, cancellation is out of scope. |
| Q-D | Nominee substitution at session creation only, same-rank only | After session creation, nominee list is locked. |
| Q-E | Substitution at session creation only | Confirmed Q-E from chat. |
| Q-F | Overdue plan items stay in queue with warning | No auto-roll, no auto-cancel. |
| Q-Dates | Internal: dates at creation. External: Planned status, dates at quote selection | Two different create flows in UI; atomic SelectPriceQuoteAsync sets dates for external |

---

## 2. Schema changes

### 2.1 Verify existing `CourseSession` entity

Phase 3 created `CourseSession`. Verify/extend it to match this schema:

```csharp
public class CourseSession : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid TenantId { get; set; }
    
    // Source plan item
    public Guid? TrainingPlanItemId { get; set; }      // Annual Plan source (this phase)
    public Guid? TrainingCenterPlanItemId { get; set; } // Center Plan source (4C-β)
    // Check constraint: exactly one source set
    
    public Guid TenantCourseId { get; set; }            // Resolved from plan item
    public CourseType CourseType { get; set; }          // Internal / ExternalLocal / ExternalIntl
    
    // Dates — nullable because external sessions start as Planned
    public DateTime? ActualStartDate { get; set; }      // Null for Planned external
    public DateTime? ActualEndDate { get; set; }
    
    // Estimated/preferred from plan
    public int PreferredQuarter { get; set; }           // 1, 2, 3, 4
    public int PlanYear { get; set; }                   // 2026
    
    // Execution references
    public Guid? SelectedPriceQuoteId { get; set; }
    
    public SessionStatus Status { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledById { get; set; }
    
    public virtual ICollection<SessionNomination> Nominations { get; set; } = new List<SessionNomination>();
}
```

If Phase 3's version differs significantly, write a migration to align. Likely additions:
- `ActualStartDate / ActualEndDate` as nullable (if they were non-nullable)
- `Status` field if missing
- `CancellationReason / CancelledAt / CancelledById`
- `PreferredQuarter / PlanYear` if these came from the plan item

### 2.2 New enum — `SessionStatus`

```csharp
public enum SessionStatus
{
    Planned       = 0,  // External only — created without final dates, awaiting quote
    Scheduled     = 1,  // Internal: from creation. External: after quote winner picked + dates locked
    InProgress    = 2,  // Course actually running (Staff transitions manually)
    Completed     = 3,  // Course finished (Staff transitions)
    Cancelled     = 4,  // Cancelled before InProgress
    FinanciallyClosed = 5  // All payments confirmed, all reallocations not applicable for sessions
}
```

### 2.3 `SessionNomination` entity

Verify exists from Phase 3. Required fields:

```csharp
public class SessionNomination : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid TenantId { get; set; }
    public Guid SessionId { get; set; }
    public Guid EmployeeId { get; set; }                // The actual nominee
    public Guid OriginalEmployeeId { get; set; }        // From plan — for audit
    public bool WasSubstituted => EmployeeId != OriginalEmployeeId;
    public string? SubstitutionReason { get; set; }
    public Guid RankId { get; set; }                    // Snapshotted from employee at creation
}
```

Add `OriginalEmployeeId`, `SubstitutionReason`, `RankId` if missing.

### 2.4 Migration `v4_10_0_AnnualPlanSessions`

Includes:
- Add `SessionStatus` enum column with default `Planned` (0)
- Add `ActualStartDate / ActualEndDate` as nullable (if needed)
- Add `CancellationReason / CancelledAt / CancelledById`
- Add `PreferredQuarter / PlanYear`
- Add check constraint on polymorphic source (`TrainingPlanItemId XOR TrainingCenterPlanItemId`)
- Add `OriginalEmployeeId / SubstitutionReason / RankId` to SessionNomination

---

## 3. Domain layer

### 3.1 New domain service — `SessionCreationValidator`

```csharp
public class SessionCreationValidator : IDomainService
{
    /// <summary>
    /// Validates that a TrainingPlanItem is ready to spawn a Session.
    /// </summary>
    public async Task ValidateForCreationAsync(Guid trainingPlanItemId)
    {
        var item = await _planItemRepo.GetAsync(trainingPlanItemId);
        
        if (item.TrainingPlan.Status != TrainingPlanStatus.THApproved)
            throw new BusinessException("Training:Session:PlanNotApproved");
        
        var existing = await _sessionRepo.AnyAsync(s => 
            s.TrainingPlanItemId == trainingPlanItemId && 
            s.Status != SessionStatus.Cancelled);
        
        if (existing)
            throw new BusinessException("Training:Session:AlreadyExists");
    }
    
    /// <summary>
    /// Validates a nominee substitution: same rank only.
    /// </summary>
    public async Task ValidateSubstitutionAsync(Guid originalEmployeeId, Guid replacementEmployeeId)
    {
        if (originalEmployeeId == replacementEmployeeId) return;
        
        var original = await _employeeRepo.GetAsync(originalEmployeeId);
        var replacement = await _employeeRepo.GetAsync(replacementEmployeeId);
        
        if (original.RankId != replacement.RankId)
            throw new BusinessException("Training:Session:RankMismatch");
    }
    
    /// <summary>
    /// For Internal courses: validates dates are within PreferredQuarter bounds (soft check, warning only).
    /// </summary>
    public bool DatesMatchPreferredQuarter(DateTime startDate, int preferredQuarter, int planYear)
    {
        var quarterStart = new DateTime(planYear, (preferredQuarter - 1) * 3 + 1, 1);
        var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);
        return startDate >= quarterStart && startDate <= quarterEnd;
    }
}
```

### 3.2 Reuse existing services

- `PriceQuoteValidator` (Phase 4B-α) — works for sessions polymorphically
- `TravelDayCalculator` (Phase 4B-α) — reused for session travel instructions

### 3.3 No new state machines beyond what's described

---

## 4. Application layer

### 4.1 New AppService — `AnnualPlanSessionAppService`

This is the main service. It does NOT replace `CourseSessionAppService` (which handles base CRUD). It adds Annual-Plan-specific operations.

| # | Method | Route | Purpose | Role |
|---|---|---|---|---|
| 1 | `GetPlanItemsQueueAsync` | `GET /api/app/annual-plan-sessions/queue` | Plan items awaiting session creation, sorted by priority + quarter | Staff |
| 2 | `CreateInternalSessionAsync` | `POST /api/app/annual-plan-sessions/create-internal` | Create session for Internal course with dates | Staff |
| 3 | `CreateExternalSessionAsync` | `POST /api/app/annual-plan-sessions/create-external` | Create session for External course (Planned status, no dates) | Staff |
| 4 | `GetAvailableSubstitutesAsync` | `GET /api/app/annual-plan-sessions/substitutes?originalEmployeeId={id}` | List of employees with same rank as the original nominee | Staff |
| 5 | `GetProgressDashboardAsync` | `GET /api/app/annual-plan-sessions/dashboard?year={year}` | Annual plan progress stats | Staff, TD, TH |

**`GetPlanItemsQueueAsync`:**

Returns plan items where:
- TrainingPlan.Status == THApproved
- TrainingPlan.PlanYear == current year (or specified)
- No active Session exists yet for the item

Sorted by:
1. Priority DESC (High → Medium → Low)
2. PreferredQuarter ASC, with overdue items first (PreferredQuarter < currentQuarter)
3. CreationTime ASC (older first as tiebreaker)

DTO includes computed `IsOverdue` flag (quarter has passed without session).

**`CreateInternalSessionAsync` DTO:**
```csharp
public class CreateInternalSessionDto
{
    public Guid TrainingPlanItemId { get; set; }
    public DateTime ActualStartDate { get; set; }       // Required
    public DateTime ActualEndDate { get; set; }         // Required
    public List<NomineeSubstitutionDto> Substitutions { get; set; } = new();
    public string? Notes { get; set; }
}

public class NomineeSubstitutionDto
{
    public Guid OriginalEmployeeId { get; set; }
    public Guid ReplacementEmployeeId { get; set; }
    public string? Reason { get; set; }
}
```

Server logic:
1. Validate via `SessionCreationValidator.ValidateForCreationAsync`
2. Validate each substitution via `ValidateSubstitutionAsync` (same-rank check)
3. Verify `CourseType == Internal` from plan item's TenantCourse
4. Validate dates: `ActualEndDate >= ActualStartDate`, duration matches course
5. Create Session with `Status = Scheduled`, `ActualStartDate/EndDate = input values`
6. Copy nominations from plan item, applying substitutions
7. Return the new session

**`CreateExternalSessionAsync` DTO:**
```csharp
public class CreateExternalSessionDto
{
    public Guid TrainingPlanItemId { get; set; }
    // NO dates — set later via SelectPriceQuoteAsync
    public List<NomineeSubstitutionDto> Substitutions { get; set; } = new();
    public string? Notes { get; set; }
}
```

Server logic:
1. Same validation
2. Verify `CourseType != Internal`
3. Create Session with `Status = Planned`, `ActualStartDate/EndDate = null`
4. Copy nominations with substitutions
5. Return new session

**`GetAvailableSubstitutesAsync`:**

Returns employees with same RankId as the specified original nominee, who are:
- Active employees in the same tenant
- Not already nominated in the parent plan item (no double-nomination)
- Not on extended leave (if HR provides such flag; otherwise skip this check)

**`GetProgressDashboardAsync`:**

Returns:
```csharp
public class AnnualPlanProgressDto
{
    public int Year { get; set; }
    public int TotalPlanItems { get; set; }
    public int PlannedSessionCount { get; set; }      // Status = Planned
    public int ScheduledSessionCount { get; set; }    // Status = Scheduled
    public int InProgressSessionCount { get; set; }
    public int CompletedSessionCount { get; set; }
    public int CancelledSessionCount { get; set; }
    public int OverdueCount { get; set; }
    public int OverallProgressPercent { get; set; }   // Completed / Total
    
    public List<QuarterProgressDto> ProgressByQuarter { get; set; }
    public List<UnitProgressDto> ProgressByUnit { get; set; }
    public List<OverdueAlertDto> Alerts { get; set; }
}
```

### 4.2 Extend `CourseSessionAppService`

Add the session-specific endpoints similar to casual courses:

| # | Method | Route | Purpose | Role |
|---|---|---|---|---|
| 6 | `GetListAsync` | `GET /api/app/course-sessions` | List with execution stage column | Staff, TD, TH, Finance |
| 7 | `GetDetailAsync` | `GET /api/app/course-sessions/{id}` | Detail with navigation properties | Staff, TD, TH, Finance |
| 8 | `SelectPriceQuoteAsync` | `POST /api/app/course-sessions/{id}/select-price-quote` | Atomic: set winner + dates + status transition | Staff |
| 9 | `MarkInProgressAsync` | `POST /api/app/course-sessions/{id}/mark-in-progress` | Status Scheduled → InProgress | Staff |
| 10 | `MarkCompletedAsync` | `POST /api/app/course-sessions/{id}/mark-completed` | Status InProgress → Completed | Staff |
| 11 | `CancelAsync` | `POST /api/app/course-sessions/{id}/cancel` | Cancel before InProgress | Staff |

**`SelectPriceQuoteAsync` DTO and logic:**
```csharp
public class SelectSessionPriceQuoteDto
{
    public Guid PriceQuoteId { get; set; }
    public DateTime ActualStartDate { get; set; }
    public DateTime ActualEndDate { get; set; }
}

public async Task<CourseSessionDto> SelectPriceQuoteAsync(
    Guid sessionId, SelectSessionPriceQuoteDto input)
{
    var session = await _repo.GetAsync(sessionId);
    
    if (session.Status != SessionStatus.Planned)
        throw new BusinessException("Training:Session:NotInPlannedStatus");
    
    if (input.ActualEndDate < input.ActualStartDate)
        throw new BusinessException("Training:Session:InvalidActualDates");

    await _quoteValidator.ValidateForSessionSelectionAsync(sessionId, input.PriceQuoteId);
    
    // Clear previous selection if any
    if (session.SelectedPriceQuoteId.HasValue)
    {
        var oldQuote = await _quoteRepo.GetAsync(session.SelectedPriceQuoteId.Value);
        oldQuote.IsSelected = false;
    }
    
    var newQuote = await _quoteRepo.GetAsync(input.PriceQuoteId);
    newQuote.IsSelected = true;
    
    // ATOMIC: 3 things at once
    session.SelectedPriceQuoteId = input.PriceQuoteId;
    session.ActualStartDate = input.ActualStartDate;
    session.ActualEndDate = input.ActualEndDate;
    session.Status = SessionStatus.Scheduled;
    
    return ObjectMapper.Map<CourseSession, CourseSessionDto>(session);
}
```

**Cancel logic:**
- Only allowed when `Status` in `(Planned, Scheduled)` — never after InProgress
- Records `CancellationReason`, `CancelledAt`, `CancelledById`
- Does NOT cascade — existing PriceQuotes/TravelInstructions/Payments remain (audit trail)

**Execution stage computation (similar to Patch 2 for casual courses):**

Add `ExecutionStage` field to `CourseSessionListDto`. Compute per row in `GetListAsync` based on session status, polymorphic relationships, etc.

```csharp
public enum SessionExecutionStage
{
    AwaitingQuoteSelection       = 1,   // External + Planned status
    AwaitingTravelInstruction    = 2,   // External + Scheduled + no TravelInstruction
    AwaitingTravelAllowances     = 3,   // Some allowances not confirmed
    AwaitingCoursePayment        = 4,   // Payment null or Draft
    AwaitingCompletion           = 5,   // All paid, session In Progress
    FinanciallyComplete          = 6,
    NoExecutionPending           = 7    // For Cancelled sessions
}
```

### 4.3 Extend existing services for polymorphic Session support

Verify these accept `SessionId` arm cleanly (they should, from 4B-α/β):
- `PriceQuoteAppService` — already polymorphic
- `TravelInstructionAppService` — already polymorphic
- `TravelAllowancePaymentAppService` — already polymorphic
- `CoursePaymentAppService` — already polymorphic
- `BudgetReallocationGenerator` — does NOT fire for sessions (sessions don't reallocate)

If any of these have hidden assumptions that they're casual-course-only, fix them.

---

## 5. Frontend

### 5.1 New page — Sessions to Create Queue (PAGE A)

**Route:** `/training/annual-plan/sessions-queue`
**Role:** Staff

Layout per mockup Screen A:
- Filters: Quarter, Priority, Type, Unit
- Grid with columns: Priority, Quarter, Course, Type, Unit, Nominees, Action
- Overdue rows highlighted (red background)
- "Create" button per row → routes to PAGE B-1 (Internal) or B-2 (External) based on CourseType

### 5.2 New page — Create Session (PAGE B)

**Routes:**
- `/training/annual-plan/create-session/internal/:planItemId`
- `/training/annual-plan/create-session/external/:planItemId`

Two distinct component variants:

**Internal variant (PAGE B-1):**
- Plan item context card (read-only)
- Section 1: Date pickers (required)
- Section 2: Nominees list with substitution dialog
- Action: "Create (Scheduled)"

**External variant (PAGE B-2):**
- Plan item context card
- Section 1: "Dates come later" amber banner with 5-step workflow visualization
- Section 2: Nominees list with substitution dialog
- Action: "Create (Planned)"

Both variants share the substitution dialog component:
- Shows original nominee (locked)
- Replacement dropdown filtered to same-rank employees only (via `GetAvailableSubstitutesAsync`)
- Optional reason field

### 5.3 New page — Sessions List (PAGE C)

**Route:** `/training/sessions`
**Role:** Staff, TD, TH, Finance (read)

Layout per mockup Screen C:
- Filters: Status, Type, Execution Stage
- Grid with columns including:
  - Dates column: shows "—" for Planned sessions with quarter hint
  - Status badge
  - Execution Stage badge (same component as casual course list from Patch 2)

### 5.4 New page — Session Detail (PAGE D)

**Route:** `/training/sessions/:id`
**Role:** Staff, TD, TH, Finance (read)

**This is the big component.** Reuses the stage-based shell pattern from Phase 4B-α.

Component architecture (mirror of casual-course-detail):
```
course-session-detail.component.ts    (shell)
├── course-session-status-pipeline.component.ts  (8 nodes)
├── course-session-header-action-bar.component.ts
├── course-session-section-details.component.ts
├── course-session-section-financials.component.ts
├── course-session-section-quotes.component.ts (delegates to existing price-quotes component)
├── course-session-section-travel.component.ts (delegates to existing travel-instructions component)
└── course-session-section-payments.component.ts (delegates to existing payments component)
```

Sections 3-5 reuse the EXISTING components from Phase 4B-α/β:
- `casual-course-price-quotes.component` accepts `sessionId` input as alternative to `casualCourseId`
- `casual-course-travel-instructions.component` same
- `casual-course-payments.component` same

These components must already be polymorphic in their API calls (using PolymorphicParentId). If they hard-code `casualCourseId`, refactor them to accept both.

**Status pipeline (8 nodes per mockup):**
1. By Plan (always done — plan was approved)
2. Created (always done if session exists)
3. Quote Selection (External only; auto-done for Internal)
4. Dates Confirmed (External: at quote select; Internal: at creation)
5. Travel Instruction (External only; skipped for Internal)
6. Payments (all sessions)
7. In Progress
8. Completed

**Section state matrix (similar to casual course detail):**

| Session Status | User Role | Section 1 | Section 2 | Section 3 (Quotes) | Section 4 (Travel) | Section 5 (Payments) |
|---|---|---|---|---|---|---|
| Planned | Staff | collapsed | collapsed | **active** | locked | locked |
| Planned | Other | collapsed | collapsed | collapsed | locked | locked |
| Scheduled (External) | Staff | collapsed | collapsed | collapsed | **active** | locked |
| Scheduled (Internal) | Staff | collapsed | collapsed | hidden | hidden | **active** |
| InProgress | All | collapsed | collapsed | collapsed | collapsed | collapsed |
| Completed | All | collapsed | collapsed | collapsed | collapsed | collapsed |
| Cancelled | All | collapsed | collapsed | collapsed | collapsed | collapsed |

Sections 3, 4 are **hidden entirely** for Internal courses (no quote, no travel).

**Header action bar buttons** by status × role:
- Planned + Staff: [Cancel]
- Scheduled (External) + Staff: [Cancel]
- Scheduled (Internal) + Staff: [Mark In Progress, Cancel]
- InProgress + Staff: [Mark Completed]
- Completed: [Print]
- Cancelled: [Print]

### 5.5 New page — Annual Plan Dashboard (PAGE E)

**Route:** `/training/annual-plan/dashboard`
**Role:** Staff, TD, TH

Per mockup Screen E:
- 5 stat cards (Total Items, Planned, Scheduled, Completed, Overdue)
- Overall progress bar
- Progress by quarter (Q1-Q4) progress bars
- Progress by unit (3 tenants)
- Alerts section: 2 alert types
  - Plan items without session (overdue)
  - Sessions in Planned status without quotes for >X days

### 5.6 Localization keys

```json
// ar.json
"Training:Session:PlanNotApproved": "الخطة السنوية لم تُعتمد بعد",
"Training:Session:AlreadyExists": "يوجد جلسة نشطة لهذا البند مسبقاً",
"Training:Session:RankMismatch": "البديل يجب أن يكون من نفس الرتبة",
"Training:Session:NotInPlannedStatus": "الجلسة ليست في حالة Planned",
"Training:Session:InvalidActualDates": "تاريخ الانتهاء يجب أن يكون بعد البدء",
"Training:Session:CannotCancelAfterInProgress": "لا يمكن إلغاء جلسة بدأت فعلياً",
"Training:Session:DurationMismatch": "مدة التواريخ لا تطابق مدة الدورة",

"Training.Session.Status.Planned": "⏱ مخطّطة",
"Training.Session.Status.Scheduled": "📅 مجدولة",
"Training.Session.Status.InProgress": "⚡ جارية",
"Training.Session.Status.Completed": "✓ مكتملة",
"Training.Session.Status.Cancelled": "⛔ ملغاة",
"Training.Session.Status.FinanciallyClosed": "✓ مكتملة مالياً",

"Training.SessionExecutionStage.AwaitingQuoteSelection": "بانتظار اختيار العرض",
"Training.SessionExecutionStage.AwaitingTravelInstruction": "بانتظار تعليمات السفر",
"Training.SessionExecutionStage.AwaitingTravelAllowances": "بدلات السفر",
"Training.SessionExecutionStage.AwaitingCoursePayment": "بانتظار دفع الرسوم",
"Training.SessionExecutionStage.FinanciallyComplete": "✓ مكتملة مالياً"
```

Plus English equivalents.

### 5.7 Permissions

```csharp
public static class AnnualPlanSessionPermissions
{
    public const string GroupName = "AnnualPlanSessions";
    public const string Default = GroupName + ".Default";
    public const string Create = GroupName + ".Create";
    public const string Substitute = GroupName + ".Substitute";
    public const string SelectQuote = GroupName + ".SelectQuote";
    public const string MarkInProgress = GroupName + ".MarkInProgress";
    public const string MarkCompleted = GroupName + ".MarkCompleted";
    public const string Cancel = GroupName + ".Cancel";
    public const string Dashboard = GroupName + ".Dashboard";
}
```

Default grants:
- Staff → all
- TD/TH → Default + Dashboard (read-only)
- Finance → Default only

---

## 6. Patterns to follow (from Phase 4B-α frontend notes)

**HARD RULES (review docs/GTMS-Phase4B-Alpha-Frontend-Notes.md §5):**

1. `takeUntilDestroyed(destroyRef)` on every service subscription
2. `refreshShell.refresh()` after every state-mutating action
3. Mode-aware HTML gating with `@if (showX())` blocks
4. BehaviorSubject hygiene: clearAttachment on shell init when no id
5. CSS specificity via `:host` when fighting shared stylesheet
6. Section state ↔ action handler coupling: verify component mounted in active state

From general patterns:
- Standalone components, NO NgModules
- Signals everywhere (`signal()`, `computed()`)
- `@if` / `@for` (not `*ngIf` / `*ngFor`)
- `firstValueFrom` (NOT `toPromise`)
- ABP proxies direct (no wrapper services)
- No inline arrow functions in templates
- No inline styles
- `$any($event.target).value` for native inputs
- Individual signals per form field

---

## 7. Verification checklist

**Schema & migration:**
- [ ] Migration v4_10_0 runs cleanly on all 3 demo tenants
- [ ] CourseSession has SessionStatus, ActualStartDate/EndDate nullable, polymorphic check constraint
- [ ] SessionNomination has OriginalEmployeeId, SubstitutionReason, RankId

**Backend:**
- [ ] SessionCreationValidator validates plan approval + uniqueness + rank-match
- [ ] AnnualPlanSessionAppService queue endpoint returns correctly sorted, overdue-flagged items
- [ ] CreateInternalSessionAsync rejects non-Internal CourseType
- [ ] CreateInternalSessionAsync sets Status = Scheduled with dates
- [ ] CreateExternalSessionAsync rejects Internal CourseType
- [ ] CreateExternalSessionAsync sets Status = Planned without dates
- [ ] GetAvailableSubstitutesAsync filters by same RankId
- [ ] SelectPriceQuoteAsync (session arm) is atomic: dates + status + IsSelected all in one transaction
- [ ] Cancel rejected after InProgress
- [ ] Dashboard returns accurate counts

**Frontend:**
- [ ] PAGE A queue sorts by priority + quarter + overdue first
- [ ] PAGE A routes to B-1 for Internal, B-2 for External
- [ ] PAGE B-1 requires dates, creates Scheduled session
- [ ] PAGE B-2 has no date fields, creates Planned session
- [ ] Substitution dialog shows only same-rank options
- [ ] PAGE C shows "—" in dates for Planned sessions
- [ ] PAGE C execution stage column reflects session state
- [ ] PAGE D shell with 8-node pipeline renders correctly
- [ ] PAGE D Section 3/4 hidden for Internal courses
- [ ] PAGE D SelectPriceQuote dialog atomically sets dates + status
- [ ] PAGE E dashboard shows accurate progress
- [ ] PAGE E alerts identify overdue items + stuck-Planned sessions

**Cross-cutting:**
- [ ] Casual course workflow not regressed
- [ ] Smoke test: Internal session full lifecycle (Create → InProgress → Completed)
- [ ] Smoke test: External session full lifecycle (Create Planned → Quotes → Select Winner → Travel → Payments → InProgress → Completed)

---

## 8. Non-regression

- **Do not modify casual course state machine or its components.** Phase 4A/4B are sealed; only add session-specific routes.
- **Do not modify `BudgetReallocationGenerator`.** Sessions don't generate reallocations.
- **Do not auto-create sessions on plan approval.** Manual creation only per Q-A.
- **Do not allow nominee substitution after session creation.** Locked per Q-E.
- **Do not allow cancellation after InProgress.** Per Q-C bound.
- **Do not enforce date-quarter match strictly.** Soft warning OK; don't block.
- **Verify cross-component polymorphism.** Sections 3-5 components from 4B-α/β must already work with sessionId. If they don't, refactor BEFORE adding session detail page.

---

## 9. Commit / checkpoint

```
feat(annual-plan-sessions): execution layer (Phase 4C-α)

Annual plan session creation + execution UI. Staff creates sessions from
approved TrainingPlanItems based on priority + preferred quarter. Internal
courses get dates at creation; external courses start as Planned and get
dates atomically with quote winner selection.

- New SessionStatus enum (Planned/Scheduled/InProgress/Completed/Cancelled/
  FinanciallyClosed)
- CourseSession extended: nullable ActualStartDate/EndDate, status,
  cancellation fields, polymorphic source (TrainingPlanItemId XOR
  TrainingCenterPlanItemId)
- SessionNomination extended: OriginalEmployeeId, SubstitutionReason, RankId
- SessionCreationValidator domain service: plan approval check, uniqueness,
  rank-match substitution validation
- AnnualPlanSessionAppService: queue, create-internal, create-external,
  substitutes lookup, dashboard
- CourseSessionAppService extended: SelectPriceQuoteAsync (atomic with dates),
  status transitions, cancellation
- 5 new pages: queue, create-internal, create-external, list, detail,
  dashboard
- Detail page uses stage-based shell from 4B-α pattern, with mode-aware
  reuse of existing PriceQuote/TravelInstruction/Payments components via
  polymorphic SessionId arm

Locked decisions:
  Q1: 4C split, α first
  Q3: one session per plan item, no batches
  Q-A: manual creation
  Q-B: Staff is creator
  Q-C: cancel allowed before InProgress
  Q-D, Q-E: same-rank substitution at creation only
  Q-F: overdue items stay in queue with warning
  Q-Dates: Internal dates-at-creation; External Planned → Scheduled at
    quote selection (atomic)

Bumps v4.9.2 → v4.10.0
```

---

*End of Phase 4C-α Implementation Prompt — v1.0*
*Estimated effort: ~10-12 days. Largest single phase since 4A — new entity workflow + 5 new pages + extends 4B-α/β components. Frontend is the bigger portion (~7 days).*
*Target file count: 1 migration, ~15 backend files (domain service, AppServices, DTOs, mappers, EF configs, permissions, localization), ~18 frontend files (5 pages + section components + status pipeline + action bar + service extensions + proxy regen).*
