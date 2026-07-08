# GTMS — Changes Reference Log

> Last updated: 2026-06-08
>
> This document tracks all backend & frontend modifications made during the flattening redesign sprint. Use it as a reference before making further updates.

---

## 1. Financial Items (`training/finance/financial-items`)

### 1.1 Backend

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Domain/Training/Finance/FinancialItem.cs` | **Flattened entity** | Removed parent/child business logic. `ParentId` kept in DB for migration safety only. `ItemType` now allowed on ALL items (was leaf-only). Removed `IsGeneral` auto-calculation. |
| `src/MOD.Training.Application/Training/Finance/FinancialItemAppService.cs` | **Flattened service** | Removed max-two-level validation, `HasChildren` check, `IsGeneral` logic. `CreateAsync` / `UpdateAsync` simplified. Returns flat list ordered by `voteCode`. Budget seeding gated on `!input.ParentId.HasValue` during transition. |
| `src/MOD.Training.Application.Contracts/Training/Finance/Dtos/FinancialItemDtos.cs` | **DTO updated** | `CreateUpdateFinancialItemDto` and `FinancialItemDto` removed `ParentId` / `IsGeneral`. `ItemType` nullable on both. |
| `src/MOD.Training.Domain/Training/Managers/FundingScenarioSourceResolver.cs` | **No change** | Still uses `FinancialItemType` for scenario resolution. |
| `src/MOD.Training.Application/Training/Payments/TravelAllowancePaymentAppService.cs` | **No change** | Still relies on `ItemType` to break down travel components. |
| `src/MOD.Training.EntityFrameworkCore/Training/TrainingDbContextModelCreatingExtensions.cs` | **DB config** | Removed `ParentId` FK constraint & index on `TrnFinancialItems` (flat list). Added `TenantId + NameAr` index. |

### 1.2 Frontend

| File | Change | Details |
|------|--------|---------|
| `angular/.../financial-items.component.html` | **Complete redesign** | Gradient hero header, 4 stat cards (Total / Active / Per-Day / Per-Nominee), filter pills, custom data table with animations, inline rank-amounts panel, modern dialogs. |
| `angular/.../financial-items.component.ts` | **Signals + Localization** | All hard-coded Arabic text removed. Added `LocalizationPipe` import. `itemTypeLabel()` now uses `l.t()` instead of static Arabic map. Confirm dialogs localized. |
| `angular/.../financial-items.component.scss` | **New design system** | 1300+ lines matching `gtms-design.scss` patterns. |

### 1.3 Localization Keys Added

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

## 2. Course Type Financial Defaults (`training/finance/defaults`)

### 2.1 Backend

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Application/Training/Finance/CourseTypeFinancialDefaultAppService.cs` | **Uses AutoMapper** | Injected `IMapper`. `GetListAsync` and `CreateAsync` now use `mapper.Map<>()` instead of manual DTO construction. |
| `src/MOD.Training.Application/Training/Mapper/TrainingAutoMapperProfile.cs` | **NEW — AutoMapper Profile** | Maps `CourseTypeFinancialItemDefault ↔ CourseTypeFinancialItemDefaultDto` (ignores enriched fields `NameAr/NameEn/Code`). Maps `CreateCourseTypeFinancialItemDefaultDto → CourseTypeFinancialItemDefault`. |
| `src/MOD.Training.Application/TrainingApplicationModule.cs` | **AutoMapper registration** | Added `typeof(AbpAutoMapperModule)` to `[DependsOn]`. `ConfigureServices` registers `AddAutoMapperObjectMapper` + `AddAutoMapper(Assembly)`. |
| `src/MOD.Training.Application/MOD.Training.Application.csproj` | **Package added** | `Volo.Abp.AutoMapper` v10.1.1 |
| `src/MOD.Training.Application/Training/Mapper/CourseTypeFinancialItemDefaultMappers.cs` | **Created then obsolete** | Mapperly mappers were created first, then replaced by AutoMapper. Can be deleted if desired. |

### 2.2 Frontend

| File | Change | Details |
|------|--------|---------|
| `angular/.../financial-item-defaults.component.html` | **Complete redesign** | Matches `financial-items` style: gradient header (teal), 2 course-type stat cards (clickable), info banner, custom data table with sort-order arrows ↑↓, empty state, modern modal dialog. Replaced DevExtreme `dx-data-grid` + `dx-popup` with custom HTML/CSS. |
| `angular/.../financial-item-defaults.component.ts` | **Signals + counts** | Added `externalIntlCount` / `externalLocalCount` signals loaded in parallel. Added `moveUp()` / `moveDown()` with `updateSortOrder` API call. `confirm()` on delete. SelectBox display shows `nameAr (voteCode)`. |
| `angular/.../financial-item-defaults.component.scss` | **Complete rewrite** | Matches `financial-items` design system. |

### 2.3 Localization Keys Added

```
Training.CourseTypeDefaults.NoItems
Training.CourseTypeDefaults.NoItemsHint
Training.CourseTypeDefaults.ReorderHint
Training.CourseTypeDefaults.AllAssigned
Training.Common.MoveUp
Training.Common.MoveDown
```

### 2.4 Text Updated

| Key | Old (AR) | New (AR) |
|-----|----------|----------|
| `Training.CourseTypeDefaults.Info` | *"اختر نوع الدورة من القائمة اليمنى..."* | *"اختر نوع الدورة ثم أضف البنود المالية المطلوبة. يمكن ترتيب البنود باستخدام الأسهم."* |

---

## 3. Exchange Rates (`training/finance/exchange-rates`)

### 3.1 Backend

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Domain/Training/Finance/ExchangeRate.cs` | **Notes persistence** | `Notes` property added to entity (was phantom field in UI only). |
| `src/MOD.Training.Application.Contracts/Training/Finance/Dtos/ExchangeRateDtos.cs` | **Notes in DTOs** | `CreateExchangeRateDto` and `ExchangeRateDto` include `Notes`. |
| `src/MOD.Training.EntityFrameworkCore/Migrations/20260607185526_AddExchangeRateNotes.cs` | **NEW Migration** | Adds `Notes` column (`nvarchar(500)`) to `TrnExchangeRates`. Also applies pending `TrnFinancialItems` schema changes (dropped `ParentId` FK/index — see §1.1). |
| `src/MOD.Training.EntityFrameworkCore/Migrations/TrainingDbContextModelSnapshot.cs` | **Updated** | Reflects new migration state. |

### 3.2 Frontend

| File | Change | Details |
|------|--------|---------|
| `angular/.../exchange-rates.component.html` | **Complete redesign** | Gradient header (amber/gold), active-rate hero card, 3 stat cards (Total / Active / Inactive), custom data table, empty state, skeleton loading, modern modal dialog. Removed all DevExtreme components (`dx-data-grid`, `dx-popup`, `dx-button`, `dx-text-box`, `dx-number-box`). |
| `angular/.../exchange-rates.component.ts` | **Signals + localization** | Added `isLoading`, `totalCount`, `activeCount`, `inactiveCount` signals. `confirm()` on delete. All labels localized. |
| `angular/.../exchange-rates.component.scss` | **Complete rewrite** | Matches `financial-items` design system with amber/gold accent. |

### 3.3 Localization Keys Added

```
Training.ExchangeRates.AddSubtitle
Training.ExchangeRates.AddFirstHint
Training.ExchangeRates.ActiveBadge
Training.ExchangeRates.NoActiveRate
Training.ExchangeRates.NotesPlaceholder
Training.ExchangeRates.RateMustBePositive
Training.ExchangeRates.ConfirmCreate
```

### 3.4 Text Updated

| Key | Old (AR) | New (AR) |
|-----|----------|----------|
| `Training.ExchangeRates.Info` | *"عند إضافة سعر صرف جديد، يتم إلغاء تفعيل السعر السابق تلقائياً. سعر واحد نشط فقط في أي وقت."* | *"اختر نوع الدورة ثم أضف البنود المالية المطلوبة. يمكن ترتيب البنود باستخدام الأسهم."* |

> ⚠️ **Note:** The EN localization for `Training.ExchangeRates.Info` was also updated to match.

---

## 4. Training Budgets (`training/finance/budgets`)

### 4.1 Backend

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Application/Training/Finance/TrainingBudgetAppService.cs` | **No functional change required** | Already serves the redesigned frontend perfectly. Uses primary constructors, batch-computes `RecoverableAmounts`, auto-creates zero-amount rows for new years, computes `Remaining`/`SpentPercent`/`IsOverThreshold` on the fly. |
| `src/MOD.Training.Application.Contracts/Training/Finance/Dtos/TrainingBudgetDtos.cs` | **No change** | `TrainingBudgetDto` already contains all fields needed by the new UI: `FinancialItemNameAr/En`, `TotalAmount`, `SpentAmount`, `Remaining`, `AmountToRecoverOMR`, `AlertThreshold`, `IsOverThreshold`, `SpentPercent`, `IsFinancialItemActive`. |
| `src/MOD.Training.Application/Training/Mapper/FinanceMappers.cs` | **Still uses Mapperly** | `TrainingBudgetToDtoMapper` remains a Mapperly mapper. Backend works correctly; migration to AutoMapper is optional for consistency with `CourseTypeFinancialDefaults`. |
| `src/MOD.Training.Application/Training/Finance/TrainingBudgetAppService.cs` — `GetRecoverableAmountsAsync` | **Batch optimization** | Loads all pending `BudgetReallocation` rows for the requested budgets in a single query, then groups in memory. Sufficient for current data volume. |

### 4.2 Frontend — Complete DevExtreme Removal & Modern Redesign

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

### 4.3 DevExtreme → Custom HTML Migration Matrix

| Old Control | New Implementation | Notes |
|-------------|-------------------|-------|
| `dx-select-box` (year) | HTML `<select class="year-select-modern">` | White pill style, no external library |
| `dx-data-grid` | `<table class="data-table">` | Sticky header, skeleton loading, empty state, row animations |
| `dx-popup` + `dx-button` (toolbar) | `.modal-backdrop-modern` + `.modal-modern` | Backdrop click to close, footer buttons |
| `dx-number-box` | `<input type="number">` | Native validation, styled with `.form-input-modern` |
| `*dxTemplate` cell templates | Inline Angular `@if` / `[class]` / `{{ }}` | No template syntax needed |

### 4.4 Localization Keys

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

## 5. Course Catalog (`training/catalog`)

### 5.1 Backend — Mapperly → AutoMapper Migration

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Application/Training/Mapper/TrainingAutoMapperProfile.cs` | **Added CourseCatalog mappings** | `CreateMap<CourseCatalog, CourseCatalogDto>()` (ignores `FieldNameAr`, `FieldNameEn`, `ConditionsCount`). `CreateMap<CreateUpdateCourseCatalogDto, CourseCatalog>()`. `CreateMap<CatalogEnrollmentCondition, CatalogEnrollmentConditionDto>()`. |
| `src/MOD.Training.Application/Training/Catalog/CourseCatalogAppService.cs` | **Uses AutoMapper** | Injected `IMapper`. Replaced `input.ToEntity()` → `mapper.Map<CourseCatalog>(input)`. Replaced `input.MapTo(entity)` → `mapper.Map(input, entity)`. Replaced `entity.ToDto()` → `mapper.Map<CourseCatalogDto>(entity)`. Replaced `condition.ToDto()` → `mapper.Map<CatalogEnrollmentConditionDto>(condition)`. `MapToDto()` changed from `static` to instance method to access `mapper`. |
| `src/MOD.Training.Application/Training/TenantCourses/TenantCourseAppService.cs` | **Uses AutoMapper for CourseCatalog** | Injected `IMapper`. Replaced `x.ToDto()` (CourseCatalog) → `mapper.Map<CourseCatalogDto>(x)` in `GetAvailableCatalogCoursesAsync`. |
| `src/MOD.Training.Application/Training/Mapper/TrainingMapper.cs` | **Commented out** CourseCatalog Mapperly mappings | `CourseCatalog.ToDto()`, `CreateUpdateCourseCatalogDto.ToEntity()`, `MapTo()` commented out. Other Mapperly mappings (CourseField, CourseProposal, TenantCourse, etc.) remain active. |
| `src/MOD.Training.Application.Contracts/Training/Catalog/Dtos/CourseCatalogDto.cs` | **No change** | DTO contains all fields needed by new UI including `FieldNameAr`, `ConditionsCount`, `IsActive`. |
| `src/MOD.Training.Domain/Training/Catalog/CourseCatalog.cs` | **No change** | Entity with navigation to `CourseField`. |

### 5.2 Frontend — Complete DevExtreme Removal & Modern Redesign

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

### 5.3 DevExtreme → Custom HTML Migration Matrix

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

### 5.4 Localization Keys Added

```
Training.CourseCatalog.NoCourses        → "No courses found" / "لم يتم العثور على دورات"
Training.CourseCatalog.NoCoursesHint    → "Add your first course to the catalog" / "أضف أول دورة إلى الكتالوج"
Training.CourseCatalog.DeleteConfirm    → "Are you sure you want to delete this course?" / "هل أنت متأكد من حذف هذه الدورة؟"
```

---

## 6. Course Proposals (`training/catalog/proposals`)

### 6.1 Backend — Mapperly → AutoMapper + Validation + Formatted Dates

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Application/Training/Mapper/TrainingAutoMapperProfile.cs` | **Added CourseProposal mappings** | `CreateMap<CourseProposal, CourseProposalDto>()` (ignores `FieldNameAr`, `ProposedByName`, `CreationTimeFormatted`, `ReviewedAtFormatted`). `CreateMap<CreateCourseProposalDto, CourseProposal>()`. |
| `src/MOD.Training.Application/Training/Mapper/TrainingMapper.cs` | **Commented out** CourseProposal Mapperly mapping | `CourseProposal.ToDto()` commented out. Other Mapperly mappings remain active. |
| `src/MOD.Training.Application/Training/CourseProposals/CourseProposalAppService.cs` | **Uses AutoMapper + validation + formatted dates** | Injected `IMapper`. `CreateAsync` now uses `mapper.Map<CourseProposal>(input)` instead of manual property assignment. `MapToDto()` uses `mapper.Map<CourseProposalDto>(entity)` then enriches `FieldNameAr`, `CreationTimeFormatted`, `ReviewedAtFormatted`. |
| `src/MOD.Training.Application.Contracts/Training/CourseProposals/Dtos/CourseProposalDto.cs` | **Validation attributes + formatted date fields** | `[Required]` + `[MinLength(2)]` on `CourseNameAr`/`CourseNameEn`. `[Required]` on `Category`, `Nature`, `FieldId`. `[Required]` on `ReviewCourseProposalDto.Decision`. `[MinLength(3)]` on `RejectionReason`. Added `CreationTimeFormatted` and `ReviewedAtFormatted` string properties. |
| `src/MOD.Training.Application.Contracts/Training/Catalog/Dtos/CourseProposalDto.cs` | **DELETED** | Duplicate file in wrong namespace (`Catalog.Dtos`) causing ambiguous reference build errors. Correct DTOs already exist in `CourseProposals/Dtos/`. |
| `angular/src/app/proxy/training/course-proposals/dtos/models.ts` | **Updated proxy** | Added `creationTimeFormatted` and `reviewedAtFormatted` to `CourseProposalDto` interface. |

### 6.2 Frontend — Complete DevExtreme Removal & Modern Redesign

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

### 6.3 DevExtreme → Custom HTML Migration Matrix

| Old Control | New Implementation | Notes |
|-------------|-------------------|-------|
| `dx-popup` (submit + review) | `.modal-backdrop-modern` + `.modal-modern` | Backdrop click to close, custom footer buttons |
| `dx-text-box` | HTML `<input type="text">` | Search + form fields |
| `dx-select-box` | HTML `<select>` | Status filter, Category, Nature, Field |
| `dx-text-area` | HTML `<textarea>` | Rejection reason |
| `dx-data-grid` | Custom card lists | Pending + reviewed proposal cards |

### 6.4 Localization Keys Added

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

## 7. Tenant Courses (`training/tenant-courses`)

### 7.1 Backend — Mapperly → AutoMapper + Validation + Formatted Dates

| File | Change | Details |
|------|--------|---------|
| `src/MOD.Training.Application/Training/Mapper/TrainingAutoMapperProfile.cs` | **Added TenantCourse mappings** | `CreateMap<TenantCourse, TenantCourseDto>()` (ignores `CatalogCourseNameAr`, `CatalogCourseNameEn`, `CatalogCourseFieldNameAr`, `CatalogCourseCategory`, `ConditionsCount`, `AddedByName`, `AddedAtFormatted`). `CreateMap<UpdateTenantCourseDto, TenantCourse>()`. `CreateMap<TenantCourseCondition, TenantCourseConditionDto>()`. |
| `src/MOD.Training.Application/Training/Mapper/TrainingMapper.cs` | **Commented out** TenantCourse Mapperly mappings | `TenantCourse.ToDto()`, `UpdateTenantCourseDto.MapTo()`, `TenantCourseCondition.ToDto()` commented out. |
| `src/MOD.Training.Application/Training/TenantCourses/TenantCourseAppService.cs` | **Uses AutoMapper + validation + formatted dates** | Injected `IMapper` (was already injected for CourseCatalog). `MapToDto()` uses `mapper.Map<TenantCourseDto>(entity)` then enriches catalog fields + conditions count + `AddedAtFormatted`. `UpdateAsync` uses `mapper.Map(input, entity)`. `GetConditionsAsync` uses `mapper.Map<TenantCourseConditionDto>(condition)`. |
| `src/MOD.Training.Application.Contracts/Training/TenantCourses/Dtos/TenantCourseDto.cs` | **Validation attributes + formatted date field** | `[Range(1, 9999)]` on `DefaultCapacity`. `[Range(1, 999)]` on `DefaultDurationWeeks`. `[Required]` on `ResultType`. Added `AddedAtFormatted` string property. |
| `angular/src/app/proxy/training/tenant-courses/dtos/models.ts` | **Updated proxy** | Added `addedAtFormatted` to `TenantCourseDto` interface. |

### 7.2 Frontend — Complete DevExtreme Removal & Modern Redesign

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

### 7.3 DevExtreme → Custom HTML Migration Matrix

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

### 7.4 Localization Keys Added

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

## 8. Plan Review (`training/plans/plan-review`)

### 8.1 Frontend

| File | Change | Details |
|------|--------|---------|
| `angular/.../plan-review.component.ts` | **Flattening fix** | Removed `parentId`-based grouping logic (`const parents = all.filter(fi => !fi.parentId)` etc.). `groupedFinancialItems` now maps directly from flat `all` array. |
| `angular/.../plan-review.component.html` | **Flattening fix** | Replaced `<optgroup>` parent/child `<select>` with flat `<select>` using `{{ nameAr }} ({{ code }})`. |

> No backend changes required — purely a frontend adaptation to the flattened `FinancialItemDto` proxy.

---

## 9. Backend Architecture Decisions

### 9.1 Flattening Strategy
- **Parent rows** remain in DB temporarily (`ParentId` column kept but business logic ignores it).
- Future migration should nullify/delete parent rows and drop `ParentId` column.
- `IsGeneral` removed from DTOs and business logic.

### 9.2 Mapperly vs AutoMapper
- Project originally uses **Mapperly** (`Riok.Mapperly`) as the default ABP 10.x mapper.
- **CourseTypeFinancialDefaults** was migrated to **AutoMapper** per explicit request.
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

### 9.3 Localization Pattern
- Backend: JSON files in `src/MOD.Training.Domain.Shared/Localization/Training/{ar,en}.json`
- Frontend: Use `{{ '::Key' | abpLocalization }}` in templates or `this.l.t('::Key')` in TS.
- Parameterized keys: `{{ '::Key' | abpLocalization : ('' + value) }}` (pipe expects `string | string[]`).

---

## 10. Files to Know (Quick Reference)

### Backend — Finance Domain
```
src/MOD.Training.Domain/Training/Finance/FinancialItem.cs
src/MOD.Training.Domain/Training/Finance/ExchangeRate.cs
src/MOD.Training.Domain/Training/Finance/CourseTypeFinancialItemDefault.cs
src/MOD.Training.Application/Training/Finance/FinancialItemAppService.cs
src/MOD.Training.Application/Training/Finance/ExchangeRateAppService.cs
src/MOD.Training.Application/Training/Finance/CourseTypeFinancialDefaultAppService.cs
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
angular/src/app/Projects/finance/financial-item-defaults/
angular/src/app/Projects/finance/exchange-rates/
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

## 11. Pending / Future Work

| # | Item | Priority |
|---|------|----------|
| 1 | **Delete legacy parent rows** from `TrnFinancialItems` and drop `ParentId` column | Medium |
| 2 | **Remove `CourseTypeFinancialItemDefaultMappers.cs`** (Mapperly) if AutoMapper is finalized | Low |
| 3 | **Data migration** for `FinancialItem` parent rows (nullify or reclassify) | Medium |
| 4 | **Sass `@import` deprecation** warnings in Angular build (`gtms-design.scss`) | Low |
| 5 | ~~**Course Fields page redesign**~~ (removed DevExtreme, added card grid + modern modal) | **Done** |
| 6 | ~~**Course Proposals page redesign**~~ (removed DevExtreme, modern UI + backend AutoMapper migration + validation) | **Done** |
| 7 | ~~**Tenant Courses page redesign**~~ (removed DevExtreme, modern UI + add-from-catalog dialog + backend AutoMapper migration + validation) | **Done** |
