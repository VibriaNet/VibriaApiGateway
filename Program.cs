using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using Serilog.Core;
using src.Config;
using src.Middleware;
using VibriaApiGateway;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddEndpointsApiExplorer();


    var basePath = Environment.GetEnvironmentVariable("BASE_PATH") ??
                   builder.Configuration.GetSection("BasePath").Value;
    if (string.IsNullOrWhiteSpace(basePath))
        throw new ArgumentException("BasePath environment variable or configuration is missing.");

    var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("ocelot.global.json", optional: false, reloadOnChange: true)
            .AddJsonFile(Path.Combine(basePath, "config.json"), optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();


    builder.Services.AddOcelot(configuration);
    builder.Services.AddSwaggerForOcelot(configuration);


    builder.Host.UseSerilog((context, loggerConfiguration) =>
    {
        var levelSwitch = new LoggingLevelSwitch();
        loggerConfiguration
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo.Console()
            .WriteTo.Seq(
                serverUrl: configuration.GetSection("SerilogConfig:SeqUrl").Value!,
                controlLevelSwitch: levelSwitch);
    });
    builder.Services.AddLogging();
    builder.Services.AddJwtAuthentication(configuration);
    builder.Services.Configure<JwtConfig>(configuration.GetSection("JwtConfig"));

    // Add Swagger for API Gateway documentation

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerForOcelotUI(opt =>
        {
            opt.PathToSwaggerGenerator = "/swagger/docs";
            opt.ReConfigureUpstreamSwaggerJson = AlterUpstream.AlterUpstreamSwaggerJson;
        });
    }

    app.UseOcelot().Wait();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Vibria API Gateway server terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
