namespace VeltrexAPI.Entities;

public class LedgerEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = "";
    public LedgerType Type { get; set; }
    public decimal Amount { get; set; }

    public User? User { get; set; }
}
