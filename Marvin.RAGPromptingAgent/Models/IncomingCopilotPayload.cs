using System.Text.Json.Serialization;

namespace Marvin.RAGPromptingAgent.Models;

public class IncomingCopilotPayload
{ 
    public List<CopilotMessage> Messages { get; set; } = [];
}
