using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GrafanaOtelDemoApp.Application;

/// <summary>
/// Example class using <see cref="IMeterFactory"/> in a DI app (recommended since we can easily test our code this way).
/// </summary>
public class TelemetryDiagnosticsDI
{
    public readonly static ActivitySource Source = new(DiagnosticsNames.DefaultServiceName);

    private readonly Counter<long> _jobTimerCount;

    public TelemetryDiagnosticsDI(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(DiagnosticsNames.DefaultServiceName);

        _jobTimerCount = meter.CreateCounter<long>(
            DiagnosticsNames.JobTimerCount,
            description: "Number of times the job timer executed.");
    }

    public void IncrementJobTimerCount() => _jobTimerCount.Add(1);
}
