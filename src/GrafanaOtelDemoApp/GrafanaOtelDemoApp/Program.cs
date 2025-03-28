using GrafanaOtelDemoApp;
using GrafanaOtelDemoApp.Application;
using GrafanaOtelDemoApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Note: To change demo choose another OtelOption.
builder.AddObservability(builder.Configuration, OtelOption.Grafana);

builder.Services.AddSingleton<CartService>();
builder.Services.AddSingleton<IEventBusGateway, RabbitMqGateway>();
builder.Services.AddHostedService<IntegrationHostedService>();

var app = builder.Build();

//TODO:app.MapPrometheusScrapingEndpoint();

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

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
