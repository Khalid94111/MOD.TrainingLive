namespace MOD.Training.Training.Payments.Dtos;

/// <summary>
/// Approval payload posted by the TD when accepting a Pending reallocation.
/// The server derives <c>ApprovedById</c> from <c>CurrentUser</c>; the
/// permission system enforces the TD role.
/// </summary>
public class MarkReallocationApprovedDto
{
    public string? ApprovalNote { get; set; }
}
