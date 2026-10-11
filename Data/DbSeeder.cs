using Microsoft.EntityFrameworkCore;
using VeltrexAPI.Entities;

namespace VeltrexAPI.Data;

/// <summary>Creates the first admin account and sample market data on an empty database.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        if (!await db.Users.AnyAsync(u => u.Role == Roles.Admin))
        {
            var email = config["Seed:AdminEmail"] ?? "admin@veltrex.com";
            var password = config["Seed:AdminPassword"] ?? "admin123";
            db.Users.Add(new User
            {
                Name = "Admin",
                Email = email,
                Role = Roles.Admin,
                Verified = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            });
            logger.LogWarning("Seeded admin account {Email}. Change its password before going live.", email);
        }

        if (!await db.MarketIndices.AnyAsync() && !await db.Stocks.AnyAsync())
        {
            db.MarketIndices.AddRange(
                new MarketIndex { Name = "Nifty 50", Value = 22520.45m, Change = 288.65m, ChangePct = 1.30m, SortOrder = 0 },
                new MarketIndex { Name = "Bank Nifty", Value = 55256.65m, Change = 741.60m, ChangePct = 1.36m, SortOrder = 1 },
                new MarketIndex { Name = "Sensex", Value = 72472.33m, Change = 879.09m, ChangePct = 1.23m, SortOrder = 2 });

            db.Stocks.AddRange(
                new Stock { Symbol = "DRREDDY", Price = 1197.00m, ChangePct = 1.4407m, Volume = 1510336, Low = 1172.00m, High = 1200.80m, SortOrder = 0 },
                new Stock { Symbol = "EICHERMOT", Price = 7050.00m, ChangePct = 4.1744m, Volume = 551231, Low = 6801.00m, High = 7078.00m, SortOrder = 1 },
                new Stock { Symbol = "GRASIM", Price = 2890.00m, ChangePct = 1.1621m, Volume = 222643, Low = 2857.80m, High = 2894.90m, SortOrder = 2 },
                new Stock { Symbol = "HCLTECH", Price = 1216.70m, ChangePct = 3.3818m, Volume = 2934650, Low = 1175.00m, High = 1221.00m, SortOrder = 3 },
                new Stock { Symbol = "HDFCBANK", Price = 707.25m, ChangePct = 2.1668m, Volume = 18397972, Low = 695.30m, High = 710.15m, SortOrder = 4 });
        }

        await db.SaveChangesAsync();
    }
}
