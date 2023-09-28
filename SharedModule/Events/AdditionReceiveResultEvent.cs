namespace Events;

public class AdditionReceiveResultEvent
{
    public IEnumerable<decimal> Operands { get; set; }
    public decimal Result { get; set; }
    public Dictionary<string, object> Headers { get; set; } = new();
}