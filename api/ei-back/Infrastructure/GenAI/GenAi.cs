using System.ClientModel;
using System.Runtime.CompilerServices;
using ei_back.Core.Application.Interfaces;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using OpenAI;
using OpenAI.Chat;

namespace ei_back.Infrastructure.GenAI;

public class GenAi : IGenAi
{
    private readonly IChatClient _genAiClient;
    private readonly ILogger<GenAi> _logger;
    private readonly Dictionary<AiRole, ChatRole> _chatRoleDict = new()
    {
        [AiRole.System] = ChatRole.System,
        [AiRole.User] = ChatRole.User,
        [AiRole.Assistant] = ChatRole.Assistant,
    };
    
    public GenAi(IConfiguration configuration, ILogger<GenAi> logger)
    {
        _logger = logger;

        var aiModel = configuration["GenAISettings:AiModel"];
        var apiToken = configuration["keys:OpenRouterApiKey"];
        if (aiModel.IsNullOrEmpty() || apiToken.IsNullOrEmpty())
            throw new ArgumentException("AIModel and apiToken must be set");

        var openAiClient = new ChatClient(aiModel!, new ApiKeyCredential(apiToken!), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://openrouter.ai/api/v1"),
        });

        _genAiClient = new ChatClientBuilder(openAiClient.AsIChatClient()).Build();
    }

    public async Task<string> GetResponse(string prompt, CancellationToken cancellationToken)
    {
        var result = await _genAiClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);

        return result.Text;
    }
    
    public async Task<string> GetResponse(List<AiPromptRequest> prompts, CancellationToken cancellationToken)
    {
        var messages = GetChatMessages(prompts);
        
        var result = await _genAiClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        return result.Text;
    }

    public async Task<string> GetResponse(
        List<AiPromptRequest> prompts, 
        int maxOutputTokens, 
        CancellationToken cancellationToken)
    {
        var messages = GetChatMessages(prompts, maxOutputTokens);
        
        return await GetResponseAsync(messages, cancellationToken: cancellationToken);
    }

    public async Task<string> GetResponse<T>(List<AiPromptRequest> prompts, int maxOutputTokens, CancellationToken cancellationToken)
    {
        var messages = GetChatMessages(prompts, maxOutputTokens);
        
        return await GetResponseAsync<T>(messages, cancellationToken: cancellationToken);
    }

    public async Task<T> GetStructureResponse<T>(List<AiPromptRequest> prompts, int maxOutputTokens, CancellationToken cancellationToken)
    {
        var messages = GetChatMessages(prompts);
        
        try
        {
            var result = await _genAiClient.GetResponseAsync<T>(messages, cancellationToken: cancellationToken);
            return result.Result;
        }
        catch (Exception e)
        {
            throw new BadGatewayException("The LLM API failed to respond.");
        }
    }

    public async IAsyncEnumerable<StreamAIDtoResponse> StreamGetResponse(
        List<AiPromptRequest> prompts, 
        int maxOutputTokens, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var messages = GetChatMessages(prompts, maxOutputTokens);
        
        await foreach (var chunk in _genAiClient
            .GetStreamingResponseAsync(messages, cancellationToken: cancellationToken)
            .WithCancellation(cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Text))
                yield return new StreamAIDtoResponse
                {
                    EventType = AIStreamEventType.Chunk,
                    Content = chunk.Text
                };
            else if (chunk.FinishReason == null)
            {
                _logger.LogError("LLM returned an empty response chunk");
                yield return new StreamAIDtoResponse
                {
                    EventType = AIStreamEventType.Error,
                    Content = "Something went wrong while attempting to get the response from the LLM API."
                };
            }
        }
    }

    private List<ChatMessage> GetChatMessages(List<AiPromptRequest> prompts, int? maxOutputTokens = null)
    {
        return prompts.Select(prompt =>
        {
            var content = prompt.Content;

            if (maxOutputTokens.HasValue && prompt.Role == AiRole.System)
            {
                content += "\n\n" + GetOutputCharactersPrompt(
                    ConvertTokensToCharactersQuantity(maxOutputTokens!.Value));
            }

            return new ChatMessage(_chatRoleDict[prompt.Role], content);
        }).ToList();
    }

    private string GetOutputCharactersPrompt(int maxOutputCharacters)
    {
        return $"Your final answer must contain no more than {maxOutputCharacters} characters (counting all letters, numbers, spaces and punctuation). " +
               $"Do not include explanations about the limit. Do not say you are limiting the answer. Just output the final answer.";
    }

    private int ConvertTokensToCharactersQuantity(int maxOutputTokens)
    {
        // The better approach is to pass the maxOutputTokens to the ChatOptions. But gemini implementation doesn't
        // work well with maxOutputTokens. Therefore, the solution is to pass the limit as a system prompt.
        // LLMs better understand characters quantity instead of token quantities, so let's convert it by inference.
        return maxOutputTokens * 4;
    }

    private async Task<string> GetResponseAsync(List<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _genAiClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            return result.Text;
        }
        catch (Exception e)
        {
            throw new BadGatewayException("The LLM API failed to respond.");
        }
    }
    
    private async Task<string> GetResponseAsync<T>(List<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _genAiClient.GetResponseAsync<T>(messages, cancellationToken: cancellationToken);
            return result.Text;
        }
        catch (Exception e)
        {
            throw new BadGatewayException("The LLM API failed to respond.");
        }
    }
}