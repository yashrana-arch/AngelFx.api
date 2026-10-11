namespace VeltrexAPI.Entities;

public class Trade
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = "";
    public TradeSide Side { get; set; }
    public OrderType OrderType { get; set; }
    public decimal Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal? ExitPrice { get; set; }
    public decimal Pnl { get; set; }
    public TradeStatus Status { get; set; }
    public DateTimeOffset Date { get; set; }

    public User? User { get; set; }
}
