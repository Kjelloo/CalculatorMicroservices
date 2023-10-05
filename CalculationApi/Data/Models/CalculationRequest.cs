using System.Text.Json.Serialization;
using SharedModels.Models;

namespace CalculationApi.Data.Models;

public class CalculationRequest
{
    public float Operand1 { get; set; }
    public float Operand2 { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperatorDto Operator { get; set; }
}