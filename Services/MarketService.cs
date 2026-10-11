using Microsoft.EntityFrameworkCore;
using VeltrexAPI.Data;
using VeltrexAPI.Dtos;
using VeltrexAPI.Entities;

namespace VeltrexAPI.Services;

public class MarketService(AppDbContext db)
{
    public async Task<MarketWatchDto> GetAsync(CancellationToken ct)
    {
        var indices = await db.MarketIndices.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync(ct);
        var stocks = await db.Stocks.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync(ct);
        return new MarketWatchDto(indices.Select(x => x.ToDto()).ToList(), stocks.Select(x => x.ToDto()).ToList());
    }

    /// <summary>Replaces all indices and stocks with the admin's list.</summary>
    public async Task<MarketWatchDto> SaveAsync(MarketWatchDto input, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.MarketIndices.ExecuteDeleteAsync(ct);
        await db.Stocks.ExecuteDeleteAsync(ct);

        db.MarketIndices.AddRange(input.Indices
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select((x, i) => new MarketIndex
            {
                Name = x.Name.Trim(), Value = x.Value, Change = x.Change, ChangePct = x.ChangePct, SortOrder = i,
            }));
        db.Stocks.AddRange(input.Stocks
            .Where(x => !string.IsNullOrWhiteSpace(x.Symbol))
            .Select((x, i) => new Stock
            {
                Symbol = x.Symbol.Trim().ToUpperInvariant(), Price = x.Price, ChangePct = x.ChangePct,
                Volume = x.Volume, Low = x.Low, High = x.High, SortOrder = i,
            }));

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetAsync(ct);
    }
}
