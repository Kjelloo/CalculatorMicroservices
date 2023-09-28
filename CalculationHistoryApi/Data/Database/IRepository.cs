namespace CalculationHistoryApi.Data.Database;

public interface IRepository<T>
{
    T Add(T entity);
    IEnumerable<T> Get();
}