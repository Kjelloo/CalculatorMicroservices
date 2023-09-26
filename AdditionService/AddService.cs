namespace AdditionService;

public class AddService
{
    public decimal Add(IEnumerable<decimal> operands)
    {
        return operands.Sum();
    }
    
}