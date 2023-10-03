using System.Diagnostics;
using System.Runtime.CompilerServices;
using EasyNetQ;
using Monitoring;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using SharedModels.Events;
using SharedModels.Helpers;
using SubtractionService.Events;

namespace SubtractionService;

public static class Program
{
    public static async Task Main()
    {
        var subtractionService = new SubtractService();
        var propagator = new TraceContextPropagator();
        var spinLock = new object();
        
        using (var bus = ConnectionHelper.GetRMQConnection())
        {
            bus.PubSub.SubscribeAsync<SubtractionEvent>("subtraction.handleCalculation", e =>
            {
                var parentContext = ExtractData(propagator, e);
                Baggage.Current = parentContext.Baggage;

                using (var activity =
                       MonitoringService.ActivitySource.StartActivity("SubtractionService", ActivityKind.Consumer,
                           parentContext.ActivityContext))
                {
                    var result = new SubtractionReceiveResultEvent
                    {
                        Result = subtractionService.Subtract(e.Operands)
                    };

                    var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;

                    InjectData(propagator, activityContext, result);

                    bus.PubSub.PublishAsync(result);
                }
            });

            lock (spinLock)
            {
                Monitor.Wait(spinLock);
            }
        }
    }

    private static void InjectData(TraceContextPropagator propagator, ActivityContext activityContext,
        SubtractionReceiveResultEvent data)
    {
        propagator.Inject(
            new PropagationContext(activityContext, Baggage.Current),
            data,
            (r, key, value) => { r.Headers.Add(key, value); });
    }
    
    private static PropagationContext ExtractData(TraceContextPropagator propagator, SubtractionEvent e)
    {
        var parentContext = propagator.Extract(default, e.Headers,
            (r, key) => { return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!); });
        return parentContext;
    }
}