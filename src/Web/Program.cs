namespace Hyaena.Web;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Yarp.ReverseProxy.Forwarder;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var serviceSection = builder.Configuration.GetSection("OpenTelemetry:Service");
        var serviceName = serviceSection["Name"] ?? builder.Environment.ApplicationName;
        var serviceNamespace = serviceSection["Namespace"] ?? "default";
        var serviceVersion = serviceSection["Version"] ?? "1.0.0";

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes(
            [
                new("service.namespace", serviceNamespace),
                new("service.instance.id", Environment.MachineName)
            ]);

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
                .AddOtlpExporter());

        builder.Services
            .AddOptions<ReverseProxyHealthCheckOptions>()
            .BindConfiguration("HealthChecks:ReverseProxy");

        builder.Services
            .AddHealthChecks()
            .AddCheck<ReverseProxyHealthCheck>("reverse_proxy");

        builder.Services.AddSingleton<IForwarderHttpClientFactory, PollyForwarderHttpClientFactory>();
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
