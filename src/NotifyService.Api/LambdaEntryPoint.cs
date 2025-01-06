using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

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
        var logger = _serviceProvider.GetService<ILogger<LambdaEntryPoint>>();
        
        var jsonString = JsonSerializer.Serialize(input);
        logger.LogInformation("Input : {JsonString}", jsonString);
        
        if (jsonString.Contains("\"httpMethod\""))
        {
            var apiGatewayRequest = JsonSerializer.Deserialize<APIGatewayProxyRequest>(jsonString);
            logger.LogInformation("ApiGatewayRequest: {ApiGatewayRequest}", apiGatewayRequest);
            return await base.FunctionHandlerAsync(apiGatewayRequest, context);
        }

        if (jsonString.Contains("\"Records\""))
        {
            var sqsEvent = JsonSerializer.Deserialize<SQSEvent>(jsonString);
            logger.LogInformation("Sqs event: {sqsEvent}", sqsEvent);
            foreach (var record in sqsEvent.Records)
            {
                logger.LogInformation("Record: {Record}", record);
            }
            
            return await Task.FromResult<APIGatewayProxyResponse>(null!);
        }

        throw new InvalidOperationException("Unsupported event type");
    }
}