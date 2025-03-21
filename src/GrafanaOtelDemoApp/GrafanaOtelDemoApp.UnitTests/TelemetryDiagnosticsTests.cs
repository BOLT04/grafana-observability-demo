using GrafanaOtelDemoApp.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using System.Diagnostics.Metrics;

namespace GrafanaOtelDemoApp.UnitTests
{
    public class TelemetryDiagnosticsTests
    {
        [Test]
        public void ShouldIncrementUsedDefaultsCounter()
        {
            // Arrange
            var services = CreateServiceProvider();
            var metrics = services.GetRequiredService<TelemetryDiagnosticsDI>();
            var meterFactory = services.GetRequiredService<IMeterFactory>();
            var collector = new MetricCollector<long>(meterFactory, DiagnosticsNames.DefaultServiceName, DiagnosticsNames.JobTimerCount);

            // Act
            metrics.IncrementJobTimerCount();

            // Assert
            var measurements = collector.GetMeasurementSnapshot();
            Assert.That(measurements.Count, Is.EqualTo(1));
            Assert.That(measurements[0].Value, Is.EqualTo(1));
        }


        private static IServiceProvider CreateServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMetrics();
            serviceCollection.AddSingleton<TelemetryDiagnosticsDI>();
            return serviceCollection.BuildServiceProvider();
        }
    }
}
