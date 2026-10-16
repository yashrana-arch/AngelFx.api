namespace VeltrexAPI.Entities;

/// <summary>
/// A payment screenshot a client uploads after paying via the deposit QR / bank details.
/// The image itself is stored in the row (<see cref="ImageData"/>), not on disk.
/// There's no payment-gateway link, so it's tied to the payment only by the uploader and
/// the optional amount / reference they enter; the admin checks it and credits the ledger by hand.
/// </summary>
public class PaymentProof
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public byte[] ImageData { get; set; } = [];
    public string ContentType { get; set; } = "";
    public string FileName { get; set; } = "";
    public int FileSize { get; set; }

    /// <summary>Optional: what the client says they paid.</summary>
    public decimal? Amount { get; set; }
    /// <summary>Optional: UTR / UPI transaction reference.</summary>
    public string Reference { get; set; } = "";

    public PaymentProofStatus Status { get; set; } = PaymentProofStatus.Pending;
    public string AdminNote { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }

    public User? User { get; set; }
}
