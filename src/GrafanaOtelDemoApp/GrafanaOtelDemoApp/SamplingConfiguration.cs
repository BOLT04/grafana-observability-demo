using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace GrafanaOtelDemoApp
{
    /// <summary>
    /// Configuration for sampling logs, traces, and metrics to reduce costs
    /// </summary>
    public static class SamplingConfiguration
    {
        /// <summary>
        /// Custom log processor that samples logs based on level and content
        /// </summary>
        public class LogSamplingProcessor : BaseProcessor<LogRecord>
        {
            private readonly Random _random = new();
            private readonly double _debugSampleRate;
            private readonly double _infoSampleRate;
            private readonly double _warningSampleRate;
            private readonly double _errorSampleRate;

            public LogSamplingProcessor(
                double debugSampleRate = 0.1,    // Sample 10% of debug logs
                double infoSampleRate = 0.5,     // Sample 50% of info logs
                double warningSampleRate = 0.8,  // Sample 80% of warning logs
                double errorSampleRate = 1.0)    // Sample 100% of error logs
            {
                _debugSampleRate = debugSampleRate;
                _infoSampleRate = infoSampleRate;
                _warningSampleRate = warningSampleRate;
                _errorSampleRate = errorSampleRate;
            }

            public override void OnEnd(LogRecord data)
            {
                var sampleRate = data.LogLevel switch
                {
                    LogLevel.Debug => _debugSampleRate,
                    LogLevel.Information => _infoSampleRate,
                    LogLevel.Warning => _warningSampleRate,
                    LogLevel.Error => _errorSampleRate,
                    LogLevel.Critical => 1.0, // Always sample critical logs
                    _ => 1.0
                };

                // If we should drop this log based on sampling
                if (_random.NextDouble() > sampleRate)
                {
                    // Skip processing this log record
                    return;
                }

                base.OnEnd(data);
            }

            public bool ShouldSample(LogLevel logLevel)
            {
                var sampleRate = logLevel switch
                {
                    LogLevel.Debug => _debugSampleRate,
                    LogLevel.Information => _infoSampleRate,
                    LogLevel.Warning => _warningSampleRate,
                    LogLevel.Error => _errorSampleRate,
                    LogLevel.Critical => 1.0, // Always sample critical logs
                    _ => 1.0
                };

                return _random.NextDouble() <= sampleRate;
            }
        }

        /// <summary>
        /// Configure trace sampling using environment variables or programmatic settings
        /// </summary>
        public static void ConfigureTraceSampling()
        {
            // These can be set via environment variables:
            // export OTEL_TRACES_SAMPLER="traceidratio"
            // export OTEL_TRACES_SAMPLER_ARG="0.25"
            // or
            // export OTEL_TRACES_SAMPLER="parentbased_traceidratio"
            // export OTEL_TRACES_SAMPLER_ARG="0.25"
            
            var sampler = Environment.GetEnvironmentVariable("OTEL_TRACES_SAMPLER");
            var samplerArg = Environment.GetEnvironmentVariable("OTEL_TRACES_SAMPLER_ARG");
            
            if (!string.IsNullOrEmpty(sampler) && !string.IsNullOrEmpty(samplerArg))
            {
                Console.WriteLine($"Using trace sampler: {sampler} with argument: {samplerArg}");
            }
            else
            {
                Console.WriteLine("No trace sampling configured. Using default sampling (all traces).");
                Console.WriteLine("To enable trace sampling, set environment variables:");
                Console.WriteLine("  OTEL_TRACES_SAMPLER=traceidratio");
                Console.WriteLine("  OTEL_TRACES_SAMPLER_ARG=0.25");
            }
        }

        /// <summary>
        /// Configure metric cost optimization strategies (informational)
        /// </summary>
        public static void ConfigureCostOptimizedMetrics()
        {
            // This method demonstrates the concept of metric optimization
            Console.WriteLine("Metric cost optimization strategies:");
            Console.WriteLine("- Reduce histogram buckets for request duration");
            Console.WriteLine("- Drop high-cardinality metrics like GC details");
            Console.WriteLine("- Use last-value aggregation for gauge-like metrics");
            Console.WriteLine("- Filter metrics by business value");
        }

        /// <summary>
        /// Configuration for different sampling strategies
        /// </summary>
        public static class Presets
        {
            public static (double debug, double info, double warning, double error) HighVolumeLowCost => (0.01, 0.1, 0.5, 1.0);
            public static (double debug, double info, double warning, double error) MediumVolumeMediumCost => (0.05, 0.3, 0.7, 1.0);
            public static (double debug, double info, double warning, double error) LowVolumeHighFidelity => (0.2, 0.8, 1.0, 1.0);
            public static (double debug, double info, double warning, double error) Development => (1.0, 1.0, 1.0, 1.0);
        }
    }
}
