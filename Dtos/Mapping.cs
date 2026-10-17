using VeltrexAPI.Entities;

namespace VeltrexAPI.Dtos;

public static class Mapping
{
    public static UserDto ToDto(this User u) =>
        new(u.Id, u.Name, u.Email, u.Phone, u.Role, u.ClientCode, u.Verified, u.Kyc?.Status);

    public static SupportMessageDto ToDto(this SupportMessage m) =>
        new(m.Id, m.FromAdmin, m.Body, m.CreatedAt, m.ReadAt);

    public static KycDto ToDto(this KycDetails k) =>
        new(k.FirstName, k.LastName, k.Email, k.Phone, k.DateOfBirth,
            k.AddressLine, k.City, k.State, k.Nationality,
            k.BankName, k.AccountHolderName, k.AccountNumber, k.IfscCode,
            k.AadhaarNumber, k.PanNumber,
            k.Status, k.AdminNote, k.SubmittedAt, k.ReviewedAt);

    public static TradeDto ToDto(this Trade t) =>
        new(t.Id, t.Symbol, t.Side, t.OrderType, t.Quantity, t.EntryPrice, t.ExitPrice, t.Pnl, t.Status, t.Date);

    public static WithdrawalDto ToDto(this Withdrawal w) =>
        new(w.Id, w.Amount, w.AccountDetails, w.Status, w.AdminNote, w.CreatedAt);

    public static PositionDto ToDto(this Position p) =>
        new(p.Id.ToString(), p.Symbol, p.Side, p.Quantity, p.AvgPrice, p.Ltp, p.Pnl);

    public static LedgerEntryDto ToDto(this LedgerEntry l) =>
        new(l.Id.ToString(), l.Date, l.Description, l.Type, l.Amount);

    public static MarketIndexDto ToDto(this MarketIndex i) =>
        new(i.Id.ToString(), i.Name, i.Value, i.Change, i.ChangePct);

    public static StockDto ToDto(this Stock s) =>
        new(s.Id.ToString(), s.Symbol, s.Price, s.ChangePct, s.Volume, s.Low, s.High);

    public static void Apply(this Trade t, TradeInput i)
    {
        t.Symbol = i.Symbol.Trim().ToUpperInvariant();
        t.Side = i.Side;
        t.OrderType = i.OrderType;
        t.Quantity = i.Quantity;
        t.EntryPrice = i.EntryPrice;
        t.ExitPrice = i.ExitPrice;
        t.Pnl = i.Pnl;
        t.Status = i.Status;
        t.Date = i.Date;
    }
}
