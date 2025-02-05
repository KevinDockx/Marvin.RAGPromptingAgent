using System.Text.Json.Serialization;

namespace Marvin.RAGPromptingAgent.Models;

public class OutgoingCopilotPayload
{
    public bool Stream { get; set; }
    public List<CopilotMessage> Messages { get; set; } = [];
}
