namespace VeltrexAPI.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }

    /// <summary>Broker-style login ID shown to the client, e.g. AD89898HH.</summary>
    public string? ClientCode { get; set; }

    /// <summary>"admin" or "client" (see <see cref="Roles"/>).</summary>
    public string Role { get; set; } = Roles.Client;

    public bool Verified { get; set; }
    public string PasswordHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Portfolio? Portfolio { get; set; }
    public KycDetails? Kyc { get; set; }
    public List<Position> Positions { get; set; } = [];
    public List<Trade> Trades { get; set; } = [];
    public List<LedgerEntry> LedgerEntries { get; set; } = [];
    public List<Withdrawal> Withdrawals { get; set; } = [];
    public List<PaymentProof> PaymentProofs { get; set; } = [];
}
