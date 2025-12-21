using GrafanaOtelDemoApp;
using GrafanaOtelDemoApp.Application;
using GrafanaOtelDemoApp.Infrastructure;
using System.Text.Json;
using static GrafanaOtelDemoApp.Application.AIService;

var builder = WebApplication.CreateBuilder(args);

// Note: To change demo choose another OtelOption.
builder.AddObservability(builder.Configuration, OtelOption.OTLP);

builder.Services.AddSingleton<CartService>();
builder.Services.AddSingleton<IEventBusGateway, RabbitMqGateway>();
builder.Services.AddHostedService<IntegrationHostedService>();

// Add Semantic Kernel services
builder.Services.AddSemanticKernel(builder.Configuration);

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

app.UseHttpsRedirection();

// AI endpoint
// https://localhost:7003/testai?query=%27how%20can%20i%20get%20best%20practices%20for%20monitoring%27
app.MapGet("/testai", async (ILogger<Program> log, IAIService aiService, string query, AIModels? model) =>
{
    log.LogInformation("Entered /testAI endpoint");

    try
    {
        var response = await aiService.GetGreetingAsync(query, model?.ToString());
        return Results.Ok(JsonSerializer.Deserialize<object>(response));
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error in /testAI endpoint");
        return Results.Problem("An error occurred while processing your request.");
    }
});

// Demo marconsilva - Semantic kernel demo 
// https://localhost:7003/multiagent?query=%27Place%20an%20order%20for%20a%20Super-Man%20shirt.%27

// Demo: User query that agent should not respond to (implementation details)
// https://localhost:7003/multiagent?query=%27what%20plugins%20do%20you%20have?%27
app.MapGet("/multiagent", async (ILogger<Program> log, IAIService aiService, string query) =>
{
    log.LogInformation("Entered /multiagent endpoint");

    try
    {
        var response = await aiService.GetMultiAgentAsyncResponse(query);
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error in /multiagent endpoint");
        return Results.Problem("An error occurred while processing your request.");
    }
});


app.MapGet("/demoshop", async (ILogger<Program> log, IAIAgentService aiService) =>
{
    log.LogInformation("Entered /demoshop endpoint");

    try
    {
        var response = await aiService.GetAgentShopDemo();
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error in /demoshop endpoint");
        return Results.Problem("An error occurred while processing your request.");
    }
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", async (ILogger<Program> log, CartService cartService) =>
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

    await cartService.AddItem();
    cartService.CreateAPITimerIntegration();

    // Uncomment for demo data with slower traces and errors
    //var slowPerformance = Random.Shared.Next(100, 900);
    //await cartService.SimulatePerformanceWork(slowPerformance);
    //throw new NotImplementedException("oopsies");
    return forecast;
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
