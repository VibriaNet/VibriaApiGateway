using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using src.Config;

namespace src.Middleware
{
    public static class JwtExtension
    {
        public static void AddJwtAuthentication(this IServiceCollection service, IConfiguration configuration)
        {
            _ = service.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer("Bearer", options =>
            {
                var jwtConfig = configuration.GetSection("JwtConfig").Get<JwtConfig>();
                var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig!.SecretKey!));

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig!.Issuer,
                    ValidAudience = jwtConfig.Audience,
                    IssuerSigningKey = secretKey,
                    ValidateLifetime = true,
                };
                
                options.RequireHttpsMetadata = true;

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        using var scope = context.HttpContext.RequestServices.CreateScope();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
                        logger.LogError(context.Exception, "Error occurred OnAuthenticationFailed, {Message}", context.Exception.Message);

                        var response = new { Message = "Authentication failed" };

                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                            response = new { Message = "Token expired, please login again" };
                            _ = context.Response.WriteAsync(JsonSerializer.Serialize(response));
                        }
                        if (context.Exception.GetType() == typeof(SecurityTokenException))
                        {
                            _ = context.Response.WriteAsync(JsonSerializer.Serialize(response));
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        }
    }
}
