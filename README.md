# Observability - Grafana Demo for Talks

This is a simple demo showcasing how we can instrument our applications with OpenTelemetry, using Azure Monitoring + Grafana + Prometheus.
It's intended to be used as the demo of a specific talk about observability.

## Demo app instructions
Read the instructions in `src/README.md`.

## Session Abstracts

### Observability with Azure Managed Grafana

Nowadays, OpenTelemetry is used extensively to collect telemetry data from our applications, and serves as an industry standard. But we need a way to visualize this data in a clear way, and that is where Azure Managed Grafana comes in.

In this session we'll go through the core concepts of observability and demonstrate how we can use Azure Managed Grafana, integrated with Prometheus Grafana Tempo and Loki to gather insights from our telemetry data.
We will cover topics such as the basics about logs, metrics and traces, manual instrumentation, OTLP, and others. We'll talk about other interesting OpenTelemetry signals that can help you troubleshoot errors in production.

By the end of this session, you will know how to use Azure Managed Grafana and OpenTelemetry, in order to get deep insights into your business.

### Exploring GenAI Observability with OpenTelemetry and Grafana

Nowadays, enterprises are building GenAI agents and LLM-powered applications. As these systems become more autonomous and integrate with our infrastructure and other systems, having observability is crucial to understanding how these behave in production.

You will learn how to use OpenTelemetry and Grafana to set up observability of GenAI systems. We'll also cover the current state of GenAI semantic conventions, that keep evolving.

In the final demonstration, we go through three instrumentation approaches and their differences: OpenTelemetry integration w/ Semantic Kernel; OpenLLMetry w/ TypeScript and OpenLit w/ Python.

### Slides
The slides for the talk are in [slides/cnl-meetup-2025-genai-observability-opentelemetry-and-grafana.pdf](slides/cnl-meetup-2025-genai-observability-opentelemetry-and-grafana.pdf). This was for the [Cloud Native Lisbon meetup 2025](https://www.meetup.com/cloudnativelx/events/310967482/).

### Duration
45min

### Target audience
Developers, Architects

## Cool Observability resources
If you're interested in learning more about the observability ecosystem, here are a few links:
- [OpenTelemetry Demo](https://opentelemetry.io/docs/demo/) (or https://github.com/open-telemetry/opentelemetry-demo/tree/main)
- [Prometheus instrumentation](https://prometheus.io/docs/practices/instrumentation/)
- Blog post I wrote about [learning observability](https://dev.to/bolt04/learning-observability-3i37)
