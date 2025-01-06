using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

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
    
    public override async Task<APIGatewayProxyResponse> FunctionHandlerAsync(APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        var logger = _serviceProvider.GetService<ILogger<LambdaEntryPoint>>();
        logger.LogInformation("LambdaEntryPoint: {Request}", request);
        return await base.FunctionHandlerAsync(request, context);
    }
    
    public void HandleSQSEventAsync(SQSEvent sqsEvent, ILambdaContext context)
    {
        var logger = _serviceProvider.GetService<ILogger<LambdaEntryPoint>>();

        foreach (var record in sqsEvent.Records)
        {
            logger.LogInformation("Record: {Record}", record);
        }
    }
}

public class LambdaFunction
{
    private readonly LambdaEntryPoint _entryPoint = new LambdaEntryPoint();

    [LambdaSerializer(typeof (DefaultLambdaJsonSerializer))]
    public async Task FunctionHandlerAsync(object input, ILambdaContext context)
    {
        switch (input)
        {
            case APIGatewayProxyRequest apiGatewayRequest:
                await _entryPoint.FunctionHandlerAsync(apiGatewayRequest, context);
                break;

            case SQSEvent sqsEvent: 
                _entryPoint.HandleSQSEventAsync(sqsEvent, context);
                break;

            default:
                throw new InvalidOperationException("Unsupported event type");
        }
    }
}