using Monitoring;

namespace AdditionService;

public class AddService
{
    public float Addition(float operand1, float operand2)
    {
        var result = operand1 + operand2;
        MonitoringService.Log.Debug("Finished addition with result {Result}", result);
        return result;
    }
}