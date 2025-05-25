# Sampling and Cost Optimization Demo

This document explains the sampling and cost optimization features implemented in the Grafana observability demo app.

## Overview

Observability can become expensive at scale. This demo showcases different sampling strategies for:
- **Traces**: Using OpenTelemetry's built-in samplers
- **Logs**: Custom log processor with level-based sampling
- **Metrics**: Metric views for cardinality reduction and cost optimization

## Prometheus SDK vs OpenTelemetry Comparison

### Prometheus SDK Features
- Native Prometheus metrics format
- Direct `/metrics` endpoint exposure
- Lower latency (no OTLP export overhead)
- Prometheus-specific metric types (Counter, Gauge, Histogram, Summary)

### OpenTelemetry Features
- Vendor-agnostic telemetry
- Unified observability (traces, metrics, logs)
- Multiple export formats (OTLP, Prometheus, Console, etc.)
- Built-in resource detection and correlation

### Side-by-Side Demo
Both implementations are used in the `CartService`:
```csharp
// OpenTelemetry metrics
TelemetryDiagnostics.IncrementJobTimerCount();

// Prometheus SDK metrics
PrometheusDiagnostics.IncrementJobTimerCount();
PrometheusDiagnostics.SetQueueSize(_eventQueue.Count);
```

## Sampling Strategies

### 1. Trace Sampling

Configured via environment variables:

```bash
# Sample 25% of traces
export OTEL_TRACES_SAMPLER="traceidratio"
export OTEL_TRACES_SAMPLER_ARG="0.25"

# Parent-based sampling (respects parent trace decisions)
export OTEL_TRACES_SAMPLER="parentbased_traceidratio"
export OTEL_TRACES_SAMPLER_ARG="0.25"
```

**Sampling Options:**
- `always_on`: Sample all traces (default)
- `always_off`: Sample no traces
- `traceidratio`: Sample based on trace ID ratio
- `parentbased_*`: Respect parent sampling decisions

### 2. Log Sampling

Custom `LogSamplingProcessor` that samples based on log level:

```csharp
// Production sampling rates
Debug: 1%      // High-volume, low-priority logs
Info: 10%      // Moderate sampling for general information
Warning: 50%   // Higher retention for potential issues
Error: 100%    // Always capture errors
Critical: 100% // Always capture critical logs
```

**Configuration Presets:**
- **Development**: 100% of all logs
- **Medium Volume**: 5% debug, 30% info, 70% warning, 100% error
- **High Volume/Low Cost**: 1% debug, 10% info, 50% warning, 100% error

### 3. Metric Sampling and Optimization

**Metric Views for Cost Reduction:**
```csharp
// Reduce histogram bucket counts
new MetricView(
    instrumentName: "http.server.duration",
    aggregation: new ExplicitBucketHistogramAggregation(customBuckets)
)

// Drop expensive high-cardinality metrics
new MetricView(
    instrumentName: "process.runtime.dotnet.gc.*",
    aggregation: Aggregation.Drop()
)
```

**Cost Optimization Strategies:**
- Reduce histogram buckets to essential percentiles
- Drop high-cardinality runtime metrics
- Use last-value aggregation for gauge-like metrics
- Filter metrics by business value

## Demo Endpoints

### `/weatherforecast`
Basic endpoint demonstrating:
- OpenTelemetry activity creation
- Structured logging with correlation IDs
- Both OTel and Prometheus metric collection

### `/metrics-demo`
Showcases metric differences:
- Increments both OpenTelemetry and Prometheus counters
- Updates queue size gauge (Prometheus only)
- Demonstrates metric correlation

### `/sampling-demo`
Log sampling demonstration:
- Generates logs at all levels (Debug → Error)
- Creates multiple logs to observe sampling effects
- Shows sampling rates in different environments

### `/metrics`
Prometheus metrics endpoint:
- Exposes both Prometheus SDK and OpenTelemetry metrics
- Different metric formats and naming conventions
- Compare metric types and metadata

## Environment Configuration

### Development
```json
{
  "EnableLogSampling": false,
  "SamplingConfig": {
    "Environment": "Development",
    "TraceSampleRate": 1.0,
    "LogSampling": {
      "Debug": 1.0,
      "Information": 1.0,
      "Warning": 1.0,
      "Error": 1.0
    }
  }
}
```

### Production
```json
{
  "EnableLogSampling": true,
  "SamplingConfig": {
    "Environment": "Production",
    "TraceSampleRate": 0.25,
    "LogSampling": {
      "Debug": 0.01,
      "Information": 0.1,
      "Warning": 0.5,
      "Error": 1.0
    }
  }
}
```

## Usage Examples

### Run with Different Sampling Configurations

```bash
# Development (no sampling)
./demo-sampling.sh development

# Staging (medium sampling)
./demo-sampling.sh staging

# Production (aggressive sampling)
./demo-sampling.sh production
```

### Manual Environment Variables

```bash
# Set trace sampling
export OTEL_TRACES_SAMPLER="traceidratio"
export OTEL_TRACES_SAMPLER_ARG="0.1"  # 10% sampling

# Set environment
export ASPNETCORE_ENVIRONMENT="Production"

# Run application
dotnet run
```

### Test Sampling

```bash
# Generate logs to test sampling
curl http://localhost:5000/sampling-demo

# Generate metrics
curl http://localhost:5000/metrics-demo

# View Prometheus metrics
curl http://localhost:5000/metrics
```

## Cost Impact Analysis

### High-Volume Production (1M requests/day)

**Without Sampling:**
- Traces: 1M spans/day
- Logs: 10M log entries/day
- Metrics: 100K metric samples/day

**With Sampling (25% traces, smart log sampling):**
- Traces: 250K spans/day (75% reduction)
- Logs: ~1.5M log entries/day (85% reduction)
- Metrics: Optimized cardinality (50% reduction)

**Estimated Cost Savings:** 60-80% reduction in ingestion costs

## Best Practices

1. **Start with high sampling rates** in development
2. **Gradually reduce sampling** as you move to production
3. **Always sample errors and critical events**
4. **Monitor sampling effectiveness** with business metrics
5. **Use head-based sampling** for deterministic results
6. **Consider tail-based sampling** for complex scenarios
7. **Regularly review and adjust** sampling rates based on costs and value

## Next Steps

1. Implement **tail-based sampling** for more intelligent trace selection
2. Add **custom metric processors** for advanced filtering
3. Integrate with **OpenTelemetry Collector** for centralized sampling
4. Implement **adaptive sampling** based on system load
5. Add **business-value-based sampling** using custom processors
