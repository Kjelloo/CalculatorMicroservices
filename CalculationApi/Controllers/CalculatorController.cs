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

        public CalculatorController()
        {
            _retryPolicyAddition = Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Increases time between tries
                    (exception, timeSpan, retryCount) =>
                    {
                        MonitoringService.Log.Error
                            ($"Exception when publishing message to addition service: {exception.Message} - Retrying after {timeSpan.TotalSeconds} seconds. Retry count: {retryCount}");
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
            var propagator = new TraceContextPropagator();
            
            MonitoringService.Log.Debug("Received calculation request: {CalculationRequest}", request);
            
            float result = 0;
            
            using var bus = ConnectionHelper.GetRmqConnection();
            
            switch (request.Operator)
            {
                // Check if the operator is addition
                case OperatorDto.Addition:
                {
                    using var activitySendAdd = MonitoringService.ActivitySource.StartActivity("BeginAdditionCalculation", ActivityKind.Client);
                    {
                        var activityContext = activitySendAdd?.Context ?? default;
                        var additionEvent = new AdditionEvent
                        {
                            Operand1 = request.Operand1,
                            Operand2 = request.Operand2
                        };
                        
                        propagator.Inject(  
                            new PropagationContext(activityContext, Baggage.Current),
                            additionEvent,
                            (r, key, value) => { r.Headers.Add(key, value); });
                        
                        _retryPolicyAddition.Execute(() =>
                        {
                            // MonitoringService.Log.Debug("AdditionEvent injected with activity context: {additionEvent}", additionEvent.Headers);
                            MonitoringService.Log.Debug("Addition calculation started for: {additionEvent}", additionEvent.ToString());
                            bus.PubSub.PublishAsync(additionEvent);
                        });
                        
                    }

                    var spinLockAdd = new object();
                    
                    bus.PubSub.SubscribeAsync<AdditionReceiveResultEvent>("Calculator.Addition.ReceiveResult",
                        message =>
                        {
                            // Extract the parent context from the event
                            var parentContext = propagator.Extract(default, message.Headers,
                                (r, key) => { return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!); });
        
                            Baggage.Current = parentContext.Baggage;

                            // Start a new activity with the parent context
                            using var activity = MonitoringService.ActivitySource.StartActivity("ReturnAdditionCalculation", ActivityKind.Consumer, parentContext.ActivityContext);
                            
                            result = message.Result;

                            // Release the lock
                            lock (spinLockAdd)
                            {
                                Monitor.Pulse(spinLockAdd);
                            }
                        });

                    // Wait for the result to be received
                    lock (spinLockAdd)
                    {
                        Monitor.Wait(spinLockAdd);
                    }
                    
                    break;
                }

                // Check if the operator is subtraction
                case OperatorDto.Subtraction:
                {
                    using var activitySendSub = MonitoringService.ActivitySource.StartActivity("BeginAdditionCalculation", ActivityKind.Client);
                    {
                        var activityContext = activitySendSub?.Context ?? default;
                        var subtractionEvent = new SubtractionEvent
                        {
                            Operand1 = request.Operand1,
                            Operand2 = request.Operand2
                        };
                        
                        propagator.Inject(  
                            new PropagationContext(activityContext, Baggage.Current),
                            subtractionEvent,
                            (r, key, value) => { r.Headers.Add(key, value); });

                        _retryPolicySubtraction.Execute(() =>
                        {
                            // MonitoringService.Log.Debug("AdditionEvent injected with activity context: {additionEvent}", subtractionEvent.Headers);
                            MonitoringService.Log.Debug("Addition calculation started : {subtractionEvent}", subtractionEvent.ToString());
                            bus.PubSub.PublishAsync(subtractionEvent);
                        });
                    }

                    var spinLockSub = new object();

                    bus.PubSub.SubscribeAsync<SubtractionReceiveResultEvent>("Calculator.Subtraction.ReceiveResult",
                        message =>
                        {
                            // Extract the parent context from the event
                            var parentContext = propagator.Extract(default, message.Headers,
                                (r, key) => { return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!); });
        
                            Baggage.Current = parentContext.Baggage;

                            // Start a new activity with the parent context
                            using var activity = MonitoringService.ActivitySource.StartActivity("ReturnSubtractionCalculation", ActivityKind.Consumer, parentContext.ActivityContext);
                            
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
                }
                default:
                    return BadRequest("Operator not supported");

                // TODO: Implement other operator calculations
            }
            return Ok(result);
        }
    }
}
