using Monitoring;

namespace SubtractionService;

public class SubtractService
{
    public float Subtract(float operand1, float operand2)
    {
        using var activity = MonitoringService.ActivitySource.StartActivity("SubtractingNumbers");
        var result = operand1 - operand2;
        MonitoringService.Log.Debug("Finished subtraction with result {Result}", result);
        return result;
    }
}