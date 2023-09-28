using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;

namespace Monitoring;

public class MonitoringService
{
    public static readonly ActivitySource ActivitySource = new("Calculator");
    private static TracerProvider _tracerProvider;

    static MonitoringService()
    {
        var serviceName = Assembly.GetExecutingAssembly().GetName().Name;
        
        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddZipkinExporter()
            .AddConsoleExporter()
            .AddSource(ActivitySource.Name)
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: serviceName))
            .Build();
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithSpan()
            .WriteTo.Seq("http://localhost:5341")
            .WriteTo.Console()
            .CreateLogger();
    }
}