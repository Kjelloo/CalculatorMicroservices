namespace SharedModels.Events;

public class AdditionReceiveResultEvent
{
    public float Operand1 { get; set; }
    public float Operand2 { get; set; }
    public float Result { get; set; }
    public string DateTime { get; set; }
    public Dictionary<string, object> Headers { get; set; } = new();
}