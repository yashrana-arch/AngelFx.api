namespace VeltrexAPI.Entities;

// Market watch data is shared by all clients.

public class MarketIndex
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Value { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePct { get; set; }
    public int SortOrder { get; set; }
}

public class Stock
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = "";
    public decimal Price { get; set; }
    public decimal ChangePct { get; set; }
    public long Volume { get; set; }
    public decimal Low { get; set; }
    public decimal High { get; set; }
    public int SortOrder { get; set; }
}
