# GTMS Phase 4C-α — Patch 3: Travel Instruction Defaults Expansion (Visa + Insurance + Tickets)

**Version:** v1.0
**Target:** v4.10.2 → v4.10.3 (patch bump)
**Scope:** Extend the auto-fill defaults from Patch 2 to cover Section 2 (Visa), Section 3 (Insurance), and Section 4 (Tickets) of the Travel Instruction dialog. Apply context-aware defaults based on CourseType.
**Reason:** Patch 2 auto-filled the 4 date fields. Staff still has to manually toggle the boolean flags (visa/insurance/tickets) and these are highly predictable based on whether the course is International vs Local.

---

## What's changing

**Before this patch:**
- Patch 2 auto-fills 4 date fields ✓
- Visa/Insurance/Tickets boolean flags + text fields remain empty/false
- Staff manually toggles them for every new travel instruction

**After this patch:**
- Patch 2 date logic preserved (unchanged)
- Visa toggle defaults based on CourseType (true for International, false for Local)
- Insurance toggle defaults based on CourseType (true for International, false for Local)
- Tickets toggle always defaults to false (booking happens later, Staff updates when done)
- Text fields (Notes, Provider, Reference) stay empty — Staff enters when info is available
- All defaults applied on Create only — Edit mode preserves persisted values (consistent with Patch 2)

---

## Locked decisions

| # | Decision | Rationale |
|---|---|---|
| Visa default — International | `VisaRequired = true` | Most international travel needs visas; Staff unchecks for visa-free destinations |
| Visa default — Local | `VisaRequired = false` | No visa needed for in-country training |
| Insurance default — International | `InsuranceArranged = true` | Standard requirement for international travel |
| Insurance default — Local | `InsuranceArranged = false` | Travel insurance typically not arranged for local courses |
| Tickets default — All | `TicketsBooked = false` | TravelInstruction issued before ticket booking; Staff updates later |
| Text fields | Always empty on Create | Staff has no info at instruction-creation time |
| Apply scope | Create only, Edit preserves persisted values | Consistent with Patch 2 |

---

## Backend changes

**No backend changes.** This is a frontend-only patch — the TravelInstruction entity already accepts these flags + text fields. We're computing better defaults on the client side.

---

## Frontend changes

### F1 — Extend the helper from Patch 2

**File:** `src/app/training/shared/travel-defaults/travel-defaults.helper.ts`

Add a second function alongside `computeTravelDefaults`:

```typescript
import { CourseType } from 'src/app/proxy/training/enums/course-type.enum';

// Existing from Patch 2:
export interface TravelDateDefaults {
  departureDate: string;
  arrivalDate: string;
  returnDate: string;
  arrivalBackDate: string;
}

// NEW for Patch 3:
export interface TravelInstructionDefaults {
  // Section 1 — dates (from Patch 2)
  departureDate: string;
  arrivalDate: string;
  returnDate: string;
  arrivalBackDate: string;
  
  // Section 2 — Visa
  visaRequired: boolean;
  visaNotes: string;
  
  // Section 3 — Insurance
  insuranceArranged: boolean;
  insuranceProvider: string;
  insurancePolicyNumber: string;
  
  // Section 4 — Tickets
  ticketsBooked: boolean;
  ticketReference: string;
  ticketProvider: string;
}

/**
 * Compute full travel instruction defaults (dates + all section flags).
 * Combines Patch 2 date logic with Patch 3 flag/text defaults based on CourseType.
 *
 * Always returns text fields as empty strings — Staff fills these when info is
 * available (e.g., after visa is issued, after tickets are booked).
 */
export function computeFullTravelDefaults(
  actualStartDate: string | null | undefined,
  actualEndDate: string | null | undefined,
  courseType: CourseType
): TravelInstructionDefaults {
  // Reuse Patch 2's date logic
  const dates = computeTravelDefaults(actualStartDate, actualEndDate, courseType);
  
  const isInternational = courseType === CourseType.ExternalInternational;
  
  return {
    ...dates,
    
    // Section 2 — Visa: true for International, false for Local
    visaRequired: isInternational,
    visaNotes: '',
    
    // Section 3 — Insurance: true for International, false for Local
    insuranceArranged: isInternational,
    insuranceProvider: '',
    insurancePolicyNumber: '',
    
    // Section 4 — Tickets: always false on creation (booked later)
    ticketsBooked: false,
    ticketReference: '',
    ticketProvider: '',
  };
}

// Existing function from Patch 2 stays unchanged:
export function computeTravelDefaults(
  actualStartDate: string | null | undefined,
  actualEndDate: string | null | undefined,
  courseType: CourseType
): TravelDateDefaults {
  // ... existing implementation
}
```

### F2 — Update the Travel Instruction component

**File:** `src/app/training/casual-courses/casual-course-travel-instructions/casual-course-travel-instructions.component.ts` (or equivalent polymorphic component path)

In Patch 2 we modified `onAddInstruction()` to call `computeTravelDefaults`. Now update it to call `computeFullTravelDefaults`:

```typescript
import { computeFullTravelDefaults } from '../../shared/travel-defaults/travel-defaults.helper';

onAddInstruction(): void {
  const courseType = this.parentCourseType();
  
  if (courseType === null) {
    this.resetFormToEmpty();
    return;
  }
  
  if (courseType === CourseType.Internal) {
    this.saveError.set(this.l.t('::Training:TravelInstruction:NotApplicableForInternal'));
    return;
  }
  
  const defaults = computeFullTravelDefaults(
    this.parentActualStartDate(),
    this.parentActualEndDate(),
    courseType
  );
  
  // Section 1 — dates (from Patch 2)
  this.fDepartureDate.set(defaults.departureDate);
  this.fArrivalDate.set(defaults.arrivalDate);
  this.fReturnDate.set(defaults.returnDate);
  this.fArrivalBackDate.set(defaults.arrivalBackDate);
  
  // Section 2 — Visa (NEW in Patch 3)
  this.fVisaRequired.set(defaults.visaRequired);
  this.fVisaNotes.set(defaults.visaNotes);
  
  // Section 3 — Insurance (NEW in Patch 3)
  this.fInsuranceArranged.set(defaults.insuranceArranged);
  this.fInsuranceProvider.set(defaults.insuranceProvider);
  this.fInsurancePolicyNumber.set(defaults.insurancePolicyNumber);
  
  // Section 4 — Tickets (NEW in Patch 3)
  this.fTicketsBooked.set(defaults.ticketsBooked);
  this.fTicketReference.set(defaults.ticketReference);
  this.fTicketProvider.set(defaults.ticketProvider);
  
  this.isEditMode.set(false);
  this.editingInstructionId.set(null);
  this.saveError.set(null);
  this.instructionDialogOpen.set(true);
}
```

**Adjust field names** if they differ in the actual component (e.g., `fInsurancePolicyNumber` might be named `fPolicyNumber`). Match the existing signals.

### F3 — Verify Edit mode doesn't reset

Same as Patch 2. The `onEditInstruction()` method should populate from persisted values, not from defaults. Confirm this still applies for the new fields:

```typescript
onEditInstruction(instruction: TravelInstructionDto): void {
  this.isEditMode.set(true);
  this.editingInstructionId.set(instruction.id);
  
  // Section 1 — dates
  this.fDepartureDate.set(instruction.departureDate?.substring(0, 10) ?? '');
  this.fArrivalDate.set(instruction.arrivalDate?.substring(0, 10) ?? '');
  this.fReturnDate.set(instruction.returnDate?.substring(0, 10) ?? '');
  this.fArrivalBackDate.set(instruction.arrivalBackDate?.substring(0, 10) ?? '');
  
  // Section 2 — Visa
  this.fVisaRequired.set(instruction.visaRequired ?? false);
  this.fVisaNotes.set(instruction.visaNotes ?? '');
  
  // Section 3 — Insurance
  this.fInsuranceArranged.set(instruction.insuranceArranged ?? false);
  this.fInsuranceProvider.set(instruction.insuranceProvider ?? '');
  this.fInsurancePolicyNumber.set(instruction.insurancePolicyNumber ?? '');
  
  // Section 4 — Tickets
  this.fTicketsBooked.set(instruction.ticketsBooked ?? false);
  this.fTicketReference.set(instruction.ticketReference ?? '');
  this.fTicketProvider.set(instruction.ticketProvider ?? '');
  
  this.instructionDialogOpen.set(true);
}
```

### F4 — Update the hint banner from Patch 2

**File:** Same component template.

Patch 2's hint banner said "Dates were auto-filled". Patch 3 expands the scope, so update the message:

```html
@if (!isEditMode() && parentActualStartDate() && parentActualEndDate()) {
  <div class="hint-banner">
    <span class="hint-icon">💡</span>
    <span class="hint-text">
      {{ '::Training.TravelInstruction.DefaultsHint' | abpLocalization }}
    </span>
  </div>
}
```

Update the localization key value (don't change the key itself):

```json
// ar.json — replace existing value
"Training.TravelInstruction.DefaultsHint": "تم ملء التواريخ والخيارات تلقائياً بناءً على نوع الدورة وتواريخها الفعلية. يمكنك تعديل أي قيمة قبل الإصدار.",

// en.json — replace existing value
"Training.TravelInstruction.DefaultsHint": "Dates and options were auto-filled based on course type and actual dates. You can adjust any value before issuing."
```

### F5 — Frontend verification

After applying F1-F4:

```bash
ng build
```

Manual test scenarios:

**Scenario 1 — International course, Create:**
1. Open a casual course with CourseType=ExternalInternational, ActualStart=15 May, ActualEnd=19 May
2. Section 4 → "Issue Travel Instruction"
3. Verify:
   - Dates: 14, 14, 19, 20 (from Patch 2)
   - Visa: ✓ Required (checked)
   - Insurance: ✓ Arranged (checked)
   - Tickets: ☐ Booked (unchecked)
   - All text fields empty (Notes, Provider, Reference, Policy Number)
4. Uncheck Visa → submit → record saved with VisaRequired=false

**Scenario 2 — Local course, Create:**
1. Open a casual course with CourseType=ExternalLocal
2. Section 4 → "Issue Travel Instruction"
3. Verify:
   - Dates: same as ActualStart / ActualEnd (no buffer)
   - Visa: ☐ Required (unchecked)
   - Insurance: ☐ Arranged (unchecked)
   - Tickets: ☐ Booked (unchecked)
4. Check Insurance → enter provider → submit → saved with values

**Scenario 3 — Edit existing International instruction:**
1. Open the casual course from Scenario 1 (instruction in Draft)
2. Click Edit
3. Verify form shows the values that were persisted (NOT re-computed defaults)
4. Edit visa notes → save → only that change persists

**Scenario 4 — Session, International:**
1. Open a session with same setup
2. Section 4 → Issue Travel Instruction
3. Same defaults applied as Scenario 1

**Scenario 5 — Session, Local:**
1. Open a session with ExternalLocal
2. Section 4 → Issue Travel Instruction
3. Same defaults as Scenario 2

---

## Cross-cutting

### Patch 2 stays operational

Patch 2's `computeTravelDefaults` function remains exported and unchanged. The new `computeFullTravelDefaults` calls it internally to reuse the date logic. Both functions coexist — no breaking change to anything that might use the older one.

### TravelAllowancePayment downstream (Phase 4B-β)

`InsuranceOMR` and `VisaFeesOMR` defaults in TravelAllowancePayment creation (Phase 4B-β) read from TravelInstruction:
- If `VisaRequired=true` → default `VisaFeesOMR` to some standard amount
- If `InsuranceArranged=true` → default `InsuranceOMR` to some standard amount

Patch 3 makes these flags more likely to be set correctly at instruction time, so the downstream defaults become more accurate. No code change needed in Phase 4B-β — it just benefits.

### Field name verification

The exact signal names in the component (`fVisaRequired`, `fInsuranceArranged`, etc.) might differ from this prompt. Claude Code should:
1. Read the existing component first
2. Map prompt field names to actual signal names
3. Adjust the patch to match

---

## Verification checklist

- [ ] `travel-defaults.helper.ts` has both `computeTravelDefaults` (Patch 2) and `computeFullTravelDefaults` (Patch 3)
- [ ] New function returns the full TravelInstructionDefaults interface
- [ ] Component's onAddInstruction calls `computeFullTravelDefaults` and sets all 11 signals
- [ ] Component's onEditInstruction populates from persisted values (no re-apply of defaults)
- [ ] Hint banner message updated in both ar.json and en.json
- [ ] International: Visa=true, Insurance=true, Tickets=false defaults verified
- [ ] Local: Visa=false, Insurance=false, Tickets=false defaults verified
- [ ] Text fields (Notes, Provider, Reference, Policy Number) always empty on Create
- [ ] Edit mode preserves all persisted values across the 4 sections
- [ ] Works for both Casual Course (4B-α path) and Session (4C-α path)
- [ ] No backend changes
- [ ] All 5 manual test scenarios pass

---

## Commit message suggestion

```
feat(travel-instructions): expand defaults to visa/insurance/tickets

Patch 2 auto-filled the 4 date fields. Patch 3 extends this to the 
remaining 3 sections (Visa, Insurance, Tickets) with context-aware 
defaults based on CourseType:

ExternalInternational:
  VisaRequired       = true
  InsuranceArranged  = true
  TicketsBooked      = false (Staff updates after booking)

ExternalLocal:
  VisaRequired       = false (no visa for in-country)
  InsuranceArranged  = false (no travel insurance needed)
  TicketsBooked      = false

Text fields (visa notes, insurance provider, ticket reference, etc.) 
remain empty on Create — Staff fills these when info becomes available.

Edit mode preserves all persisted values across the 4 sections.

Backwards compatible: Patch 2's computeTravelDefaults remains exported.
Patch 3's computeFullTravelDefaults reuses it internally for the dates.

Frontend-only. No backend, schema, or API changes.

Bumps v4.10.2 → v4.10.3
```

---

*End of Phase 4C-α Patch 3 — v1.0*
*Estimated effort: ~1 hour Claude Code work. Helper expansion + onAddInstruction extension + 1 localization message update.*
*Risk: Very low — additive UI improvement, no logic changes, no schema. Worst case: Staff toggles defaults if they're wrong for the situation.*
