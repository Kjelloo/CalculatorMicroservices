using SharedModule.Models;

namespace Events;

public class SubtractionReceiveResultEvent
{
    public IEnumerable<decimal> Operands { get; set; }
    public decimal Result { get; set; }
    public Dictionary<string, object> Headers { get; set; } = new();
}