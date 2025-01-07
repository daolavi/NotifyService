using System.Text.Json;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using NotifyService.Api.Requests;
using NotifyService.Api.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NotifyService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SendGridController(
    ILogger<SendGridController> logger,
    ISendGridClient sendGridClient,
    IPublishEndpoint publishEndpoint,
    ISendGridSignatureValidationService sendGridSignatureValidationService,
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
        var result = await sendGridClient.SendEmailAsync(email, CancellationToken.None);
        
        if (!result.IsSuccessStatusCode)
        {
            return BadRequest(new { Message = $"Failed to send request to SendGrid. SendEmailRequestId : {request.SendEmailRequestId}"});
        }

        logger.LogInformation("Sent request to SendGrid successfully");
        return Ok(new { Message = $"Sent request to SendGrid successfully. SendEmailRequestId : {request.SendEmailRequestId}"});
    }

    [HttpPost("receive-events")]
    public async Task<IActionResult> ReceiveEvents(CancellationToken cancellationToken)
    {
        var signature = Request.Headers["X-Twilio-Email-Event-Webhook-Signature"].ToString();
        var timestamp = Request.Headers["X-Twilio-Email-Event-Webhook-Timestamp"].ToString();

        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

        var verificationKey = configuration["VerificationKey"] ?? string.Empty;

        if (!sendGridSignatureValidationService.IsValidSignature(timestamp, requestBody, signature, verificationKey))
        {
            return Unauthorized();
        }

        var events = JsonSerializer.Deserialize<List<SendGridEvent>>(requestBody);
        foreach (var sgEvent in events)
        {
            await publishEndpoint.Publish(sgEvent, cancellationToken);
            logger.LogInformation("Published event {Event} to SNS", JsonSerializer.Serialize(sgEvent));
        }

        return Ok();
    }
}