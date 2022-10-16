using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using System.Diagnostics;

namespace DistributedTracing;

class MyProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        Console.WriteLine($"OnStart: {activity.DisplayName}");
    }

    public override void OnEnd(Activity activity)
    {
        Console.WriteLine($"OnEnd: {activity.DisplayName}");
    }
}

public static class OpenTelemetryCustomServices
{
    // Use OpenTelemetry to instrument the request pipeline.
    private static Action<ResourceBuilder> ConfigureResource = r => r.AddService(
          TelemetryConstants.ServiceName,
          serviceVersion: TelemetryConstants.ServiceVersion,
          serviceInstanceId: $"{Environment.MachineName}");

    // OpenTelemetry
    public static void AddOpenTelemetryCustomServices2(this WebApplicationBuilder builder)
    {
        // Add OpenTelemetry
        #region Tracing

        var otlpEndpoint = builder.Configuration.GetValue<string>("Otlp:Endpoint")!;
        var jaegeEndpoint = builder.Configuration.GetValue<string>("Jaeger:Endpoint")!;
        var seqEndpoint = builder.Configuration.GetValue<string>("SeqConfiguration:Uri")!;

        Log.Information("OTLP Endpoint: {OtlpEndpoint}", otlpEndpoint);
        Log.Information("Jaeger Endpoint: {JaegerEndpoint}", jaegeEndpoint);


        builder.Services.AddOpenTelemetryTracing(options =>
        {
            options
                .ConfigureResource(ConfigureResource)
                .AddSource(TelemetryConstants.MyActivitySource.Name)
                .SetSampler(new AlwaysOnSampler())
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation((options) => options.Enrich
        = (activity, eventName, rawObject) =>
        {
            // Exclude custom html pages from tracing
            options.Filter = (req) =>
                !req.Request.Path.ToUriComponent().Contains("index.html", StringComparison.OrdinalIgnoreCase) &&
                !req.Request.Path.ToUriComponent().Contains("swagger", StringComparison.OrdinalIgnoreCase) &&
                !req.Request.Path.ToUriComponent().Contains("_framework", StringComparison.OrdinalIgnoreCase) &&
                !req.Request.Path.ToUriComponent().Contains("5342", StringComparison.OrdinalIgnoreCase);

        })
                .AddSqlClientInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.RecordException = true;
                })
                // .AddJaegerExporter()
                .AddOtlpExporter()
                .AddConsoleExporter();
        });

        // For options which can be bound from IConfiguration.
        builder.Services.Configure<AspNetCoreInstrumentationOptions>(builder.Configuration.GetSection("AspNetCoreInstrumentation"));
        builder.Services.Configure<OtlpExporterOptions>(builder.Configuration.GetSection("Otlp"));


        #endregion

        // Logging
        #region Logging

        builder.Logging
            .ClearProviders()
            .AddConsole()
            .AddSeq(serverUrl: seqEndpoint)
            .AddOpenTelemetry(options =>
            {
                options.ConfigureResource(ConfigureResource);
                options.AddConsoleExporter();
                options.AddOtlpExporter(options => { options.Endpoint = new Uri(otlpEndpoint); });
            });


        builder.Services.Configure<OpenTelemetryLoggerOptions>(opt =>
        {
            opt.IncludeScopes = true;
            opt.ParseStateValues = true;
            opt.IncludeFormattedMessage = true;
        });


        #endregion

        # region Metrics

        builder.Services.AddOpenTelemetryMetrics(options =>
        {
            options.ConfigureResource(ConfigureResource);
            options.AddHttpClientInstrumentation();
            options.AddAspNetCoreInstrumentation();
            options.AddMeter(TelemetryConstants.GetMeter.Name);
            options.AddOtlpExporter(options => { options.Endpoint = new Uri(otlpEndpoint); });
        });

        # endregion
    }

    public static void UsePrometheusMetrics(this WebApplication app)
    {
        app.UseHttpMetrics();
        app.MapMetrics();
    }

    public static void UsePrometheusExporter(this WebApplication app)
    {

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(TelemetryConstants.GetMeter.Name)
            .ConfigureResource(ConfigureResource)
            .AddConsoleExporter()
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
            .AddPrometheusExporter()
            .AddRuntimeInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .Build();

        app.MapPrometheusScrapingEndpoint("/metrics", meterProvider);
    }
}