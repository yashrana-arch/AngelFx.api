using System.ComponentModel.DataAnnotations;
using VeltrexAPI.Entities;

namespace VeltrexAPI.Dtos;

// Shapes mirror the frontend's src/types.ts. JSON is camelCase; enums are UPPER_CASE strings.

/* ---------- Auth ---------- */

public record LoginRequest(
    [Required] string Username,
    [Required] string Password);

/// <summary><c>KycStatus</c> is null until the client submits their KYC details.</summary>
public record UserDto(Guid Id, string Name, string Email, string? Phone, string Role, string? ClientCode, bool Verified, KycStatus? KycStatus);

public record LoginResponse(string Token, UserDto User);

/// <summary>Self sign-up. Creates an unverified client; the admin can assign a User ID later.</summary>
public record RegisterRequest(
    [Required, MaxLength(150)] string Name,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [MaxLength(20)] string? Phone,
    [Required, MinLength(6)] string Password);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(6)] string NewPassword);

/* ---------- Market watch ---------- */

public record MarketIndexDto(string? Id, [Required, MaxLength(60)] string Name, decimal Value, decimal Change, decimal ChangePct);

public record StockDto(string? Id, [Required, MaxLength(60)] string Symbol, decimal Price, decimal ChangePct, long Volume, decimal Low, decimal High);

public record MarketWatchDto([Required] List<MarketIndexDto> Indices, [Required] List<StockDto> Stocks);

/* ---------- Portfolio ---------- */

public record PositionDto(
    string? Id,
    [Required, MaxLength(60)] string Symbol,
    TradeSide Side,
    decimal Quantity,
    decimal AvgPrice,
    decimal Ltp,
    decimal Pnl);

public record PortfolioDto(
    decimal LedgerBalance,
    decimal MarginAvailable,
    decimal MarginUsed,
    decimal M2m,
    decimal TodayPnl,
    [MaxLength(500)] string? Message,
    [Required] List<PositionDto> Positions);

/* ---------- Trades ---------- */

public record TradeDto(
    Guid Id, string Symbol, TradeSide Side, OrderType OrderType, decimal Quantity,
    decimal EntryPrice, decimal? ExitPrice, decimal Pnl, TradeStatus Status, DateTimeOffset Date);

public record TradeInput(
    [Required, MaxLength(60)] string Symbol,
    TradeSide Side,
    OrderType OrderType,
    [Range(0, double.MaxValue)] decimal Quantity,
    decimal EntryPrice,
    decimal? ExitPrice,
    decimal Pnl,
    TradeStatus Status,
    DateTimeOffset Date);

/* ---------- Ledger ---------- */

public record LedgerEntryDto(
    string? Id,
    DateOnly Date,
    [MaxLength(250)] string? Description,
    LedgerType Type,
    [Range(0, double.MaxValue)] decimal Amount);

/* ---------- Payment (deposit details, common for all clients) ---------- */

public record PaymentInfoDto(
    string? QrImageUrl, string UpiId, string BankName, string AccountHolderName,
    string AccountNumber, string IfscCode, string Note);

public class PaymentUpdateForm
{
    [MaxLength(100)] public string? UpiId { get; set; }
    [MaxLength(100)] public string? BankName { get; set; }
    [MaxLength(150)] public string? AccountHolderName { get; set; }
    [MaxLength(34), RegularExpression(@"^\s*[0-9A-Za-z]*\s*$", ErrorMessage = "Account number can contain only letters and digits")]
    public string? AccountNumber { get; set; }
    [MaxLength(11), RegularExpression(@"^\s*([A-Za-z]{4}0[A-Za-z0-9]{6})?\s*$", ErrorMessage = "Enter a valid 11-character IFSC code (e.g. HDFC0001234)")]
    public string? IfscCode { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    public IFormFile? QrImage { get; set; }
}

/* ---------- Withdrawals ---------- */

public record WithdrawalDto(Guid Id, decimal Amount, string AccountDetails, WithdrawalStatus Status, string AdminNote, DateTimeOffset CreatedAt);

public record WithdrawalInput(
    [Range(0.01, double.MaxValue, ErrorMessage = "Enter a valid amount")] decimal Amount,
    [Required, MaxLength(300)] string AccountDetails);

public record WithdrawalDecision(WithdrawalStatus Status, [MaxLength(300)] string? AdminNote);

/* ---------- Payment proofs (deposit screenshots) ---------- */

/// <summary>Metadata only; the image is fetched separately from the <c>.../image</c> endpoint.</summary>
public record PaymentProofDto(
    Guid Id, Guid UserId, string ClientName, string? ClientCode,
    decimal? Amount, string Reference, string FileName, int FileSize,
    PaymentProofStatus Status, string AdminNote, DateTimeOffset CreatedAt, DateTimeOffset? ReviewedAt);

public class PaymentProofUploadForm
{
    [Required(ErrorMessage = "Please choose a screenshot to upload")] public IFormFile? Image { get; set; }
    [Range(0.01, 1_000_000_000, ErrorMessage = "Enter a valid amount")] public decimal? Amount { get; set; }
    [MaxLength(50)] public string? Reference { get; set; }
}

public record PaymentProofDecision(PaymentProofStatus Status, [MaxLength(300)] string? AdminNote);

/* ---------- KYC ---------- */

public record KycDto(
    string FirstName, string LastName, string Email, string Phone, DateOnly DateOfBirth,
    string AddressLine, string City, string State, string Nationality,
    string BankName, string AccountHolderName, string AccountNumber, string IfscCode,
    string AadhaarNumber, string PanNumber,
    KycStatus Status, string AdminNote, DateTimeOffset SubmittedAt, DateTimeOffset? ReviewedAt);

/// <summary>The KYC form. Number formats (Aadhaar, PAN, IFSC, ...) are checked in KycService after spaces are stripped.</summary>
public record KycInput(
    [Required, MaxLength(75)] string FirstName,
    [Required, MaxLength(75)] string LastName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(20)] string Phone,
    DateOnly DateOfBirth,
    [Required, MaxLength(250)] string AddressLine,
    [Required, MaxLength(100)] string City,
    [Required, MaxLength(100)] string State,
    [Required, MaxLength(60)] string Nationality,
    [Required, MaxLength(100)] string BankName,
    [Required, MaxLength(150)] string AccountHolderName,
    [Required, MaxLength(30)] string AccountNumber,
    [Required, MaxLength(15)] string IfscCode,
    [Required, MaxLength(20)] string AadhaarNumber,
    [Required, MaxLength(15)] string PanNumber);

public record KycDecision(KycStatus Status, [MaxLength(300)] string? AdminNote);

/* ---------- Admin: clients ---------- */

public record NewClientInput(
    [Required, MaxLength(150)] string Name,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [MaxLength(20)] string? Phone,
    [MaxLength(30)] string? ClientCode,
    [Required, MinLength(6)] string Password);

public record ClientUpdate(
    [Required, MaxLength(150)] string Name,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [MaxLength(20)] string? Phone,
    [MaxLength(30)] string? ClientCode,
    bool Verified,
    [MinLength(6)] string? Password);
