namespace Travel;

public static class TravelErrorCodes
{
    public const string ExceedsMaxAllowanceDays = "TravelManagement:ExceedsMaxAllowanceDays";
    public const string InvalidStatusTransition = "TravelManagement:InvalidStatusTransition";
    public const string TravelRequestNotFound = "TravelManagement:TravelRequestNotFound";
    public const string EmployeeNotFound = "TravelManagement:EmployeeNotFound";
    public const string UnauthorizedStatusChange = "TravelManagement:UnauthorizedStatusChange";
    public const string CanOnlyEditDraftOrReturned = "TravelManagement:CanOnlyEditDraftOrReturned";
    public const string InvalidAllowanceSegment = "TravelManagement:InvalidAllowanceSegment";
    public const string NameTooLong = "TravelManagement:NameTooLong";
    public const string NegativeAmount = "TravelManagement:NegativeAmount";
    public const string AllowanceRuleNotFound = "TravelManagement:AllowanceRuleNotFound";
    public const string AllowanceRateNotFound = "TravelManagement:AllowanceRateNotFound";
    public const string NoMatchingAllowanceRule = "TravelManagement:NoMatchingAllowanceRule";
    public const string NoMatchingAllowanceRate = "TravelManagement:NoMatchingAllowanceRate";
    public const string UseClothingAllowanceRules = "TravelManagement:UseClothingAllowanceRules";
    public const string InvalidTravelTypeCode = "TravelManagement:InvalidTravelTypeCode";
    public const string FlightOfferNotFound = "TravelManagement:FlightOfferNotFound";
}
