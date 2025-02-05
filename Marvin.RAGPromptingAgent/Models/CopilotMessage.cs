using System.Text.Json.Serialization;

namespace Marvin.RAGPromptingAgent.Models;

public class CopilotMessage
{ 
    public required string Role { get; set; } 
    public required string Content { get; set; } 
    [JsonPropertyName("copilot_references")]
    public List<CopilotReference> CopilotReferences { get; set; } = [];
}