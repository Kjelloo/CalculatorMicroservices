namespace CalculationHistoryApi.Data.Database;

public class DbInitializer : IDbInitializer
{
    public void Initialize(CalculationHistoryContext context)
    {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }
}