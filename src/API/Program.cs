using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Core.Domain.Interfaces;
using Core.Application.Services;
using Core.Application.Interfaces;
using System.Text.Json.Serialization;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using API.Authentication;

// Configurar Serilog ANTES de crear el builder
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/ms-core-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

// Configurar Npgsql para manejar correctamente timestamps con zona horaria
//AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

try
{
    Log.Information("Starting MS Core API");

    var builder = WebApplication.CreateBuilder(args);

    // Add environment variables configuration
    builder.Configuration.AddEnvironmentVariables();

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Configurar para que los Enums se serialicen como strings en lugar de números
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    // Configure routing options to handle encoded characters in URLs
    builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(options =>
    {
        options.ConstraintMap["regex"] = typeof(Microsoft.AspNetCore.Routing.Constraints.RegexRouteConstraint);
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Riwi Wallet - MS Core API", Version = "v1" });
    });

    // Database Configuration (prioriza variable de entorno sobre appsettings)
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "No se encontró la cadena de conexión. Configure ConnectionStrings__DefaultConnection en variables de entorno.");
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Dependency Injection - Repositories
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
    builder.Services.AddScoped<IFinancialRuleRepository, FinancialRuleRepository>();
    builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    builder.Services.AddScoped<ITelegramLinkCodeRepository, TelegramLinkCodeRepository>();

    // Dependency Injection - Services
    builder.Services.AddScoped<ISpendingValidationService, SpendingValidationService>();
    builder.Services.AddScoped<ITransactionService, TransactionService>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<ITelegramService, TelegramService>();

    // Authentication Configuration - Dual scheme: JWT + API Key for internal services
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "");

    builder.Services.AddAuthentication(options =>
    {
        // Use a policy scheme that chooses between JWT and API Key
        options.DefaultAuthenticateScheme = "JwtOrApiKey";
        options.DefaultChallengeScheme = "JwtOrApiKey";
    })
    .AddPolicyScheme("JwtOrApiKey", "JWT or API Key", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            // If X-Api-Key header is present, use API Key authentication
            if (context.Request.Headers.ContainsKey("X-Api-Key"))
            {
                return "ApiKey";
            }
            // Otherwise use JWT
            return JwtBearerDefaults.AuthenticationScheme;
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            // Use ONLY the resolver (not IssuerSigningKey) to handle tokens without 'kid'
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                // Always return the configured key, regardless of 'kid' value
                return new[] { new SymmetricSecurityKey(secretKey) };
            },
            // TEMPORARILY disabled to test - tokens without iss/aud will fail otherwise
            ValidateIssuer = false,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = false,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.Zero
        };

        // Logging detallado
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Error($"❌ JWT Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Log.Information("✅ JWT Token validated successfully");
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // CORS Configuration for Microservices
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowMicroservices", policy =>
        {
            policy.WithOrigins(
                // Local development
                "http://localhost:8080",  // MS AI Worker (Spring Boot)
                "http://localhost:5173",  // Dashboard (Vue.js)
                "http://localhost:8081",  // Gateway (Spring Boot)
                // Production
                "https://riwiwallet.stevencardona.dev",  // Frontend production
                "https://sbservice.riwiwallet.stevencardona.dev"  // Spring Boot backend production
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
    });

    var app = builder.Build();

    // Ejecuta migraciones pendientes al iniciar (se puede desactivar con Database:AutoMigrateOnStartup=false)
    var autoMigrateOnStartup = builder.Configuration.GetValue<bool?>("Database:AutoMigrateOnStartup") ?? true;
    if (autoMigrateOnStartup)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Log.Information("Applying database migrations...");
        dbContext.Database.Migrate();
        Log.Information("Database migrations applied successfully");
    }
    else
    {
        Log.Information("Automatic database migrations are disabled by configuration");
    }

    // Configure the HTTP request pipeline
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Riwi Wallet - MS Core API v1"));

    // Agregar Serilog request logging
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    // Enable CORS
    app.UseCors("AllowMicroservices");
    
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("MS Core API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
