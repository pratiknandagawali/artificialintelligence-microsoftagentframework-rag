namespace MicrosoftAgentFramework.Rag.Configuration;

/// <summary>
/// Configuration settings for Azure OpenAI
/// Supports separate endpoints and API keys for chat and embedding deployments
/// </summary>
public class AzureOpenAIConfig
{
    // Chat configuration
    public string ChatEndpoint { get; set; } = string.Empty;
    public string ChatApiKey { get; set; } = string.Empty;
    public string ChatDeploymentName { get; set; } = string.Empty;

    // Embedding configuration
    public string EmbeddingEndpoint { get; set; } = string.Empty;
    public string EmbeddingApiKey { get; set; } = string.Empty;
    public string EmbeddingDeploymentName { get; set; } = string.Empty;

    // Legacy properties for backward compatibility (if both use same endpoint/key)
    public string Endpoint
    {
        get => string.IsNullOrEmpty(ChatEndpoint) ? string.Empty : ChatEndpoint;
        set
        {
            if (string.IsNullOrEmpty(ChatEndpoint)) ChatEndpoint = value;
            if (string.IsNullOrEmpty(EmbeddingEndpoint)) EmbeddingEndpoint = value;
        }
    }

    public string ApiKey
    {
        get => string.IsNullOrEmpty(ChatApiKey) ? string.Empty : ChatApiKey;
        set
        {
            if (string.IsNullOrEmpty(ChatApiKey)) ChatApiKey = value;
            if (string.IsNullOrEmpty(EmbeddingApiKey)) EmbeddingApiKey = value;
        }
    }
}