using GrafanaOtelDemoApp.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace GrafanaOtelDemoApp.Application;

public interface IAIAgentService
{
    Task<string?> GetAgentShopDemo();
}

public class AIAgentService : IAIAgentService
{
    private readonly Kernel _demoOpenAIKernel;
    private readonly ILogger<AIAgentService> _logger;

    public AIAgentService(
        [FromKeyedServices("DemoOpenAI")] Kernel demoOpenAIKernel,
        ILogger<AIAgentService> logger)
    {
        _demoOpenAIKernel = demoOpenAIKernel;
        _logger = logger;
    }

    public async Task<string> GetAgentShopDemo()
    {
        try
        {
            _logger.LogInformation("Start GetAgentShopDemo");

            OpenAIPromptExecutionSettings jsonSettings = new();// { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() };
            PromptExecutionSettings autoInvokeSettings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };

            ChatCompletionAgent internalLeaderAgent = CreateAgent(
                AIAgentConstants.InternalLeaderName,
                AIAgentConstants.InternalLeaderInstructions);
            ChatCompletionAgent internalGiftIdeaAgent = CreateAgent(
                AIAgentConstants.InternalGiftIdeaAgentName,
                AIAgentConstants.InternalGiftIdeaAgentInstructions);
            ChatCompletionAgent internalGiftReviewerAgent = CreateAgent(
                AIAgentConstants.InternalGiftReviewerName,
                AIAgentConstants.InternalGiftReviewerInstructions);

            KernelFunction innerSelectionFunction = KernelFunctionFactory.CreateFromPrompt(
                AIAgentConstants.InnerSelectionInstructions, jsonSettings);
            KernelFunction outerTerminationFunction = KernelFunctionFactory.CreateFromPrompt(
                AIAgentConstants.OuterTerminationInstructions, jsonSettings);

            AggregatorAgent personalShopperAgent = new(CreateChat)
            {
                Name = "PersonalShopper",
                Mode = AggregatorMode.Nested,
            };

            AgentGroupChat chat = new(personalShopperAgent)
            {
                ExecutionSettings =
                    new()
                    {
                        TerminationStrategy =
                            new KernelFunctionTerminationStrategy(outerTerminationFunction, _demoOpenAIKernel)
                            {
                                ResultParser =
                                    (result) =>
                                    {
                                        OuterTerminationResult? jsonResult = CustomJsonResultTranslator.Translate<OuterTerminationResult>(result.GetValue<string>());
                                        _logger.LogInformation("My result parser: {0}", result.GetValue<string>());

                                        return jsonResult?.isAnswered ?? false;
                                        //return true;
                                    },
                                MaximumIterations = 5,
                            },
                    }
            };

            // Invoke chat and display messages.
            _logger.LogInformation("\n######################################");
            _logger.LogInformation("# DYNAMIC CHAT");
            _logger.LogInformation("######################################");

            await InvokeChatAsync("Can you provide three original birthday gift ideas. I don't want a gift that someone else will also pick.");

            await InvokeChatAsync("The gift is for my adult brother.");

            if (!chat.IsComplete)
            {
                await InvokeChatAsync("He likes photography.");
            }

            _logger.LogInformation("\n\n>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
            _logger.LogInformation(">>>> AGGREGATED CHAT");
            _logger.LogInformation(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");

            await foreach (ChatMessageContent message in chat.GetChatMessagesAsync(personalShopperAgent).Reverse())
            {
                AIUtilities.WriteAgentChatMessage(message);
            }

            async Task InvokeChatAsync(string input)
            {
                ChatMessageContent message = new(AuthorRole.User, input);
                chat.AddChatMessage(message);
                AIUtilities.WriteAgentChatMessage(message);

                await foreach (ChatMessageContent response in chat.InvokeAsync(personalShopperAgent))
                {
                    AIUtilities.WriteAgentChatMessage(response);
                }

                _logger.LogInformation($"\n# IS COMPLETE: {chat.IsComplete}");
            }

            AgentGroupChat CreateChat() =>
                new(internalLeaderAgent, internalGiftReviewerAgent, internalGiftIdeaAgent)
                {
                    ExecutionSettings =
                        new()
                        {
                            SelectionStrategy = new KernelFunctionSelectionStrategy(innerSelectionFunction, _demoOpenAIKernel)
                            {
                                ResultParser = AgentSelectorResultParser
                            },
                            TerminationStrategy = new AgentTerminationStrategy()
                            {
                                Agents = [internalLeaderAgent],
                                MaximumIterations = 7,
                                AutomaticReset = true,
                            },
                        }
                };

            return "sopa";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI shop demo");
            return "nope error";
        }
    }

    private ChatCompletionAgent CreateAgent(string agentName, string agentInstructions) => new()
    {
        Instructions = agentInstructions,
        Name = agentName,
        Kernel = _demoOpenAIKernel,
    };

    private string AgentSelectorResultParser(FunctionResult result)
    {
        AgentSelectionResult? jsonResult = CustomJsonResultTranslator.Translate<AgentSelectionResult>(result.GetValue<string>());
        _logger.LogInformation("My result parser 2: {result}", result.GetValue<string>());

        string? agentName = string.IsNullOrWhiteSpace(jsonResult?.name) ? null : jsonResult?.name;
        agentName ??= AIAgentConstants.InternalGiftIdeaAgentName;

        _logger.LogInformation($"\t>>>> INNER TURN: {agentName}");

        return agentName;
    }

    private sealed record OuterTerminationResult(bool isAnswered, string reason);

    private sealed record AgentSelectionResult(string name, string reason);

    private sealed class AgentTerminationStrategy : TerminationStrategy
    {
        /// <inheritdoc/>
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    // Other examples
    private sealed class ApprovalTerminationStrategy : TerminationStrategy
    {
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
            => Task.FromResult(history[history.Count - 1].Content?.Contains("[OK]", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private sealed class AgentTerminationStrategy2(Agent lastAgent) : TerminationStrategy
    {
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(agent == lastAgent);
        }
    }
}
