namespace VeltrexAPI.Entities;

/// <summary>
/// A client's KYC submission: one row per client, keyed by <see cref="UserId"/>.
/// No row means the client hasn't filled it in yet (the account still works).
/// </summary>
public class KycDetails
{
    public Guid UserId { get; set; }

    /* Personal details */
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }

    /* Address */
    public string AddressLine { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Nationality { get; set; } = "";

    /* Bank account (for payouts) */
    public string BankName { get; set; } = "";
    public string AccountHolderName { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string IfscCode { get; set; } = "";

    /* Documents */
    public string AadhaarNumber { get; set; } = "";
    public string PanNumber { get; set; } = "";

    public KycStatus Status { get; set; } = KycStatus.Submitted;
    public string AdminNote { get; set; } = "";
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }

    public User? User { get; set; }
}
