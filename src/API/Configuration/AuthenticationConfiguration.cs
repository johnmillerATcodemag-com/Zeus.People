using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Zeus.People.API.Configuration;

/// <summary>
/// Authentication configuration extensions
/// </summary>
public static class AuthenticationConfiguration
{
    /// <summary>
    /// Adds JWT authentication configuration with Key Vault support
    /// </summary>
    public static async Task<IServiceCollection> AddJwtAuthenticationAsync(this IServiceCollection services, IServiceProvider serviceProvider)
    {
        var configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Configuring JWT authentication");

            var jwtSettings = await configurationService.GetConfigurationAsync<JwtSettings>(JwtSettings.SectionName);
            jwtSettings.Validate();

            var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Use HTTPS in production
                options.RequireHttpsMetadata = !jwtSettings.AllowHttpInDevelopment;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(jwtSettings.ClockSkewMinutes),
                    RequireExpirationTime = true,
                    RequireSignedTokens = true
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers["Token-Expired"] = "true";
                        }
                        logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        logger.LogDebug("JWT token validated successfully for user: {User}",
                            context.Principal?.Identity?.Name ?? "Unknown");
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                // Add custom authorization policies
                options.AddPolicy("RequireAdminRole", policy =>
                    policy.RequireRole("Admin"));

                options.AddPolicy("RequireUserRole", policy =>
                    policy.RequireRole("User", "Admin"));

                options.AddPolicy("RequireManagerRole", policy =>
                    policy.RequireRole("Manager", "Admin"));

                // Add claim-based policies
                options.AddPolicy("RequireValidUser", policy =>
                    policy.RequireAuthenticatedUser());
            });

            logger.LogInformation("JWT authentication configured successfully");
            return services;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to configure JWT authentication");
            throw;
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new ArgumentNullException("JwtSettings:SecretKey");
        var key = Encoding.ASCII.GetBytes(secretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // Set to true in production
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers["Token-Expired"] = "true";
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            // Add custom authorization policies
            options.AddPolicy("RequireAdminRole", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("RequireUserRole", policy =>
                policy.RequireRole("User", "Admin"));
        });

        return services;
    }
}

/// <summary>
/// JWT settings configuration with validation
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// Secret key for JWT token signing and validation
    /// </summary>
    [Required(ErrorMessage = "JWT secret key is required")]
    [MinLength(32, ErrorMessage = "JWT secret key must be at least 32 characters long")]
    [JsonIgnore] // Sensitive data should not be serialized
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer
    /// </summary>
    [Required(ErrorMessage = "JWT issuer is required")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience
    /// </summary>
    [Required(ErrorMessage = "JWT audience is required")]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    [Range(1, 10080, ErrorMessage = "JWT expiration must be between 1 minute and 7 days")]
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Clock skew tolerance in minutes
    /// </summary>
    [Range(0, 30, ErrorMessage = "Clock skew must be between 0 and 30 minutes")]
    public int ClockSkewMinutes { get; set; } = 5;

    /// <summary>
    /// Allow HTTP in development (should be false in production)
    /// </summary>
    public bool AllowHttpInDevelopment { get; set; } = true;

    /// <summary>
    /// Validate the JWT settings
    /// </summary>
    public void Validate()
    {
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"JWT settings validation failed: {errors}");
        }

        // Additional business logic validation
        if (SecretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT secret key must be at least 32 characters long for security");
        }

        if (ExpirationMinutes > 1440 && !AllowHttpInDevelopment) // More than 24 hours in production
        {
            throw new InvalidOperationException("JWT expiration should not exceed 24 hours in production environments");
        }
    }
}
