namespace DemoReleaseChatPlugin.Assistant;

/// Connection settings for Azure OpenAI, bound from the "AzureOpenAI" configuration section.
/// The three required values are only validated when Assistant:Enabled is true. Each optional
/// deployment switches its feature off when left empty, rather than breaking it.
public sealed class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    /// The resource's base URL, e.g. https://name.openai.azure.com/ — not the /openai/v1 address;
    /// the SDK appends the rest of the path itself.
    public string Endpoint { get; set; }

    /// The chat deployment's name (the Name column in Foundry), not the model's name.
    public string DeploymentName { get; set; }

    public string ApiKey { get; set; }

    /// Empty = the knowledge base uses keyword search only.
    public string EmbeddingDeploymentName { get; set; }

    /// Empty = no attachments; the paperclip isn't shown.
    public string VisionDeploymentName { get; set; }

    /// Empty = no voice input; the microphone isn't shown.
    public string TranscriptionDeploymentName { get; set; }
}
