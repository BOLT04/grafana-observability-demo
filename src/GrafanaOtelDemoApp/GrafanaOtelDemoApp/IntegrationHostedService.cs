using GrafanaOtelDemoApp.Application;

namespace GrafanaOtelDemoApp
{
    public class IntegrationHostedService(
        ILogger<IntegrationHostedService> _logger,
        CartService _cartService,
        IEventBusGateway _eventBusGateway) : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cartService.CreateTimerIntegration();
            _eventBusGateway.CreateGenerateCountersTimer();
            _logger.LogInformation("IntegrationHostedService.ExecuteAsync Created timers successfully.");

            return Task.CompletedTask;
        }
    }
}
