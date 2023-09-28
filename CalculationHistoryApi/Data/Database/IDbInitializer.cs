namespace CalculationHistoryApi.Data.Database;

public interface IDbInitializer
{
    void Initialize(CalculationHistoryContext context);
}