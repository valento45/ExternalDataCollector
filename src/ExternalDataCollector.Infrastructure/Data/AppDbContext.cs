using ExternalDataCollector.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExternalDataCollector.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<ExchangeRate>();

        e.ToTable("exchange_rates");
        e.HasKey(x => x.Id);

        e.Property(x => x.BaseCurrency).HasMaxLength(8).IsRequired();
        e.Property(x => x.QuoteCurrency).HasMaxLength(8).IsRequired();
        e.Property(x => x.Rate).HasPrecision(18, 8).IsRequired();

        e.Property(x => x.AsOfDate)
            .HasConversion(
                v => v.ToString("yyyy-MM-dd"),
                v => DateOnly.Parse(v))
            .HasColumnType("TEXT")
            .IsRequired();

        e.Property(x => x.RetrievedAt).IsRequired();

        e.HasIndex(x => new { x.BaseCurrency, x.QuoteCurrency, x.AsOfDate })
            .IsUnique();
    }
}
