using CalculationHistoryService.Data.Models;

namespace CalculationHistoryApi.Data.Database;

public class CalculationRepository : IRepository<Calculation>
{
    private readonly CalculationHistoryContext _context;

    public CalculationRepository(CalculationHistoryContext context)
    {
        _context = context;
    }

    public Calculation Add(Calculation entity)
    {
        var newCalculation = _context.Calculations.Add(entity).Entity; 
        _context.SaveChanges();
        return newCalculation;
    }

    public IEnumerable<Calculation> Get()
    {
        return _context.Calculations;
    }
}