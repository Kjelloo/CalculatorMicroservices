namespace SubtractionService;

public class SubtractionEvent
{
    public IEnumerable<decimal> Operands { get; set; }
    public Dictionary<string, object> Headers { get; set; }
}