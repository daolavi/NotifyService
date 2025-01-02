using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NotifyService.Api.Requests;
using SendGrid;
using SendGrid.Helpers.Mail;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NotifyService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SendGridController(
    ILogger<SendGridController> logger, 
    ISendGridClient sendGridClient,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("send-email")]
    public async Task<IActionResult> SendEmail(SendEmailRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received SendEmailRequest {Request}", request);
        var email = new SendGridMessage()
        {
            TemplateId = request.TemplateId,
            Subject = request.Subject,
            From = new EmailAddress(request.From),
            ReplyTo = new EmailAddress(request.ReplyTo)
        };
        
        email.AddTo(new EmailAddress(request.To));
        
        email.SetTemplateData(request.Data);
        var result = await sendGridClient.SendEmailAsync(email, cancellationToken);
        var response = await result.DeserializeResponseBodyAsync();
        logger.LogInformation("Sent request to SendGrid - {Response}", response);
        if (result.IsSuccessStatusCode)
        {
            return Ok();
        }
        return BadRequest();
    }

    [HttpPost("receive-events")]
    public async Task<IActionResult> ReceiveEvents(CancellationToken cancellationToken)
    {
        // Extract headers
        var signature = Request.Headers["X-Twilio-Email-Event-Webhook-Signature"].ToString();
        var timestamp = Request.Headers["X-Twilio-Email-Event-Webhook-Timestamp"].ToString();

        // Read raw request body
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

        var verificationKey = configuration["VerificationKey"] ?? string.Empty;
        
        // Validate signature
        if (!IsValidSignature(timestamp, requestBody, signature, verificationKey))
        {
            return Unauthorized(new { message = "Invalid signature" });
        }

        // Deserialize and process events
        var events = JsonSerializer.Deserialize<List<SendGridEvent>>(requestBody);
        foreach (var sgEvent in events)
        {
            logger.LogInformation("Received Event {Event}", sgEvent);
        }

        return Ok();
    }
    
    private bool IsValidSignature(string timestamp, string payload, string providedSignature, string key)
    {
        // Concatenate timestamp and payload
        var data = $"{timestamp}{payload}";

        // Compute HMAC-SHA256 hash
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        var computedSignature = Convert.ToBase64String(hash);

        // Compare computed signature with provided signature
        return computedSignature == providedSignature;
    }
}