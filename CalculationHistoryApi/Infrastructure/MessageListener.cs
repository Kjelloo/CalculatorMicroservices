using System.Diagnostics;
using CalculationHistoryApi.Data.Database;
using CalculationHistoryService.Data.Models;
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
    private TraceContextPropagator propagator;
    IBus bus;
    
    public MessageListener(IServiceProvider provider)
    {
        this.provider = provider;
    }
    
    public void Start()
    {
        using (bus = ConnectionHelper.GetRMQConnection())
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
        var parentContext = ExtractData(propagator, additionEvent);
        
        Baggage.Current = parentContext.Baggage;

        // Start a new activity with the parent context
        using var activity = MonitoringService.ActivitySource.StartActivity("SaveAdditionCalculationToDatabase", ActivityKind.Consumer, parentContext.ActivityContext);

        using var scope = provider.CreateScope();
        
        var services = scope.ServiceProvider;
        var calculationHistoryRepo = services.GetService<IRepository<Calculation>>();
        
        var calculationHistory = new Calculation
        {
            Operands = additionEvent.Operands,
            Result = additionEvent.Result,
            Operator = Operators.Addition
        };

        var added = calculationHistoryRepo?.Add(calculationHistory);

        if (added is not null)
        {
            Log.Logger.Debug("Added calculation to database: {CalculationHistory}", added);
        }
        else
        {
            Log.Logger.Error("Could not add calculation to database, {calculationHistory}", calculationHistory);
        }
    }
    
    private void HandleSubtractionEvent(SubtractionReceiveResultEvent subtractionEvent)
    {
        // Extract the parent context from the event
        var parentContext = ExtractData(propagator, subtractionEvent);
        
        Baggage.Current = parentContext.Baggage;

        // Start a new activity with the parent context
        using var activity = MonitoringService.ActivitySource.StartActivity("SaveSubtractionCalculationToDatabase", ActivityKind.Consumer, parentContext.ActivityContext);

        using var scope = provider.CreateScope();
        
        var services = scope.ServiceProvider;
        var calculationHistoryRepo = services.GetService<IRepository<Calculation>>();
        
        var calculationHistory = new Calculation
        {
            Operands = subtractionEvent.Operands,
            Result = subtractionEvent.Result,
            Operator = Operators.Addition
        };

        var added = calculationHistoryRepo?.Add(calculationHistory);

        if (added is not null)
        {
            Log.Logger.Debug("Added calculation to database: {CalculationHistory}", added);
        }
        else
        {
            Log.Logger.Error("Could not add calculation to database, {calculationHistory}", calculationHistory);
        }
    }
    
    private static PropagationContext ExtractData(TraceContextPropagator propagator, object e)
    {
        switch (e)
        {
            case SubtractionReceiveResultEvent subtractionEvent:
            {
                var parentContext = propagator.Extract(default, subtractionEvent.Headers,
                    (r, key) => { return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!); });
                return parentContext;
            }
            case AdditionReceiveResultEvent additionEvent:
            {
                var parentContext = propagator.Extract(default, additionEvent.Headers,
                    (r, key) => { return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!); });
                return parentContext;
            }
            default:
                throw new ArgumentException("Invalid event type");
        }
    }
}