# Observability - Grafana Demo for Talks

This is a simple demo showcasing how we can instrument our applications with OpenTelemetry, using Azure Monitoring + Grafana + Prometheus.
It's intended to be used as the demo of a specific talk about observability.

## Session Abstract
Nowadays OpenTelemetry is used extensively to collect telemetry data from our applications, and serves as an industry standard. But we need a way to visualize this data in a clear way, and that is where Azure Managed Grafana comes in.

In this session we'll go through the core concepts of observability and demonstrate how we can use Azure Managed Grafana, integrated with Prometheus Grafana Tempo and Loki to gather insights from our telemetry data.
We will cover topics such as the basics about logs, metrics and traces, manual instrumentation, OTLP, and others. We'll talk about other interesting OpenTelemetry signals that can help you troubleshoot errors in production.

By the end of this session, you will know how to use Azure Managed Grafana and OpenTelemetry, in order to get deep insights into your business.

### Slides
The slides for the talk are hosted here: TODO ......

### Duration
45min

### Target audience
Developers, Architects

## Setup
TODO
To get this repo running you'll need a few setup steps outlined below.

### Azure Prerequisites
Specify an Azure account for the Managed Grafana and Prometheus setup and to configure your subscription information following the [Configuration section in Local Azure provisioning documentation](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azure/local-provisioning#configuration).
TODO

#### Cool Observability resources
If you're interested in learning more about the observability ecosystem, here are a few links:
- https://opentelemetry.io/docs/demo/
- https://prometheus.io/docs/practices/instrumentation/
