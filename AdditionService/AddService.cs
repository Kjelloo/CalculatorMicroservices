using Serilog;

namespace AdditionService;

public class AddService
{
    public decimal Addition(IEnumerable<decimal> operands)
    {
        var result = operands.Sum();
        Log.Logger.Debug("Finished addition with result {Result}", result);
        return result;
    }
}