using System.Text;
using Application.Common;
using Infraestructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WebAPI.Middleware;
using WebAPI.Security;

namespace WebAPI.Extensions;

public static class AuthenticationExtensions
{
    public const string AdministratorPolicy = "AdministratorOnly";

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
            throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = CurrentUser.SubjectClaim,
                    RoleClaimType = CurrentUser.RoleClaim
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return ExceptionHandlingMiddleware.WriteAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            AppErrorCodes.Unauthorized,
                            "Authentication is required.");
                    },
                    OnForbidden = context => ExceptionHandlingMiddleware.WriteAsync(
                        context.HttpContext,
                        StatusCodes.Status403Forbidden,
                        AppErrorCodes.Forbidden,
                        "You do not have permission to perform this action.")
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AdministratorPolicy, policy => policy.RequireRole(Roles.Administrator));

        return services;
    }
}
