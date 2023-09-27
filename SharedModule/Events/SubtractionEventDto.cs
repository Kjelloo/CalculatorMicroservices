namespace Events;

public class SubtractionEventDto
{
    public IEnumerable<decimal> Operands { get; set; }
    public Dictionary<string, object> Headers { get; set; }
}