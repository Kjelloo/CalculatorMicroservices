namespace SubtractionService;

public class SubtractService
{
    public decimal Subtract(IEnumerable<decimal> operands)
    {
        return operands.Aggregate((total, next) => total - next);
    }
}