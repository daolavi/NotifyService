using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using MassTransit.Transports;
using NotifyService.Api.Consumers;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace NotifyService.Api;

public class LambdaEntryPoint : APIGatewayProxyFunction
{
    private IServiceProvider _serviceProvider;
    
    protected override void Init(IWebHostBuilder builder)
    {
        builder.UseStartup<Startup>();
    }

    protected override void Init(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddAmazonSecretManager("eu-west-2", "NotifyServiceSecrets");
        });
        
        builder.ConfigureServices(services =>
        {
            // Capture the service provider
            _serviceProvider = services.BuildServiceProvider();
        });
    }
    
    public async Task<APIGatewayProxyResponse> FunctionHandlerAsync(object input, ILambdaContext context)
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<LambdaEntryPoint>>();
        
        var jsonString = JsonSerializer.Serialize(input);
        logger.LogInformation("Input : {JsonString}", jsonString);
        
        if (jsonString.Contains("\"httpMethod\""))
        {
            var apiGatewayRequest = JsonSerializer.Deserialize<APIGatewayProxyRequest>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            logger.LogInformation("ApiGatewayRequest: {ApiGatewayRequest}", JsonSerializer.Serialize(apiGatewayRequest));
            return await base.FunctionHandlerAsync(apiGatewayRequest, context);
        }

        if (jsonString.Contains("\"Records\""))
        {
            var sqsEvent = JsonSerializer.Deserialize<SQSEvent>(jsonString);
            logger.LogInformation("Sqs event: {sqsEvent}", JsonSerializer.Serialize(sqsEvent));
            var ep = scope.ServiceProvider.GetRequiredService<IReceiveEndpointDispatcher<SendEmailRequestConsumer>>();
            
            foreach (var record in sqsEvent?.Records ?? [])
            {
                var headers = new Dictionary<string, object>();
                foreach (var key in record.Attributes.Keys)
                {
                    headers[key] = record.Attributes[key];
                }
                foreach (var key in record.MessageAttributes.Keys)
                {
                    headers[key] = record.MessageAttributes[key];
                }
                var body = Encoding.UTF8.GetBytes(record.Body);
                await ep.Dispatch(body, headers, CancellationToken.None);
            }
            return await Task.FromResult<APIGatewayProxyResponse>(null!);
        }

        throw new InvalidOperationException("Unsupported event type");
    }
}