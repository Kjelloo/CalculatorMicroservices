﻿namespace SharedModels.Events;

public class AdditionEvent
{
    public float Operand1 { get; set; }
    public float Operand2 { get; set; }
    public string DateTime { get; set; }
    public Dictionary<string, object> Headers { get; set; } = new();

    public override string ToString()
    {
        return Operand1 + " + " + Operand2;
    }
}