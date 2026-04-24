# GTMS Phase 4A — Patch Prompt 3 (Funding Scenario Fixes)

**Target version:** v4.5.2 → v4.5.3
**Scope:** Fix two defects on PAGE 4.3 Review visible in production: (1) funding scenario description keys showing raw JSON paths in the UI instead of Arabic text, (2) every financial item row showing `FundingSource` regardless of scenario — scenario→source derivation logic is broken.
**Base code:** Phase 4A v4.5.2 (per-rank review shipped).
**Companion docs:** `docs/GTMS-BRD-v4_0.md` §17.2 (3 Funding Scenarios table), `docs/GTMS-ERD-v4_0.html` line 264 (confirms `FinancialItem.ItemType` exists), `docs/GTMS-Phase4A-Implementation-Prompt.md` §3.1 (scenario semantics).

---

## 1. Why this patch exists

A production screenshot of PAGE 4.3 (Staff review, casual course) shows two defects:

**Defect A — Localization keys leaking:**
```
Training.FundingScenario.NoReallocation
Training.FundingScenario.PartialReallocation
Training.FundingScenario.FullReallocation
```
These render raw as the scenario card descriptions. They should resolve to Arabic text.

**Defect B — Scenario 2 not splitting sources correctly:**
Scenario 2 is selected. Per BRD §17.2:
- Course cost → `FundingSource` (project code)
- Travel items (ticket, insurance, visa, allowance, clothing) → `FinancialItems` (internal budget, triggers reallocation)

Actual rendered source column: **every row shows `الجهة المُموِّلة` (FundingSource)**. Travel items should show `FinancialItem` instead.

Both defects affect the same page and same workflow moment, so they're bundled into one patch.

**ERD note:** `FinancialItem.ItemType` already exists (line 264: `CourseCost / Ticket / ...`). This patch does not need to add a new classification field — use the existing `ItemType` enum.

---

## 2. Defect A fix — localization keys

### 2.1 Verify key format per project rules

Per the established GTMS rule:
- **TS / HTML:** `::Training.FundingScenario.PartialReallocation` (with `::` prefix)
- **JSON:** `"Training.FundingScenario.PartialReallocation"` (dot notation, no `::` prefix)
- **NEVER:** `"Training::FundingScenario.PartialReallocation"` (colon-colon in the middle)

### 2.2 Add missing keys to both JSON files

**File:** `src/YourApp.Domain.Shared/Localization/Training/ar.json`

Add (or verify present):
```json
{
  "Training.FundingScenario.NoReallocation": "مصدر التمويل يغطي كل شيء — لا توجد إعادة تخصيص",
  "Training.FundingScenario.PartialReallocation": "مصدر التمويل يغطي تكلفة الدورة — السفر عبر إعادة التخصيص",
  "Training.FundingScenario.FullReallocation": "إعادة تخصيص كاملة من البنود المالية للدورة والسفر"
}
```

**File:** `src/YourApp.Domain.Shared/Localization/Training/en.json`

Add (or verify present):
```json
{
  "Training.FundingScenario.NoReallocation": "FundingSource covers everything — no reallocation",
  "Training.FundingScenario.PartialReallocation": "FundingSource covers course cost — travel via reallocation",
  "Training.FundingScenario.FullReallocation": "Full reallocation from financial items for course + travel"
}
```

### 2.3 Verify the enum value names match

The enum is:
```csharp
public enum FundingScenario
{
    FundingSourceCoversAll    = 1,  // UI label key → NoReallocation
    FundingSourceCoversCourse = 2,  // UI label key → PartialReallocation
    FinancialItemsCoverAll    = 3   // UI label key → FullReallocation
}
```

**Two naming conventions here** — the enum member names (in code) and the localization key suffixes (in JSON). They differ because the code names describe "who pays what" and the UI keys describe "reallocation impact."

Check the scenario card component. If it renders the label using the **enum member name** (e.g., `::Training.FundingScenario.{{ enumName }}`), the key resolver is looking up `Training.FundingScenario.FundingSourceCoversAll` but the JSON has `NoReallocation`. That's the bug.

**Fix one of two ways:**

**Option X (recommended):** Keep JSON keys as `NoReallocation / PartialReallocation / FullReallocation` (clearer for translators), and maintain a mapping in TypeScript:

```typescript
// casual-course-review.component.ts
readonly scenarioLabelKey: Record<FundingScenario, string> = {
  [FundingScenario.FundingSourceCoversAll]: '::Training.FundingScenario.NoReallocation',
  [FundingScenario.FundingSourceCoversCourse]: '::Training.FundingScenario.PartialReallocation',
  [FundingScenario.FinancialItemsCoverAll]: '::Training.FundingScenario.FullReallocation'
};
```

Then template:
```html
{{ scenarioLabelKey[scenario.value] | abpLocalization }}
```

**Option Y:** Rename JSON keys to match enum members (`FundingSourceCoversAll` etc). Easier but less translator-friendly.

Pick **Option X** unless the team has a strong preference otherwise.

### 2.4 Verify after fix

Reload PAGE 4.3. The three scenario cards should show:
- Card 1: "مصدر التمويل يغطي كل شيء — لا توجد إعادة تخصيص"
- Card 2: "مصدر التمويل يغطي تكلفة الدورة — السفر عبر إعادة التخصيص"
- Card 3: "إعادة تخصيص كاملة من البنود المالية للدورة والسفر"

No raw `Training.FundingScenario.*` strings anywhere on the page.

---

## 3. Defect B fix — scenario→source derivation

### 3.1 Root cause

The `Source` column on each `CasualCourseFinancial` row is set by a server-side derivation during `AssignScenarioAsync`. The current logic (suspected) sets every row to `FundingSource` regardless of scenario or item type.

**Correct rule per BRD §17.2:**

| Scenario | Course Cost items (ItemType = CourseCost) | All other items (travel, ancillary) |
|---|---|---|
| 1 — FundingSourceCoversAll | `FundingSource` | `FundingSource` |
| 2 — FundingSourceCoversCourse | `FundingSource` | **`FinancialItem`** |
| 3 — FinancialItemsCoverAll | `FinancialItem` | `FinancialItem` |

### 3.2 The classification uses existing `FinancialItem.ItemType`

No new column needed. The `FinancialItem` catalog entity already has `ItemType` with values like `CourseCost`, `Ticket`, `Insurance`, `Visa`, `Allowance`, etc. (per ERD line 264).

Course-cost classification = `financialItem.ItemType == FinancialItemType.CourseCost`.

**If `FinancialItemType` enum doesn't have a `CourseCost` member, stop and ask.** Every tenant should have at least one `FinancialItem` catalog row tagged as the course-cost item. If seeder didn't set this, that's a secondary bug to fix in this patch.

### 3.3 Implement the derivation helper

**File:** `src/YourApp.Domain/Training/Managers/FundingScenarioSourceResolver.cs` (new, small domain service)

```csharp
/// <summary>
/// Derives the Source (FundingSource | FinancialItem) for each
/// CasualCourseFinancial row based on scenario and item type.
/// Single source of truth — used by AssignScenarioAsync and AutoFillFromDefaultsAsync.
/// </summary>
public class FundingScenarioSourceResolver : IDomainService
{
    public FinancialAmountSource Resolve(FundingScenario scenario, FinancialItem item)
    {
        return scenario switch
        {
            FundingScenario.FundingSourceCoversAll    => FinancialAmountSource.FundingSource,
            FundingScenario.FinancialItemsCoverAll    => FinancialAmountSource.FinancialItem,
            FundingScenario.FundingSourceCoversCourse => item.ItemType == FinancialItemType.CourseCost
                ? FinancialAmountSource.FundingSource
                : FinancialAmountSource.FinancialItem,
            _ => throw new BusinessException("Training:CasualCourse:UnknownFundingScenario")
        };
    }
}
```

### 3.4 Wire it into `CasualCourseAppService.AssignScenarioAsync`

Find the block that creates or updates `CasualCourseFinancial` rows. Wherever `Source` is currently assigned (whether from a client DTO or a hardcoded value), replace with:

```csharp
financial.Source = _scenarioSourceResolver.Resolve(dto.FundingScenario, financialItem);
```

Where `financialItem` is the loaded `FinancialItem` entity for this row.

### 3.5 Wire it into `CasualCourseFinancialAppService.AutoFillFromDefaultsAsync`

Same change — whenever auto-fill creates a new `CasualCourseFinancial` row, set `Source` via the resolver:

```csharp
var parent = new CasualCourseFinancial
{
    // ... other fields
    Source = _scenarioSourceResolver.Resolve(course.FundingScenario ?? default, financialItem),
    // ...
};
```

Note — `course.FundingScenario` can be null if Staff hasn't picked yet. In that case auto-fill should default to `FundingScenario.FinancialItemsCoverAll` (scenario 3 = safest — everything from budget until Staff confirms).

Actually, better: **block auto-fill if `FundingScenario` is null.** Throw a localized business exception: "Training:CasualCourse:ScenarioRequiredBeforeAutoFill". Forces Staff to pick the scenario first. Cleaner rule.

### 3.6 Data migration for existing rows

Existing casual courses that were created before this patch have wrong `Source` values on their financial rows. Write a one-time data migration to fix them:

**Migration file:** `v4_5_3_CorrectFinancialItemSources`

```csharp
// Pseudocode — translate to EF migration
var resolver = new FundingScenarioSourceResolver();
var courses = await _casualCourseRepo.GetListAsync(c => c.FundingScenario != null);
foreach (var course in courses)
{
    var financials = await _financialRepo.GetListAsync(f => f.CasualCourseId == course.Id);
    foreach (var f in financials)
    {
        var item = await _financialItemRepo.GetAsync(f.FinancialItemId);
        f.Source = resolver.Resolve(course.FundingScenario.Value, item);
        await _financialRepo.UpdateAsync(f);
    }
}
```

Run in a single transaction per tenant. Log count of rows updated.

Alternative (simpler if EF Core migrations are awkward): write a one-shot C# console script that calls the resolver via DI and writes via the repositories. Either works — pick the lower-friction option for your setup.

---

## 4. Verification checklist

**Defect A — Localization**
- [ ] Three keys present in `ar.json` under dot-notation format
- [ ] Three keys present in `en.json`
- [ ] Scenario cards render Arabic text (or English, depending on UI language) — no raw `Training.FundingScenario.*` strings visible
- [ ] Works in both UTM view (PAGE 4.2 doesn't have the picker — skip) and Staff view (PAGE 4.3)

**Defect B — Scenario source derivation**
- [ ] `FundingScenarioSourceResolver` registered in DI (ABP auto-registers domain services usually)
- [ ] `AssignScenarioAsync` uses the resolver for every row
- [ ] `AutoFillFromDefaultsAsync` uses the resolver OR throws when scenario is null
- [ ] Creating a new casual course with scenario 2: course cost row shows `FundingSource`, all other rows show `FinancialItem`
- [ ] Creating a new casual course with scenario 1: every row shows `FundingSource`
- [ ] Creating a new casual course with scenario 3: every row shows `FinancialItem`
- [ ] Changing the scenario on an existing in-review casual course and re-saving: all rows re-classify correctly
- [ ] Data migration runs cleanly on all 3 demo tenants
- [ ] After migration, pull the production screenshot's casual course via API — verify travel items now show `Source = FinancialItem`

**Cross-cutting**
- [ ] TD / TH approval cost gate still enforces `EstimatedTotalCost > 0` (nothing in this patch should break that)
- [ ] Rank-aware preview (v4.5.1) still renders correctly — no regression
- [ ] PAGE 3.3 annual plan review untouched

---

## 5. Non-regression

- **Do not touch the `FundingScenario` enum values or their integer mapping.** Downstream Phase 4B reallocation logic depends on them.
- **Do not add a new column to `FinancialItem`.** Use existing `ItemType`.
- **Do not change the three scenario cards' order in the UI.** Numbering (1/2/3) is user-facing and in training docs.
- **Do not change the relationship between `FinancialItem.ItemType` and any other entity.** The `ItemType` enum is shared; this patch only reads it, never writes.

---

## 6. Commit / checkpoint

Commit message:
```
fix(casual-courses): funding scenario localization keys + source derivation

- Localization: add missing Training.FundingScenario.* keys to ar.json + en.json
- Scenario card component: map enum → locale key explicitly (Option X pattern)
- New FundingScenarioSourceResolver domain service — single source of truth
  for mapping (FundingScenario × FinancialItem.ItemType) → FinancialAmountSource
- AssignScenarioAsync + AutoFillFromDefaultsAsync now both use the resolver
- Auto-fill blocked when FundingScenario is null (forces Staff to pick first)
- Data migration v4_5_3: recompute Source on existing CasualCourseFinancial rows

Closes #<issue>
```

Append to checkpoint (v4.5.2 → v4.5.3) under "Post-Integration Bug Fixes":

| # | Issue | Root Cause | Fix |
|---|---|---|---|
| 11 | Scenario description keys rendered raw in UI (`Training.FundingScenario.NoReallocation`) | Missing JSON entries + unclear mapping from enum name to locale key suffix | Added 3 keys to ar.json + en.json; explicit enum→key map in the component |
| 12 | Every CasualCourseFinancial row showed `FundingSource` regardless of scenario | No scenario-aware derivation logic — `Source` was effectively hardcoded | New `FundingScenarioSourceResolver` domain service driven by `FinancialItem.ItemType`; called from both `AssignScenarioAsync` and `AutoFillFromDefaultsAsync`; data migration backfills existing rows |

---

## 7. Questions to confirm before starting

1. **Does `FinancialItemType` enum have a `CourseCost` member?** If not, stop and report back — adding this enum value should be a prerequisite, not part of this patch.
2. **Is the seeder tagging at least one `FinancialItem` per tenant with `ItemType = CourseCost`?** If not, fix the seeder as part of this patch — otherwise the resolver works but produces all-travel-items for scenario 2 because no item matches the course-cost branch.
3. **Does the UI scenario card component currently compute the label key from the enum value name directly** (i.e., `Training.FundingScenario.{{enum.FundingSourceCoversAll}}`)? Confirm before implementing Option X — if the code uses a different mapping already, just fix whichever keys are actually being looked up.

---

*End of Patch Prompt 3 — v1.0*
*Estimated effort: ~1 hour Claude Code work. Smaller than Patch 2. Main risk is the data migration — test on dev DB first.*
*Target file count: 2 JSON edits, 1 new domain service, 2 AppService edits, 1 component edit, 1 migration, 3 unit tests.*
