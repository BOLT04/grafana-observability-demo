using GrafanaOtelDemoApp.Application;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text;

namespace GrafanaOtelDemoApp.Infrastructure
{
    public class RabbitMqGateway(ILogger<RabbitMqGateway> logger) : IEventBusGateway
    {
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
        private ILogger<RabbitMqGateway> _logger = logger;

        public void CreateGenerateCountersTimer()
        {
            _ = new Timer(
                async (state) => await OnTimerGenerateCounters(),
                null,
                new TimeSpan(0, 0, 1),
                new TimeSpan(0, 1, 0)
            );
        }

        public Task OnTimerGenerateCounters()
        {
            TelemetryDiagnosticsInfra.ShutdownErrorCount.Add(1);
            TelemetryDiagnosticsInfra.AckCount.Add(10);
            TelemetryDiagnosticsInfra.NackCount.Add(1);
            TelemetryDiagnosticsInfra.OpenConnectionsCount.Add(1);
            TelemetryDiagnosticsInfra.OpenChannelsCount.Add(1);
            TelemetryDiagnosticsInfra.ConsumedCount.Add(2);
            TelemetryDiagnosticsInfra.PublishedCount.Add(1);
            TelemetryDiagnosticsInfra.DeadLetterCount.Add(1);
            TelemetryDiagnosticsInfra.OpenChannelsCount.Add(1);
            _logger.LogInformation("RabbitMqGateway: Incremented counters.");
            return Task.CompletedTask;
        }

        public async Task PublishEvent()
        {
            var sw = Stopwatch.StartNew();
            Activity? activity = null;
            try
            {
                var exchangeToSend = "mock.exchange";

                activity = TelemetryDiagnosticsInfra.Source.StartActivity($"{exchangeToSend} create", ActivityKind.Producer);

                // Depending on Sampling (and whether a listener is registered or not), the
                // activity above may not be created.
                // If it is created, then propagate its context.
                // If it is not created, the propagate the Current context,
                // if any.
                ActivityContext contextToInject = default;
                if (activity != null)
                {
                    contextToInject = activity.Context;
                }
                else if (Activity.Current != null)
                {
                    contextToInject = Activity.Current.Context;
                }
                IBasicProperties propertiesMock = new BasicProperties
                {
                    DeliveryMode = DeliveryModes.Persistent
                };

                // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
                Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), propertiesMock, this.InjectTraceContextIntoBasicProperties);

                AddMessagingTags(activity, "exchange", exchangeToSend);

                await SimulatePubSubWork();
                _logger.LogInformation("RabbitMqGateway.PublishEvent: Sent event successfully.");

                sw.Stop();
                TelemetryDiagnosticsInfra.PublishedDurationInS.Record(sw.Elapsed.Seconds, new TagList() { { "exchange", exchangeToSend } });
                TelemetryDiagnosticsInfra.PublishedCount.Add(1, new TagList() { { "exchange", exchangeToSend } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMqGateway.PublishEvent: Error while publishing event.");

                // Note: We use AddException since RecordException is obsolete
                // Docs about exceptions in a trace: https://opentelemetry.io/docs/specs/otel/trace/exceptions/
                activity?.AddException(ex);
            }
            finally
            {
                activity?.Dispose();
            }
        }

        public async Task ConsumeEvent()
        {
            var sw = Stopwatch.StartNew();
            Activity? activity = null;
            try
            {
                var queue = "mock.queue";

                // This properties object usually comes from the RabbitMQ message we are consuming.
                IBasicProperties propertiesMock = new BasicProperties
                {
                    DeliveryMode = DeliveryModes.Persistent
                };

                // Extract the PropagationContext of the upstream parent from the message headers.
                var parentContext = Propagator.Extract(default, propertiesMock, ExtractTraceContextFromBasicProperties);
                Baggage.Current = parentContext.Baggage;
                activity = TelemetryDiagnosticsInfra.Source.StartActivity($"{queue} receive", ActivityKind.Consumer, parentContext.ActivityContext);

                AddMessagingTags(activity, "queue", queue);

                await SimulatePubSubWork();
                _logger.LogInformation("RabbitMqGateway.ConsumeEvent: Consumed event successfully.");

                sw.Stop();
                TelemetryDiagnosticsInfra.ConsumedDurationInS.Record(sw.Elapsed.Seconds, new TagList() { { "queue", queue } });
                TelemetryDiagnosticsInfra.ConsumedCount.Add(1, new TagList() { { "queue", queue } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMqGateway.ConsumeEvent: Error while consuming event.");

                // Note: We use AddException since RecordException is obsolete
                // Docs about exceptions in a trace: https://opentelemetry.io/docs/specs/otel/trace/exceptions/
                activity?.AddException(ex);
            }
            finally
            {
                activity?.Dispose();
            }
        }

        private static async Task SimulatePubSubWork()
        {
            int randomNumber = new Random().Next(50, 500);
            await Task.Delay(1000 + randomNumber);
        }

        private void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
        {
            try
            {
                props.Headers ??= new Dictionary<string, object?>();
                props.Headers[key] = value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMqGateway: Failed to inject trace context. Continuing.");
            }
        }

        private IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties? props, string key)
        {
            try
            {
                if (props?.Headers?.TryGetValue(key, out var value) ?? false)
                {
                    if (value is byte[] bytes)
                    {
                        return [Encoding.UTF8.GetString(bytes)];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMqGateway: Failed to extract trace context. Continuing with empty list.");
            }

            return [];
        }

        /// <summary>
        /// Add tags according to RabbitMQ semantic conventions of the OpenTelemetry messaging specification.
        /// <see cref="https://opentelemetry.io/docs/specs/semconv/messaging/rabbitmq/"/>
        /// </summary>
        private static void AddMessagingTags(Activity? activity, string kind, string destination)
        {
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination_kind", kind);
            activity?.SetTag("messaging.destination.name", destination);
        }
    }
}
