using System.Diagnostics;
using System.Reflection;
using EasyNetQ;
using Monitoring;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Polly;
using Serilog;
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

        MonitoringService.Log.Debug("Waiting for rabbitmq to start in addition service: {additionServiceName}", Assembly.GetCallingAssembly().GetName().Name);
        
        // Wait for RabbitMQ to start
        Thread.Sleep(15000);
        using var bus = ConnectionHelper.GetRmqConnection();
        
        MonitoringService.Log.Debug("Addition service running...");
        
        bus.PubSub.SubscribeAsync<AdditionEvent>("AdditionService.HandleCalculation", e =>
        {
            MonitoringService.Log.Debug("Received addition event: {additionEvent}", e.ToString());
            
            var parentContext = propagator.Extract(default, e.Headers,
                (r, key) => { return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!); });
            
            Baggage.Current = parentContext.Baggage;
            
            using (var activity =
                   MonitoringService.ActivitySource.StartActivity("AdditionService", ActivityKind.Consumer,
                       parentContext.ActivityContext))
            {
                var result = new AdditionReceiveResultEvent
                {
                    Operand1 = e.Operand1,
                    Operand2 = e.Operand2,
                    Result = subtractionService.Addition(e.Operand1, e.Operand2)
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
                
                MonitoringService.Log.Debug("Sending addition result event: {additionReceiveResultEvent}", result.Result);
            }
        });

        lock (spinLock)
        {
            Monitor.Wait(spinLock);
        }
    }
}