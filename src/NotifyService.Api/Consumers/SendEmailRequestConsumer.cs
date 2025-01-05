using MassTransit;
using NotifyService.Api.Requests;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NotifyService.Api.Consumers;

public class SendEmailRequestConsumer(ILogger<SendEmailRequestConsumer> logger,
    ISendGridClient sendGridClient) : IConsumer<SendEmailRequest>
{
    public async Task Consume(ConsumeContext<SendEmailRequest> context)
    {
        var message = context.Message;
        logger.LogInformation("Consuming SendEmailRequest {Request}", message);
        var email = new SendGridMessage()
        {
            TemplateId = message.TemplateId,
            Subject = message.Subject,
            From = new EmailAddress(message.From),
            ReplyTo = new EmailAddress(message.ReplyTo),
            CustomArgs = new Dictionary<string, string>
            {
                {"sendEmailRequestId", message.SendEmailRequestId.ToString()}
            }
        };
        
        email.AddTo(new EmailAddress(message.To));
        
        email.SetTemplateData(message.Data);
        var result = await sendGridClient.SendEmailAsync(email, CancellationToken.None);
        var response = await result.DeserializeResponseBodyAsync();
        
        if (!result.IsSuccessStatusCode)
        {
            throw new SendEmailException($"Failed to send emails. SendEmailRequestId : {message.SendEmailRequestId}");
        }
        else
        {
            logger.LogInformation("Sent request to SendGrid successfully");
        }
    }
}

public class SendEmailException(string message) : Exception(message);