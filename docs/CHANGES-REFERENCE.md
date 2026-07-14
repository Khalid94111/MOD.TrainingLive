# GTMS — Changes Reference Log

> Last updated: 2026-06-04
>
> This document tracks all backend & frontend modifications made during the flattening redesign sprint. Use it as a reference before making further updates.

---

## 1. Annual Plans List (`training/plans`)

### 1.1 Frontend

| File | Change | Details |
|------|--------|---------|
| `angular/src/app/Projects/plans/annual-plan-list/annual-plan-list.component.html` | **Complete redesign** | Gradient hero header (blue), 6 stat cards (Total / Open / Under Review / Approved / Returned / Total Cost), modern filter bar (search + status), custom data table, refreshed workflow card showing the 6 approval steps, modern modal dialog. |
| `angular/src/app/Projects/plans/annual-plan-list/annual-plan-list.ts` | **Signals + stats + filters** | Added `searchText`, `filterStatus` signals and `filteredPlans` computed. Added `totalPlans`, `openPlansCount`, `submittedPlansCount`, `approvedPlansCount`, `returnedPlansCount`, `totalEstimatedCost` computed signals. Updated table column label from "عدد البنود" to "عدد الدورات" and tooltip from "إدخال البنود" to "إدخال الدورات". No business logic changed. |
| `angular/src/app/Projects/plans/annual-plan-list/annual-plan-list.component.scss` | **New design system styles** | Added `.header-banner`, `.stat-cards-row`, `.main-card`, `.filters-bar-modern`, `.data-table`, `.workflow-card`, `.modal-modern`, and responsive rules. Kept existing `.row-returned` highlight. |

### 1.2 Notes

- All UI text on this page is still hard-coded Arabic. A future pass can externalize it into `Training.AnnualPlansList.*` localization keys if needed.
- Workflow visualization was kept but redesigned as a card with numbered steps.

---

## 2. Plan Entry (`training/plans/entry`)

### 2.1 Terminology Change

Replaced user-facing term **"بند / بنود" (item / items)** with **"دورة / دورات" (course / courses)** across the plan entry page and related localization keys. A plan item in this context is always a `TrainingPlanItem` linked to a `TenantCourse`, so the new term is more accurate for end users.

### 2.2 Frontend

| File | Change | Details |
|------|--------|---------|
| `angular/src/app/Projects/plans/plan-entry/plan-entry.component.html` | **Complete redesign** | Gradient hero header (blue), 5 stat cards (Courses / Nominees / Total Cost / Returned / Missing Nominees), info bar with plan window + status, modern filter bar, refreshed accordion/flat table layout, modern modal dialog with clearer section labels. |
| `angular/src/app/Projects/plans/plan-entry/plan-entry.component.ts` | **Stats signals + terminology** | Added `totalCourses`, `totalNominees`, `totalPlanCost`, `returnedCoursesCount`, `coursesMissingNominees` computed signals. Updated dialog title, delete confirmation, and notes title from "بند" to "دورة". No business logic changed. |
| `angular/src/app/Projects/plans/plan-entry/plan-entry.component.scss` | **New design system styles** | Added `.header-banner`, `.stat-cards-row`, `.info-bar`, `.filters-bar-modern`, `.data-table`, `.modal-modern`, and responsive rules. Kept existing accordion, return banner, detail card, and dialog styles. |

### 2.3 Localization Keys Updated

```
Permission:TrainingPlans.SubmitItems        تقديم دورات الخطة / Submit Plan Courses
Training.PlanItems                          دورات الخطة / Plan Courses
Training.Menu.PlanEntry                     إدخال دورات الخطة / Plan Course Entry
Training.TrainingPlanItem                   دورة الخطة / Plan Course
Training.TrainingPlanItems                  دورات الخطة / Plan Courses
Training.AddPlanItem                        إضافة دورة / Add Plan Course
Permission:TrainingPlan.Submit              إرسال دورات الخطة / Submit Plan Courses
Permission:TrainingPlanItem                 إدارة دورات الخطة / Plan Course Management
Permission:TrainingPlanItem.Create          Create Plan Courses
Permission:TrainingPlanItem.Update          Update Plan Courses
Permission:TrainingPlanItem.Delete          Delete Plan Courses
Training:CenterPlanItem:PlanNotDraft        لا يمكن تعديل الدورات... / Courses can only be modified...
Training:TrainingPlanItem:NotFound          دورة الخطة غير موجودة / Plan course not found
Training:TrainingPlanItem:CannotReturnInThisStatus  ...الدورة... / Plan courses can only be returned...
Training:TrainingPlanItem:MustHaveAtLeastOneNominee ...الدورة... / ...creating a plan course
Training.TrainingPlan.SubmittedBannerText   مراجعة الدورات / reviewing courses
Training.PlanReview.ConsolidatedItems       دورات الخطة الموحدة / Consolidated Plan Courses
Training.PlanReview.NoItems                 لا توجد دورات في الخطة / No plan courses yet
Training.PlanReview.SelectItemHint          اختر دورة... / Select a course...
Training.AnnualPlan.SessionsQueue.Title     دورات الخطة بانتظار إنشاء جلسة / Plan Courses Awaiting Session Creation
Training.AnnualPlan.SessionsQueue.Subtitle  دورات الخطة السنوية... / TH-approved plan courses...
Training.AnnualPlan.SessionsQueue.NoItems   No plan courses currently awaiting session creation.
Training.AnnualPlan.CreateSession.MissingPlanItem   دورة الخطة المطلوبة / Plan course not found.
Training.AnnualPlan.CreateSession.NoNominees        ...دورة الخطة هذه / ...this plan course.
Training.AnnualPlan.Substitute.SameRankHint ...في دورة الخطة / ...on this plan course.
Training.AnnualPlan.Dashboard.Subtitle      تقدّم دورات الخطة... / Approved plan-course progress...
Training.AnnualPlan.Dashboard.Alert.OverduePlanItem  دورة خطة متأخّرة / Overdue plan course
Training.Menu.SessionsQueue                 Courses Awaiting Session
```

### 2.4 Notes

- Financial-item keys (`Training.FinancialItems.*`, `Training.PlanReview.AddFinancialItem`, etc.) intentionally kept as "بند مالي" / "financial item" because they refer to budget line items, not training courses.
- The `TrainingPlanItem` entity/DTO names remain unchanged in code to preserve API contracts and backend mapping.

---

## 3. Financial Items (`training/finance/financial-items`)

### 3.1 Backend

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Domain/Training/Finance/FinancialItem.cs` | **Flattened entity** | Removed parent/child business logic. `ParentId` kept in DB for migration safety only. `ItemType` now allowed on ALL items (was leaf-only). Removed `IsGeneral` auto-calculation. |
| `src/MOD.Training.Application/Training/Finance/FinancialItemAppService.cs` | **Flattened service** | Removed max-two-level validation, `HasChildren` check, `IsGeneral` logic. `CreateAsync` / `UpdateAsync` simplified. Returns flat list ordered by `voteCode`. Budget seeding gated on `!input.ParentId.HasValue` during transition. |
| `src/MOD.Training.Application.Contracts/Training/Finance/Dtos/FinancialItemDtos.cs` | **DTO updated** | `CreateUpdateFinancialItemDto` and `FinancialItemDto` removed `ParentId` / `IsGeneral`. `ItemType` nullable on both. |
| `src/MOD.Training.Domain/Training/Managers/FundingScenarioSourceResolver.cs` | **No change** | Still uses `FinancialItemType` for scenario resolution. |
| `src/MOD.Training.Application/Training/Payments/TravelAllowancePaymentAppService.cs` | **No change** | Still relies on `ItemType` to break down travel components. |
| `src/MOD.Training.EntityFrameworkCore/Training/TrainingDbContextModelCreatingExtensions.cs` | **DB config** | Removed `ParentId` FK constraint & index on `TrnFinancialItems` (flat list). Added `TenantId + NameAr` index. |

### 3.2 Frontend

| File | Change | Details |
|------|--------|---------|
| `angular/.../financial-items.component.html` | **Complete redesign** | Gradient hero header, 4 stat cards (Total / Active / Per-Day / Per-Nominee), filter pills, custom data table with animations, inline rank-amounts panel, modern dialogs. |
| `angular/.../financial-items.component.ts` | **Signals + Localization** | All hard-coded Arabic text removed. Added `LocalizationPipe` import. `itemTypeLabel()` now uses `l.t()` instead of static Arabic map. Confirm dialogs localized. |
| `angular/.../financial-items.component.scss` | **New design system** | 1300+ lines matching `gtms-design.scss` patterns. |

### 3.3 Localization Keys Added

```
Training.FinancialItems.HeaderTitle
Training.FinancialItems.HeaderDesc
Training.FinancialItems.AddNew
Training.FinancialItems.TotalItems
Training.FinancialItems.ActiveItems
Training.FinancialItems.PerDayItems
Training.FinancialItems.PerNomineeItems
Training.FinancialItems.SearchPlaceholder
Training.FinancialItems.Status.{Label,All,Active,Inactive}
Training.FinancialItems.PerDay.{Label,All,Yes,No}
Training.FinancialItems.PerNominee.{Label,All,Yes,No}
Training.FinancialItems.Table.{Code,Name,PerDay,PerNominee,DefaultAmount,RankAmounts,Status,Actions,Yes,No}
Training.FinancialItems.NoMatches
Training.FinancialItems.NoMatchesHint
Training.FinancialItems.AddFirstItem
Training.FinancialItems.RankAmounts.{Title,NoData,DefaultUsed,DefaultAmountLabel,Add,AddAmount,Edit,Delete,DeleteConfirm,Rank,SelectRank,Amount}
Training.FinancialItems.FormulaPreview.{Title,Rate,Days,Nominees}
Training.FinancialItems.Dialog.{EditTitle,EditSubtitle,AddTitle,AddSubtitle,RankEditTitle,RankAddTitle,RankSubtitle}
Training.FinancialItems.Form.{BasicInfo,NameAr,NameEn,VoteCode,DefaultAmount,DefaultAmountHint,Classification,Unclassified,Settings,PerDay,PerDayDesc,PerNominee,PerNomineeDesc,ExtraDays,BeforeCourse,BeforeCourseHint,AfterCourse,AfterCourseHint,TravelPreview,StatusActive,StatusInactive,StatusActiveDesc,StatusInactiveDesc}
Training.FinancialItems.Confirm.DeleteItem
Training.FinancialItems.ItemType.{CourseCost,Ticket,Insurance,Visa,Allowance,Clothing,Other}
```

---

## 4. Course Type Financial Defaults (`training/finance/defaults`) — REMOVED

> **Note:** This feature was completely removed in [Section 13](#13-removal-of-course-type-financial-defaults-trainingfinancedefaults). The information below is retained for historical reference only.

### 4.1 Backend

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Application/Training/Finance/CourseTypeFinancialDefaultAppService.cs` | **Uses AutoMapper** | Injected `IMapper`. `GetListAsync` and `CreateAsync` now use `mapper.Map<>()` instead of manual DTO construction. |
| `src/MOD.Training.Application/Training/Mapper/TrainingAutoMapperProfile.cs` | **NEW — AutoMapper Profile** | Maps `CourseTypeFinancialItemDefault ↔ CourseTypeFinancialItemDefaultDto` (ignores enriched fields `NameAr/NameEn/Code`). Maps `CreateCourseTypeFinancialItemDefaultDto → CourseTypeFinancialItemDefault`. |
| `src/MOD.Training.Application/TrainingApplicationModule.cs` | **AutoMapper registration** | Added `typeof(AbpAutoMapperModule)` to `[DependsOn]`. `ConfigureServices` registers `AddAutoMapperObjectMapper` + `AddAutoMapper(Assembly)`. |
| `src/MOD.Training.Application/MOD.Training.Application.csproj` | **Package added** | `Volo.Abp.AutoMapper` v10.1.1 |
| `src/MOD.Training.Application/Training/Mapper/CourseTypeFinancialItemDefaultMappers.cs` | **Created then deleted** | Mapperly mappers were created first, then replaced by AutoMapper, then deleted when the feature was removed. |

### 4.2 Frontend

| File | Change | Details |
|------|--------|---------|
| `angular/.../financial-item-defaults.component.html` | **Complete redesign** | Matches `financial-items` style: gradient header (teal), 2 course-type stat cards (clickable), info banner, custom data table with sort-order arrows ↑↓, empty state, modern modal dialog. Replaced DevExtreme `dx-data-grid` + `dx-popup` with custom HTML/CSS. |
| `angular/.../financial-item-defaults.component.ts` | **Signals + counts** | Added `externalIntlCount` / `externalLocalCount` signals loaded in parallel. Added `moveUp()` / `moveDown()` with `updateSortOrder` API call. `confirm()` on delete. SelectBox display shows `nameAr (voteCode)`. |
| `angular/.../financial-item-defaults.component.scss` | **Complete rewrite** | Matches `financial-items` design system. |

### 4.3 Localization Keys Added

```
Training.CourseTypeDefaults.NoItems
Training.CourseTypeDefaults.NoItemsHint
Training.CourseTypeDefaults.ReorderHint
Training.CourseTypeDefaults.AllAssigned
Training.Common.MoveUp
Training.Common.MoveDown
```

### 4.4 Text Updated

| Key | Old (AR) | New (AR) |
|-----|----------|----------|
| `Training.CourseTypeDefaults.Info` | *"اختر نوع الدورة من القائمة اليمنى..."* | *"اختر نوع الدورة ثم أضف البنود المالية المطلوبة. يمكن ترتيب البنود باستخدام الأسهم."* |

---

## 5. Exchange Rates (`training/finance/exchange-rates`)

### 5.1 Backend

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Domain/Training/Finance/ExchangeRate.cs` | **Notes persistence** | `Notes` property added to entity (was phantom field in UI only). |
| `src/MOD.Training.Application.Contracts/Training/Finance/Dtos/ExchangeRateDtos.cs` | **Notes in DTOs** | `CreateExchangeRateDto` and `ExchangeRateDto` include `Notes`. |
| `src/MOD.Training.EntityFrameworkCore/Migrations/20260607185526_AddExchangeRateNotes.cs` | **NEW Migration** | Adds `Notes` column (`nvarchar(500)`) to `TrnExchangeRates`. Also applies pending `TrnFinancialItems` schema changes (dropped `ParentId` FK/index — see §1.1). |
| `src/MOD.Training.EntityFrameworkCore/Migrations/TrainingDbContextModelSnapshot.cs` | **Updated** | Reflects new migration state. |

### 5.2 Frontend

| File | Change | Details |
|------|--------|---------|
| `angular/.../exchange-rates.component.html` | **Complete redesign** | Gradient header (amber/gold), active-rate hero card, 3 stat cards (Total / Active / Inactive), custom data table, empty state, skeleton loading, modern modal dialog. Removed all DevExtreme components (`dx-data-grid`, `dx-popup`, `dx-button`, `dx-text-box`, `dx-number-box`). |
| `angular/.../exchange-rates.component.ts` | **Signals + localization** | Added `isLoading`, `totalCount`, `activeCount`, `inactiveCount` signals. `confirm()` on delete. All labels localized. |
| `angular/.../exchange-rates.component.scss` | **Complete rewrite** | Matches `financial-items` design system with amber/gold accent. |

### 5.3 Localization Keys Added

```
Training.ExchangeRates.AddSubtitle
Training.ExchangeRates.AddFirstHint
Training.ExchangeRates.ActiveBadge
Training.ExchangeRates.NoActiveRate
Training.ExchangeRates.NotesPlaceholder
Training.ExchangeRates.RateMustBePositive
Training.ExchangeRates.ConfirmCreate
```

### 5.4 Text Updated

| Key | Old (AR) | New (AR) |
|-----|----------|----------|
| `Training.ExchangeRates.Info` | *"عند إضافة سعر صرف جديد، يتم إلغاء تفعيل السعر السابق تلقائياً. سعر واحد نشط فقط في أي وقت."* | *"اختر نوع الدورة ثم أضف البنود المالية المطلوبة. يمكن ترتيب البنود باستخدام الأسهم."* |

> ⚠️ **Note:** The EN localization for `Training.ExchangeRates.Info` was also updated to match.

---

## 6. Training Budgets (`training/finance/budgets`)

### 6.1 Backend

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Application/Training/Finance/TrainingBudgetAppService.cs` | **No functional change required** | Already serves the redesigned frontend perfectly. Uses primary constructors, batch-computes `RecoverableAmounts`, auto-creates zero-amount rows for new years, computes `Remaining`/`SpentPercent`/`IsOverThreshold` on the fly. |
| `src/MOD.Training.Application.Contracts/Training/Finance/Dtos/TrainingBudgetDtos.cs` | **No change** | `TrainingBudgetDto` already contains all fields needed by the new UI: `FinancialItemNameAr/En`, `TotalAmount`, `SpentAmount`, `Remaining`, `AmountToRecoverOMR`, `AlertThreshold`, `IsOverThreshold`, `SpentPercent`, `IsFinancialItemActive`. |
| `src/MOD.Training.Application/Training/Mapper/FinanceMappers.cs` | **Still uses Mapperly** | `TrainingBudgetToDtoMapper` remains a Mapperly mapper. Backend works correctly; migration to AutoMapper is optional for consistency with `CourseTypeFinancialDefaults`. |
| `src/MOD.Training.Application/Training/Finance/TrainingBudgetAppService.cs` — `GetRecoverableAmountsAsync` | **Batch optimization** | Loads all pending `BudgetReallocation` rows for the requested budgets in a single query, then groups in memory. Sufficient for current data volume. |

### 6.2 Frontend — Complete DevExtreme Removal & Modern Redesign

#### Architecture (TypeScript)
| File | Change | Details |
|------|--------|---------|
| `angular/.../training-budgets.component.ts` | **Remove DevExtreme imports** | Removed `DxDataGridModule`, `DxPopupModule`, `DxNumberBoxModule`, `DxSelectBoxModule`, `DxButtonModule`, `ToolbarItem`. |
| | **Loading states** | Added `isLoading = signal(false)` (triggers skeleton rows) and `isSaving = signal(false)` (disables save button in modal). |
| | **Expand/collapse toggle** | Added `areBudgetCardsExpanded = signal(true)` and `toggleBudgetCards()`. Toggle button rendered above budget cards section. Cards hidden with `@if` when collapsed.
| | **Computed stats** | `totalCount` = `visibleBudgets().length`; `activeCount` = `activeBudgets().length`; `overThresholdCount` = budgets where `isOverThreshold === true`. |
| | **Year options** | `yearOptions` generated as `[currentYear+1, currentYear, currentYear-1, currentYear-2]`. |
| | **Threshold editing** | `editingId` holds the budget ID being edited; `thresholdValue` signal bound to modal input. `onSaveThreshold()` calls `service.updateThreshold()` then reloads data. |
| | **Currency formatting** | `formatCurrency(amount)` uses `toLocaleString('en-US', {minimumFractionDigits:0, maximumFractionDigits:0})` to enforce Latin numerals. |
| | **Palette rotation** | `paletteFor(index)` returns 1 of 6 color palettes (blue, green, purple, red, amber, teal) cycling via `index % 6`. |

#### Template (HTML) — Section by Section

**Header Banner**
- Gradient: `linear-gradient(135deg, #1e3a8a, #3730a3, #7c3aed)` (indigo/purple).
- Icon: 💰 in a glass-morphism badge.
- Title + subtitle localized.
- **Year selector**: Native HTML `<select>` styled as a white pill (no DevExtreme). Triggers `onYearChange()` on `(change)`.

**Stat Cards Row (3 cards)**
| Card | Icon | Value | Color Accent |
|------|------|-------|--------------|
| Total Budgets | 📊 | `totalCount()` | Blue `#3b82f6` |
| Active Budgets | ✅ | `activeCount()` | Green `#22c55e` |
| Over Threshold | ⚠️ | `overThresholdCount()` | Red `#ef4444` |
- Bottom bar width reflects proportion (e.g. active / total).
- `stat-card-glow` orb on hover.

**Active Budget Cards (per-item cards)**
- Rendered only for `activeBudgets()`.
- 2-column grid (`repeat(2, 1fr)`), collapses to 1 on mobile.
- Each card gets a dynamic palette via `paletteFor(i)`:
  - Border color, background gradient, badge colors, progress-bar gradient all bound via `[style.*]`.
- **Header**: Arabic name (bold, colored) + English name (small, muted) + percent badge (with ⚠️ pulse animation if `isOverThreshold`).
- **Progress bar**: 10px rounded bar, fill width = `spentPercent`, animated gradient fill.
- **Details grid**: 2-col layout showing Total, Spent, Remaining (green/red), Amount to Recover (purple), Alert Threshold.
- Empty state: "📭 No active budgets for this year" banner if `activeBudgets().length === 0 && !isLoading()`.

**Info Banner**
- Background: `#eff6ff` (blue-tinted), border `#bfdbfe`.
- Text: `Training.TrainingBudgets.DetailedTableInfo`.

**Data Table**
- `<table class="data-table">` inside `.table-card`.
- 7 columns: Financial Item | Total | Spent | Remaining | Recover | Threshold | Actions.
- **Sticky header** with gradient background.
- **Skeleton loading**: 5 shimmer rows when `isLoading()`.
- **Empty state**: centered hero with 📭, title, description when `visibleBudgets().length === 0`.
- **Row animation**: `fadeInUp` with stagger delay `idx * 30ms`.
- **Alert row**: `.row-alert` gives light-red background (`#fef2f2`) if `isOverThreshold`.
- **Cell highlights**:
  - Financial item: gray pill + English name subtitle.
  - Total: bold black.
  - Spent: bold, colored via palette.
  - Remaining: green (`#16a34a`) or red (`#dc2626`) if over threshold.
  - Recover: purple (`#7c3aed`).
  - Threshold: gray badge, turns red if over threshold.
- **Actions**: single edit button (SVG pencil icon, 14×14). Hover turns blue (`#2563eb`).

**Edit Threshold Modal**
- Backdrop: `rgba(15,23,42,0.5)` + `backdrop-filter: blur(4px)`.
- Modal: white, 20px radius, max-width 400px (`modal-sm`).
- Header: ✏️ icon in blue badge + localized title/subtitle + close button (SVG ×).
- Body: single number input (`min="0" max="100" step="1"`) + hint text.
- Footer: Cancel (ghost) + Save (primary purple gradient with glow). Save disabled while `isSaving()`.
- Close on backdrop click, close button, or successful save.

#### Styles (SCSS)
- **Complete rewrite** to match `exchange-rates` / `financial-items` design system.
- Key sections: animations (`fadeInUp`, `fadeIn`, `shimmer`, `pulse-alert`), header banner (purple/indigo), year select pill, stat cards (3-col grid), budget cards (2-col grid with dynamic palettes), info banner, table card, data table, empty state, skeleton, modal, form inputs, responsive breakpoints.
- **Responsive**:
  - ≤992px: stat cards → 2 cols, budget cards → 1 col.
  - ≤768px: header padding reduced, title smaller, stat cards → 1 col.
  - ≤576px: everything stacks.

### 6.3 DevExtreme → Custom HTML Migration Matrix

| Old Control | New Implementation | Notes |
|-------------|-------------------|-------|
| `dx-select-box` (year) | HTML `<select class="year-select-modern">` | White pill style, no external library |
| `dx-data-grid` | `<table class="data-table">` | Sticky header, skeleton loading, empty state, row animations |
| `dx-popup` + `dx-button` (toolbar) | `.modal-backdrop-modern` + `.modal-modern` | Backdrop click to close, footer buttons |
| `dx-number-box` | `<input type="number">` | Native validation, styled with `.form-input-modern` |
| `*dxTemplate` cell templates | Inline Angular `@if` / `[class]` / `{{ }}` | No template syntax needed |

### 6.4 Localization Keys

**Existing keys reused (no change)**
```
Training.TrainingBudgets.Title
Training.TrainingBudgets.Year
Training.TrainingBudgets.FinancialItem
Training.TrainingBudgets.TotalAmount
Training.TrainingBudgets.SpentAmount
Training.TrainingBudgets.AlertThreshold
Training.TrainingBudgets.DetailedTable
Training.BudgetAutoCalculated
Training.Remaining
Training.AmountToRecover
Training.EditThreshold
Training.Currency.OMR
Training.Common.Actions
Training.Common.Save
Training.Common.Cancel
```

**New keys added for this redesign**
```
Training.TrainingBudgets.TotalBudgets        → "Total Budgets" / "إجمالي الميزانيات"
Training.TrainingBudgets.ActiveBudgets       → "Active Budgets" / "الميزانيات النشطة"
Training.TrainingBudgets.OverThreshold       → "Over Threshold" / "تجاوزت الحد"
Training.TrainingBudgets.NoActiveBudgets     → "No active budgets for this year" / "لا توجد ميزانيات نشطة لهذه السنة"
Training.TrainingBudgets.NoBudgets           → "No budgets found" / "لم يتم العثور على ميزانيات"
Training.TrainingBudgets.DetailedTableInfo   → "Detailed view of all budgets..." / "عرض تفصيلي لجميع الميزانيات..."
Training.TrainingBudgets.ThresholdHint       → "Percentage at which a budget alert is triggered" / "نسبة الحد التي يتم عندها تنشيط تنبيه الميزانية"
Training.TrainingBudgets.BudgetCards         → "Budget Cards" / "بطاقات الميزانيات"
Training.TrainingBudgets.ShowCards           → "Show Cards" / "إظهار البطاقات"
Training.TrainingBudgets.HideCards           → "Hide Cards" / "إخفاء البطاقات"
```

---

## 7. Course Catalog (`training/catalog`)

### 7.1 Backend — Mapperly → AutoMapper Migration

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Application/Training/Mapper/TrainingAutoMapperProfile.cs` | **Added CourseCatalog mappings** | `CreateMap<CourseCatalog, CourseCatalogDto>()` (ignores `FieldNameAr`, `FieldNameEn`, `ConditionsCount`). `CreateMap<CreateUpdateCourseCatalogDto, CourseCatalog>()`. `CreateMap<CatalogEnrollmentCondition, CatalogEnrollmentConditionDto>()`. |
| `src/MOD.Training.Application/Training/Catalog/CourseCatalogAppService.cs` | **Uses AutoMapper** | Injected `IMapper`. Replaced `input.ToEntity()` → `mapper.Map<CourseCatalog>(input)`. Replaced `input.MapTo(entity)` → `mapper.Map(input, entity)`. Replaced `entity.ToDto()` → `mapper.Map<CourseCatalogDto>(entity)`. Replaced `condition.ToDto()` → `mapper.Map<CatalogEnrollmentConditionDto>(condition)`. `MapToDto()` changed from `static` to instance method to access `mapper`. |
| `src/MOD.Training.Application/Training/TenantCourses/TenantCourseAppService.cs` | **Uses AutoMapper for CourseCatalog** | Injected `IMapper`. Replaced `x.ToDto()` (CourseCatalog) → `mapper.Map<CourseCatalogDto>(x)` in `GetAvailableCatalogCoursesAsync`. |
| `src/MOD.Training.Application/Training/Mapper/TrainingMapper.cs` | **Commented out** CourseCatalog Mapperly mappings | `CourseCatalog.ToDto()`, `CreateUpdateCourseCatalogDto.ToEntity()`, `MapTo()` commented out. Other Mapperly mappings (CourseField, CourseProposal, TenantCourse, etc.) remain active. |
| `src/MOD.Training.Application.Contracts/Training/Catalog/Dtos/CourseCatalogDto.cs` | **No change** | DTO contains all fields needed by new UI including `FieldNameAr`, `ConditionsCount`, `IsActive`. |
| `src/MOD.Training.Domain/Training/Catalog/CourseCatalog.cs` | **No change** | Entity with navigation to `CourseField`. |

### 7.2 Frontend — Complete DevExtreme Removal & Modern Redesign

#### Main Page (`course-catalog.component`)
| File | Change | Details |
|------|--------|---------|
| `angular/.../course-catalog.component.ts` | **Remove DevExtreme** | Removed `DxDataGridModule`, `DxButtonModule`, `DxTextBoxModule`, `DxSelectBoxModule`, `DxDataGridComponent`, `createAbpStore`. Added `courses`, `isLoading` signals. Added computed stats (`totalCount`, `activeCount`, `inactiveCount`, `conditionsCount`). Direct `catalogService.getList()` calls with filters. Added `onDeleteCourse()` and `onToggleActive()`. |
| `angular/.../course-catalog.component.html` | **Modern UI** | Gradient hero header (teal), 4 stat cards (Total/Active/Inactive/Conditions), HTML filter bar (search input + 3 selects: field, category, status), custom data table with 8 columns (#, Name, Field, Category, Result Type, Conditions, Status, Actions), skeleton loading + empty state, SVG action icons (edit, activate/deactivate, delete). |
| `angular/.../course-catalog.component.scss` | **Complete rewrite** | Matches `exchange-rates` design system. Teal header accent. Custom table, filters, stat cards, empty state, skeleton, responsive breakpoints. |

#### Form Dialog (`catalog-form-dialog.component`)
| File | Change | Details |
|------|--------|---------|
| `angular/.../catalog-form-dialog.component.ts` | **Remove DevExtreme** | Removed `DxPopupModule`, `DxTextBoxModule`, `DxTextAreaModule`, `DxSelectBoxModule`, `DxCheckBoxModule`, `DxSwitchModule`, `DxButtonModule`, `ToolbarItem`. Kept all condition management logic. |
| `angular/.../catalog-form-dialog.component.html` | **Modern modal** | Custom modal backdrop + `.modal-modern.modal-lg`. HTML inputs (text, textarea, select), custom styled switch, custom checkboxes, conditions section with HTML select + text input + SVG action icons. |
| `angular/.../catalog-form-dialog.component.scss` | **New** | Modal styles (backdrop, header, body, footer), form grid layouts, custom switch, custom checkbox, conditions section, responsive breakpoints. |

### 7.3 DevExtreme → Custom HTML Migration Matrix

| Old Control | New Implementation | Notes |
|-------------|-------------------|-------|
| `dx-data-grid` + `createAbpStore` | `<table class="data-table">` + direct `getList()` | Client-side list with signals |
| `dx-text-box` (search) | HTML `<input type="text">` + search button | Filter bar |
| `dx-select-box` (filters) | HTML `<select>` | Field, Category, Status filters |
| `dx-popup` + `dx-button` toolbar | `.modal-backdrop-modern` + `.modal-modern` | Form dialog |
| `dx-text-box` | HTML `<input type="text">` | Form fields |
| `dx-text-area` | HTML `<textarea>` | Description fields |
| `dx-select-box` (form) | HTML `<select>` | Category, Nature, Field, ResultType |
| `dx-check-box` | Custom styled HTML `<input type="checkbox">` | Evaluation, Certificate toggles |
| `dx-switch` | Custom CSS switch | IsActive toggle |
| `*dxTemplate` cell templates | Inline Angular `@if` / `[class]` | Table cells |

### 7.4 Localization Keys Added

```
Training.CourseCatalog.NoCourses        → "No courses found" / "لم يتم العثور على دورات"
Training.CourseCatalog.NoCoursesHint    → "Add your first course to the catalog" / "أضف أول دورة إلى الكتالوج"
Training.CourseCatalog.DeleteConfirm    → "Are you sure you want to delete this course?" / "هل أنت متأكد من حذف هذه الدورة؟"
```

---

## 8. Course Proposals (`training/catalog/proposals`)

### 8.1 Backend — Mapperly → AutoMapper + Validation + Formatted Dates

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Application/Training/Mapper/TrainingAutoMapperProfile.cs` | **Added CourseProposal mappings** | `CreateMap<CourseProposal, CourseProposalDto>()` (ignores `FieldNameAr`, `ProposedByName`, `CreationTimeFormatted`, `ReviewedAtFormatted`). `CreateMap<CreateCourseProposalDto, CourseProposal>()`. |
| `src/MOD.Training.Application/Training/Mapper/TrainingMapper.cs` | **Commented out** CourseProposal Mapperly mapping | `CourseProposal.ToDto()` commented out. Other Mapperly mappings remain active. |
| `src/MOD.Training.Application/Training/CourseProposals/CourseProposalAppService.cs` | **Uses AutoMapper + validation + formatted dates** | Injected `IMapper`. `CreateAsync` now uses `mapper.Map<CourseProposal>(input)` instead of manual property assignment. `MapToDto()` uses `mapper.Map<CourseProposalDto>(entity)` then enriches `FieldNameAr`, `CreationTimeFormatted`, `ReviewedAtFormatted`. |
| `src/MOD.Training.Application.Contracts/Training/CourseProposals/Dtos/CourseProposalDto.cs` | **Validation attributes + formatted date fields** | `[Required]` + `[MinLength(2)]` on `CourseNameAr`/`CourseNameEn`. `[Required]` on `Category`, `Nature`, `FieldId`. `[Required]` on `ReviewCourseProposalDto.Decision`. `[MinLength(3)]` on `RejectionReason`. Added `CreationTimeFormatted` and `ReviewedAtFormatted` string properties. |
| `src/MOD.Training.Application.Contracts/Training/Catalog/Dtos/CourseProposalDto.cs` | **DELETED** | Duplicate file in wrong namespace (`Catalog.Dtos`) causing ambiguous reference build errors. Correct DTOs already exist in `CourseProposals/Dtos/`. |
| `angular/src/app/proxy/training/course-proposals/dtos/models.ts` | **Updated proxy** | Added `creationTimeFormatted` and `reviewedAtFormatted` to `CourseProposalDto` interface. |

### 8.2 Frontend — Complete DevExtreme Removal & Modern Redesign

#### Architecture (TypeScript)
| File | Change | Details |
|------|--------|---------|
| `angular/.../course-proposals.component.ts` | **Remove DevExtreme** | Removed `DxDataGridModule`, `DxPopupModule`, `DxTextBoxModule`, `DxSelectBoxModule`, `DxTextAreaModule`, `DxButtonModule`, `ToolbarItem`. |
| | **Loading + validation** | Added `isLoading`, `isSubmitting`, `isReviewing` signals. Added `submitValidationErrors` + `reviewValidationErrors` signals with `validateSubmitForm()` and `validateReviewForm()`. |
| | **Toaster + Confirmation** | Injected `ToasterService` and `ConfirmationService`. Success/error toasts on submit, review, and load. |
| | **Computed stats** | `pendingCount`, `approvedCount`, `rejectedCount`, `totalCount` computed signals. |
| | **Form state** | `proposalForm` signal for submit modal. `reviewDecision` + `rejectionReason` signals for review modal. |

#### Template (HTML)
**Header Banner**
- Gradient: `linear-gradient(135deg, #0f766e, #0d9488, #14b8a6)` (teal/green).
- Icon: 💡 in glass-morphism badge.
- Title + subtitle localized. "Propose New Course" white pill button.

**Stat Cards Row (4 cards)**
| Card | Icon | Value | Color Accent |
|------|------|-------|--------------|
| Pending | ⏳ | `pendingCount()` | Amber `#f59e0b` |
| Approved | ✅ | `approvedCount()` | Green `#22c55e` |
| Rejected | ❌ | `rejectedCount()` | Red `#ef4444` |
| Total | 📊 | `totalCount()` | Blue `#3b82f6` |

**Filter Bar**
- Search input with SVG magnifying glass icon + clear button.
- Status filter: native `<select>` with `All`, `Pending`, `Approved`, `Rejected` options.

**Pending Proposals Section**
- Card per proposal with `border-left: 4px solid #f59e0b`.
- Title row: Arabic name + English name + pending status pill.
- Detail chips: Field, Category, Nature, ProposedBy.
- Actions: Approve button (green icon + text) + Reject button (red icon + text).
- Empty state: 📭 + localized message.

**Reviewed Proposals Section**
- Compact cards with left border (green for approved, red for rejected).
- Name + meta row (field • category • nature).
- Rejection reason displayed inline when applicable.
- Status pill on the right.
- Empty state: 📝 + localized message.

**Skeleton Loading**
- 3 shimmer proposal cards while `isLoading()`.

#### Modals

**Submit Proposal Modal**
- Backdrop + modal (max-width 560px).
- Header: 💡 icon + localized title/subtitle + close button.
- Info banner (teal) with proposal instructions.
- Validation banner (red) when errors present.
- Form: 2-column (NameAr, NameEn) + 3-column (Category, Nature, Field) native selects.
- Footer: Cancel (ghost) + Submit (primary teal gradient).

**Review Modal**
- Backdrop + modal (max-width 520px).
- Header: 📝 icon + localized title/subtitle + close button.
- Course info card (teal gradient) showing name + field + category + nature.
- Decision cards: Approve (green) / Reject (red). Selected state with border + background change.
- Rejection reason textarea appears when Reject selected.
- Warning banner (yellow) appears when Approve selected.
- Footer: Cancel (ghost) + Confirm (primary teal gradient).

#### Styles (SCSS)
- Complete rewrite matching `course-fields` / `exchange-rates` design system.
- Key sections: animations, header banner (teal), stat cards (4-col grid), filter bar, proposal cards, reviewed cards, detail chips, status pills, empty states, skeleton, modals, form inputs, decision cards, validation banner, responsive breakpoints.

### 8.3 DevExtreme → Custom HTML Migration Matrix

| Old Control | New Implementation | Notes |
|-------------|-------------------|-------|
| `dx-popup` (submit + review) | `.modal-backdrop-modern` + `.modal-modern` | Backdrop click to close, custom footer buttons |
| `dx-text-box` | HTML `<input type="text">` | Search + form fields |
| `dx-select-box` | HTML `<select>` | Status filter, Category, Nature, Field |
| `dx-text-area` | HTML `<textarea>` | Rejection reason |
| `dx-data-grid` | Custom card lists | Pending + reviewed proposal cards |

### 8.4 Localization Keys Added

**Backend validation keys**
```
Training:CourseProposal:NameArTooShort
Training:CourseProposal:NameEnTooShort
Training:CourseProposal:RejectionReasonTooShort
```

**Frontend keys**
```
Training.CourseProposal.Validation.NameArRequired
Training.CourseProposal.Validation.NameEnRequired
Training.CourseProposal.Validation.CategoryRequired
Training.CourseProposal.Validation.NatureRequired
Training.CourseProposal.Validation.FieldRequired
Training.CourseProposal.Validation.DecisionRequired
Training.CourseProposal.Validation.RejectionReasonRequired
Training.CourseProposal.SubmitSuccess
Training.CourseProposal.ApproveSuccess
Training.CourseProposal.RejectSuccess
Training.CourseProposal.NoReviewed
```

**Reused Common keys**
```
Training.Common.Total
Training.Common.LoadError
Training.Common.SaveError
```

---

## 9. Tenant Courses (`training/tenant-courses`)

### 9.1 Backend — Mapperly → AutoMapper + Validation + Formatted Dates

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Application/Training/Mapper/TrainingAutoMapperProfile.cs` | **Added TenantCourse mappings** | `CreateMap<TenantCourse, TenantCourseDto>()` (ignores `CatalogCourseNameAr`, `CatalogCourseNameEn`, `CatalogCourseFieldNameAr`, `CatalogCourseCategory`, `ConditionsCount`, `AddedByName`, `AddedAtFormatted`). `CreateMap<UpdateTenantCourseDto, TenantCourse>()`. `CreateMap<TenantCourseCondition, TenantCourseConditionDto>()`. |
| `src/MOD.Training.Application/Training/Mapper/TrainingMapper.cs` | **Commented out** TenantCourse Mapperly mappings | `TenantCourse.ToDto()`, `UpdateTenantCourseDto.MapTo()`, `TenantCourseCondition.ToDto()` commented out. |
| `src/MOD.Training.Application/Training/TenantCourses/TenantCourseAppService.cs` | **Uses AutoMapper + validation + formatted dates** | Injected `IMapper` (was already injected for CourseCatalog). `MapToDto()` uses `mapper.Map<TenantCourseDto>(entity)` then enriches catalog fields + conditions count + `AddedAtFormatted`. `UpdateAsync` uses `mapper.Map(input, entity)`. `GetConditionsAsync` uses `mapper.Map<TenantCourseConditionDto>(condition)`. |
| `src/MOD.Training.Application.Contracts/Training/TenantCourses/Dtos/TenantCourseDto.cs` | **Validation attributes + formatted date field** | `[Range(1, 9999)]` on `DefaultCapacity`. `[Range(1, 999)]` on `DefaultDurationWeeks`. `[Required]` on `ResultType`. Added `AddedAtFormatted` string property. |
| `angular/src/app/proxy/training/tenant-courses/dtos/models.ts` | **Updated proxy** | Added `addedAtFormatted` to `TenantCourseDto` interface. |

### 9.2 Frontend — Complete DevExtreme Removal & Modern Redesign

#### Main Page (`tenant-courses-list.component`)
| File | Change | Details |
|------|--------|---------|
| `angular/.../tenant-courses-list.component.ts` | **Remove DevExtreme** | Removed `DxDataGridModule`, `DxPopupModule`, `DxTextBoxModule`, `DxSelectBoxModule`, `DxNumberBoxModule`, `DxCheckBoxModule`, `DxSwitchModule`, `DxDataGridComponent`, `createAbpStore`. |
| | **Signals + loading** | `tenantCourses`, `isLoading`, `searchText`, `filterFieldId`, `filterResultType`, `filterStatus` signals. Computed stats: `totalCount`, `activeCount`, `withConditionsCount`, `withCertificateCount`. |
| | **Validation + UX** | `editValidationErrors` signal with `validateEditForm()` for capacity (1-9999) and duration (1-999). `ConfirmationService` + `ToasterService` for delete and save. |
| | **Field options** | Loads active course fields via `CourseFieldService.getAllActive()` for the field filter dropdown. |
| `angular/.../tenant-courses-list.component.html` | **Modern UI** | Gradient hero header (indigo/purple), 4 stat cards (Total/Active/With Conditions/With Certificate), info banner, filter bar with 4 filters (search + field + result type + status), custom data table with 11 columns, skeleton loading, empty state, custom edit modal. |
| `angular/.../tenant-courses-list.component.scss` | **Complete rewrite** | Matches `training-budgets` / `course-fields` design system. Indigo/purple header accent. Custom table, filters, stat cards, empty state, skeleton, modal, custom switch, custom checkboxes, conditions section, responsive breakpoints. |

#### Add From Catalog Dialog (`add-from-catalog-dialog.component`)
| File | Change | Details |
|------|--------|---------|
| `angular/.../add-from-catalog-dialog.component.ts` | **Remove DevExtreme** | Removed `DxDataGridModule`, `DxButtonModule`, `DxPopupModule`, `createAbpStore`. Direct API call to `getAvailableCatalogCourses()`. |
| | **Multi-select logic** | `selectedIds = signal<Set<string>>(new Set())`. `toggleSelection(id)`, `toggleAll()`, `isSelected()`, `isAllSelected()`, `isPartialSelected()`. Client-side search via `filteredCourses` computed. |
| `angular/.../add-from-catalog-dialog.component.html` | **Custom modal + selectable table** | `.modal-backdrop-modern` + `.modal-modern.modal-lg`. Native HTML table with checkbox in each row + header checkbox with indeterminate state. Info banner. Search input. Selection count footer. |
| `angular/.../add-from-catalog-dialog.component.scss` | **Complete rewrite** | Modal styles, selectable table with hover + selected row highlight, custom checkbox, skeleton, empty state, responsive footer. |

### 9.3 DevExtreme → Custom HTML Migration Matrix

| Old Control | New Implementation | Notes |
|-------------|-------------------|-------|
| `dx-data-grid` (main list) | `<table class="data-table">` | 11 columns, sticky header, row animations, skeleton loading |
| `dx-data-grid` (dialog) | `<table class="catalog-table">` | Multi-select with native checkboxes + header indeterminate state |
| `dx-popup` (edit + dialog) | `.modal-backdrop-modern` + `.modal-modern` | Backdrop click to close |
| `dx-text-box` | HTML `<input type="text">` | Search filters |
| `dx-select-box` | HTML `<select>` | Field, ResultType, Status filters and form selects |
| `dx-number-box` | HTML `<input type="number">` | Capacity + DurationWeeks |
| `dx-check-box` | Custom styled HTML `<input type="checkbox">` | Evaluation toggles |
| `dx-switch` | Custom CSS switch (`.switch-modern`) | IsActive toggle |
| `*dxTemplate` cell templates | Inline Angular `@if` / `[class]` | Table cells |

### 9.4 Localization Keys Added

**Backend validation keys**
```
Training:TenantCourse:CapacityOutOfRange
Training:TenantCourse:DurationOutOfRange
```

**Frontend keys**
```
Training.TenantCourse.NoCourses
Training.TenantCourse.NoCoursesHint
Training.TenantCourse.NoCatalogCourses
Training.TenantCourse.Validation.CapacityRange
Training.TenantCourse.Validation.DurationRange
Training.TenantCourse.UpdateSuccess
Training.TenantCourse.DeleteSuccess
Training.TenantCourse.AddSuccess
Training.TenantCourse.DeleteConfirm
```

**Reused Common keys**
```
Training.Common.Total
Training.Common.LoadError
Training.Common.SaveError
Training.Common.DeleteError
Training.ConfirmDelete
```

---

## 10. Plan Review (`training/plans/plan-review`)

### 10.1 Frontend

| File | Change | Details |
|------|--------|---------|
| `angular/.../plan-review.component.ts` | **Flattening fix** | Removed `parentId`-based grouping logic (`const parents = all.filter(fi => !fi.parentId)` etc.). `groupedFinancialItems` now maps directly from flat `all` array. |
| `angular/.../plan-review.component.html` | **Flattening fix** | Replaced `<optgroup>` parent/child `<select>` with flat `<select>` using `{{ nameAr }} ({{ code }})`. |

> No backend changes required — purely a frontend adaptation to the flattened `FinancialItemDto` proxy.

---

## 11. Backend Architecture Decisions

### 11.1 Flattening Strategy
- **Parent rows** remain in DB temporarily (`ParentId` column kept but business logic ignores it).
- Future migration should nullify/delete parent rows and drop `ParentId` column.
- `IsGeneral` removed from DTOs and business logic.

### 11.2 Mapperly vs AutoMapper
- Project originally uses **Mapperly** (`Riok.Mapperly`) as the default ABP 10.x mapper.
- ~~**CourseTypeFinancialDefaults** was migrated to **AutoMapper** per explicit request.~~ (Feature removed in section 13.)
- **CourseCatalog** was migrated to **AutoMapper** (including `CatalogEnrollmentCondition`). `TenantCourseAppService` updated to use `IMapper` for `CourseCatalog` mapping.
- **CourseProposal** was migrated to **AutoMapper**. `CreateCourseProposalDto → CourseProposal` mapping added. `CourseProposal → CourseProposalDto` mapping added (ignores enriched fields). Old Mapperly `CourseProposal.ToDto()` commented out in `TrainingMapper.cs`.
- **TenantCourse + TenantCourseCondition** migrated to **AutoMapper**. `TenantCourse → TenantCourseDto` mapping added (ignores 7 enriched fields). `UpdateTenantCourseDto → TenantCourse` and `TenantCourseCondition → TenantCourseConditionDto` mappings added. Old Mapperly mappings commented out.
- AutoMapper is registered in `TrainingApplicationModule` alongside existing Mapperly mappers — both can coexist.
- Remaining Mapperly mappings: CourseField and various other domains.
- If future modules prefer AutoMapper, follow the same pattern:
  1. Add mapping to `TrainingAutoMapperProfile`
  2. Inject `IMapper` into AppService
  3. Update any dependent services that used the old Mapperly extension methods
  4. Comment out / remove the old Mapperly mappings from `TrainingMapper.cs`

### 11.3 Localization Pattern
- Backend: JSON files in `src/MOD.Training.Domain.Shared/Localization/Training/{ar,en}.json`
- Frontend: Use `{{ '::Key' | abpLocalization }}` in templates or `this.l.t('::Key')` in TS.
- Parameterized keys: `{{ '::Key' | abpLocalization : ('' + value) }}` (pipe expects `string | string[]`).

---

## 12. Files to Know (Quick Reference)

### Backend — Finance Domain
```
src/MOD.Training.Domain/Training/Finance/FinancialItem.cs
src/MOD.Training.Application/Training/Finance/FinancialItemAppService.cs
src/MOD.Training.Application/Training/Mapper/TrainingAutoMapperProfile.cs
src/MOD.Training.EntityFrameworkCore/Training/TrainingDbContextModelCreatingExtensions.cs
```

### Backend — Catalog Domain
```
src/MOD.Training.Domain/Training/Catalog/CourseCatalog.cs
src/MOD.Training.Domain/Training/Catalog/CatalogEnrollmentCondition.cs
src/MOD.Training.Domain/Training/Catalog/CourseProposal.cs
src/MOD.Training.Domain/Training/TenantCourses/TenantCourse.cs
src/MOD.Training.Domain/Training/TenantCourses/TenantCourseCondition.cs
src/MOD.Training.Application/Training/Catalog/CourseCatalogAppService.cs
src/MOD.Training.Application/Training/CourseProposals/CourseProposalAppService.cs
src/MOD.Training.Application/Training/TenantCourses/TenantCourseAppService.cs
src/MOD.Training.Application.Contracts/Training/Catalog/Dtos/CourseCatalogDto.cs
src/MOD.Training.Application.Contracts/Training/CourseProposals/Dtos/CourseProposalDto.cs
src/MOD.Training.Application.Contracts/Training/TenantCourses/Dtos/TenantCourseDto.cs
```

### Frontend — Finance Pages
```
angular/src/app/Projects/finance/financial-items/
angular/src/app/Projects/finance/training-budgets/
angular/src/app/Projects/catalog/course-catalog/
angular/src/app/Projects/catalog/course-fields/
angular/src/app/Projects/catalog/course-proposals/
angular/src/app/Projects/tenant-courses/
angular/src/app/Projects/plans/plan-review/
```

### Localization
```
src/MOD.Training.Domain.Shared/Localization/Training/ar.json
src/MOD.Training.Domain.Shared/Localization/Training/en.json
```

---

## 13. Removal of Course-Type Financial Defaults (`training/finance/defaults`)

### 13.1 What Was Removed

The entire **Course-Type Financial Defaults** feature has been removed. This feature previously linked financial items to `CourseType` (`ExternalLocal` / `ExternalInternational`) and auto-filled those items in plan review and casual course review.

After this change:
- Financial items are managed only through `/training/finance/financial-items`.
- There is no auto-fill based on course type.
- Staff add financial items manually per plan item / casual course.
- The internal/external distinction no longer affects financial-item defaults.

### 13.2 Backend Changes

| File | Change |
|------|--------|
| `src/MOD.Training.Domain/Training/Finance/CourseTypeFinancialItemDefault.cs` | **Deleted** entity |
| `src/MOD.Training.Application.Contracts/Training/Finance/Dtos/CourseTypeFinancialItemDefaultDtos.cs` | **Deleted** DTOs |
| `src/MOD.Training.Application/Training/Finance/CourseTypeFinancialDefaultAppService.cs` | **Deleted** app service |
| `src/MOD.Training.Application.Contracts/Training/Finance/ICourseTypeFinancialDefaultAppService.cs` | **Deleted** contract |
| `src/MOD.Training.Application/Training/Mapper/TrainingAutoMapperProfile.cs` | Removed `CourseTypeFinancialItemDefault` mappings |
| `src/MOD.Training.EntityFrameworkCore/EntityFrameworkCore/TrainingDbContext.cs` | Removed `CourseTypeFinancialItemDefaults` DbSet |
| `src/MOD.Training.EntityFrameworkCore/Training/TrainingDbContextModelCreatingExtensions.cs` | Removed `CourseTypeFinancialItemDefault` EF configuration |
| `src/MOD.Training.EntityFrameworkCore/Migrations/20260708170406_RemoveCourseTypeFinancialItemDefaults.cs` | **New migration** drops `TrnCourseTypeFinancialItemDefaults` table |
| `src/MOD.Training.Application.Contracts/Training/Permissions/TrainingPermissions.cs` | Removed `CourseTypeFinancialDefaults` permission group |
| `src/MOD.Training.Application.Contracts/Training/Permissions/TrainingPermissionDefinitionProvider.cs` | Removed defaults permission registration |
| `src/MOD.Training.Domain/Training/DataSeeder/TenantDataSeeder.cs` | Removed `SeedFinancialDefaultsAsync`; casual-course seed now uses a hardcoded list of financial item IDs |
| `src/MOD.Training.Application/Training/Plans/PlanItemFinancialItemAppService.cs` | Removed `AutoFillFromDefaultsAsync` and `defaultsRepository` injection |
| `src/MOD.Training.Application.Contracts/Training/Plans/IPlanItemFinancialItemAppService.cs` | Removed `AutoFillFromDefaultsAsync` contract method |
| `src/MOD.Training.Application/Training/CasualCourses/CasualCourseFinancialItemAppService.cs` | Removed `AutoFillFromDefaultsAsync`, `AutoFillInternalAsync`, `GetNomineesByRankAsync`, and `defaultsRepo` injection |
| `src/MOD.Training.Application.Contracts/Training/CasualCourses/ICasualCourseFinancialItemAppService.cs` | Removed `AutoFillFromDefaultsAsync` contract method |
| `src/MOD.Training.Application/Training/CasualCourses/CasualCourseAppService.cs` | Removed auto-fill trigger in `AssignScenarioAsync`; `CalculatePreviewAsync` now uses all active financial items; removed `defaultsRepo` injection |
| `src/MOD.Training.Domain/Training/Managers/FundingScenarioSourceResolver.cs` | Updated XML-doc comment |

### 13.3 Frontend Changes

| File | Change |
|------|--------|
| `angular/src/app/Projects/finance/financial-item-defaults/*` | **Deleted** component (TS, HTML, SCSS) |
| `angular/src/app/Projects/training.routes.ts` | Removed `finance/defaults` route |
| `angular/src/app/Projects/training-route.provider.ts` | Removed `CourseTypeDefaults` menu item |
| `angular/src/app/Projects/shared/services/finance-proxy.service.ts` | Removed `CourseTypeFinancialDefaultService` wrapper |
| `angular/src/app/proxy/training/finance/course-type-financial-default.service.ts` | **Deleted** generated proxy |
| `angular/src/app/proxy/training/finance/index.ts` | Removed export of deleted proxy |
| `angular/src/app/proxy/training/finance/dtos/models.ts` | Removed `CourseTypeFinancialItemDefaultDto` and `CreateCourseTypeFinancialItemDefaultDto` |
| `angular/src/app/proxy/training/plans/plan-item-financial-item.service.ts` | Removed `autoFillFromDefaults` generated method |
| `angular/src/app/proxy/training/casual-courses/casual-course-financial-item.service.ts` | Removed `autoFillFromDefaults` generated method |
| `angular/src/app/Projects/plans/plan-review/plan-review.component.ts` | Removed `onAutoFill`, `batchAutoFillUnit`, `batchFilling`, `batchProgress` |
| `angular/src/app/Projects/plans/plan-review/plan-review.component.html` | Removed auto-fill buttons and batch alert; updated empty-state text |
| `angular/src/app/Projects/casual-courses/casual-course-review/casual-course-review.component.ts` | Removed `onAutoFill` |
| `angular/src/app/Projects/casual-courses/casual-course-review/casual-course-review.component.html` | Removed auto-fill button |

### 13.4 Localization

Removed from `ar.json` and `en.json`:
- `Permission:CourseTypeFinancialDefaults.*`
- `Training.CourseTypeDefaults.*`
- `Training.Menu.CourseTypeDefaults`

### 13.5 Generated Proxy Metadata

`angular/src/app/proxy/generate-proxy.json` still contains stale metadata for the deleted endpoints. It should be regenerated with `abp generate-proxy -t ng` the next time the backend is running.

---

## 14. Removal of Exchange Rates (`training/finance/exchange-rates`)

### 14.1 What Was Removed

The entire **Exchange Rates** feature has been removed. The system no longer maintains an admin page for USD→OMR (or other) exchange rates, and no business logic auto-calculates USD amounts from exchange rates.

After this change:
- The route `/training/finance/exchange-rates` is gone.
- The `TrnExchangeRates` table is dropped.
- `PlanItemFinancialItem.UpdateAmountAsync` no longer auto-calculates `EstimatedAmountUSD` from an exchange rate.
- Payment pages and the casual-course detail no longer display USD conversions.
- USD columns on `PlanItemFinancialItem` remain as nullable manual-entry fields (historical data is preserved).

### 14.2 Backend Changes

| File | Change |
|------|--------|
| `src/MOD.Training.Domain/Training/Finance/ExchangeRate.cs` | **Deleted** entity |
| `src/MOD.Training.Application.Contracts/Training/Finance/Dtos/ExchangeRateDtos.cs` | **Deleted** DTOs |
| `src/MOD.Training.Application/Training/Finance/ExchangeRateAppService.cs` | **Deleted** app service |
| `src/MOD.Training.Application.Contracts/Training/Finance/IExchangeRateAppService.cs` | **Deleted** contract |
| `src/MOD.Training.Application/Training/Mapper/FinanceMappers.cs` | Removed `ExchangeRateToDtoMapper` |
| `src/MOD.Training.EntityFrameworkCore/EntityFrameworkCore/TrainingDbContext.cs` | Removed `ExchangeRates` DbSet |
| `src/MOD.Training.EntityFrameworkCore/Training/TrainingDbContextModelCreatingExtensions.cs` | Removed `ExchangeRate` EF configuration |
| `src/MOD.Training.EntityFrameworkCore/Migrations/20260708174512_RemoveExchangeRates.cs` | **Migration** drops `TrnExchangeRates` table |
| `src/MOD.Training.Application.Contracts/Training/Permissions/TrainingPermissions.cs` | Removed `ExchangeRates` permission class and `Finance.ManageExchangeRates` |
| `src/MOD.Training.Application.Contracts/Training/Permissions/TrainingPermissionDefinitionProvider.cs` | Removed exchange-rates permission definitions |
| `src/MOD.Training.Domain/Training/DataSeeder/TenantDataSeeder.cs` | Removed `exchangeRateRepo` injection, `SeedExchangeRatesAsync`, and USD assignments in `AssignFinancials` |
| `src/MOD.Training.Application/Training/Plans/PlanItemFinancialItemAppService.cs` | Removed `IRepository<ExchangeRate>` injection and USD auto-calculation from `UpdateAmountAsync` |

### 14.3 Frontend Changes

| File | Change |
|------|--------|
| `angular/src/app/Projects/finance/exchange-rates/*` | **Deleted** component (TS, HTML, SCSS) |
| `angular/src/app/Projects/training.routes.ts` | Removed `finance/exchange-rates` route |
| `angular/src/app/Projects/training-route.provider.ts` | Removed `ExchangeRates` menu item |
| `angular/src/app/Projects/shared/services/finance-proxy.service.ts` | Removed `ExchangeRateService` wrapper |
| `angular/src/app/proxy/training/finance/exchange-rate.service.ts` | **Deleted** generated proxy |
| `angular/src/app/proxy/training/finance/index.ts` | Removed export of deleted proxy |
| `angular/src/app/proxy/training/finance/dtos/models.ts` | Removed `ExchangeRateDto`, `CreateExchangeRateDto`, `ExchangeRateGetListInput` |
| `angular/src/app/Projects/payments/travel-allowance-payments/travel-allowance-payments.component.{ts,html}` | Removed exchange-rate loading and USD display |
| `angular/src/app/Projects/payments/course-payments/course-payments.component.{ts,html}` | Removed exchange-rate loading and USD display |
| `angular/src/app/Projects/payments/budget-reallocations/budget-reallocations.component.{ts,html}` | Removed exchange-rate loading and USD display |
| `angular/src/app/Projects/casual-courses/casual-course-detail/casual-course-detail.component.{ts,html}` | Removed exchange-rate loading and `[exchangeRate]` binding |
| `angular/src/app/Projects/casual-courses/casual-course-detail/sections/section-payments/casual-course-section-payments.component.ts` | Removed `exchangeRate` input and `toUSD` helper |

### 14.4 Localization

Removed from `ar.json` and `en.json`:
- `Permission:Finance.ManageExchangeRates`
- `Permission:ExchangeRates.*`
- `Training.Menu.ExchangeRates`
- `Training.ExchangeRates.*`

### 14.5 Generated Proxy Metadata

`angular/src/app/proxy/generate-proxy.json` still contains stale metadata for the deleted exchange-rate endpoints. It should be regenerated with `abp generate-proxy -t ng` the next time the backend is running.

---

## 15. Pending / Future Work

| # | Item | Priority |
|---|------|----------|
| 1 | **Delete legacy parent rows** from `TrnFinancialItems` and drop `ParentId` column | Medium |
| 2 | ~~**Remove `CourseTypeFinancialItemDefaultMappers.cs`** (Mapperly) if AutoMapper is finalized~~ | **Done** |
| 3 | **Data migration** for `FinancialItem` parent rows (nullify or reclassify) | Medium |
| 4 | **Sass `@import` deprecation** warnings in Angular build (`gtms-design.scss`) | Low |
| 5 | ~~**Course Fields page redesign**~~ (removed DevExtreme, added card grid + modern modal) | **Done** |
| 6 | ~~**Course Proposals page redesign**~~ (removed DevExtreme, modern UI + backend AutoMapper migration + validation) | **Done** |
| 7 | ~~**Tenant Courses page redesign**~~ (removed DevExtreme, modern UI + add-from-catalog dialog + backend AutoMapper migration + validation) | **Done** |
