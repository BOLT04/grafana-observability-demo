using Azure.Monitor.OpenTelemetry.AspNetCore;
using Grafana.OpenTelemetry;
using GrafanaOtelDemoApp.Application;
using GrafanaOtelDemoApp.Infrastructure;
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
                OtelOption.AzureMonitor => AddOtelAzureMonitor(builder, configuration),
                _ => throw new ArgumentException("Option not supported"),
            };
            return builder;
        }

        private static IHostApplicationBuilder AddOtelOTLP(IHostApplicationBuilder builder, IConfiguration configuration)
        {
            var otlpServiceName = configuration["Otlp:ServiceName"] ?? DiagnosticsNames.DefaultServiceName;
            var otlpInfraServiceName = InfrastructureDiagnosticsNames.DefaultServiceName;
            var otlpEndpoint = configuration["Otlp:Endpoint"] ?? string.Empty;
            var services = builder.Services;

            // Configure trace sampling
            SamplingConfiguration.ConfigureTraceSampling();

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
                        .AddMeter(otlpServiceName, otlpInfraServiceName)
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddNpgsqlInstrumentation()
                        .AddViews(SamplingConfiguration.GetCostOptimizedMetricViews()) // Add metric views for cost optimization
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
                        .AddSource(otlpServiceName, otlpInfraServiceName)
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddNpgsql();
                    // Uncomment to test RabbitMQ instrumentation with RabbitMQ.Client.OpenTelemetry nuget (only has traces for now)
                    //.AddRabbitMQInstrumentation();

                    if (!string.IsNullOrEmpty(otlpEndpoint))
                    {
                        tracing.AddOtlpExporter(options =>
                        {
                            // Uncomment to test Batch export
                            //options.ExportProcessorType = ExportProcessorType.Batch;
                        });
                    }
                    else
                    {
                        tracing.AddConsoleExporter();
                    }
                });

            builder.Logging
                .AddOpenTelemetry(options => 
                {
                    // Add log sampling processor
                    var samplingRates = configuration.GetValue<bool>("EnableLogSampling") 
                        ? SamplingConfiguration.Presets.MediumVolumeMediumCost 
                        : SamplingConfiguration.Presets.Development;
                    
                    options.AddProcessor(new SamplingConfiguration.LogSamplingProcessor(
                        samplingRates.debug, 
                        samplingRates.info, 
                        samplingRates.warning, 
                        samplingRates.error));
                    
                    options.AddOtlpExporter();
                })
                .AddConsole();

            return builder;
        }

        private static IHostApplicationBuilder AddOtelGrafana(IHostApplicationBuilder builder, IConfiguration configuration)
        {
            var otlpServiceName = configuration["Otlp:ServiceName"] ?? DiagnosticsNames.DefaultServiceName;
            var otlpInfraServiceName = InfrastructureDiagnosticsNames.DefaultServiceName;

            var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .UseGrafana()
                .ConfigureServices(services =>
                    services
                        .AddOpenTelemetry()
                        .WithTracing(tracing => tracing.AddSource(otlpServiceName, otlpInfraServiceName))
                )
                .AddConsoleExporter()
                .Build();
            var meterProvider = Sdk.CreateMeterProviderBuilder()
                .UseGrafana()
                .ConfigureServices(services =>
                    services
                        .AddOpenTelemetry()
                        .WithMetrics(metrics => metrics.AddMeter(otlpServiceName, otlpInfraServiceName))
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
