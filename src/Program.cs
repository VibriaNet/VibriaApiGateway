using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using Serilog.Core;
using src.Config;
using src.Middleware;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Base path configuration
    var basePath = Environment.GetEnvironmentVariable("BASE_PATH") ?? builder.Configuration.GetSection("BasePath").Value;
    if (string.IsNullOrWhiteSpace(basePath))
        throw new ArgumentException("BasePath environment variable or configuration is missing.");

    var configuration = new ConfigurationBuilder()
        .SetBasePath(basePath)
        .AddJsonFile("config.json", optional: true, reloadOnChange: true)
        .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

    builder.Services.AddOcelot(configuration);

    // Serilog Configuration
    builder.Host.UseSerilog((context, loggerConfiguration) =>
    {
        var levelSwitch = new LoggingLevelSwitch();
        loggerConfiguration.WriteTo.Console()
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo.Seq(
                serverUrl: configuration.GetSection("SerilogConfig:SeqUrl").Value!,
                controlLevelSwitch: levelSwitch);
    });
    
    builder.Services.AddJwtAuthentication(configuration);

    builder.Services.Configure<JwtConfig>(configuration.GetSection("JwtConfig"));

    // Add Swagger for API Gateway documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // Ocelot Middleware
    await app.UseOcelot();

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
