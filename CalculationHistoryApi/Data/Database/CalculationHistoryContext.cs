using CalculationHistoryService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CalculationHistoryApi.Data.Database;

public class CalculationHistoryContext : DbContext
{
    public DbSet<Calculation> Calculations { get; set; }

    public CalculationHistoryContext(DbContextOptions options) : base(options) { }
}