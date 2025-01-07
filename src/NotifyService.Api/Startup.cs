using MassTransit;
using NotifyService.Api.Requests;
using NotifyService.Api.Services;
using SendGrid.Extensions.DependencyInjection;

namespace NotifyService.Api;

public class Startup(IConfiguration configuration)
{
    private IConfiguration Configuration { get; } = configuration;

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddHealthChecks();
        services.AddSwaggerGen();
        services.AddSendGrid(options =>
        {
            options.ApiKey = Configuration["SendGridApiKey"];
        });
        services.AddMassTransit(config =>
        {
            config.UsingAmazonSqs((context, cfg) =>
            {
                cfg.Host("eu-west-2", h =>
                {
                });
                
                cfg.Message<SendGridEvent>(m =>
                {
                    m.SetEntityName("sendgrid-events-topic");
                });
            });
        });
        services.AddSingleton<ISendGridSignatureValidationService, SendGridSignatureValidationService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/",
                async context =>
                {
                    await context.Response.WriteAsync("Welcome to the Notify Service!");
                });
            endpoints.MapHealthChecks("/api/status").AllowAnonymous();
        });
        app.UseSwagger();
        app.UseSwaggerUI();
    }
}