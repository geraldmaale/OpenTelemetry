using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Distributed.Tracing;

public static class OpenTelemetryCustomServices
{
    // OpenTelemetry
    public static void AddOpenTelemetryCustomServices(this WebApplicationBuilder builder,
        string serviceName = "Tracing", string serviceVersion = "1.0.0", string sourceName = "ActivitySource")
    {
        // Add OpenTelemetry
        #region Tracing

        // Tracing Settings
        var tracingExporter = builder.Configuration.GetValue<string>("UseTracingExporter")?.ToLowerInvariant();

        // Get metrics exporter
        var metricsExporter = builder.Configuration.GetValue<string>("UseMetricsExporter")!.ToLowerInvariant();

        Action<ResourceBuilder> configureResource = r => r.AddService(
       serviceName, serviceVersion, serviceInstanceId: $"{Environment.MachineName}");

        builder.Services.AddOpenTelemetryTracing(options =>
        {
            options
                .ConfigureResource(configureResource)
                .AddSource(sourceName)
                .SetSampler(new AlwaysOnSampler())
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation(options =>
                {
                    // Exclude custom html pages from tracing
                    options.Filter = (req) =>
                        !req.Request.Path.ToUriComponent().Contains("index.html", StringComparison.OrdinalIgnoreCase) &&
                        !req.Request.Path.ToUriComponent().Contains("swagger", StringComparison.OrdinalIgnoreCase) &&
                        !req.Request.Path.ToUriComponent().Contains("_framework", StringComparison.OrdinalIgnoreCase);
                })
                .AddSqlClientInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.RecordException = true;
                })
                .AddJaegerExporter()
                .AddOtlpExporter()
                .AddConsoleExporter();
        });

        // For options which can be bound from IConfiguration.
        builder.Services.Configure<AspNetCoreInstrumentationOptions>(builder.Configuration.GetSection("AspNetCoreInstrumentation"));


        #endregion

        // Logging
        #region Logging

        builder.Logging.ClearProviders();

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.ConfigureResource(configureResource);

            // Switch between Console/OTLP by setting UseLogExporter in appsettings.json.
            var logExporter = builder.Configuration.GetValue<string>("UseLogExporter")!.ToLowerInvariant();
            switch (logExporter)
            {
                case "otlp":
                    options.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint")!);
                    });
                    break;
                default:
                    options.AddConsoleExporter();
                    break;
            }
        });

        builder.Services.Configure<OpenTelemetryLoggerOptions>(opt =>
        {
            opt.IncludeScopes = true;
            opt.ParseStateValues = true;
            opt.IncludeFormattedMessage = true;
        });

        #endregion

        // Metrics
        #region Metrics

        // Switch between Prometheus/OTLP/Console by setting UseMetricsExporter in appsettings.json.

        builder.Services.AddOpenTelemetryMetrics(options =>
        {
            options.ConfigureResource(configureResource)
                .AddRuntimeInstrumentation()
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation();

            switch (metricsExporter)
            {
                case "prometheus":
                    options.AddPrometheusExporter();
                    break;
                case "otlp":
                    options.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint")!);
                    });
                    break;
                default:
                    options.AddConsoleExporter();
                    break;
            }
        });

        #endregion

    }
}