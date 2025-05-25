using Prometheus;

namespace GrafanaOtelDemoApp.Application
{
    /// <summary>
    /// Prometheus SDK diagnostics to showcase difference with OpenTelemetry metrics
    /// </summary>
    public static class PrometheusDiagnostics
    {
        // Counter - tracks the number of job timer executions
        public static readonly Counter JobTimerCount = Metrics
            .CreateCounter("prometheus_job_timer_count_total", "Number of times the job timer executed using Prometheus SDK");

        // Gauge - tracks the current queue size
        public static readonly Gauge QueueSize = Metrics
            .CreateGauge("prometheus_queue_size", "Current size of the event queue using Prometheus SDK");

        // Histogram - tracks request duration
        public static readonly Histogram RequestDuration = Metrics
            .CreateHistogram("prometheus_request_duration_seconds", "Duration of requests in seconds using Prometheus SDK");

        // Summary - tracks response times with quantiles
        public static readonly Summary ResponseTime = Metrics
            .CreateSummary("prometheus_response_time_seconds", "Response time in seconds using Prometheus SDK");

        public static void IncrementJobTimerCount() => JobTimerCount.Inc();
        
        public static void SetQueueSize(int size) => QueueSize.Set(size);
        
        public static IDisposable TimeRequest() => RequestDuration.NewTimer();
        
        public static void ObserveResponseTime(double seconds) => ResponseTime.Observe(seconds);
    }
}
