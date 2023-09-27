using System.Diagnostics;
using EasyNetQ;
using OpenTelemetry.Context.Propagation;
using SharedModule.Helpers;

namespace SubtractionService;

public static class Program
{
    public static async Task Main()
    {
        var subtractionService = new SubtractService();
        var propagator = new TraceContextPropagator();
        
        var connectionEstablished = false;
        
        using var bus = ConnectionHelper.GetRMQConnection();
        while (!connectionEstablished)
        {
            var subscriptionResult = bus.PubSub.SubscribeAsync<SubtractionEvent>("SS", e =>
            {
                // Extract parent context from event headers
                var parentContext = PropagationContext(propagator, e);
                
                using var activity =
                    Monitoring.ActivitySource.StartActivity("SubtractionService", ActivityKind.Consumer, parentContext.ActivityContext);
                
                // TODO: Send subtract result to calculation history service
                subtractionService.Subtract(e.Operands);
                
            }).AsTask();
            
            await subscriptionResult.WaitAsync(CancellationToken.None);
            connectionEstablished = subscriptionResult.Status == TaskStatus.RanToCompletion;
            if (!connectionEstablished) Thread.Sleep(1000);
        }

        while (true) Thread.Sleep(5000);
    }

    private static PropagationContext PropagationContext(TraceContextPropagator propagator, SubtractionEvent e)
    {
        var parentContext = propagator.Extract(default, e.Headers,
            (r, key) => { return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!); });
        return parentContext;
    }
}