using CalculationApi.Data.Models;
using EasyNetQ;
using Microsoft.AspNetCore.Mvc;
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
        [HttpPost]
        public async Task<ActionResult<float>> Calculate([FromBody] CalculationRequest request)
        {
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

                    bus.PubSub.PublishAsync(additionEvent);

                    var spinLockAdd = new object();

                    bus.PubSub.SubscribeAsync<AdditionReceiveResultEvent>("Calculator.Addition.ReceiveResult",
                         message =>
                        {
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

                    bus.PubSub.PublishAsync(subtractionEvent);
                    
                    var spinLockSub = new object();

                    bus.PubSub.SubscribeAsync<SubtractionReceiveResultEvent>("Calculator.Subtraction.ReceiveResult",
                        message =>
                        {
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

                // TODO: Implement multiple operator calculation
            }
            return Ok(result);
        }
    }
}
