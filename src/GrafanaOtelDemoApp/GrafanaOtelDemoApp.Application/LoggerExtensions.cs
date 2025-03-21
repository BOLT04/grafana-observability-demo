using Microsoft.Extensions.Logging;

namespace GrafanaOtelDemoApp.Application
{
    internal static partial class CartServiceLoggerExtensions
    {
        [LoggerMessage(LogLevel.Information, "CartService.OnTimerCartIntegrationAsync Completed {count} in {elapsed}")]
        public static partial void IntegrationCompleted(this ILogger logger, int count, TimeSpan elapsed);
    }

    internal static partial class LoggerExtensions
    {
        [LoggerMessage(LogLevel.Information, "Start")]
        public static partial void StartAp(this ILogger logger);
    }
}
