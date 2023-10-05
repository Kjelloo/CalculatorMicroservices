using System.Diagnostics;
using CalculationHistoryApi.Data.Database;
using CalculationHistoryApi.Data.Models;
using EasyNetQ;
using Monitoring;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Serilog;
using SharedModels.Events;
using SharedModels.Helpers;

namespace CalculationHistoryApi.Infrastructure;

public class MessageListener
{
    IServiceProvider provider;
    private readonly TraceContextPropagator _propagator;
    IBus bus;
    
    public MessageListener(IServiceProvider provider)
    {
        _propagator = new TraceContextPropagator();
        this.provider = provider;
    }
    
    public void Start()
    {
        // Wait for RabbitMQ to start
        Thread.Sleep(10000);
        using (bus = ConnectionHelper.GetRmqConnection())
        {
            // Listen for addition and subtraction events
            bus.PubSub.Subscribe<AdditionReceiveResultEvent>("History.Addition.ReceiveResult", HandleAdditionEvent);
            
            bus.PubSub.Subscribe<SubtractionReceiveResultEvent>("History.Subtraction.ReceiveResult", HandleSubtractionEvent);
            
            lock (this)
            {
                Monitor.Wait(this);
            }
        }
    }
    
    private void HandleAdditionEvent(AdditionReceiveResultEvent additionEvent)
    {
        // Extract the parent context from the event
        var parentContext = _propagator.Extract(default, additionEvent.Headers,
            (r, key) => { return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!); });
        
        Baggage.Current = parentContext.Baggage;

        // Start a new activity with the parent context
        using var activity = MonitoringService.ActivitySource.StartActivity("SaveAdditionCalculationToDatabase", ActivityKind.Consumer, parentContext.ActivityContext);

        using var scope = provider.CreateScope();
        
        var services = scope.ServiceProvider;
        var calculationHistoryRepo = services.GetService<IRepository<Calculation>>();
        
        var calculationHistory = new Calculation
        {
            Operand1 = additionEvent.Operand1,
            Operand2 = additionEvent.Operand2,
            Result = additionEvent.Result,
            Operator = Operators.Addition
        };

        var added = calculationHistoryRepo?.Add(calculationHistory);

        if (added is not null)
        { 
            MonitoringService.Log.Debug("Added calculation to database: {CalculationHistory}", added);
        }
        else
        {
            MonitoringService.Log.Error("Could not add calculation to database, {calculationHistory}", calculationHistory);
        }
    }
    
    private void HandleSubtractionEvent(SubtractionReceiveResultEvent subtractionEvent)
    {
        // Extract the parent context from the event
        var parentContext = _propagator.Extract(default, subtractionEvent.Headers,
            (r, key) => { return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!); });
        
        Baggage.Current = parentContext.Baggage;

        // Start a new activity with the parent context
        using var activity = MonitoringService.ActivitySource.StartActivity("SaveSubtractionCalculationToDatabase", ActivityKind.Consumer, parentContext.ActivityContext);

        using var scope = provider.CreateScope();
        
        var services = scope.ServiceProvider;
        var calculationHistoryRepo = services.GetService<IRepository<Calculation>>();
        
        var calculationHistory = new Calculation
        {
            Operand1 = subtractionEvent.Operand1,
            Operand2 = subtractionEvent.Operand2,
            Result = subtractionEvent.Result,
            Operator = Operators.Addition
        };

        var added = calculationHistoryRepo?.Add(calculationHistory);

        if (added is not null)
        {
        }
        else
        {
            MonitoringService.Log.Error("Could not add calculation to database, {calculationHistory}", calculationHistory);
        }
    }
}