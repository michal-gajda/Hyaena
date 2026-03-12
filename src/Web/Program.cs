namespace Hyaena.Web;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

public static class Program
{
    private const string SERVICE_NAME = "hyaena";
    private const string SERVICE_NAMESPACE = "poc";
    private const string SERVICE_VERSION = "1.0.0";

    public static async Task Main(string[] args)
    {
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(serviceName: SERVICE_NAME, serviceVersion: SERVICE_VERSION)
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new("service.namespace", SERVICE_NAMESPACE),
                new("service.instance.id", Environment.MachineName)
            });

        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.AddOpenTelemetry(options => options.SetResourceBuilder(resourceBuilder).AddOtlpExporter());

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .SetResourceBuilder(resourceBuilder)
                .SetSampler(new AlwaysOnSampler())
                .AddAspNetCoreInstrumentation(opt => opt.RecordException = true)
                .AddHttpClientInstrumentation(opt => opt.RecordException = true)
                .AddSource("Yarp.ReverseProxy")
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resourceBuilder)
                .AddProcessInstrumentation()
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
                ;

        builder.Services
            .AddHealthChecks()
            .AddCheck<ReverseProxyHealthCheck>("reverse_proxy");

        builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        var app = builder.Build();

        app.MapHealthChecks("/healthz");

        app.MapReverseProxy();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.MapFallbackToFile("index.html");

        await app.RunAsync();
    }
}
