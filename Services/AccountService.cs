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
                u.Kyc != null ? u.Kyc.Status : null))
            .ToListAsync(ct);

    public async Task<UserDto> CreateClientAsync(NewClientInput input, CancellationToken ct) =>
        (await AddClientAsync(input.Name, input.Email, input.Phone, input.ClientCode, input.Password, ct)).ToDto();

    /// <summary>Self sign-up: an unverified client without a User ID (the admin can assign one later).</summary>
    public Task<User> RegisterClientAsync(RegisterRequest input, CancellationToken ct) =>
        AddClientAsync(input.Name, input.Email, input.Phone, null, input.Password, ct);

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

    private async Task EnsureUniqueAsync(string email, string? code, Guid? exceptId, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Email == email && u.Id != exceptId, ct))
            throw ApiException.Conflict("A user with this email already exists");
        if (code != null && await db.Users.AnyAsync(u => u.ClientCode == code && u.Id != exceptId, ct))
            throw ApiException.Conflict("This User ID is already taken");
    }

    /* ---------- Portfolio ---------- */

    public async Task<PortfolioDto> GetPortfolioAsync(Guid userId, CancellationToken ct)
    {
        var p = await db.Portfolios.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        var positions = await db.Positions.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);

        return new PortfolioDto(
            p?.LedgerBalance ?? 0, p?.MarginAvailable ?? 0, p?.MarginUsed ?? 0,
            p?.M2M ?? 0, p?.TodayPnl ?? 0, p?.Message ?? "",
            positions.Select(x => x.ToDto()).ToList());
    }

    public async Task<PortfolioDto> SavePortfolioAsync(Guid userId, PortfolioDto input, CancellationToken ct)
    {
        await GetClientAsync(userId, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var p = await db.Portfolios.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (p == null) db.Portfolios.Add(p = new Portfolio { UserId = userId });
        p.LedgerBalance = input.LedgerBalance;
        p.MarginAvailable = input.MarginAvailable;
        p.MarginUsed = input.MarginUsed;
        p.M2M = input.M2m;
        p.TodayPnl = input.TodayPnl;
        p.Message = input.Message?.Trim() ?? "";

        // Positions are edited as a whole list: replace them.
        await db.Positions.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        db.Positions.AddRange(input.Positions
            .Where(x => !string.IsNullOrWhiteSpace(x.Symbol))
            .Select((x, i) => new Position
            {
                UserId = userId,
                Symbol = x.Symbol.Trim().ToUpperInvariant(),
                Side = x.Side,
                Quantity = x.Quantity,
                AvgPrice = x.AvgPrice,
                Ltp = x.Ltp,
                Pnl = x.Pnl,
                SortOrder = i,
            }));

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetPortfolioAsync(userId, ct);
    }

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

    public async Task<List<LedgerEntryDto>> GetLedgerAsync(Guid userId, CancellationToken ct) =>
        (await db.LedgerEntries.AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.Date)
            .ToListAsync(ct))
        .Select(l => l.ToDto()).ToList();

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
        var available = await db.Portfolios.Where(p => p.UserId == userId).Select(p => p.MarginAvailable).FirstOrDefaultAsync(ct);
        var pending = await db.Withdrawals
            .Where(w => w.UserId == userId && w.Status == WithdrawalStatus.Pending)
            .SumAsync(w => (decimal?)w.Amount, ct) ?? 0;

        if (input.Amount > available - pending)
            throw ApiException.BadRequest(pending > 0
                ? "Amount exceeds available margin (including your pending requests)"
                : "Amount exceeds available margin");

        var w = new Withdrawal { UserId = userId, Amount = input.Amount, AccountDetails = input.AccountDetails.Trim() };
        db.Withdrawals.Add(w);
        await db.SaveChangesAsync(ct);
        return w.ToDto();
    }

    public async Task<WithdrawalDto> DecideWithdrawalAsync(Guid userId, Guid withdrawalId, WithdrawalDecision input, CancellationToken ct)
    {
        var w = await db.Withdrawals.FirstOrDefaultAsync(x => x.Id == withdrawalId && x.UserId == userId, ct)
            ?? throw ApiException.NotFound("Request not found");
        w.Status = input.Status;
        w.AdminNote = input.AdminNote?.Trim() ?? "";
        await db.SaveChangesAsync(ct);
        return w.ToDto();
    }

    /* ---------- helpers ---------- */

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string? NormalizeCode(string? s) => Blank(s)?.ToUpperInvariant();
}
