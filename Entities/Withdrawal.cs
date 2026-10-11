namespace VeltrexAPI.Entities;

public class Withdrawal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string AccountDetails { get; set; } = "";
    public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;
    public string AdminNote { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
}
