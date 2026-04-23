# GTMS Phase 4A — API List (Gate 2)

**Date:** April 23, 2026
**Scope:** Casual Courses foundation — 3 AppServices, 24 endpoints
**Prerequisites:** Phase 4A Implementation Prompt v1.1 approved, all 8 Round-1 decisions locked

---

## Summary

| AppService | Endpoints | Purpose |
|---|---|---|
| `CasualCourseAppService` | 15 | CRUD + 8 workflow transitions + default cost preview (for UTM form + UGM approval) |
| `CasualCourseFinancialAppService` | 5 | Staff assigns & edits per-item amounts (CHG-07 formula) |
| `CasualCourseNominationAppService` | 5 | Add / remove / replace / return nominees with condition validation |
| **Total** | **25** | — |

---

## 1. `CasualCourseAppService` — 15 endpoints

Extends `CrudAppService<CasualCourse, CasualCourseDto, Guid, CasualCourseGetListInput, CreateUpdateCasualCourseDto>`. All list / get queries respect `EmployeeResolver.IsCurrentUserUnitScopedAsync` (UTM / UGM see their unit only).

| # | Method | Route | Purpose | Role(s) | Request DTO | Response DTO | Permission |
|---|---|---|---|---|---|---|---|
| 1 | `POST` | `/api/app/casual-courses` | Create request (Draft). Requires ≥ 1 nominee with passing conditions; `FundingSource` required when `CourseType ≠ Internal`. | UTM | `CreateUpdateCasualCourseDto` | `CasualCourseDto` | `CasualCourses.Create` |
| 2 | `POST` | `/api/app/casual-courses/estimate-preview` | **Default cost preview (no DB write).** Computes per-item subtotals and grand total from `CourseTypeFinancialItemDefaults` × CHG-07 formula. Used live by UTM form + UGM approval view. No permission check beyond authenticated user — pure calculator. | UTM / UGM / Staff / TD / TH | `EstimatePreviewInput { tenantCourseId, courseType, durationDays, nomineeCount }` | `EstimatePreviewDto { items[], total }` | `CasualCourses.Default` |
| 3 | `GET` | `/api/app/casual-courses` | Paged list with filters: `Year`, `Status[]`, `UnitId?`, `OnlyMyRequests`, `IsReturnedOnly`, `Search`. | all | `CasualCourseGetListInput` | `PagedResultDto<CasualCourseListItemDto>` | `CasualCourses.Default` |
| 4 | `GET` | `/api/app/casual-courses/{id}` | Full detail: course + nominations + financials + latest return note. | all | — | `CasualCourseDetailDto` | `CasualCourses.Default` |
| 5 | `PUT` | `/api/app/casual-courses/{id}` | Update while `Draft` or `ReturnedToCreator`. Diff-based nominee handling (preserve / add / remove). | UTM | `CreateUpdateCasualCourseDto` | `CasualCourseDto` | `CasualCourses.Edit` |
| 6 | `DELETE` | `/api/app/casual-courses/{id}` | Delete only when `Draft`. Cascade to financials + nominations in single UoW. | UTM | — | — | `CasualCourses.Delete` |
| 7 | `POST` | `/api/app/casual-courses/{id}/submit` | `Draft → Submitted` (first submit) or `ReturnedToCreator → <ReturnedFromStatus>` (resubmit per tweak 2). Runs `CasualCourseValidator.ValidateForSubmitAsync`. | UTM | — | `CasualCourseDto` | `CasualCourses.Submit` |
| 8 | `POST` | `/api/app/casual-courses/{id}/ugm-approve` | `Submitted → UGMApproved`. Optional approval note. | UGM | `NoteDto?` | `CasualCourseDto` | `CasualCourses.Approve` |
| 9 | `POST` | `/api/app/casual-courses/{id}/start-review` | `UGMApproved → UnderReview`. Explicit Staff action (avoids the Phase 3 bug #6 trap). | Staff | — | `CasualCourseDto` | `CasualCourses.Review` |
| 10 | `PUT` | `/api/app/casual-courses/{id}/assign-scenario` | Write `FundingScenario` + `EstimatedTotalCost` + upsert financial assignments (with `Source` derived from scenario). `Commit=false` stays in `UnderReview`; `Commit=true` transitions to `StaffReviewed` (tweak 1). | Staff | `AssignScenarioDto { commit, ... }` | `CasualCourseDetailDto` | `CasualCourses.Review` |
| 11 | `POST` | `/api/app/casual-courses/{id}/td-approve` | Cost gate (`EstimatedTotalCost > 0`). `StaffReviewed → TDApproved`. | TD | `NoteDto?` | `CasualCourseDto` | `CasualCourses.TDApprove` |
| 12 | `POST` | `/api/app/casual-courses/{id}/head-approve` | Cost gate (double-check). `TDApproved → THApproved`. Terminal approval. | TH | `NoteDto?` | `CasualCourseDto` | `CasualCourses.HeadApprove` |
| 13 | `POST` | `/api/app/casual-courses/{id}/return` | Any stage → `ReturnedToCreator`. Writes `AppPlanNote` (type `CasualCourse`), sets `IsReturned=true`, stores `LastReturnNoteId` + `ReturnedFromStatus` (tweak 2). | UGM / Staff / TD / TH | `ReturnDto` (reason required, min 10 chars) | `CasualCourseDto` | `CasualCourses.Return` |
| 14 | `POST` | `/api/app/casual-courses/{id}/resubmit` | Alias of `Submit` when status is `ReturnedToCreator`. Clears `IsReturned` + resumes at `ReturnedFromStatus` (tweak 2). | UTM | — | `CasualCourseDto` | `CasualCourses.Submit` |
| 15 | `POST` | `/api/app/casual-courses/{id}/reject` | Any approval stage → `Rejected` (terminal). Writes `RejectedReason`. | UGM / Staff / TD / TH | `RejectDto` (reason required) | `CasualCourseDto` | `CasualCourses.Reject` |

---

## 2. `CasualCourseFinancialAppService` — 5 endpoints

Item-level Staff operations during `UnderReview`. All guards check parent `CasualCourse.Status == UnderReview`.

| # | Method | Route | Purpose | Role | Request DTO | Response DTO | Permission |
|---|---|---|---|---|---|---|---|
| 16 | `GET` | `/api/app/casual-course-financials` | List financials for one casual course (via query param `casualCourseId`). Ordered by `FinancialItem.SortOrder`. Used by Review + Approval pages. | all | `{ casualCourseId: Guid }` | `ListResultDto<CasualCourseFinancialDto>` | `CasualCourses.Default` |
| 17 | `POST` | `/api/app/casual-course-financials/auto-fill` | Additive auto-fill from `CourseTypeFinancialItemDefaults` based on `CourseType`. Applies CHG-07 formula via `FinancialItemDefaultResolver.ComputeSubtotal`. Skips existing rows. | Staff | `{ casualCourseId: Guid }` | `ListResultDto<CasualCourseFinancialDto>` | `CasualCourses.Review` |
| 18 | `POST` | `/api/app/casual-course-financials` | Add one custom item to a casual course. | Staff | `CreateCasualCourseFinancialDto` | `CasualCourseFinancialDto` | `CasualCourses.Review` |
| 19 | `PUT` | `/api/app/casual-course-financials/{id}/amount` | Inline edit of `EstimatedAmountOMR` + optional `Notes`. Triggers `EstimatedTotalCost` recompute on parent. | Staff | `UpdateAmountDto { amount, notes? }` | `CasualCourseFinancialDto` | `CasualCourses.Review` |
| 20 | `DELETE` | `/api/app/casual-course-financials/{id}` | Remove one item. Triggers `EstimatedTotalCost` recompute. | Staff | — | — | `CasualCourses.Review` |

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
| `AssignScenarioDto` | `FundingScenario: enum`, `EstimatedTotalCost: decimal`, `FinancialItems: AssignmentLineDto[]`, `Commit: bool` | `Commit=true` transitions to `StaffReviewed`; `Commit=false` saves as draft of review. |
| `AssignmentLineDto` | `Id: Guid?` (null = new), `FinancialItemId: Guid`, `Amount: decimal`, `Notes: string?` | Source is inferred server-side from scenario. |
| `CasualCourseListItemDto` | Flat row for the list page — includes `CourseName`, `UnitName`, `RequesterName`, `NomineesCount`, `EstimatedTotalCost?`, `Status`, `IsReturned`, `LastReturnReason?`. | Denormalized for grid perf. |
| `CasualCourseDetailDto` | `CasualCourseDto` + `Nominations[]` + `Financials[]` + `LatestReturnNote?` + `ConditionSummary` | For Review / Approval pages — single call hydrates everything. |
| `EstimatePreviewInput` | `TenantCourseId: Guid`, `CourseType: enum`, `DurationDays: int`, `NomineeCount: int` | Inputs for the default-cost preview — everything the frontend knows while filling the form. |
| `EstimatePreviewDto` | `Items: EstimatePreviewItemDto[]`, `Total: decimal`, `CourseType: enum`, `ComputedAt: DateTime` | Response from `/estimate-preview`. `Items[]` holds one row per default financial item. |
| `EstimatePreviewItemDto` | `FinancialItemId`, `FinancialItemName`, `IsPerDay: bool`, `IsPerNominee: bool`, `Rate: decimal`, `EffectiveDays: int`, `EffectiveCount: int`, `ComputedAmount: decimal` | Single row in the preview grid — shows the formula inputs so frontend can render the "5+1+1 days × 2 nominees × 80" breakdown. |

---

## Response envelopes

| Scenario | Shape |
|---|---|
| Success — entity | `200 OK` with DTO body |
| Success — void | `204 No Content` |
| Validation fail | `400` with `{ error: { code, message, details } }` — codes like `Training:CasualCourse:FundingSourceRequired`, `Training:CasualCourse:InvalidStatusTransition`, `Training:CasualCourse:CostRequired`, `Training:CasualCourse:NoNominations`, `Training:CasualCourse:ConditionsFailed` |
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
