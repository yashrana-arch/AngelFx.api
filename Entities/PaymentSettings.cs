namespace VeltrexAPI.Entities;

/// <summary>
/// Deposit details shown to every client on the Deposit page: UPI QR plus bank transfer details.
/// Single-row table; the row always has <see cref="SingletonId"/>.
/// </summary>
public class PaymentSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    /// <summary>Path of the uploaded QR image relative to wwwroot, e.g. uploads/qr/abc.png.</summary>
    public string? QrImagePath { get; set; }

    public string UpiId { get; set; } = "";
    public string BankName { get; set; } = "";
    public string AccountHolderName { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string IfscCode { get; set; } = "";
    public string Note { get; set; } = "";

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
