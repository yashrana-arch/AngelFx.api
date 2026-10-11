namespace VeltrexAPI.Entities;

/// <summary>Account summary for one client (1:1 with <see cref="User"/>).</summary>
public class Portfolio
{
    public Guid UserId { get; set; }
    public decimal LedgerBalance { get; set; }
    public decimal MarginAvailable { get; set; }
    public decimal MarginUsed { get; set; }
    public decimal M2M { get; set; }
    public decimal TodayPnl { get; set; }
    public string Message { get; set; } = "";

    public User? User { get; set; }
}

/// <summary>An open net position shown on the client's Portfolio page.</summary>
public class Position
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = "";
    public TradeSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal AvgPrice { get; set; }
    public decimal Ltp { get; set; }
    public decimal Pnl { get; set; }
    public int SortOrder { get; set; }

    public User? User { get; set; }
}
