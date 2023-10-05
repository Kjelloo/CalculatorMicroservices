using CalculationHistoryApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CalculationHistoryApi.Data.Database;

public class CalculationHistoryContext : DbContext
{
    public DbSet<Calculation> Calculations { get; set; }

    public CalculationHistoryContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Calculation>()
            .HasKey(c => c.Id);
        base.OnModelCreating(modelBuilder);
    }
}