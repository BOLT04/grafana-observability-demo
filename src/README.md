# Grafana Demo App

To get this repo running you'll need a few setup steps outlined below.

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
