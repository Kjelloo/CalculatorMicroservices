namespace CalculationHistoryService.Data.Models;

public class Calculation
{
    public IEnumerable<decimal> Operands { get; set; }
    public Operators Operator { get; set; }
    public decimal Result { get; set; }
}