using System.Net;
using AutoFixture;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NotifyService.Api.Services;
using SendGrid;

namespace NotifyService.Api.Integration.Tests;

public class IntegrationTestBase : IDisposable
{
    private readonly WebApplicationFactory<Startup> _factory;
    protected readonly HttpClient Sut;
    protected readonly Mock<ISendGridSignatureValidationService> SendGridSignatureValidationServiceMock;
    protected readonly Mock<ISendGridClient> SendGridClientMock;
    protected HttpResponseMessage Response = null!;
    protected readonly Fixture Fixture;

    protected IntegrationTestBase()
    {
        SendGridSignatureValidationServiceMock = new Mock<ISendGridSignatureValidationService>();
        SendGridClientMock = new Mock<ISendGridClient>();
        Mock<IPublishEndpoint> publishEndpointMock = new();
        Fixture = new Fixture();
        _factory = new WebApplicationFactory<Startup>();
        Sut = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(configureServices =>
                {
                    configureServices.AddSingleton<ISendGridSignatureValidationService>(
                        s => SendGridSignatureValidationServiceMock.Object);
                    configureServices.AddTransient<ISendGridClient>(s => SendGridClientMock.Object);
                    configureServices.AddTransient<IPublishEndpoint>(s => publishEndpointMock.Object);
                    configureServices.AddMassTransitTestHarness();
                });
                
                builder.UseEnvironment("Development");
            })
            .CreateClient();
    }
    
    protected void ThenReturnOk()
    {
        Assert.Equal(HttpStatusCode.OK, Response.StatusCode);
    }
    
    protected void ThenReturnBadRequest()
    {
        Assert.Equal(HttpStatusCode.BadRequest, Response.StatusCode);
    }

    protected void ThenReturnUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, Response.StatusCode);
    }

    public void Dispose()
    {
        Sut.Dispose();
        _factory.Dispose();
    }
}