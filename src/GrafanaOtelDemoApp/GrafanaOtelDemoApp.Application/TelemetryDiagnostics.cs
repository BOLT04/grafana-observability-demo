using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GrafanaOtelDemoApp.Application
{
    public class TelemetryDiagnostics
    {
        public readonly static ActivitySource Source = new(DiagnosticsNames.DefaultServiceName);
        public readonly static Meter Meter = new(DiagnosticsNames.DefaultServiceName);

        public static Counter<long> JobTimerCount = Meter.CreateCounter<long>(
            DiagnosticsNames.JobTimerCount,
            description: "Number of times the job timer executed.");

        // Example using env variables to define the service name of custom traces and metrics (with Otlp:ServiceName)
        //public static void Init(IConfiguration configuration)
        //{
        //    var serviceName = configuration["Otlp:ServiceName"] ?? DiagnosticsNames.DefaultServiceName;
        //    Source = new(serviceName);
        //    Meter = new(serviceName);

        //JobTimerCount = Meter.CreateCounter<long>(
        //    DiagnosticsNames.JobTimerCount,
        //    description: "Number of times the job timer executed.");
        //}

        public static void IncrementJobTimerCount() => JobTimerCount.Add(1);
    }

    public static class DiagnosticsNames
    {
        public const string DefaultServiceName = "GrafanaApp";
        public const string JobTimerCount = "app.job_timer.count";
        public const string IntegrationIdLabel = "app.integration_id";
    }
}
