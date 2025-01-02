using System.Text.Json.Serialization;

namespace NotifyService.Api.Requests;

public class SendGridEvent
{
    [JsonPropertyName("event")]
    public required string EventType { get; set; }
    
    public required string Email { get; set; }
    
    public long Timestamp { get; set; }
    
    [JsonPropertyName("custom_args")]
    public Dictionary<string, string>? CustomArgs { get; set; }
}