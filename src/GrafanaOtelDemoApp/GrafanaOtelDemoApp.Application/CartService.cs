using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Mime;

namespace GrafanaOtelDemoApp.Application
{
    public class CartService(ILogger<CartService> _logger, IEventBusGateway _eventBusGateway)
    {
        private const int DUE_TIME = 2;
        private const int INTEGRATION_PERIOD = 1;
        private List<string> _eventQueue = [];
        private Timer? _APIIntegrationTimer;
        private Timer? _HostedServiceIntegrationTimer;

        public Task AddItem()
        {
            _eventQueue.Add("item");
            // Update Prometheus gauge with current queue size
            PrometheusDiagnostics.SetQueueSize(_eventQueue.Count);
            return Task.CompletedTask;
        }

        public void CreateAPITimerIntegration()
        {
            _APIIntegrationTimer = new Timer(
                async (state) => await OnTimerCartIntegrationAsync(),
                null,
                new TimeSpan(0, 0, DUE_TIME),
                new TimeSpan(0, INTEGRATION_PERIOD, 0)
            );
        }

        public void CreateTimerIntegration()
        {
            _HostedServiceIntegrationTimer = new Timer(
                async (state) => await OnTimerCartIntegrationAsync(),
                null,
                new TimeSpan(0, 0, DUE_TIME),
                new TimeSpan(0, INTEGRATION_PERIOD, 0)
            );
        }

        private async Task OnTimerCartIntegrationAsync()
        {
            var sw = Stopwatch.StartNew();
            var integrationId = Guid.NewGuid();

            // Note: This activity will have a parentId when called by the API endpoint, since ASP.NET starts an activity by default.
            // In case this timer is created in a Hosted Service that runs in the background, that is not the case.
            using var activity = TelemetryDiagnostics.Source.StartActivity("OnTimerCartIntegrationAsync");
            activity?.SetTag(DiagnosticsNames.IntegrationIdLabel, integrationId);
            using var _ = _logger.BeginScope(new Dictionary<string, object> { [DiagnosticsNames.IntegrationIdLabel] = integrationId });

            // Prometheus metrics - time the entire operation
            using var prometheusTimer = PrometheusDiagnostics.TimeRequest();

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://jsonplaceholder.typicode.com/todos/1"),
                    Headers =
                    {
                        { "accept", MediaTypeNames.Application.Json },
                    },
                };
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(body);
                }

                _logger.LogInformation("CartService.OnTimerCartIntegrationAsync Processing...");
                await SimulateWork();

                sw.Stop();
                _logger.IntegrationCompleted(_eventQueue.Count, sw.Elapsed);
                
                // Both OpenTelemetry and Prometheus metrics
                TelemetryDiagnostics.IncrementJobTimerCount();
                PrometheusDiagnostics.IncrementJobTimerCount();
                PrometheusDiagnostics.ObserveResponseTime(sw.Elapsed.TotalSeconds);

                await _eventBusGateway.PublishEvent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CartService.OnTimerCartIntegrationAsync: Error in integrationId: [{integrationId}]", integrationId);
                // Note: We use AddException since RecordException is obsolete
                // Docs about exceptions in a trace: https://opentelemetry.io/docs/specs/otel/trace/exceptions/
                activity?.AddException(ex);
            }
        }

        private async Task SimulateWork()
        {
            await Task.Delay(1000);
        }
    }
}
