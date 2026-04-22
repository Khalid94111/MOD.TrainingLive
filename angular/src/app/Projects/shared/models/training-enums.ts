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
  FundingCoversAll = 0,
  FundingCoversCourseOnly = 1,
  FinancialCoversAll = 2,
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

export enum ConditionType {
  Rank = 0,
  Age = 1,
  ServiceYears = 2,
  Education = 3,
  MedicalFitness = 4,
  SecurityClearance = 5,
  LanguageLevel = 6,
  PreviousCourse = 7,
  Custom = 8,
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
  UGMApproved = 1,
  StaffReviewed = 2,
  TDApproved = 3,
  THApproved = 4,
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

export const CONDITION_TYPE_OPTIONS = [
  { value: ConditionType.Rank, key: '::Training.ConditionType.Rank' },
  { value: ConditionType.Age, key: '::Training.ConditionType.Age' },
  { value: ConditionType.ServiceYears, key: '::Training.ConditionType.ServiceYears' },
  { value: ConditionType.Education, key: '::Training.ConditionType.Education' },
  { value: ConditionType.MedicalFitness, key: '::Training.ConditionType.MedicalFitness' },
  { value: ConditionType.SecurityClearance, key: '::Training.ConditionType.SecurityClearance' },
  { value: ConditionType.LanguageLevel, key: '::Training.ConditionType.LanguageLevel' },
  { value: ConditionType.PreviousCourse, key: '::Training.ConditionType.PreviousCourse' },
  { value: ConditionType.Custom, key: '::Training.ConditionType.Custom' },
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
