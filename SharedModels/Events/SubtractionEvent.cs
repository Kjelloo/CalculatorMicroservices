namespace SharedModels.Events;

public class SubtractionEvent
{
    public float Operand1 { get; set; }
    public float Operand2 { get; set; }
    public string DateTime { get; set; }
    public Dictionary<string, object> Headers { get; set; } = new();
}