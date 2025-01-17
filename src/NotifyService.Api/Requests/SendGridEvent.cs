using System.Text.Json.Serialization;

namespace NotifyService.Api.Requests;

public class SendGridEvent
{
    [JsonPropertyName("event")]
    public required string EventType { get; set; }
    
    [JsonPropertyName("email")]
    public required string Email { get; set; }
    
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
    
    [JsonPropertyName("sendEmailRequestId")]
    public string? SendEmailRequestId { get; set; }
}