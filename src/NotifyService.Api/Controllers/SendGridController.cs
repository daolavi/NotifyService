using System.Text.Json;
using EllipticCurve;
using Microsoft.AspNetCore.Mvc;
using NotifyService.Api.Requests;
using SendGrid;
using SendGrid.Helpers.Mail;

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
            ReplyTo = new EmailAddress(request.ReplyTo),
            CustomArgs = new Dictionary<string, string>
            {
                {"sendEmailRequestId", request.SendEmailRequestId.ToString()}
            }
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
            return Unauthorized();
        }

        // Deserialize and process events
        var events = JsonSerializer.Deserialize<List<SendGridEvent>>(requestBody);
        foreach (var sgEvent in events)
        {
            logger.LogInformation("Received Event {Event}", JsonSerializer.Serialize(sgEvent));
        }

        return Ok();
    }
    
    private static bool IsValidSignature(string timestamp, string payload, string providedSignature, string verificationKey)
    {
        var data = $"{timestamp}{payload}";

        var publicKey = PublicKey.fromPem(verificationKey);
        var decodedSignature = Signature.fromBase64(providedSignature);
    
        return Ecdsa.verify(data, decodedSignature, publicKey);
    }
}