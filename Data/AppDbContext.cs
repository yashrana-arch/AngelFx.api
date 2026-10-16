using Microsoft.EntityFrameworkCore;
using VeltrexAPI.Entities;

namespace VeltrexAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<PaymentSettings> PaymentSettings => Set<PaymentSettings>();
    public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();
    public DbSet<KycDetails> KycDetails => Set<KycDetails>();
    public DbSet<PaymentProof> PaymentProofs => Set<PaymentProof>();
    public DbSet<MarketIndex> MarketIndices => Set<MarketIndex>();
    public DbSet<Stock> Stocks => Set<Stock>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Money and prices: 18 digits, 2 decimals everywhere unless overridden below.
        builder.Properties<decimal>().HavePrecision(18, 2);
        // Store enums as readable text (e.g. "Buy", "Active") instead of ints.
        builder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.ClientCode).HasMaxLength(30);
            e.Property(x => x.Role).HasMaxLength(20);
            e.Property(x => x.PasswordHash).HasMaxLength(100);
            e.HasIndex(x => x.Email).IsUnique();
            // MySQL unique indexes allow multiple NULLs, so clients without a User ID don't clash.
            e.HasIndex(x => x.ClientCode).IsUnique();

            e.HasOne(x => x.Kyc).WithOne(x => x.User).HasForeignKey<KycDetails>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Portfolio).WithOne(x => x.User).HasForeignKey<Portfolio>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Positions).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Trades).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.LedgerEntries).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Withdrawals).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.PaymentProofs).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Portfolio>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.Message).HasMaxLength(500);
        });

        b.Entity<Position>(e =>
        {
            e.Property(x => x.Symbol).HasMaxLength(60);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.HasIndex(x => new { x.UserId, x.SortOrder });
        });

        b.Entity<Trade>(e =>
        {
            e.Property(x => x.Symbol).HasMaxLength(60);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.HasIndex(x => new { x.UserId, x.Status, x.Date });
        });

        b.Entity<LedgerEntry>(e =>
        {
            e.Property(x => x.Description).HasMaxLength(250);
            e.HasIndex(x => new { x.UserId, x.Date });
        });

        b.Entity<PaymentSettings>(e =>
        {
            e.ToTable("PaymentSettings");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.QrImagePath).HasMaxLength(300);
            e.Property(x => x.UpiId).HasMaxLength(100);
            e.Property(x => x.BankName).HasMaxLength(100);
            e.Property(x => x.AccountHolderName).HasMaxLength(150);
            e.Property(x => x.AccountNumber).HasMaxLength(34);
            e.Property(x => x.IfscCode).HasMaxLength(11);
            e.Property(x => x.Note).HasMaxLength(500);
        });

        b.Entity<Withdrawal>(e =>
        {
            e.Property(x => x.AccountDetails).HasMaxLength(300);
            e.Property(x => x.AdminNote).HasMaxLength(300);
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
        });

        b.Entity<KycDetails>(e =>
        {
            e.ToTable("KycDetails");
            e.HasKey(x => x.UserId);
            e.Property(x => x.FirstName).HasMaxLength(75);
            e.Property(x => x.LastName).HasMaxLength(75);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.AddressLine).HasMaxLength(250);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.State).HasMaxLength(100);
            e.Property(x => x.Nationality).HasMaxLength(60);
            e.Property(x => x.BankName).HasMaxLength(100);
            e.Property(x => x.AccountHolderName).HasMaxLength(150);
            e.Property(x => x.AccountNumber).HasMaxLength(20);
            e.Property(x => x.IfscCode).HasMaxLength(11);
            e.Property(x => x.AadhaarNumber).HasMaxLength(12);
            e.Property(x => x.PanNumber).HasMaxLength(10);
            e.Property(x => x.AdminNote).HasMaxLength(300);
            e.HasIndex(x => x.Status);
        });

        b.Entity<PaymentProof>(e =>
        {
            // MEDIUMBLOB holds up to 16 MB; uploads are capped at 5 MB in PaymentProofService.
            e.Property(x => x.ImageData).HasColumnType("mediumblob");
            e.Property(x => x.ContentType).HasMaxLength(50);
            e.Property(x => x.FileName).HasMaxLength(255);
            e.Property(x => x.Reference).HasMaxLength(50);
            e.Property(x => x.AdminNote).HasMaxLength(300);
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.HasIndex(x => new { x.Status, x.CreatedAt });
        });

        b.Entity<MarketIndex>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(60);
            e.Property(x => x.ChangePct).HasPrecision(9, 4);
        });

        b.Entity<Stock>(e =>
        {
            e.Property(x => x.Symbol).HasMaxLength(60);
            e.Property(x => x.ChangePct).HasPrecision(9, 4);
        });
    }
}
