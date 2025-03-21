# Grafana Demo App

This is a demo app instrumented using the `System.Diagnostics.DiagnosticSource` package and OpenTelemetry APIs when necessary. This way we instrument our application layer without the need of third-party APIs.
**Note**: For instrumenting metrics you could choose the well known [Prometheus library](https://github.com/prometheus-net/prometheus-net) too.

To get this repo running you'll need a few setup steps outlined below:

## Setup
```
dotnet build
dotnet run --launch-profile http
```

### Azure Prerequisites
Specify an Azure account for the Managed Grafana and Prometheus setup and to configure your subscription information following the [Configuration section in Local Azure provisioning documentation](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azure/local-provisioning#configuration).
TODO

#### Cool Observability resources
If you're interested in learning more about the observability ecosystem, here are a few links:
- https://opentelemetry.io/docs/demo/
- https://prometheus.io/docs/practices/instrumentation/
