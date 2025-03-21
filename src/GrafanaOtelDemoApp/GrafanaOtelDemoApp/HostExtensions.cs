using Azure.Monitor.OpenTelemetry.AspNetCore;
using Grafana.OpenTelemetry;
using GrafanaOtelDemoApp.Application;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GrafanaOtelDemoApp
{
    public static class HostExtensions
    {
        /// <summary>
        /// Setup OpenTelemetry with logs, metrics and traces for a given option (depending on the demo).
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        /// <param name="otelOption"></param>
        /// <returns></returns>
        public static IHostApplicationBuilder AddObservability(this IHostApplicationBuilder builder, IConfiguration configuration, OtelOption otelOption)
        {
            builder.Services.AddSingleton<TelemetryDiagnosticsDI>();

            builder = otelOption switch
            {
                OtelOption.OTLP => AddOtelOTLP(builder, configuration),
                OtelOption.Grafana => AddOtelGrafana(builder, configuration),
                OtelOption.AzureMonitor => AddOtelOTLP(builder, configuration),
                _ => throw new ArgumentException("Option not supported"),
            };
            return builder;
        }

        private static IHostApplicationBuilder AddOtelOTLP(IHostApplicationBuilder builder, IConfiguration configuration)
        {
            var otlpServiceName = configuration["Otlp:ServiceName"] ?? DiagnosticsNames.DefaultServiceName;
            var otlpEndpoint = configuration["Otlp:Endpoint"] ?? string.Empty;
            var services = builder.Services;

            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(otlpServiceName)
                .AddHostDetector()
                .AddEnvironmentVariableDetector()
                .AddTelemetrySdk();

            services
                .AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddMeter(otlpServiceName)
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddNpgsqlInstrumentation()
                        .AddPrometheusExporter(options =>
                        {
                            options.ScrapeEndpointPath = "/metrics";
                        });

                    if (!string.IsNullOrEmpty(otlpEndpoint))
                    {
                        metrics.AddOtlpExporter();
                    }
                    else
                    {
                        metrics.AddConsoleExporter();
                    }
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(otlpServiceName)
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddNpgsql();

                    if (!string.IsNullOrEmpty(otlpEndpoint))
                    {
                        tracing.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(otlpEndpoint);
                            options.ExportProcessorType = ExportProcessorType.Batch;
                        });
                    }
                    else
                    {
                        tracing.AddConsoleExporter();
                    }
                });

            builder.Logging
                .AddOpenTelemetry(options => options.AddOtlpExporter())
                .AddConsole();

            return builder;
        }

        private static IHostApplicationBuilder AddOtelGrafana(IHostApplicationBuilder builder, IConfiguration configuration)
        {
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .UseGrafana()
                .ConfigureServices(services =>
                    services.AddOpenTelemetry().WithTracing(T => T.AddSource(DiagnosticsNames.DefaultServiceName))
                )
                .AddConsoleExporter()
                .Build();
            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .UseGrafana()
                .ConfigureServices(services =>
                    services.AddOpenTelemetry().WithMetrics(T => T.AddMeter(DiagnosticsNames.DefaultServiceName))
                )
                .AddConsoleExporter()
                .Build();

            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                        otlpOptions.Endpoint = new Uri(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
                    })
                    .AddConsoleExporter();
            });

            return builder;
        }

        private static IHostApplicationBuilder AddOtelAzureMonitor(IHostApplicationBuilder builder, IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            {
                throw new ArgumentException("APPLICATIONINSIGHTS_CONNECTION_STRING is not configured, to use Azure Monitor this value is necessary.");
            }

            builder.Services.AddOpenTelemetry().UseAzureMonitor();

            return builder;
        }
    }

    public enum OtelOption
    {
        Grafana,
        OTLP,
        AzureMonitor
    }
}
