using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;

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
}