using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using EasyNetQ;
using Monitoring;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Polly;
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
        
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetry(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Increases time between tries
                (exception, timeSpan, retryCount) =>
                {
                    MonitoringService.Log.Error($"Exception when publishing message from addition service: {exception.Message} - Retrying after {timeSpan.TotalSeconds} seconds. Retry count: {retryCount}");
                });

        // Wait for RabbitMQ to start
        MonitoringService.Log.Debug("Waiting for rabbitmq to start in subtraction service: {subtractionServiceName}", Assembly.GetCallingAssembly().GetName().Name);
        Thread.Sleep(15000);
        using var bus = ConnectionHelper.GetRmqConnection();
        
        MonitoringService.Log.Debug("Subtraction service running...");
            
        bus.PubSub.SubscribeAsync<SubtractionEvent>("SubtractionService.HandleCalculation", e =>
        {
            MonitoringService.Log.Debug("Received subtraction event: {SubtractionEvent}", e.ToString());
            
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
                    Result = subtractionService.Subtract(e.Operand1, e.Operand2)
                };

                var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
                
                propagator.Inject(
                    new PropagationContext(activityContext, Baggage.Current),
                    result,
                    (r, key, value) => { r.Headers.Add(key, value); });

                retryPolicy.Execute(() =>
                {
                    bus.PubSub.PublishAsync(result);
                });
                
                MonitoringService.Log.Debug("Sending subtraction result event: {SubtractionReceiveResultEvent}", result.Result);
            }
        });

        lock (spinLock)
        {
            Monitor.Wait(spinLock);
        }
    }
}