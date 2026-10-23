using Microsoft.EntityFrameworkCore;
using VeltrexAPI.Data;
using VeltrexAPI.Dtos;
using VeltrexAPI.Entities;
using VeltrexAPI.Infrastructure;

namespace VeltrexAPI.Services;

/// <summary>
/// Reads and writes a client's data. Client endpoints call it with their own id (from the token);
/// admin endpoints call it with the id from the URL.
/// </summary>
public class AccountService(AppDbContext db)
{
    /* ---------- Users ---------- */

    public async Task<User> GetClientAsync(Guid userId, CancellationToken ct) =>
        await db.Users.Include(u => u.Kyc).FirstOrDefaultAsync(u => u.Id == userId && u.Role == Roles.Client, ct)
        ?? throw ApiException.NotFound("Client not found");

    public async Task<List<UserDto>> ListClientsAsync(CancellationToken ct) =>
        await db.Users.AsNoTracking()
            .Where(u => u.Role == Roles.Client)
            .OrderBy(u => u.Name)
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Phone, u.Role, u.ClientCode, u.Verified,
                u.Kyc != null ? u.Kyc.Status : null, u.Disabled))
            .ToListAsync(ct);

    public async Task<UserDto> CreateClientAsync(NewClientInput input, CancellationToken ct) =>
        (await AddClientAsync(input.Name, input.Email, input.Phone, input.ClientCode, input.Password, ct)).ToDto();

    /// <summary>Self sign-up: an unverified client without a User ID (the admin can assign one later).
    /// The email / phone of a disabled account can't be reused.</summary>
    public async Task<User> RegisterClientAsync(RegisterRequest input, CancellationToken ct)
    {
        var email = input.Email.Trim();
        var phone = Blank(input.Phone);
        if (await db.Users.AnyAsync(u => u.Disabled && (u.Email == email || (phone != null && u.Phone == phone)), ct))
            throw ApiException.Conflict("This email or phone number can't be used");

        return await AddClientAsync(input.Name, email, phone, null, input.Password, ct);
    }

    private async Task<User> AddClientAsync(string name, string email, string? phone, string? clientCode, string password, CancellationToken ct)
    {
        email = email.Trim();
        var code = NormalizeCode(clientCode);
        await EnsureUniqueAsync(email, code, null, ct);

        var user = new User
        {
            Name = name.Trim(),
            Email = email,
            Phone = Blank(phone),
            ClientCode = code,
            Role = Roles.Client,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<UserDto> UpdateClientAsync(Guid userId, ClientUpdate input, CancellationToken ct)
    {
        var user = await GetClientAsync(userId, ct);
        var email = input.Email.Trim();
        var code = NormalizeCode(input.ClientCode);
        await EnsureUniqueAsync(email, code, userId, ct);

        user.Name = input.Name.Trim();
        user.Email = email;
        user.Phone = Blank(input.Phone);
        user.ClientCode = code;
        user.Verified = input.Verified;
        if (!string.IsNullOrEmpty(input.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password);

        await db.SaveChangesAsync(ct);
        return user.ToDto();
    }

    /// <summary>Disable / re-enable a client. A disabled client can't log in and their current session stops working.</summary>
    public async Task<UserDto> SetClientDisabledAsync(Guid userId, bool disabled, CancellationToken ct)
    {
        var user = await GetClientAsync(userId, ct);
        user.Disabled = disabled;
        await db.SaveChangesAsync(ct);
        return user.ToDto();
    }

    private async Task EnsureUniqueAsync(string email, string? code, Guid? exceptId, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Email == email && u.Id != exceptId, ct))
            throw ApiException.Conflict("A user with this email already exists");
        if (code != null && await db.Users.AnyAsync(u => u.ClientCode == code && u.Id != exceptId, ct))
            throw ApiException.Conflict("This User ID is already taken");
    }

    /* ---------- Portfolio ---------- */

    // Figures are derived, not stored, so the admin only manages funds and trades:
    //   Ledger balance   = ledger entries (funds added, adjustments) + closed-trade P&L − approved withdrawals
    //   M2M              = P&L of active trades
    //   Margin used      = entry value (price × qty) of active and pending trades
    //   Margin available = ledger balance − margin used − pending withdrawals + M2M losses
    //                      (unrealised profit only counts once the trade is closed)
    //   Today P&L        = M2M + P&L of closed trades dated today (IST)
    // Only the announcement is read from the Portfolios table.

    private static readonly TimeSpan Ist = TimeSpan.FromMinutes(330);
    private static DateOnly IstDate(DateTimeOffset d) => DateOnly.FromDateTime(d.ToOffset(Ist).DateTime);

    private record Account(List<LedgerEntryDto> Ledger, List<Trade> Trades, List<Withdrawal> Withdrawals);

    private async Task<Account> LoadAccountAsync(Guid userId, CancellationToken ct)
    {
        var manual = await db.LedgerEntries.AsNoTracking().Where(l => l.UserId == userId).ToListAsync(ct);
        var trades = await db.Trades.AsNoTracking().Where(t => t.UserId == userId).ToListAsync(ct);
        var withdrawals = await db.Withdrawals.AsNoTracking().Where(w => w.UserId == userId).ToListAsync(ct);

        var ledger = manual.Select(l => l.ToDto())
            .Concat(withdrawals
                .Where(w => w.Status == WithdrawalStatus.Approved)
                .Select(w => new LedgerEntryDto($"wd_{w.Id}", IstDate(w.CreatedAt), "Withdrawal", LedgerType.Debit, w.Amount)))
            .Concat(trades
                .Where(t => t.Status == TradeStatus.Closed && t.Pnl != 0)
                .Select(t => new LedgerEntryDto(
                    $"pnl_{t.Id}", IstDate(t.Date),
                    $"{(t.Pnl > 0 ? "Profit" : "Loss")} on {t.Side.ToString().ToUpperInvariant()} {t.Symbol}",
                    t.Pnl > 0 ? LedgerType.Credit : LedgerType.Debit, Math.Abs(t.Pnl))))
            .OrderBy(l => l.Date)
            .ToList();

        return new Account(ledger, trades, withdrawals);
    }

    private static PortfolioDto Summarize(Account a, string message)
    {
        var active = a.Trades.Where(t => t.Status == TradeStatus.Active).OrderByDescending(t => t.Date).ToList();
        var ledgerBalance = a.Ledger.Sum(l => l.Type == LedgerType.Credit ? l.Amount : -l.Amount);
        var m2m = active.Sum(t => t.Pnl);
        var marginUsed = a.Trades.Where(t => t.Status != TradeStatus.Closed).Sum(t => t.EntryPrice * t.Quantity);
        var held = a.Withdrawals.Where(w => w.Status == WithdrawalStatus.Pending).Sum(w => w.Amount);
        var today = IstDate(DateTimeOffset.UtcNow);
        var closedToday = a.Trades.Where(t => t.Status == TradeStatus.Closed && IstDate(t.Date) == today).Sum(t => t.Pnl);

        var positions = active.Select(t =>
        {
            var perUnit = t.Quantity == 0 ? 0 : t.Pnl / t.Quantity;
            var ltp = t.Side == TradeSide.Buy ? t.EntryPrice + perUnit : t.EntryPrice - perUnit;
            return new PositionDto(t.Id.ToString(), t.Symbol, t.Side, t.Quantity, t.EntryPrice, Math.Round(ltp, 2), t.Pnl);
        }).ToList();

        return new PortfolioDto(
            ledgerBalance,
            ledgerBalance - marginUsed - held + Math.Min(m2m, 0),
            marginUsed, m2m, m2m + closedToday, message, positions);
    }

    public async Task<PortfolioDto> GetPortfolioAsync(Guid userId, CancellationToken ct)
    {
        var message = await db.Portfolios.AsNoTracking().Where(p => p.UserId == userId).Select(p => p.Message).FirstOrDefaultAsync(ct);
        return Summarize(await LoadAccountAsync(userId, ct), message ?? "");
    }

    /// <summary>Only the announcement is editable; every figure is calculated (see above).</summary>
    public async Task<PortfolioDto> SavePortfolioAsync(Guid userId, PortfolioDto input, CancellationToken ct)
    {
        await GetClientAsync(userId, ct);
        var p = await db.Portfolios.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (p == null) db.Portfolios.Add(p = new Portfolio { UserId = userId });
        p.Message = input.Message?.Trim() ?? "";
        await db.SaveChangesAsync(ct);
        return await GetPortfolioAsync(userId, ct);
    }

    private async Task<decimal> MarginAvailableAsync(Guid userId, CancellationToken ct) =>
        Summarize(await LoadAccountAsync(userId, ct), "").MarginAvailable;

    /* ---------- Trades ---------- */

    public async Task<List<TradeDto>> GetTradesAsync(Guid userId, CancellationToken ct) =>
        (await db.Trades.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct))
        .Select(t => t.ToDto()).ToList();

    public async Task<TradeDto> AddTradeAsync(Guid userId, TradeInput input, CancellationToken ct)
    {
        await GetClientAsync(userId, ct);
        var trade = new Trade { UserId = userId };
        trade.Apply(input);
        db.Trades.Add(trade);
        await db.SaveChangesAsync(ct);
        return trade.ToDto();
    }

    public async Task<TradeDto> UpdateTradeAsync(Guid userId, Guid tradeId, TradeInput input, CancellationToken ct)
    {
        var trade = await db.Trades.FirstOrDefaultAsync(t => t.Id == tradeId && t.UserId == userId, ct)
            ?? throw ApiException.NotFound("Trade not found");
        trade.Apply(input);
        await db.SaveChangesAsync(ct);
        return trade.ToDto();
    }

    public async Task DeleteTradeAsync(Guid userId, Guid tradeId, CancellationToken ct)
    {
        var deleted = await db.Trades.Where(t => t.Id == tradeId && t.UserId == userId).ExecuteDeleteAsync(ct);
        if (deleted == 0) throw ApiException.NotFound("Trade not found");
    }

    /* ---------- Ledger ---------- */

    /// <summary>What the client sees: manual entries plus withdrawals and closed-trade P&L.</summary>
    public async Task<List<LedgerEntryDto>> GetFullLedgerAsync(Guid userId, CancellationToken ct) =>
        (await LoadAccountAsync(userId, ct)).Ledger;

    /// <summary>Manual entries only (funds added, adjustments): what the admin edits.</summary>
    public async Task<List<LedgerEntryDto>> GetLedgerAsync(Guid userId, CancellationToken ct) =>
        (await db.LedgerEntries.AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.Date)
            .ToListAsync(ct))
        .Select(l => l.ToDto()).ToList();

    /// <summary>Admin credits the client's account, e.g. after confirming their payment.</summary>
    public async Task<LedgerEntryDto> AddFundsAsync(Guid userId, AddFundsInput input, CancellationToken ct)
    {
        await GetClientAsync(userId, ct);
        var entry = new LedgerEntry
        {
            UserId = userId,
            Date = IstDate(DateTimeOffset.UtcNow),
            Description = string.IsNullOrWhiteSpace(input.Description) ? "Funds added" : input.Description.Trim(),
            Type = LedgerType.Credit,
            Amount = input.Amount,
        };
        db.LedgerEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.ToDto();
    }

    public async Task<List<LedgerEntryDto>> SaveLedgerAsync(Guid userId, List<LedgerEntryDto> entries, CancellationToken ct)
    {
        await GetClientAsync(userId, ct);

        // The ledger is edited as a whole list: replace it in one transaction.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.LedgerEntries.Where(l => l.UserId == userId).ExecuteDeleteAsync(ct);
        db.LedgerEntries.AddRange(entries
            .Where(e => e.Amount > 0)
            .Select(e => new LedgerEntry
            {
                UserId = userId,
                Date = e.Date,
                Description = e.Description?.Trim() ?? "",
                Type = e.Type,
                Amount = e.Amount,
            }));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetLedgerAsync(userId, ct);
    }

    /* ---------- Withdrawals ---------- */

    public async Task<List<WithdrawalDto>> GetWithdrawalsAsync(Guid userId, CancellationToken ct) =>
        (await db.Withdrawals.AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct))
        .Select(w => w.ToDto()).ToList();

    public async Task<WithdrawalDto> RequestWithdrawalAsync(Guid userId, WithdrawalInput input, CancellationToken ct)
    {
        // Available margin already holds back pending requests.
        if (input.Amount > await MarginAvailableAsync(userId, ct))
            throw ApiException.BadRequest("Amount exceeds available margin");

        var w = new Withdrawal { UserId = userId, Amount = input.Amount, AccountDetails = input.AccountDetails.Trim() };
        db.Withdrawals.Add(w);
        await db.SaveChangesAsync(ct);
        return w.ToDto();
    }

    public async Task<WithdrawalDto> DecideWithdrawalAsync(Guid userId, Guid withdrawalId, WithdrawalDecision input, CancellationToken ct)
    {
        var w = await db.Withdrawals.FirstOrDefaultAsync(x => x.Id == withdrawalId && x.UserId == userId, ct)
            ?? throw ApiException.NotFound("Request not found");

        // A pending request is already held back from available margin; a rejected one would be newly deducted.
        if (input.Status == WithdrawalStatus.Approved && w.Status != WithdrawalStatus.Approved)
        {
            var hold = w.Status == WithdrawalStatus.Pending ? 0 : w.Amount;
            if (await MarginAvailableAsync(userId, ct) - hold < 0)
                throw ApiException.BadRequest("The client's available margin is too low to approve this withdrawal");
        }

        w.Status = input.Status;
        w.AdminNote = input.AdminNote?.Trim() ?? "";
        await db.SaveChangesAsync(ct);
        return w.ToDto();
    }

    /* ---------- helpers ---------- */

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string? NormalizeCode(string? s) => Blank(s)?.ToUpperInvariant();
}
