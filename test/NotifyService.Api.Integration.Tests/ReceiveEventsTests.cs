using System.Net.Http.Json;
using AutoFixture;
using Moq;
using NotifyService.Api.Requests;

namespace NotifyService.Api.Integration.Tests;

public class ReceiveEventsTests : IntegrationTestBase
{
    [Fact]
    public async Task ValidSignature_ReturnOk()
    {
        var events = GivenSendGridEvents();
        GivenValidSignature();
        await WhenCallingEndpoint(events);
        ThenReturnOk();
    }
    
    [Fact]
    public async Task InvalidSignature_ReturnUnauthorized()
    {
        var events = GivenSendGridEvents();
        GivenInvalidSignature();
        await WhenCallingEndpoint(events);
        ThenReturnUnauthorized();
    }

    private List<SendGridEvent> GivenSendGridEvents()
    {
        var list = new List<SendGridEvent>()
        {
            new()
            {
                EventType = Fixture.Create<string>(),
                Email = Fixture.Create<string>(),
                Timestamp = Fixture.Create<long>(),
                SendEmailRequestId = Fixture.Create<string>(),
            }
        };
        
        return list;
    }

    private void GivenValidSignature()
        => SendGridSignatureValidationServiceMock.Setup(
        s => s.IsValidSignature(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>())).Returns(true);
    
    private void GivenInvalidSignature()
        => SendGridSignatureValidationServiceMock.Setup(
            s => s.IsValidSignature(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>())).Returns(false);
    
    private async Task WhenCallingEndpoint(List<SendGridEvent> events)
    {
        var content = JsonContent.Create(events);
        Response = await Sut.PostAsync($"api/sendgrid/receive-events", content);
    }
}