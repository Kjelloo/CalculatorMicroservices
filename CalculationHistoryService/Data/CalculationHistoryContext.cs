using CalculationHistoryService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CalculationHistoryService.Data;

public class CalculationHistoryContext : DbContext
{
    public DbSet<Calculation> Calculations { get; set; }

    public CalculationHistoryContext(DbContextOptions options) : base(options) { }
}