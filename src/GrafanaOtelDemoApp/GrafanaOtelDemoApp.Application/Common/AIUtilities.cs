using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Chat;

namespace GrafanaOtelDemoApp.Application.Common
{
    public static class AIUtilities
    {
        /// <summary>
        /// The metadata key that identifies code-interpreter content.
        /// </summary>
        public const string OpenAIAssistantAgent_CodeInterpreterMetadataKey = "code";

        /// <summary>
        /// Common method to write formatted agent chat content to the console.
        /// </summary>
        public static void WriteAgentChatMessage(Microsoft.SemanticKernel.ChatMessageContent message)
        {
            // Include ChatMessageContent.AuthorName in output, if present.
            string authorExpression = message.Role == AuthorRole.User ? string.Empty : FormatAuthor();
            // Include TextContent (via ChatMessageContent.Content), if present.
            string contentExpression = string.IsNullOrWhiteSpace(message.Content) ? string.Empty : message.Content;
            bool isCode = message.Metadata?.ContainsKey(OpenAIAssistantAgent_CodeInterpreterMetadataKey) ?? false;
            string codeMarker = isCode ? "\n  [CODE]\n" : " ";
            System.Console.WriteLine($"\n# {message.Role}{authorExpression}:{codeMarker}{contentExpression}");

            // Provide visibility for inner content (that isn't TextContent).
            foreach (KernelContent item in message.Items)
            {
                if (item is AnnotationContent annotation)
                {
                    if (annotation.Kind == AnnotationKind.UrlCitation)
                    {
                        Console.WriteLine($"AgentChat: [{item.GetType().Name}] {annotation.Label}: {annotation.ReferenceId} - {annotation.Title}");
                    }
                    else
                    {
                        Console.WriteLine($"AgentChat: [{item.GetType().Name}] {annotation.Label}: File #{annotation.ReferenceId}");
                    }
                }
                else if (item is ActionContent action)
                {
                    Console.WriteLine($"AgentChat: [{item.GetType().Name}] {action.Text}");
                }
                else if (item is ReasoningContent reasoning)
                {
                    Console.WriteLine($"AgentChat: [{item.GetType().Name}] {reasoning.Text ?? "Thinking..."}");
                }
                else if (item is FileReferenceContent fileReference)
                {
                    Console.WriteLine($"AgentChat: [{item.GetType().Name}] File #{fileReference.FileId}");
                }
                else if (item is ImageContent image)
                {
                    Console.WriteLine($"AgentChat: [{item.GetType().Name}] {image.Uri?.ToString() ?? image.DataUri ?? $"{image.Data?.Length} bytes"}");
                }
                else if (item is Microsoft.SemanticKernel.FunctionCallContent functionCall)
                {
                    Console.WriteLine($"AgentChat: [{item.GetType().Name}] {functionCall.Id}");
                }
                else if (item is Microsoft.SemanticKernel.FunctionResultContent functionResult)
                {
                    Console.WriteLine($"AgentChat: [{item.GetType().Name}] {functionResult.CallId} - {functionResult.Result?.ToString() ?? "*"}");
                }
            }

            if (message.Metadata?.TryGetValue("Usage", out object? usage) ?? false)
            {
                //if (usage is Microsoft.SemanticKernel.RunStepTokenUsage assistantUsage)
                //{
                //    WriteUsage(assistantUsage.TotalTokenCount, assistantUsage.InputTokenCount, assistantUsage.OutputTokenCount);
                //}
                //else if (usage is RunStepCompletionUsage agentUsage)
                //{
                //    WriteUsage(agentUsage.TotalTokens, agentUsage.PromptTokens, agentUsage.CompletionTokens);
                //}
                if (usage is ChatTokenUsage chatUsage)
                {
                    WriteUsage(chatUsage.TotalTokenCount, chatUsage.InputTokenCount, chatUsage.OutputTokenCount);
                }
                else if (usage is UsageDetails usageDetails)
                {
                    WriteUsage(usageDetails.TotalTokenCount ?? 0, usageDetails.InputTokenCount ?? 0, usageDetails.OutputTokenCount ?? 0);
                }
            }

            string FormatAuthor() => message.AuthorName is not null ? $" - {message.AuthorName ?? " * "}" : string.Empty;
        }
        public static void WriteUsage(long totalTokens, long inputTokens, long outputTokens)
        {
            Console.WriteLine($"WriteUsage:  [Usage] Tokens: {totalTokens}, Input: {inputTokens}, Output: {outputTokens}");
        }
    }
}
