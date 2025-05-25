using GrafanaOtelDemoApp;
using GrafanaOtelDemoApp.Application;
using GrafanaOtelDemoApp.Infrastructure;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Note: To change demo choose another OtelOption.
builder.AddObservability(builder.Configuration, OtelOption.Grafana);

builder.Services.AddSingleton<CartService>();
builder.Services.AddSingleton<IEventBusGateway, RabbitMqGateway>();
builder.Services.AddHostedService<IntegrationHostedService>();

var app = builder.Build();

// Enable Prometheus metrics endpoint for prometheus-net SDK
app.UseMetricServer(); // This exposes /metrics endpoint for Prometheus SDK

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (ILogger<Program> log, CartService cartService) =>
{
    log.LogInformation("Entered /weatherforecast");
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

    cartService.AddItem();
    cartService.CreateAPITimerIntegration();

    return forecast;
});

app.MapGet("/metrics-demo", (ILogger<Program> log, CartService cartService) =>
{
    log.LogInformation("Demonstrating both OpenTelemetry and Prometheus metrics");
    
    // Add multiple items to showcase different metrics
    for (int i = 0; i < 3; i++)
    {
        cartService.AddItem();
    }
    
    // Increment both OpenTelemetry and Prometheus counters
    TelemetryDiagnostics.IncrementJobTimerCount();
    PrometheusDiagnostics.IncrementJobTimerCount();
    
    return new { 
        message = "Metrics updated! Check /metrics for Prometheus metrics and OTLP export for OpenTelemetry metrics",
        openTelemetryEndpoint = "/metrics", // OTel Prometheus exporter
        prometheusEndpoint = "/metrics"     // Native Prometheus endpoint (same path but different implementation)
    };
});

app.MapGet("/sampling-demo", (ILogger<Program> log) =>
{
    // Generate logs at different levels to demonstrate sampling
    log.LogDebug("Debug message - should be heavily sampled in production");
    log.LogInformation("Information message - moderately sampled in production");
    log.LogWarning("Warning message - lightly sampled in production");
    log.LogError("Error message - always captured in production");
    
    // Generate multiple logs to see sampling in action
    for (int i = 0; i < 10; i++)
    {
        log.LogDebug("Debug log #{LogNumber}", i);
        log.LogInformation("Info log #{LogNumber}", i);
    }
    
    for (int i = 0; i < 5; i++)
    {
        log.LogWarning("Warning log #{LogNumber}", i);
    }
    
    log.LogError("Error log - this should always appear");
    
    return new { 
        message = "Generated logs at different levels. Check console output to see sampling in action.",
        note = "Set EnableLogSampling=true in configuration to see sampling effects",
        environmentVariables = new {
            tracesSampler = "Set OTEL_TRACES_SAMPLER=traceidratio",
            tracesSamplerArg = "Set OTEL_TRACES_SAMPLER_ARG=0.25"
        }
    };
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
