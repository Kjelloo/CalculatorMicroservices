﻿using System.Diagnostics;
using EasyNetQ;
using OpenTelemetry.Context.Propagation;

namespace AdditionService;

using Monitoring;

public static class Program
{
    public static async Task Main()
    {
        var subtractionService = new AddService();
        var propagator = new TraceContextPropagator();
        
        var connectionEstablished = false;
        
        using var bus = ConnectionHelper.GetRMQConnection();
        while (!connectionEstablished)
        {
            var subscriptionResult = bus.PubSub.SubscribeAsync<AdditionEvent>("AS", e =>
            {
                var parentContext = propagator.Extract(default, e.Headers, (r, key) =>
                {
                    return new List<string>(new[] { r.ContainsKey(key) ? r[key].ToString() : String.Empty }!);
                });
                
                using var activity =
                    Monitoring.ActivitySource.StartActivity("AdditionService", ActivityKind.Consumer, parentContext.ActivityContext);
                
                // TODO: Send subtract result to calculation history service
                subtractionService.Add(e.Operands);
                
            }).AsTask();
            
            await subscriptionResult.WaitAsync(CancellationToken.None);
            connectionEstablished = subscriptionResult.Status == TaskStatus.RanToCompletion;
            if (!connectionEstablished) Thread.Sleep(1000);
        }

        while (true) Thread.Sleep(5000);
    }
}