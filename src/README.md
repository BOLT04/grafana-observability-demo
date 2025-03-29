# Grafana Demo App

This is a demo app instrumented using the `System.Diagnostics.DiagnosticSource` package and OpenTelemetry APIs when necessary. This way we instrument our application layer without the need for third-party APIs.
**Note**: For instrumenting metrics, you could choose the well-known [Prometheus library](https://github.com/prometheus-net/prometheus-net) too.

## Setup
To get this application running you'll need a few setup steps outlined below:

### Azure Prerequisites
Specify an Azure account for the Managed Grafana and Prometheus setup and to configure your subscription information following the [Configuration section in Local Azure provisioning documentation](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azure/local-provisioning#configuration).

### Grafana Cloud
Check the docs on using the [Grafana Cloud OTLP endpoint](https://grafana.com/docs/grafana-cloud/send-data/otlp/send-data-otlp/) and create your Grafana Cloud account. The env variables you need are these:
```json
{
"OTEL_SERVICE_NAME": "GrafanaApp",
"OTEL_EXPORTER_OTLP_PROTOCOL": "http/protobuf",
"OTEL_EXPORTER_OTLP_ENDPOINT": "https://<grafana-otlp-domain>/otlp",
"OTEL_EXPORTER_OTLP_HEADERS": "Authorization=Basic <your token>"
}
```

### Run the application
```
dotnet build
dotnet run --launch-profile http
```

### Run Unit Tests
```
dotnet test
```

### Grafana Dashboards

Here are the Grafana dashboards for the Demo (you can add them [by ID](https://grafana.com/docs/grafana/latest/dashboards/build-dashboards/import-dashboards/) or JSON)

- [RED metrics - MLT](https://github.com/grafana/intro-to-mltp/blob/main/grafana/definitions/mlt.json)
- [ASP.NET Otel Metrics](https://grafana.com/grafana/dashboards/17706-asp-net-otel-metrics/)
- [Azure infrastructure](https://grafana.com/grafana/dashboards/21257-azure-infrastructure-apps-monitoring/)
