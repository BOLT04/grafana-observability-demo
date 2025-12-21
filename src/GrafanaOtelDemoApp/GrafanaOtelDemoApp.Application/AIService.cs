using GrafanaOtelDemoApp.Application.Common;
using GrafanaOtelDemoApp.Application.Filters;
using GrafanaOtelDemoApp.Application.Plugins;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Diagnostics;

namespace GrafanaOtelDemoApp.Application;

public interface IAIService
{
    Task<string> GetGreetingAsync(string query, string? model);
    Task<string> GetMultiAgentAsyncResponse(string query);
    Task<string?> GetStreamingDemo(string query);
}

public class AIService : IAIService
{
    private readonly Kernel _kernel;
    private readonly Kernel _demoOpenAIKernel;
    private readonly Kernel _multiAgentKernel;
    private readonly ChatCompletionAgent _chatCompletionAgent;
    private ChatHistory history = new ChatHistory();

    private readonly ILogger<AIService> _logger;
    private readonly IConfiguration _configuration;

    public AIService(
        Kernel kernel,
        [FromKeyedServices("MultiAgentKernel")] Kernel multiAgentKernel,
        [FromKeyedServices("DemoOpenAI")] Kernel demoOpenAIKernel,
        [FromKeyedServices("SalesAssistentKernel")] Kernel salesAssistentKernel,
        [FromKeyedServices("RefundsAssistantKernel")] Kernel refundsAssistantKernel,
        ILogger<AIService> logger,
        IConfiguration configuration)
    {
        _kernel = kernel;
        _kernel.Plugins.AddFromType<OrderPlugin>("Orders");
        _kernel.Plugins.AddFromType<BookingPlugin>("Booking");
        _kernel.Plugins.AddFromType<RefundPlugin>("Refund2");

        _demoOpenAIKernel = demoOpenAIKernel;
        _multiAgentKernel = multiAgentKernel;

        // Add plugins
        var agentPlugin = KernelPluginFactory.CreateFromFunctions("AgentPlugin",
            [
                AgentKernelFunctionFactory.CreateFromAgent(new ChatCompletionAgent() {
                Name = "SalesAssistant",
                Instructions = "You are a sales assistant. Place orders for items the user requests.",
                Description = "Agent to invoke to place orders for items the user requests.",
                Kernel = salesAssistentKernel,
                Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            }),

            AgentKernelFunctionFactory.CreateFromAgent(new ChatCompletionAgent() {
                Name = "RefundAgent",
                Instructions = "You are a refund agent. Help the user with refunds.",
                Description = "Agent to invoke to execute a refund an item on behalf of the user.",
                Kernel = refundsAssistantKernel,
                Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            })
            ]);

        _multiAgentKernel.Plugins.Add(agentPlugin);
        _multiAgentKernel.Plugins.AddFromType<BookingPlugin>();
        _multiAgentKernel.AutoFunctionInvocationFilters.Add(new AutoFunctionInvocationFilter());
        _chatCompletionAgent = new()
        {
            Name = "ShoppingAssistant",
            Instructions = AIAgentConstants.ShoppingAssistantAgentInstructions,
            Kernel = _multiAgentKernel,
            Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
        };

        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string> GetGreetingAsync(string query, string? model)
    {
        using var activity = TelemetryDiagnostics.Source.StartActivity("GetGreetingAsync");

        try
        {
            var sw = Stopwatch.StartNew();
            var integrationId = Guid.NewGuid();

            activity?.SetTag(DiagnosticsNames.IntegrationIdLabel, integrationId);
            using var _ = _logger.BeginScope(new Dictionary<string, object> { [DiagnosticsNames.IntegrationIdLabel] = integrationId });

            model ??= "gpt4";

            _logger.LogInformation("Processing AI greeting request");
            var settings = new OpenAIPromptExecutionSettings()
            {
                ServiceId = model,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            if (model != AIModels.gpt4mini.ToString())
            {
                // NOTE: Aren't added as span attributes yet... but a man can hope
                settings.Temperature = 0.7;
                settings.MaxTokens = 500;
                settings.Logprobs = true;
                settings.TopP = 0.6;
            }

            var response = await _kernel.InvokePromptAsync(
                    $@"You are a helpful assistant. Respond to the following question '{query}' in a friendly manner.
                Please provide the output in JSON format (but don't use the \n character). Keep the response under 500 characters please. If needed, provide links
                as an additional field as an array of strings", new KernelArguments(settings));

            var result = response.GetValue<string>() ?? "nope";

            var settings2 = new OpenAIPromptExecutionSettings()
            {
                ServiceId = model
            };
            if (model != AIModels.gpt4mini.ToString())
            {
                settings2.Temperature = 0.7;
                settings2.MaxTokens = 500;
                settings2.Logprobs = true;
                settings2.TopP = 0.6;
            }

            // Convert to IChatClient to use GetStreamingResponseAsync that is instrumented with more GenAI attributes in traces
            IChatClient chatClient = _demoOpenAIKernel.GetRequiredService<IChatCompletionService>().AsChatClient();
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> requestMessages = [new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, query)];
            var responses = await chatClient.GetResponseAsync(requestMessages, new ChatOptions()
            {
                Temperature = (float)0.7,
                TopP = (float)0.6,
                MaxOutputTokens = 500
            });

            _logger.LogInformation("AI trace demo, {chunk}", responses?.ToString());
            _logger.LogInformation("AI greeting response generated successfully using");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI greeting");
            activity?.AddException(ex);

            return "nope error";
        }
    }

    private AgentThread? agentThread = null;
    public async Task<string> GetMultiAgentAsyncResponse(string query)
    {
        // Add user input
        if (!string.IsNullOrEmpty(query))
        {
            history.AddUserMessage(query);
        }

        // Get the response from the AI
        var responseItems = _chatCompletionAgent.InvokeAsync(new Microsoft.SemanticKernel.ChatMessageContent(AuthorRole.User, query), agentThread);
        await foreach (var responseItem in responseItems)
        {
            if (responseItem.Message is not null)
            {
                agentThread = responseItem.Thread;

                // Add the message from the agent to the chat history
                history.AddMessage(responseItem.Message.Role, responseItem.Message.Content ?? string.Empty);

                return $"{responseItem.Message.AuthorName} ({responseItem.Message.Role}) > {responseItem.Message.Content}";
            }
        }

        return "no response";
    }


    private const string ParrotName = "Parrot";
    private const string ParrotInstructions = "Repeat the user message in the voice of a pirate and then end with a parrot sound.";

    public async Task<string> GetStreamingDemo(string query)
    {
        ChatCompletionAgent agent =
            new()
            {
                Name = ParrotName,
                Instructions = ParrotInstructions,
                Kernel = _demoOpenAIKernel,
            };

        ChatHistoryAgentThread agentThread = new();

        // Respond to user input
        await InvokeAgentAsync(agent, agentThread, "Fortune favors the bold.");
        await InvokeAgentAsync(agent, agentThread, "I came, I saw, I conquered.");
        await InvokeAgentAsync(agent, agentThread, "Practice makes perfect.");

        // Output the entire chat history
        await DisplayChatHistory(agentThread);

        return "Cool demo completed.";
    }

    private async Task DisplayChatHistory(ChatHistoryAgentThread agentThread)
    {
        Console.WriteLine("================================");
        Console.WriteLine("CHAT HISTORY");
        Console.WriteLine("================================");

        await foreach (Microsoft.SemanticKernel.ChatMessageContent message in agentThread.GetMessagesAsync())
        {
            AIUtilities.WriteAgentChatMessage(message);
        }
    }

    // Local function to invoke agent and display the conversation messages.
    private async Task InvokeAgentAsync(ChatCompletionAgent agent, ChatHistoryAgentThread agentThread, string input)
    {
        Microsoft.SemanticKernel.ChatMessageContent message = new(AuthorRole.User, input);
        AIUtilities.WriteAgentChatMessage(message);

        int historyCount = agentThread.ChatHistory.Count;

        bool isFirst = false;
        await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, agentThread))
        {
            if (string.IsNullOrEmpty(response.Content))
            {
                StreamingFunctionCallUpdateContent? functionCall = response.Items.OfType<StreamingFunctionCallUpdateContent>().SingleOrDefault();
                if (!string.IsNullOrEmpty(functionCall?.Name))
                {
                    Console.WriteLine($"\n# {response.Role} - {response.AuthorName ?? "*"}: FUNCTION CALL - {functionCall.Name}");
                }

                continue;
            }

            if (!isFirst)
            {
                Console.WriteLine($"\n# {response.Role} - {response.AuthorName ?? "*"}:");
                isFirst = true;
            }

            Console.WriteLine($"\t > streamed: '{response.Content}'");
        }

        if (historyCount <= agentThread.ChatHistory.Count)
        {
            for (int index = historyCount; index < agentThread.ChatHistory.Count; index++)
            {
                AIUtilities.WriteAgentChatMessage(agentThread.ChatHistory[index]);
            }
        }
    }

    public enum AIModels
    {
        gpt4mini,
        gpt4,
    }
}
