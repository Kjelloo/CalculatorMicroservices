using System.Diagnostics;
using CalculationApi.Data.Models;
using EasyNetQ;
using Microsoft.AspNetCore.Mvc;
using Monitoring;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Polly;
using Polly.Retry;
using Serilog;
using SharedModels.Events;
using SharedModels.Helpers;
using SharedModels.Models;

namespace CalculationApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalculatorController : ControllerBase
    {
        private readonly RetryPolicy _retryPolicyAddition;
        private readonly RetryPolicy _retryPolicySubtraction;
        private readonly TraceContextPropagator _propagator;
        
        public CalculatorController()
        {
            _propagator = new TraceContextPropagator();
            _retryPolicyAddition = Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Increases time between tries
                    (exception, timeSpan, retryCount) =>
                    {
                        MonitoringService.Log.Error($"Exception when publishing message to addition service: {exception.Message} - Retrying after {timeSpan.TotalSeconds} seconds. Retry count: {retryCount}");
                    });
            
            _retryPolicySubtraction = Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount) =>
                    {
                        MonitoringService.Log.Error($"Exception when publishing message to subtraction service: {exception.Message} - Retrying after {timeSpan.TotalSeconds} seconds. Retry count: {retryCount}");
                    });
        }

        [HttpPost]
        public async Task<ActionResult<float>> Calculate([FromBody] CalculationRequest request)
        {
            using var activity = MonitoringService.ActivitySource.StartActivity("Begin Calculation");
            
            MonitoringService.Log.Debug("Received calculation request: {CalculationRequest}", request);
            
            var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
            
            float result = 0;
            
            using var bus = ConnectionHelper.GetRmqConnection();
            
            switch (request.Operator)
            {
                // Check if the operator is addition
                case OperatorDto.Addition:
                    var additionEvent = new AdditionEvent
                    {
                        Operand1 = request.Operand1,
                        Operand2 = request.Operand2
                    };

                    Log.Logger.Debug("AdditionEvent created: {additionEvent}", additionEvent);
                    
                    _propagator.Inject(  
                        new PropagationContext(activityContext, Baggage.Current),
                        additionEvent,
                        (r, key, value) => { r.Headers.Add(key, value); });
                    
                    _retryPolicyAddition.Execute(() =>
                    {
                        bus.PubSub.PublishAsync(additionEvent);
                    });
                    
                    var spinLockAdd = new object();

                    bus.PubSub.SubscribeAsync<AdditionReceiveResultEvent>("Calculator.Addition.ReceiveResult",
                         message =>
                        {
                            // Extract the parent context from the event
                            // var parentContext = _propagator.Extract(default, message.Headers,
                            //     (r, key) => { return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!); });
                            //
                            // Baggage.Current = parentContext.Baggage;
                            //
                            // // Start a new activity with the parent context
                            // using var activityadd = MonitoringService.ActivitySource.StartActivity("ReturnAdditionToRequester", ActivityKind.Consumer, parentContext.ActivityContext);
                            
                            result = message.Result;

                            lock (spinLockAdd)
                            {
                                Monitor.Pulse(spinLockAdd);
                            }
                        });

                    lock (spinLockAdd)
                    {
                        Monitor.Wait(spinLockAdd);
                    }
                    break;

                // Check if the operator is subtraction
                case OperatorDto.Subtraction:
                    var subtractionEvent = new SubtractionEvent
                    {
                        Operand1 = request.Operand1,
                        Operand2 = request.Operand2
                    };

                    _propagator.Inject(  
                        new PropagationContext(activityContext, Baggage.Current),
                        subtractionEvent,
                        (r, key, value) => { r.Headers.Add(key, value); });
                    
                    _retryPolicySubtraction.Execute(() =>
                    {
                        bus.PubSub.PublishAsync(subtractionEvent);
                    });
                    
                    var spinLockSub = new object();

                    bus.PubSub.SubscribeAsync<SubtractionReceiveResultEvent>("Calculator.Subtraction.ReceiveResult",
                        message =>
                        {
                            // Extract the parent context from the event
                            // var parentContext = _propagator.Extract(default, message.Headers,
                            //     (r, key) => { return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!); });
                            //
                            // Baggage.Current = parentContext.Baggage;
                            //
                            // // Start a new activity with the parent context
                            // using var activitysub = MonitoringService.ActivitySource.StartActivity("ReturnSubtractionToRequester", ActivityKind.Consumer, parentContext.ActivityContext);
                            
                            result = message.Result;
                            
                            lock (spinLockSub)
                            {
                                Monitor.Pulse(spinLockSub);
                            }
                        });

                    lock (spinLockSub)
                    {
                        Monitor.Wait(spinLockSub);
                    }
                    
                    break;
                default:
                    return BadRequest("Operator not supported");

                // TODO: Implement other operator calculations
            }
            return Ok(result);
        }
    }
}
