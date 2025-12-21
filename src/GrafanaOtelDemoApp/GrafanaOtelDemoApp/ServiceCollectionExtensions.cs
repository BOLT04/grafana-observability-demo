using GrafanaOtelDemoApp.Application;
using GrafanaOtelDemoApp.Application.Plugins;
using Microsoft.SemanticKernel;
using OpenTelemetry.Logs;
using static GrafanaOtelDemoApp.Application.AIService;

namespace GrafanaOtelDemoApp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSemanticKernel(this IServiceCollection services, IConfiguration configuration)
    {
        // Support two models for the demo
        var azureEndpoint = configuration["AzureOpenAI:o4mini:Endpoint"];
        var azureDeploymentName = configuration["AzureOpenAI:o4mini:DeploymentName"];
        var gpt_o4miniApiKey = configuration["AzureOpenAI:o4mini:ApiKey"];
        var gpt4ApiKey = configuration["AzureOpenAI:gpt4:ApiKey"];
        var gpt4DeploymentName = configuration["AzureOpenAI:gpt4:DeploymentName"];

        var kernelBuilder = services.AddKernel();
        kernelBuilder.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace));

        var gpt_o4miniDeploymentName = configuration["AzureOpenAI:o4mini:DeploymentName"];
        kernelBuilder.AddAzureOpenAIChatCompletion(gpt_o4miniDeploymentName, azureEndpoint!, gpt_o4miniApiKey!, serviceId: AIModels.gpt4mini.ToString());
        kernelBuilder.AddAzureOpenAIChatCompletion(gpt4DeploymentName, azureEndpoint!, gpt4ApiKey!, serviceId: AIModels.gpt4.ToString());

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddOpenTelemetry(options =>
                {
                    options.AddOtlpExporter();

                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                })
                .AddConsole();

            builder.SetMinimumLevel(LogLevel.Information);
        });

        // For tracing demo
        var traceKernelBuilder = Kernel
            .CreateBuilder()
            .AddAzureOpenAIChatCompletion(gpt4DeploymentName, azureEndpoint, gpt4ApiKey, serviceId: "DemoOpenAI");

        traceKernelBuilder.Plugins.AddFromType<OrderPlugin>("Orders");
        traceKernelBuilder.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace));

        // For multi-agent demo
        var multiAgentKernelBuilder = Kernel
            .CreateBuilder()
            .AddAzureOpenAIChatCompletion(azureDeploymentName!, azureEndpoint!, gpt_o4miniApiKey!);

        services.AddKeyedTransient<Kernel>("SalesAssistentKernel", (sp, key) =>
        {
            Kernel salesAssistentKernel = multiAgentKernelBuilder.Build();
            salesAssistentKernel.Plugins.AddFromType<OrderPlugin>("Orders");

            return salesAssistentKernel;
        });

        services.AddKeyedTransient<Kernel>("RefundsAssistantKernel", (sp, key) =>
        {
            Kernel refundsAssistantKernel = multiAgentKernelBuilder.Build();
            refundsAssistantKernel.Plugins.AddFromType<RefundPlugin>("Refunds");

            return refundsAssistantKernel;
        });

        services.AddKeyedTransient<Kernel>("MultiAgentKernel", (sp, key) => multiAgentKernelBuilder.Build());
        services.AddKeyedTransient<Kernel>("DemoOpenAI", (sp, key) => traceKernelBuilder.Build());

        services.AddScoped<IAIService, AIService>();
        services.AddScoped<IAIAgentService, AIAgentService>();

        return services;
    }
}
