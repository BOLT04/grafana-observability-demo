using Microsoft.SemanticKernel.Agents.Chat;

namespace GrafanaOtelDemoApp.Application;

public class AIAgentConstants
{
    // Normal Agent Constants
    public const string ShoppingAssistantAgentInstructions = "You are a sales assistant. Delegate to the provided agents to help the user with placing orders and requesting refunds.";

    // AgentShopDemo Constants
    public const string InternalLeaderName = "InternalLeader";
    public const string InternalLeaderInstructions =
        """
        Your job is to clearly and directly communicate the current assistant response to the user.

        If information has been requested, only repeat the request.

        If information is provided, only repeat the information.

        Do not come up with your own shopping suggestions.
        """;

    public const string InternalGiftIdeaAgentName = "InternalGiftIdeas";
    public const string InternalGiftIdeaAgentInstructions =
        """        
        You are a personal shopper that provides gift ideas.

        Only provide ideas when the following is known about the gift recipient:
        - Relationship to giver
        - Reason for gift

        Request any missing information before providing ideas.

        Only describe the gift by name.

        Always immediately incorporate review feedback and provide an updated response.
        """;

    public const string InternalGiftReviewerName = "InternalGiftReviewer";
    public const string InternalGiftReviewerInstructions =
        """
        Review the most recent shopping response.

        Either provide critical feedback to improve the response without introducing new ideas or state that the response is adequate.
        """;

    public const string InnerSelectionInstructions =
        $$$"""
        Select which participant will take the next turn based on the conversation history.
        
        Only choose from these participants:
        - {{{InternalGiftIdeaAgentName}}}
        - {{{InternalGiftReviewerName}}}
        - {{{InternalLeaderName}}}
        
        Choose the next participant according to the action of the most recent participant:
        - After user input, it is {{{InternalGiftIdeaAgentName}}}'a turn.
        - After {{{InternalGiftIdeaAgentName}}} replies with ideas, it is {{{InternalGiftReviewerName}}}'s turn.
        - After {{{InternalGiftIdeaAgentName}}} requests additional information, it is {{{InternalLeaderName}}}'s turn.
        - After {{{InternalGiftReviewerName}}} provides feedback or instruction, it is {{{InternalGiftIdeaAgentName}}}'s turn.
        - After {{{InternalGiftReviewerName}}} states the {{{InternalGiftIdeaAgentName}}}'s response is adequate, it is {{{InternalLeaderName}}}'s turn.
                
        Respond in JSON format.  The JSON schema can include only:
        {
            "name": "string (the name of the assistant selected for the next turn)",
            "reason": "string (the reason for the participant was selected)"
        }
        
        History:
        {{${{{KernelFunctionSelectionStrategy.DefaultHistoryVariableName}}}}}
        """;

    public const string OuterTerminationInstructions =
        $$$"""
        Determine if user request has been fully answered.
        
        Respond in JSON format.  The JSON schema can include only:
        {
            "isAnswered": "bool (true if the user request has been fully answered)",
            "reason": "string (the reason for your determination)"
        }
        
        History:
        {{${{{KernelFunctionTerminationStrategy.DefaultHistoryVariableName}}}}}
        """;
}
