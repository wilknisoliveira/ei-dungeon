namespace ei_back.Core.Application.Interfaces;

public interface IGenAi
{
    Task<string> GetResponse(string prompt, CancellationToken cancellationToken);
    Task<string> GetResponse(List<AiPromptRequest> prompts, CancellationToken cancellationToken);
    Task<string> GetResponse(List<AiPromptRequest> prompts, int maxOutputTokens, CancellationToken cancellationToken);
    Task<string> GetResponse<T>(List<AiPromptRequest> prompts, int maxOutputTokens, CancellationToken cancellationToken);
    Task<T> GetStructureResponse<T>(List<AiPromptRequest> prompts, int maxOutputTokens, CancellationToken cancellationToken);
    IAsyncEnumerable<StreamAIDtoResponse> StreamGetResponse(List<AiPromptRequest> prompts, int maxOutputTokens, CancellationToken cancellationToken);
}