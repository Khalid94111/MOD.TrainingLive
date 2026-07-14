// ── Training Module Enums ──
// Mirrors Domain.Shared/Training/Enums/*

export enum CourseType {
  Internal = 0,
  ExternalLocal = 1,
  ExternalInternational = 2,
}

export enum PricingType {
  PerPerson = 0,
  Total = 1,
}

export enum PersonnelType {
  Officer = 0,
  Enlisted = 1,
}

export enum ResultType {
  AttendanceOnly = 0,
  PassFail = 1,
  CompletedNotCompleted = 2,
  GradeScore = 3,
}

export enum FundingScenario {
  FundingSourceCoversAll = 1,
  FundingSourceCoversCourse = 2,
  FinancialItemsCoverAll = 3,
}

export enum PaymentStatus {
  Pending = 0,
  Confirmed = 1,
  Rejected = 2,
}

export enum CompletionStatus {
  NotStarted = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3,
}

export enum ApprovalStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Returned = 3,
}

export enum PlanStatus {
  Draft = 0,
  Open = 1,
  Submitted = 2,
  UnderReview = 3,
  TDApproved = 4,
  THApproved = 5,
  Closed = 6,
  ReturnedToCreator = 7,
  Rejected = 8,
}

export enum CasualCourseStatus {
  Draft = 0,
  Submitted = 1,
  UGMApproved = 2,
  UnderReview = 3,
  StaffReviewed = 4,
  TDApproved = 5,
  THApproved = 6,
  ReturnedToCreator = 7,
  Rejected = 8,
}

export enum FinancialAmountSource {
  FundingSource = 0,
  FinancialItem = 1,
}

export enum NominationStatus {
  Nominated = 0,
  UTMApproved = 1,
  UGMApproved = 2,
  TDApproved = 3,
  Rejected = 4,
}

export enum ProposalStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
}

// ── Enum → Localization Key Maps ──
// Values are ABP localization keys resolved at runtime via LocalizationService

export const RESULT_TYPE_OPTIONS = [
  { value: ResultType.AttendanceOnly, key: '::Training.ResultType.AttendanceOnly' },
  { value: ResultType.PassFail, key: '::Training.ResultType.PassFail' },
  { value: ResultType.CompletedNotCompleted, key: '::Training.ResultType.CompletedNotCompleted' },
  { value: ResultType.GradeScore, key: '::Training.ResultType.GradeScore' },
];

export const PROPOSAL_STATUS_OPTIONS = [
  { value: ProposalStatus.Pending, key: '::Training.ProposalStatus.Pending', cssClass: 'badge-warning' },
  { value: ProposalStatus.Approved, key: '::Training.ProposalStatus.Approved', cssClass: 'badge-success' },
  { value: ProposalStatus.Rejected, key: '::Training.ProposalStatus.Rejected', cssClass: 'badge-danger' },
];

export const CATEGORY_OPTIONS = [
  { value: 'Military', key: '::Training.Category.Military' },
  { value: 'Civilian', key: '::Training.Category.Civilian' },
];

export const NATURE_OPTIONS = [
  { value: 'Mandatory', key: '::Training.Nature.Mandatory' },
  { value: 'Qualifying', key: '::Training.Nature.Qualifying' },
];

export const CASUAL_COURSE_STATUS_OPTIONS = [
  { value: CasualCourseStatus.Draft,             key: '::Training.CasualCourseStatus.Draft',             cssClass: 'status-draft' },
  { value: CasualCourseStatus.Submitted,         key: '::Training.CasualCourseStatus.Submitted',         cssClass: 'status-submitted' },
  { value: CasualCourseStatus.UGMApproved,       key: '::Training.CasualCourseStatus.UGMApproved',       cssClass: 'status-ugm-approved' },
  { value: CasualCourseStatus.UnderReview,       key: '::Training.CasualCourseStatus.UnderReview',       cssClass: 'status-under-review' },
  { value: CasualCourseStatus.StaffReviewed,     key: '::Training.CasualCourseStatus.StaffReviewed',     cssClass: 'status-staff-reviewed' },
  { value: CasualCourseStatus.TDApproved,        key: '::Training.CasualCourseStatus.TDApproved',        cssClass: 'status-td-approved' },
  { value: CasualCourseStatus.THApproved,        key: '::Training.CasualCourseStatus.THApproved',        cssClass: 'status-th-approved' },
  { value: CasualCourseStatus.ReturnedToCreator, key: '::Training.CasualCourseStatus.ReturnedToCreator', cssClass: 'status-returned' },
  { value: CasualCourseStatus.Rejected,          key: '::Training.CasualCourseStatus.Rejected',          cssClass: 'status-rejected' },
];

export const FUNDING_SCENARIO_OPTIONS = [
  { value: FundingScenario.FundingSourceCoversAll,    key: '::Training.FundingScenario.FundingSourceCoversAll',    hint: '::Training.FundingScenario.NoReallocation' },
  { value: FundingScenario.FundingSourceCoversCourse, key: '::Training.FundingScenario.FundingSourceCoversCourse', hint: '::Training.FundingScenario.PartialReallocation' },
  { value: FundingScenario.FinancialItemsCoverAll,    key: '::Training.FundingScenario.FinancialItemsCoverAll',    hint: '::Training.FundingScenario.FullReallocation' },
];

// Mirrors ExecutionStage enum in MOD.Training.Domain.Shared/Training/Enums.
// Pure UI metadata: localization key + badge color class. The progress-bearing
// stages (AwaitingTravelAllowances, AwaitingReallocationApproval) interpolate
// {0}/{1} from executionStageProgressCurrent / executionStageProgressTotal at render time.
export enum ExecutionStage {
  AwaitingQuoteSelection       = 1,
  AwaitingTravelInstruction    = 2,
  AwaitingTravelAllowances     = 3,
  AwaitingCoursePayment        = 4,
  AwaitingReallocationApproval = 5,
  FinanciallyComplete          = 6,
}

export const EXECUTION_STAGE_OPTIONS = [
  { value: ExecutionStage.AwaitingQuoteSelection,       key: '::Training.ExecutionStage.AwaitingQuoteSelection',       progressKey: null,                                                            cssClass: 'badge-stage-pending'  },
  { value: ExecutionStage.AwaitingTravelInstruction,    key: '::Training.ExecutionStage.AwaitingTravelInstruction',    progressKey: null,                                                            cssClass: 'badge-stage-pending'  },
  { value: ExecutionStage.AwaitingTravelAllowances,     key: '::Training.ExecutionStage.AwaitingTravelAllowances',     progressKey: '::Training.ExecutionStage.AwaitingTravelAllowancesProgress',     cssClass: 'badge-stage-progress' },
  { value: ExecutionStage.AwaitingCoursePayment,        key: '::Training.ExecutionStage.AwaitingCoursePayment',        progressKey: null,                                                            cssClass: 'badge-stage-pending'  },
  { value: ExecutionStage.AwaitingReallocationApproval, key: '::Training.ExecutionStage.AwaitingReallocationApproval', progressKey: '::Training.ExecutionStage.AwaitingReallocationApprovalProgress', cssClass: 'badge-stage-progress' },
  { value: ExecutionStage.FinanciallyComplete,          key: '::Training.ExecutionStage.FinanciallyComplete',          progressKey: null,                                                            cssClass: 'badge-stage-complete' },
];
