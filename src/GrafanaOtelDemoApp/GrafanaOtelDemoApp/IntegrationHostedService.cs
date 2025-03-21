using GrafanaOtelDemoApp.Application;
using GrafanaOtelDemoApp.Infrastructure;

namespace GrafanaOtelDemoApp
{
    public class IntegrationHostedService(ILogger<IntegrationHostedService> logger, CartService cartService, RabbitMqGateway rabbitMqGateway) : BackgroundService
    {
        private readonly ILogger<IntegrationHostedService> _logger = logger;
        private readonly CartService _cartService = cartService;
        private readonly RabbitMqGateway _rabbitMqGateway = rabbitMqGateway;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cartService.CreateTimerIntegration();
            _rabbitMqGateway.CreateGenerateCountersTimer();
            return Task.CompletedTask;
        }
    }
}
