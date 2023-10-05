using SharedModels.Models;

namespace CalculationApi.Data.Models;

public class CalculationRequest
{
    public float Operand1 { get; set; }
    public float Operand2 { get; set; }
    public OperatorDto Operator { get; set; }
    public DateTime DateTime { get; set; }
}