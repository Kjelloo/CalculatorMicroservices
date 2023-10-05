using System.Diagnostics;
using EasyNetQ;
using Monitoring;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Polly;
using SharedModels.Events;
using SharedModels.Helpers;

namespace AdditionService;

public static class Program
{
    public static void Main(string[] args)
    {
        var subtractionService = new AddService();
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

        MonitoringService.Log.Debug("Waiting for rabbitmq to start in addition service");
        
        // Wait for RabbitMQ to start
        Thread.Sleep(10000);
        using var bus = ConnectionHelper.GetRmqConnection();
        
        MonitoringService.Log.Debug("Addition service running...");
        
        bus.PubSub.SubscribeAsync<AdditionEvent>("AdditionService.HandleCalculation", e =>
        {
            var parentContext = propagator.Extract(default, e.Headers,
                (r, key) => { return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!); });
                
            Baggage.Current = parentContext.Baggage;
            
            MonitoringService.Log.Debug("Received addition event: {additionEvent}", e);

            using (var activity =
                   MonitoringService.ActivitySource.StartActivity("AdditionService", ActivityKind.Consumer,
                       parentContext.ActivityContext))
            {
                var result = new AdditionReceiveResultEvent
                {
                    Operand1 = e.Operand1,
                    Operand2 = e.Operand2,
                    Result = subtractionService.Addition(e.Operand1, e.Operand2),
                    DateTime = e.DateTime
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
                
                MonitoringService.Log.Debug("Sending addition result event: {additionReceiveResultEvent}", result);
            }
        });

        lock (spinLock)
        {
            Monitor.Wait(spinLock);
        }
    }
}