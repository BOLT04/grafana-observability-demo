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
        private const string SemanticKernelName = "Microsoft.SemanticKernel*";

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

            // Enable Semantic Kernel model diagnostics with sensitive data.
            AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

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
                        .AddMeter(otlpServiceName, otlpInfraServiceName, SemanticKernelName)
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation()
                        .AddRuntimeInstrumentation(o =>
                        {
                            // Note: In the current nuget version, there are no options available. (https://github.com/open-telemetry/opentelemetry-dotnet-contrib/issues/1929)
                        })
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

                    metrics.AddConsoleExporter();
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(otlpServiceName, otlpInfraServiceName, SemanticKernelName)
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            // Configure options for ASP.NET Core instrumentation here
                        })
                        .AddHttpClientInstrumentation(options =>
                        {
                            // Disable URL query redaction
                            options.FilterHttpRequestMessage = (httpRequestMessage) =>
                            {
                                return true; // Allow all requests
                            };

                            // Set enrichment to include full URLs
                            options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                            {
                                activity.SetTag(DiagnosticsNames.AppHttpUrlLabel, httpRequestMessage.RequestUri?.ToString());

                                // Get the request body if it exists
                                if (httpRequestMessage.Content != null)
                                {
                                    var requestBody = httpRequestMessage.Content.ReadAsStringAsync().Result; // Synchronously read the content
                                    activity.SetTag("app.request.body", requestBody);
                                }
                            };

                            options.EnrichWithHttpResponseMessage = async (activity, httpResponseMessage) =>
                            {
                                // Showcase adding response enrichment
                                var response = await httpResponseMessage.Content.ReadAsStringAsync();
                                activity.SetTag("app.response", response);
                            };

                            // Key setting: Disable query redaction
                            options.RecordException = true;
                        })
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

                    tracing.AddConsoleExporter();
                });

            builder.Logging
                .AddOpenTelemetry(options => options.AddOtlpExporter())
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
                        .WithTracing(tracing => tracing.AddSource(otlpServiceName, otlpInfraServiceName, SemanticKernelName))
                )
                .AddConsoleExporter()
                .Build();
            var meterProvider = Sdk.CreateMeterProviderBuilder()
                .UseGrafana()
                .AddPrometheusExporter(options =>
                {
                    options.ScrapeEndpointPath = "/metrics";
                })
                .ConfigureServices(services =>
                    services
                        .AddOpenTelemetry()
                        .WithMetrics(metrics => metrics.AddMeter(otlpServiceName, otlpInfraServiceName, SemanticKernelName))
                )
                .AddConsoleExporter()
                .Build();

            builder.Services.AddSingleton(tracerProvider);
            builder.Services.AddSingleton(meterProvider);

            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
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
