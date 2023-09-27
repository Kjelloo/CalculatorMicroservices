namespace Events;

public class AdditionEventDto
{
    public IEnumerable<decimal> Operands { get; set; }
    public Dictionary<string, object> Headers { get; set; }
}