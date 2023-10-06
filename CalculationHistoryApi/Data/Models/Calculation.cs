namespace CalculationHistoryApi.Data.Models;

public class Calculation
{
    public int Id { get; set; }
    public float Operand1 { get; set; }
    public float Operand2 { get; set; }
    
    public Operators Operator { get; set; }
    public float Result { get; set; }

    public override string ToString()
    {
        return Operand1 + " " + Operator + " " + Operand2 + " = " + Result;
    }
}