using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

namespace NotifyService.Api;

/// <summary>
/// This class extends from APIGatewayProxyFunction which contains the method FunctionHandlerAsync which is the 
/// actual Lambda function entry point. The Lambda handler field should be set to
/// 
/// NotifyService.Api::NotifyService.Api.LambdaEntryPoint::FunctionHandlerAsync
/// </summary>
public class LambdaEntryPoint :

    // The base class must be set to match the AWS service invoking the Lambda function. If not Amazon.Lambda.AspNetCoreServer
    // will fail to convert the incoming request correctly into a valid ASP.NET Core request.
    //
    // API Gateway REST API                         -> Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
    // API Gateway HTTP API payload version 1.0     -> Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
    // API Gateway HTTP API payload version 2.0     -> Amazon.Lambda.AspNetCoreServer.APIGatewayHttpApiV2ProxyFunction
    // Application Load Balancer                    -> Amazon.Lambda.AspNetCoreServer.ApplicationLoadBalancerFunction
    // 
    // Note: When using the AWS::Serverless::Function resource with an event type of "HttpApi" then payload version 2.0
    // will be the default and you must make Amazon.Lambda.AspNetCoreServer.APIGatewayHttpApiV2ProxyFunction the base class.
    Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
{
    private IServiceProvider _serviceProvider;
    
    /// <summary>
    /// The builder has configuration, logging and Amazon API Gateway already configured. The startup class
    /// needs to be configured in this method using the UseStartup<>() method.
    /// </summary>
    /// <param name="builder">The IWebHostBuilder to configure.</param>
    protected override void Init(IWebHostBuilder builder)
    {
        builder.UseStartup<Startup>();
    }

    /// <summary>
    /// Use this override to customize the services registered with the IHostBuilder. 
    /// 
    /// It is recommended not to call ConfigureWebHostDefaults to configure the IWebHostBuilder inside this method.
    /// Instead customize the IWebHostBuilder in the Init(IWebHostBuilder) overload.
    /// </summary>
    /// <param name="builder">The IHostBuilder to configure.</param>
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
    
    [LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
    public async Task FunctionHandlerAsync(object input, ILambdaContext context)
    {
        var logger = _serviceProvider.GetService<ILogger<LambdaEntryPoint>>();
        logger.LogInformation("LambdaEntryPoint: {Input}", input);
        var input2 = JsonSerializer.Deserialize<object>(input.ToString());
        if (input2 is APIGatewayProxyRequest apiGatewayRequest)
        {
            logger.LogInformation("LambdaEntryPoint: {ApiGatewayRequest}", apiGatewayRequest);
            await base.FunctionHandlerAsync(apiGatewayRequest, context);
        }
        else if (input2 is SQSEvent sqsEvent)
        {
            foreach (var record in sqsEvent.Records)
            {
                logger.LogInformation("Processing SQS message: {Message}", record.Body);
            }
        }
        else
        {
            throw new InvalidOperationException("Unsupported input type");
        }
    }
}