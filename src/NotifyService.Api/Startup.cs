using MassTransit;
using SendGrid.Extensions.DependencyInjection;

namespace NotifyService.Api;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

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
            });
        });
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