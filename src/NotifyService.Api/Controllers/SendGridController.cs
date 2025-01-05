using System.Text.Json;
using EllipticCurve;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using NotifyService.Api.Requests;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NotifyService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SendGridController(
    ILogger<SendGridController> logger, 
    ISendEndpointProvider sendEndpointProvider,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("send-email")]
    public async Task<IActionResult> SendEmail(SendEmailRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received SendEmailRequest {Request}", request);
        var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:send-email-requests"));
        await endpoint.Send(request, cancellationToken);
        logger.LogInformation("Sent message to send-email-requests queue");
        return Ok();
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

        if (!IsValidSignature(timestamp, requestBody, signature, verificationKey))
        {
            return Unauthorized();
        }

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