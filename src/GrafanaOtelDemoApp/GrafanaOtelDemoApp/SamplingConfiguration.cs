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
                    // Mark as not to be exported by clearing the data
                    return;
                }

                base.OnEnd(data);
            }
        }

        /// <summary>
        /// Custom metric reader that can filter and sample metrics
        /// </summary>
        public class MetricSamplingReader : BaseExportingMetricReader
        {
            private readonly BaseExportingMetricReader _baseReader;
            private readonly HashSet<string> _allowedMetrics;
            private readonly double _sampleRate;
            private readonly Random _random = new();

            public MetricSamplingReader(
                BaseExportingMetricReader baseReader, 
                double sampleRate = 1.0,
                HashSet<string>? allowedMetrics = null)
            {
                _baseReader = baseReader;
                _sampleRate = sampleRate;
                _allowedMetrics = allowedMetrics ?? new HashSet<string>();
            }

            protected override bool OnCollect(int timeoutMilliseconds)
            {
                // Sample based on rate
                if (_random.NextDouble() > _sampleRate)
                {
                    return true; // Skip this collection cycle
                }

                return _baseReader.Collect(timeoutMilliseconds);
            }

            protected override bool OnShutdown(int timeoutMilliseconds)
            {
                return _baseReader.Shutdown(timeoutMilliseconds);
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
        /// Get metric views for reducing cardinality and controlling costs
        /// </summary>
        public static MetricView[] GetCostOptimizedMetricViews()
        {
            return new MetricView[]
            {
                // Reduce histogram buckets for request duration
                new MetricView(
                    instrumentName: "http.server.duration",
                    aggregation: new ExplicitBucketHistogramAggregation(new double[] { 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1.0, 2.5, 5.0, 7.5, 10.0 })
                ),
                
                // Drop high-cardinality metrics that might be expensive
                new MetricView(
                    instrumentName: "process.runtime.dotnet.gc.*",
                    aggregation: Aggregation.Drop() // Drop all GC metrics to save costs
                ),
                
                // Reduce cardinality by aggregating similar metrics
                new MetricView(
                    instrumentName: "system.cpu.utilization",
                    aggregation: new LastValueAggregation()
                )
            };
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
