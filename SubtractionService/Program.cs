using System.Diagnostics;
using System.Runtime.CompilerServices;
using EasyNetQ;
using Monitoring;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using SharedModels.Events;
using SharedModels.Helpers;

namespace SubtractionService;

public static class Program
{
    public static void Main(string[] args)
    {
        var subtractionService = new SubtractService();
        var propagator = new TraceContextPropagator();
        var spinLock = new object();

        // Wait for RabbitMQ to start
        MonitoringService.Log.Debug("Waiting for rabbitmq to start");
        Thread.Sleep(10000);
        using var bus = ConnectionHelper.GetRmqConnection();
        
        MonitoringService.Log.Debug("Subtraction service running...");
            
        bus.PubSub.SubscribeAsync<SubtractionEvent>("SubtractionService.HandleCalculation", e =>
        {
            MonitoringService.Log.Debug("Received subtraction event: {SubtractionEvent}", e);
            
            var parentContext = propagator.Extract(default, e.Headers,
                (r, key) => { return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!); });
            Baggage.Current = parentContext.Baggage;

            using (var activity =
                   MonitoringService.ActivitySource.StartActivity("SubtractionService", ActivityKind.Consumer,
                       parentContext.ActivityContext))
            {
                var result = new SubtractionReceiveResultEvent
                {
                    Operand1 = e.Operand1,
                    Operand2 = e.Operand2,
                    Result = subtractionService.Subtract(e.Operand1, e.Operand2),
                    DateTime = e.DateTime
                };

                var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
                
                propagator.Inject(
                    new PropagationContext(activityContext, Baggage.Current),
                    result,
                    (r, key, value) => { r.Headers.Add(key, value); });

                MonitoringService.Log.Debug("Sending subtraction result event: {SubtractionReceiveResultEvent}", result);
                bus.PubSub.PublishAsync(result);
            }
        });

        lock (spinLock)
        {
            Monitor.Wait(spinLock);
        }
    }
}