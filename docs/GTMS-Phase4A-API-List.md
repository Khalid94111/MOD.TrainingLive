# GTMS Phase 4A — API List (Gate 2)

**Date:** April 23, 2026 — last updated April 26, 2026 (v4.7.0, Patch 5)
**Scope:** Casual Courses foundation — 3 AppServices, 25 endpoints
**Prerequisites:** Phase 4A Implementation Prompt v1.1 approved, all 8 Round-1 decisions locked

> **v4.7.0 (Patch 5) changes:**
> - `CasualCourseFinancial` → `CasualCourseFinancialItem` (entity, table, routes, DTOs).
> - `CasualCourse.FundingSource` → split into `FundingSourceName` + `FundingSourceVoteCode`; both required when external.
> - New `CasualCourse.CourseCost` field — UTM enters in form, used as seed.
> - `CreateAsync` no longer auto-populates financial rows; `UpdateAsync` no longer recomputes them. UTM's view becomes a read-only calculator.
> - Auto-fill is now triggered by `AssignScenarioAsync` on the first scenario pick (no placeholder Source phase).
> - **New endpoint** `POST /casual-courses/calculate-preview` — server-side calculator for UTM (PAGE 4.2 Section E) and UGM (PAGE 4.4) views. Pure function, no DB writes.
>
> **v4.6.0 (Patch 4) — historical:** UTM entered full breakdown at creation; reverted in v4.7.0.

---

## Summary

| AppService | Endpoints | Purpose |
|---|---|---|
| `CasualCourseAppService` | 15 | CRUD + 8 workflow transitions + server-side calculator (v4.7.0) |
| `CasualCourseFinancialItemAppService` | 5 | Staff assigns & edits per-item amounts (CHG-07 formula) — renamed v4.7.0 |
| `CasualCourseNominationAppService` | 5 | Add / remove / replace / return nominees with condition validation |
| **Total** | **25** | — |

---

## 1. `CasualCourseAppService` — 15 endpoints

Extends `CrudAppService<CasualCourse, CasualCourseDto, Guid, CasualCourseGetListInput, CreateUpdateCasualCourseDto>`. All list / get queries respect `EmployeeResolver.IsCurrentUserUnitScopedAsync` (UTM / UGM see their unit only).

| # | Method | Route | Purpose | Role(s) | Request DTO | Response DTO | Permission |
|---|---|---|---|---|---|---|---|
| 1 | `POST` | `/api/app/casual-courses` | Create request (Draft). Persists course + nominations only — financial rows are NOT created here in v4.7.0. UTM's `CourseCost` is stored on the entity for later seeding. | UTM | `CreateUpdateCasualCourseDto` | `CasualCourseDto` | `CasualCourses.Create` |
| 2 | `GET` | `/api/app/casual-courses` | Paged list with filters: `Year`, `Status[]`, `UnitId?`, `OnlyMyRequests`, `IsReturnedOnly`, `Search`. | all | `CasualCourseGetListInput` | `PagedResultDto<CasualCourseListItemDto>` | `CasualCourses.Default` |
| 3 | `GET` | `/api/app/casual-courses/{id}` | Full detail: course + nominations + financial items + latest return note. | all | — | `CasualCourseDetailDto` | `CasualCourses.Default` |
| 4 | `PUT` | `/api/app/casual-courses/{id}` | Update while `Draft` or `ReturnedToCreator`. Diff-based nominee handling. v4.7.0: no rank-row recompute and no `FinancialOverrides` — financial rows do not exist yet during these statuses. | UTM | `CreateUpdateCasualCourseDto` | `CasualCourseDto` | `CasualCourses.Edit` |
| 5 | `DELETE` | `/api/app/casual-courses/{id}` | Delete only when `Draft`. Cascade to financial items + nominations in single UoW. | UTM | — | — | `CasualCourses.Delete` |
| 6 | `POST` | `/api/app/casual-courses/{id}/submit` | `Draft → Submitted`. Runs `CasualCourseValidator.ValidateForSubmitAsync` — requires `FundingSourceName` + `FundingSourceVoteCode` for external courses; financial rows are no longer required (v4.7.0). | UTM | — | `CasualCourseDto` | `CasualCourses.Submit` |
| 7 | `POST` | `/api/app/casual-courses/{id}/ugm-approve` | `Submitted → UGMApproved`. Optional approval note. | UGM | `NoteDto?` | `CasualCourseDto` | `CasualCourses.Approve` |
| 8 | `POST` | `/api/app/casual-courses/{id}/start-review` | `UGMApproved → UnderReview`. Explicit Staff action. | Staff | — | `CasualCourseDto` | `CasualCourses.Review` |
| 9 | `PUT` | `/api/app/casual-courses/{id}/assign-scenario` | Pick `FundingScenario` + optional rank-row `Adjustments[]`. **First call** auto-fills the financial-item tree from `CourseTypeFinancialItemDefaults` with correct Source per row (v4.7.0); `CourseCost` seeds the course-cost row's rate (`RateSource = "FromUTMForm"`). **Subsequent calls** re-derive Source on existing rows without touching Staff's manual edits. `Commit=true` transitions `UnderReview → StaffReviewed`. | Staff | `AssignScenarioDto { fundingScenario, adjustments?, commit }` | `CasualCourseDto` | `CasualCourses.Review` |
| 10 | `POST` | `/api/app/casual-courses/{id}/td-approve` | Cost gate (`EstimatedTotalCost > 0`). `StaffReviewed → TDApproved`. | TD | `NoteDto?` | `CasualCourseDto` | `CasualCourses.TDApprove` |
| 11 | `POST` | `/api/app/casual-courses/{id}/head-approve` | Cost gate (double-check). `TDApproved → THApproved`. Terminal approval. | TH | `NoteDto?` | `CasualCourseDto` | `CasualCourses.HeadApprove` |
| 12 | `POST` | `/api/app/casual-courses/{id}/return` | Any stage → `ReturnedToCreator`. Writes `AppPlanNote` (type `CasualCourse`), sets `IsReturned=true`, stores `LastReturnNoteId` + `ReturnedFromStatus`. | UGM / Staff / TD / TH | `ReturnDto` (reason required, min 10 chars) | `CasualCourseDto` | `CasualCourses.Return` |
| 13 | `POST` | `/api/app/casual-courses/{id}/resubmit` | `ReturnedToCreator → <ReturnedFromStatus>`. Clears return flags. | UTM | — | `CasualCourseDto` | `CasualCourses.Submit` |
| 14 | `POST` | `/api/app/casual-courses/{id}/reject` | Any approval stage → `Rejected` (terminal). Writes `RejectedReason`. | UGM / Staff / TD / TH | `RejectDto` (reason required) | `CasualCourseDto` | `CasualCourses.Reject` |
| 15 | `POST` | `/api/app/casual-courses/calculate-preview` | **v4.7.0** — server-side calculator. Pure function over `CourseType`, `DurationDays`, `NomineeEmployeeIds[]`, `CourseCost?`. Returns per-item / per-rank breakdown with grand total. No DB writes; `FundingScenario` deliberately not part of the input (it isn't picked yet). Powers PAGE 4.2 Section E (UTM live updates) and PAGE 4.4 UGM variant. | all (auth) | `CalculatePreviewInput` | `CalculatePreviewDto` | `CasualCourses.Default` |

---

## 2. `CasualCourseFinancialItemAppService` — 5 endpoints

Item-level Staff operations during `UnderReview`. All guards check parent `CasualCourse.Status == UnderReview`. Renamed from `CasualCourseFinancialAppService` in v4.7.0 (entity + route rename).

| # | Method | Route | Purpose | Role | Request DTO | Response DTO | Permission |
|---|---|---|---|---|---|---|---|
| 16 | `GET` | `/api/app/casual-course-financial-items` | List financial items for one casual course (via query param `casualCourseId`). Ordered by `FinancialItem.SortOrder`. Used by Review + Approval pages. | all | `{ casualCourseId: Guid }` | `ListResultDto<CasualCourseFinancialItemDto>` | `CasualCourses.Default` |
| 17 | `POST` | `/api/app/casual-course-financial-items/auto-fill` | Additive auto-fill from `CourseTypeFinancialItemDefaults` based on `CourseType`. v4.7.0 accepts optional `courseCostSeed` — when provided and a `CourseCost` financial item exists in defaults, that row's rate is seeded from this value with `RateSource="FromUTMForm"`. Skips existing rows. Requires `FundingScenario` already on the course. | Staff | `{ casualCourseId: Guid, courseCostSeed?: decimal }` | `ListResultDto<CasualCourseFinancialItemDto>` | `CasualCourses.Review` |
| 18 | `POST` | `/api/app/casual-course-financial-items` | Add one custom item to a casual course. | Staff | `CreateCasualCourseFinancialItemDto` | `CasualCourseFinancialItemDto` | `CasualCourses.Review` |
| 19 | `PUT` | `/api/app/casual-course-financial-items/{id}/amount` | Inline edit of `EstimatedAmountOMR` + optional `Notes`. Triggers `EstimatedTotalCost` recompute on parent. | Staff | `UpdateAmountDto { amount, notes? }` | `CasualCourseFinancialItemDto` | `CasualCourses.Review` |
| 20 | `DELETE` | `/api/app/casual-course-financial-items/{id}` | Remove one item. Triggers `EstimatedTotalCost` recompute. | Staff | — | — | `CasualCourses.Review` |

---

## 3. `CasualCourseNominationAppService` — 5 endpoints

Diff-based child management called directly from the Review page (single-nominee actions) or indirectly through `CasualCourseAppService.UpdateAsync` (bulk replace).

| # | Method | Route | Purpose | Role | Request DTO | Response DTO | Permission |
|---|---|---|---|---|---|---|---|
| 21 | `GET` | `/api/app/casual-course-nominations` | List nominations for one casual course. With details: employee, rank, position, latest condition snapshot. | all | `{ casualCourseId: Guid }` | `ListResultDto<CasualCourseNominationDto>` | `CasualCourses.Default` |
| 22 | `POST` | `/api/app/casual-course-nominations` | Add a nominee. Runs `NominationConditionValidator.ValidateByTenantCourseAsync`. Allowed only when parent status is `Draft` or `ReturnedToCreator`. | UTM | `{ casualCourseId: Guid, employeeId: Guid }` | `CasualCourseNominationDto` | `CasualCourses.Edit` |
| 23 | `DELETE` | `/api/app/casual-course-nominations/{id}` | Remove a nominee. Same state guard. Recomputes `EstimatedAmount` on per-nominee financials. | UTM | — | — | `CasualCourses.Edit` |
| 24 | `POST` | `/api/app/casual-course-nominations/{id}/return` | Nomination-level return — status `Returned`, writes note, sets `IsReturned=true`. Parent casual course also flagged unless all nominations are cleared. | Staff | `ReturnDto` | `CasualCourseNominationDto` | `CasualCourses.Return` |
| 25 | `PUT` | `/api/app/casual-course-nominations/{id}/replace` | Swap one employee for another (preserves nomination row ID + audit trail). Re-runs condition validator. | UTM | `{ newEmployeeId: Guid }` | `CasualCourseNominationDto` | `CasualCourses.Edit` |

---

## Cross-cutting DTOs (shared across endpoints)

| DTO | Fields | Notes |
|---|---|---|
| `NoteDto` | `Text: string (required)`, `IsReturnReason: bool (default false)` | Reused from Phase 3 Changes; `EntityType` inferred server-side. |
| `ReturnDto` | `Reason: string (required, min 10)` | Creates `AppPlanNote` with `IsReturnReason=true`. |
| `RejectDto` | `Reason: string (required, min 10)` | Writes `CasualCourse.RejectedReason`. |
| `AssignScenarioDto` | `FundingScenario: enum`, `Adjustments?: StaffAdjustmentDto[]`, `Commit: bool` | v4.6.0 simplified shape; v4.7.0 — first call also auto-fills the financial-item tree. `Commit=true` transitions to `StaffReviewed`. |
| `StaffAdjustmentDto` | `CasualCourseFinancialItemRankId: Guid`, `NewRatePerUnitOMR: decimal`, `AdjustmentNote?: string` | Targeted rate override on a single rank row. |
| `CasualCourseListItemDto` | Flat row for the list page — includes `CourseName`, `UnitName`, `RequesterName`, `NomineesCount`, `EstimatedTotalCost?`, `FundingSourceName?`, `FundingSourceVoteCode?`, `Status`, `IsReturned`, `LastReturnReason?`. | Denormalized for grid perf. |
| `CasualCourseDetailDto` | `CasualCourseDto` + `Nominations[]` + `FinancialItems[]` + `LatestReturnNote?` + `ConditionSummary` | For Review / Approval pages — single call hydrates everything. |
| `CalculatePreviewInput` | `CourseType: enum`, `DurationDays: int`, `NomineeEmployeeIds: Guid[]`, `CourseCost?: decimal` | **v4.7.0** — input for `/casual-courses/calculate-preview`. No `FundingScenario` (not picked yet at UTM/UGM time). |
| `CalculatePreviewDto` | `Items: PreviewItemDto[]`, `TotalOMR: decimal`, `CourseType: enum`, `ComputedAt: DateTime` | **v4.7.0** — response. Server-side projection only; nothing persisted. |
| `PreviewItemDto` | `FinancialItemId`, `FinancialItemNameAr`, `IsPerDay`, `IsPerNominee`, `EffectiveDays`, `SubtotalOMR`, `RankBreakdown?: PreviewRankRowDto[]` | One per default financial item. `RankBreakdown` populated only for per-nominee items. |
| `PreviewRankRowDto` | `RankId`, `RankNameAr`, `NomineeCount`, `RatePerUnitOMR`, `SubtotalOMR` | One per rank under a per-nominee item. |

---

## Response envelopes

| Scenario | Shape |
|---|---|
| Success — entity | `200 OK` with DTO body |
| Success — void | `204 No Content` |
| Validation fail | `400` with `{ error: { code, message, details } }` — codes like `Training:CasualCourse:FundingSourceNameRequired`, `Training:CasualCourse:FundingSourceVoteCodeRequired`, `Training:CasualCourse:ScenarioRequiredBeforeFinancials`, `Training:CasualCourse:InvalidStatusTransition`, `Training:CasualCourse:CostRequired`, `Training:CasualCourse:NoNominations`, `Training:CasualCourse:ConditionsFailed` |
| Auth fail | `401 / 403` |
| Not found | `404` with `Training:CasualCourse:NotFound` |

---

## Derived endpoint counts

| Metric | Value |
|---|---|
| New endpoints (Phase 4A) | **25** |
| Workflow-transition endpoints | 8 (submit / UGM / start-review / assign-scenario / TD / TH / return / reject) |
| CRUD endpoints | 5 (create / read-list / read-one / update / delete) |
| Preview / calculator endpoints | 1 (estimate-preview — new) |
| Resubmit endpoint | 1 |
| Child-item endpoints (financials + nominations) | 10 |

**Cumulative Phase-end endpoint count:** ~135 (v4.4) + 25 = **~160** by end of Phase 4A.

---

## What's NOT in this list (intentionally deferred)

| Need | Owner phase | Notes |
|---|---|---|
| `POST /casual-course-price-quotes` + quote comparison | Phase 4B | Polymorphic reuse of existing `PriceQuotes` entity; `CasualCourses.SelectedPriceQuoteId` already declared |
| `POST /casual-courses/{id}/confirm-payment` | Phase 4B | Triggers `BudgetReallocation` generation for scenarios 2 and 3 |
| `POST /casual-courses/{id}/travel-instructions` | Phase 4B | Per-nominee travel setup (Nebras handoff deferred to integration) |
| Stats / summary endpoint for dashboard cards | Phase 5 | Current list-page cards computed client-side from filter results; dedicated stats endpoint if dashboard pages need it |

---

## Approval checklist

Before moving to the backend implementation prompt:

- [ ] 15 `CasualCourseAppService` endpoints reviewed — routes / roles / DTOs match expected flow (including the new `estimate-preview` calculator)
- [ ] 5 `CasualCourseFinancialAppService` endpoints reviewed — covers Staff review operations adequately
- [ ] 5 `CasualCourseNominationAppService` endpoints reviewed — diff-based handling from Phase 3 is acceptable
- [ ] DTO naming matches ABP convention (`XxxGetListInput`, not `GetXxxListDto`)
- [ ] Permissions layout under `CasualCourses` group is acceptable
- [ ] No missing endpoint the page mockups require (list stats, nominee condition check preview, etc.)

---

*End of API List — Gate 2 — April 23, 2026*
