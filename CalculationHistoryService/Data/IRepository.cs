namespace CalculationHistoryService.Data;

public interface IRepository<T>
{
    T Add(T entity);
    IEnumerable<T> Get();
}