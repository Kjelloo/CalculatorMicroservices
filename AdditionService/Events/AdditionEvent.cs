namespace AdditionService.Events;

public class AdditionEvent
{
    public IEnumerable<decimal> Operands { get; set; }
    public Dictionary<string, object> Headers { get; set; }
}