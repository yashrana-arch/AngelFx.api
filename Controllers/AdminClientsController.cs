using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeltrexAPI.Dtos;
using VeltrexAPI.Entities;
using VeltrexAPI.Services;

namespace VeltrexAPI.Controllers;

/// <summary>Admin management of client accounts and everything they see.</summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public class AdminClientsController(AccountService accounts) : ControllerBase
{
    /* ---------- Clients ---------- */

    [HttpGet]
    public Task<List<UserDto>> List(CancellationToken ct) => accounts.ListClientsAsync(ct);

    [HttpPost]
    public Task<UserDto> Create(NewClientInput input, CancellationToken ct) => accounts.CreateClientAsync(input, ct);

    [HttpGet("{userId:guid}")]
    public async Task<UserDto> Get(Guid userId, CancellationToken ct) => (await accounts.GetClientAsync(userId, ct)).ToDto();

    [HttpPut("{userId:guid}")]
    public Task<UserDto> Update(Guid userId, ClientUpdate input, CancellationToken ct) =>
        accounts.UpdateClientAsync(userId, input, ct);

    /* ---------- Portfolio ---------- */

    [HttpGet("{userId:guid}/portfolio")]
    public async Task<PortfolioDto> GetPortfolio(Guid userId, CancellationToken ct)
    {
        await accounts.GetClientAsync(userId, ct);
        return await accounts.GetPortfolioAsync(userId, ct);
    }

    [HttpPut("{userId:guid}/portfolio")]
    public Task<PortfolioDto> SavePortfolio(Guid userId, PortfolioDto input, CancellationToken ct) =>
        accounts.SavePortfolioAsync(userId, input, ct);

    /* ---------- Trades ---------- */

    [HttpGet("{userId:guid}/trades")]
    public async Task<List<TradeDto>> GetTrades(Guid userId, CancellationToken ct)
    {
        await accounts.GetClientAsync(userId, ct);
        return await accounts.GetTradesAsync(userId, ct);
    }

    [HttpPost("{userId:guid}/trades")]
    public Task<TradeDto> AddTrade(Guid userId, TradeInput input, CancellationToken ct) =>
        accounts.AddTradeAsync(userId, input, ct);

    [HttpPut("{userId:guid}/trades/{tradeId:guid}")]
    public Task<TradeDto> UpdateTrade(Guid userId, Guid tradeId, TradeInput input, CancellationToken ct) =>
        accounts.UpdateTradeAsync(userId, tradeId, input, ct);

    [HttpDelete("{userId:guid}/trades/{tradeId:guid}")]
    public async Task<IActionResult> DeleteTrade(Guid userId, Guid tradeId, CancellationToken ct)
    {
        await accounts.DeleteTradeAsync(userId, tradeId, ct);
        return NoContent();
    }

    /* ---------- Ledger ---------- */

    [HttpGet("{userId:guid}/ledger")]
    public async Task<List<LedgerEntryDto>> GetLedger(Guid userId, CancellationToken ct)
    {
        await accounts.GetClientAsync(userId, ct);
        return await accounts.GetLedgerAsync(userId, ct);
    }

    [HttpPut("{userId:guid}/ledger")]
    public Task<List<LedgerEntryDto>> SaveLedger(Guid userId, List<LedgerEntryDto> entries, CancellationToken ct) =>
        accounts.SaveLedgerAsync(userId, entries, ct);

    /* ---------- Withdrawals ---------- */

    [HttpGet("{userId:guid}/withdrawals")]
    public async Task<List<WithdrawalDto>> GetWithdrawals(Guid userId, CancellationToken ct)
    {
        await accounts.GetClientAsync(userId, ct);
        return await accounts.GetWithdrawalsAsync(userId, ct);
    }

    [HttpPut("{userId:guid}/withdrawals/{withdrawalId:guid}")]
    public Task<WithdrawalDto> DecideWithdrawal(Guid userId, Guid withdrawalId, WithdrawalDecision input, CancellationToken ct) =>
        accounts.DecideWithdrawalAsync(userId, withdrawalId, input, ct);
}
