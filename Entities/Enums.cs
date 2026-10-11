namespace VeltrexAPI.Entities;

// Serialized to JSON as UPPER_CASE strings (BUY, ACTIVE, CREDIT, ...) to match the frontend.

public enum TradeSide { Buy, Sell }

public enum OrderType { Market, Limit }

public enum TradeStatus { Pending, Active, Closed }

public enum LedgerType { Credit, Debit }

public enum WithdrawalStatus { Pending, Approved, Rejected }

public static class Roles
{
    public const string Admin = "admin";
    public const string Client = "client";
}
