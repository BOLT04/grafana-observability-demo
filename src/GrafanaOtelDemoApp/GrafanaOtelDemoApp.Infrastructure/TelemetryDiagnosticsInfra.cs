using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GrafanaOtelDemoApp.Infrastructure
{
    /// <summary>
    /// Example diagnostics class for manual instrumentation of infrastructure components.
    /// RabbitMQ in the future will provably have a
    /// <see href="https://www.nuget.org/packages/RabbitMQ.Client.OpenTelemetry">v1 released supporting OpenTelemetry</see> 
    /// </summary>
    public class TelemetryDiagnosticsInfra
    {
        public static readonly ActivitySource Source = new(InfrastructureDiagnosticsNames.DefaultServiceName);
        public static readonly Meter Meter = new(InfrastructureDiagnosticsNames.DefaultServiceName);

        public static readonly Counter<long> ShutdownErrorCount = Meter.CreateCounter<long>(InfrastructureDiagnosticsNames.ShutdownErrorCount);
        public static readonly Counter<long> OpenConnectionsCount = Meter.CreateCounter<long>(InfrastructureDiagnosticsNames.OpenConnectionsCount);
        public static readonly Counter<long> OpenChannelsCount = Meter.CreateCounter<long>(InfrastructureDiagnosticsNames.OpenChannelsCount);
        public static readonly Counter<long> PublishedCount = Meter.CreateCounter<long>(InfrastructureDiagnosticsNames.PublishedCount);
        public static readonly Counter<long> ConsumedCount = Meter.CreateCounter<long>(InfrastructureDiagnosticsNames.ConsumedCount);
        public static readonly Counter<long> AckCount = Meter.CreateCounter<long>(InfrastructureDiagnosticsNames.AckCount);
        public static readonly Counter<long> NackCount = Meter.CreateCounter<long>(InfrastructureDiagnosticsNames.NackCount);
        public static readonly Counter<long> DeadLetterCount = Meter.CreateCounter<long>(InfrastructureDiagnosticsNames.DeadLetterCount);

        // Note: Using seconds metric unit since it's the best practice for histograms. Even though most operations will be done in ms.
        public static readonly Histogram<double> ConsumedDurationInS = Meter.CreateHistogram<double>(
            InfrastructureDiagnosticsNames.ConsumedDurationInMs, "s", "Time it took to consume an event from a queue in seconds");
        public static readonly Histogram<double> PublishedDurationInS = Meter.CreateHistogram<double>(
            InfrastructureDiagnosticsNames.PublishedDurationInMs, "s", "Time it took to publish an event to an exchange in seconds");
    }

    /// <summary>
    ///  You can see more examples of metrics on other RabbitMQ SDKs:
    /// - https://github.com/rabbitmq/rabbitmq-amqp-dotnet-client/blob/main/RabbitMQ.AMQP.Client/MetricsReporter.cs
    /// - https://www.rabbitmq.com/client-libraries/java-api-guide#metrics 
    /// 
    /// References:
    /// - https://opentelemetry.io/docs/specs/semconv/messaging/messaging-metrics/
    /// - https://opentelemetry.io/docs/specs/semconv/messaging/rabbitmq/
    /// </summary>
    public static class InfrastructureDiagnosticsNames
    {
        // Best practice Note: Use dots '.' in the service name to separate the infrastructure metrics.
        // We can then specify while configuring OpenTelemetry which metrics are enabled, e.g. "GrafanaApp.*"
        public const string DefaultServiceName = "GrafanaApp.Infrastructure";

        public const string Prefix = "app.rabbitmq";
        public const string ShutdownErrorCount = $"{Prefix}.shutdown_errors.count";
        public const string OpenConnectionsCount = $"{Prefix}.connections.count";
        public const string OpenChannelsCount = $"{Prefix}.channels.count";
        public const string PublishedCount = $"{Prefix}.published.count";
        public const string ConsumedCount = $"{Prefix}.consumed.count";
        public const string AckCount = $"{Prefix}.acknowledged_published.count";
        public const string NackCount = $"{Prefix}.not_acknowledged_published.count";

        public const string DeadLetterCount = $"{Prefix}.dead_letter.count";
        public const string ConsumedDurationInMs = $"{Prefix}.consumed.duration_total";
        public const string PublishedDurationInMs = $"{Prefix}.published.duration_total";
    }
}
