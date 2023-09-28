using Serilog;

namespace SubtractionService;

public class SubtractService
{
    public decimal Subtract(IEnumerable<decimal> operands)
    {
        var result = operands.Aggregate((total, next) => total - next);
        Log.Logger.Debug("Finished subtraction with result {Result}", result);
        return result;
    }
}