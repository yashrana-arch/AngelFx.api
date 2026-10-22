using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeltrexAPI.Data;
using VeltrexAPI.Dtos;
using VeltrexAPI.Entities;
using VeltrexAPI.Infrastructure;
using VeltrexAPI.Services;

namespace VeltrexAPI.Controllers;

/// <summary>Endpoints for the logged-in client. Every call is scoped to the user in the token.</summary>
[ApiController]
[Route("api/client")]
[Authorize(Roles = Roles.Client)]
public class ClientController(AccountService accounts, KycService kyc, AppDbContext db) : ControllerBase
{
    private Guid Me => User.GetUserId();

    /// <summary>The client's KYC details, or 204 No Content if they haven't filled them in yet.</summary>
    [HttpGet("kyc")]
    public Task<KycDto?> Kyc(CancellationToken ct) => kyc.GetAsync(Me, ct);

    /// <summary>Submit or update KYC details. Not allowed once approved.</summary>
    [HttpPut("kyc")]
    public Task<KycDto> SubmitKyc(KycInput input, CancellationToken ct) => kyc.SubmitAsync(Me, input, ct);

    [HttpGet("portfolio")]
    public Task<PortfolioDto> Portfolio(CancellationToken ct) => accounts.GetPortfolioAsync(Me, ct);

    [HttpGet("trades")]
    public Task<List<TradeDto>> Trades(CancellationToken ct) => accounts.GetTradesAsync(Me, ct);

    [HttpGet("ledger")]
    public Task<List<LedgerEntryDto>> Ledger(CancellationToken ct) => accounts.GetFullLedgerAsync(Me, ct);

    [HttpGet("withdrawals")]
    public Task<List<WithdrawalDto>> Withdrawals(CancellationToken ct) => accounts.GetWithdrawalsAsync(Me, ct);

    [HttpPost("withdrawals")]
    public Task<WithdrawalDto> RequestWithdrawal(WithdrawalInput input, CancellationToken ct) =>
        accounts.RequestWithdrawalAsync(Me, input, ct);

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([Me], ct) ?? throw ApiException.NotFound("User not found");
        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            throw ApiException.BadRequest("Current password is incorrect");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
