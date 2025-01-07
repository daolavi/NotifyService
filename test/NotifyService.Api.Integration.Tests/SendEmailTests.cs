using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using Moq;
using NotifyService.Api.Requests;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NotifyService.Api.Integration.Tests;

public class SendEmailTests : IntegrationTestBase
{
    [Fact]
    public async Task SendSuccessfully_ReturnOk()
    {
        var request = GivenSendEmailRequest(
            Fixture.Create<Guid>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            new Dictionary<string, string>()
            {
                {"firstName", Fixture.Create<string>()},
                {"subject", Fixture.Create<string>()},
            }
        );
        GivenSendRequestToSendGridSuccessfully();
        await WhenCallingEndpoint(request);
        ThenReturnOk();
    }
    
    [Fact]
    public async Task SendFailed_ReturnBadRequest()
    {
        var request = GivenSendEmailRequest(
            Fixture.Create<Guid>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            new Dictionary<string, string>()
            {
                {"firstName", Fixture.Create<string>()},
                {"subject", Fixture.Create<string>()},
            }
        );
        GivenSendRequestToSendGridFailed();
        await WhenCallingEndpoint(request);
        ThenReturnBadRequest();
    }

    private SendEmailRequest GivenSendEmailRequest(Guid sendEmailRequestId, string subject, string from, string to, 
        string replyTo, string templateId, Dictionary<string, string> data) 
        => new SendEmailRequest(sendEmailRequestId, subject, from, to, replyTo, templateId, data);

    private void GivenSendRequestToSendGridSuccessfully()
    {
        SendGridClientMock.Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), CancellationToken.None))
            .ReturnsAsync(new Response(HttpStatusCode.OK, null, null));
    }
    
    private void GivenSendRequestToSendGridFailed()
    {
        SendGridClientMock.Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), CancellationToken.None))
            .ReturnsAsync(new Response(HttpStatusCode.BadRequest, null, null));
    }
    
    private async Task WhenCallingEndpoint(SendEmailRequest request)
    {
        var content = JsonContent.Create(request);
        Response = await Sut.PostAsync($"api/sendgrid/send-email", content);
    }
}