using System.Text.Json.Serialization;

namespace Marvin.RAGPromptingAgent.Models;

public class CopilotReference
{
    public required string Type { get; set; } 
    public required object Data { get; set; }   
    public required string Id { get; set; } 
    [JsonPropertyName("is_implicit")]
    public required bool IsImplicit { get; set; }
    public required CopilotMetadata Metadata { get; set; }  
}
