using CalculationHistoryApi.Data.Database;
using CalculationHistoryService.Data.Models;
using EasyNetQ;
using Events;
using Serilog;
using SharedModule.Helpers;

namespace CalculationHistoryApi.Infrastructure;

public class MessageListener
{
    IServiceProvider provider;
    IBus bus;
    
    public MessageListener(IServiceProvider provider)
    {
        this.provider = provider;
    }
    
    public void Start()
    {
        using (bus = ConnectionHelper.GetRMQConnection())
        {
            // TODO: Use tracing to log the events
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
        using (var scope = provider.CreateScope())
        {
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
                Log.Logger.Debug("Added calculation history to database: {CalculationHistory}", added);
            }
            else
            {
                Log.Logger.Error("Could not add calculation history to database, {calculationHistory}", calculationHistory);
            }
        }
    }
    
    private void HandleSubtractionEvent(SubtractionReceiveResultEvent subtractionEvent)
    {
        using (var scope = provider.CreateScope())
        {
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
                Log.Logger.Debug("Added calculation history to database: {CalculationHistory}", added);
            }
            else
            {
                Log.Logger.Error("Could not add calculation history to database, {calculationHistory}", calculationHistory);
            }
        }
    }
}