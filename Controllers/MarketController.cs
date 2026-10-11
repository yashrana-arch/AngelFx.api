using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeltrexAPI.Dtos;
using VeltrexAPI.Entities;
using VeltrexAPI.Services;

namespace VeltrexAPI.Controllers;

[ApiController]
[Authorize]
public class MarketController(MarketService market) : ControllerBase
{
    /// <summary>Indices and stocks shown on every client's watchlist.</summary>
    [HttpGet("api/market")]
    public Task<MarketWatchDto> Get(CancellationToken ct) => market.GetAsync(ct);

    /// <summary>Replace the whole market watch (admin only).</summary>
    [HttpPut("api/admin/market")]
    [Authorize(Roles = Roles.Admin)]
    public Task<MarketWatchDto> Save(MarketWatchDto input, CancellationToken ct) => market.SaveAsync(input, ct);
}
