using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using MassTransit;
using NotifyService.Api.Requests;

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
            var sqsEvent = JsonSerializer.Deserialize<SQSEvent>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            logger.LogInformation("Sqs event: {sqsEvent}", JsonSerializer.Serialize(sqsEvent));
            
            var busControl = scope.ServiceProvider.GetRequiredService<IBusControl>();

            foreach (var message in sqsEvent!.Records)
            {
                await busControl.Publish(JsonSerializer.Deserialize<SendEmailRequest>(message.Body)!, CancellationToken.None);
            }
            
            return await Task.FromResult<APIGatewayProxyResponse>(null!);
        }

        throw new InvalidOperationException("Unsupported event type");
    }
}