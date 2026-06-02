using BankLite.API.Hubs;
using BankLite.API.Middleware;
using BankLite.API.Options;
using BankLite.API.Services;
using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using BankLite.Application.Options;
using BankLite.Application.Services;
using BankLite.Application.Validators;
using BankLite.Domain.Interfaces;
using BankLite.Infrastructure.Data;
using BankLite.Infrastructure.Repositories;
using BankLite.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SendGrid;
using Serilog;
using Serilog.Events;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Is(context.HostingEnvironment.IsDevelopment()
            ? LogEventLevel.Information
            : LogEventLevel.Warning)
        .WriteTo.Console();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Title = "BankLite API",
            Version = "v1",
            Description =
                "A clean architecture banking API with JWT authentication via HttpOnly cookies. Rate limited: 30 req/min global, 5 req/min login, 3 req/min register, 3 req/min forgot/reset password.",
            Contact = new OpenApiContact { Name = "Nick", Url = new Uri("https://github.com/NicholasXydis") }
        });

    c.AddSecurityDefinition("cookieAuth",
        new OpenApiSecurityScheme
        {
            Name = "accessToken",
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Cookie,
            Description = "JWT access token stored in HttpOnly cookie. Login or register to authenticate."
        });

    OpenApiSecurityRequirement requirement = new()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "cookieAuth" }
            },
            Array.Empty<string>()
        }
    };
    c.AddSecurityRequirement(requirement);

    c.EnableAnnotations();

    string xmlFile = $"{typeof(AccountResponseDto).Assembly.GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddDbContext<BankLiteDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            5,
            TimeSpan.FromSeconds(30),
            null
        )
    ));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
builder.Services.AddSingleton<ISendGridClient>(serviceProvider =>
{
    SendGridSettings settings = serviceProvider.GetRequiredService<IOptions<SendGridSettings>>().Value;
    return new SendGridClient(settings.ApiKey);
});
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddHttpClient<IGroqService, GroqService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<IValidator<CreateAccountDto>, CreateAccountValidator>();
builder.Services.AddScoped<IValidator<LoginUserDto>, LoginUserValidator>();
builder.Services.AddScoped<IValidator<RegisterUserDto>, RegisterUserValidator>();
builder.Services.AddScoped<IValidator<DepositWithdrawDto>, DepositWithdrawValidator>();
builder.Services.AddScoped<IValidator<TransferDto>, TransferValidator>();
builder.Services.AddScoped<IValidator<ExternalTransferDto>, ExternalTransferValidator>();
builder.Services.AddScoped<IValidator<ChangePasswordDto>, ChangePasswordValidator>();
builder.Services.AddScoped<IValidator<ChatMessageDto>, ChatMessageValidator>();
builder.Services.AddScoped<IValidator<ForgotPasswordDto>, ForgotPasswordValidator>();
builder.Services.AddScoped<IValidator<ResetPasswordDto>, ResetPasswordValidator>();
builder.Services.AddResponseCompression();
builder.Services.AddSignalR();
builder.Services.AddScoped<IBalanceNotifier, SignalRBalanceNotifier>();
builder.Services.AddHostedService<TokenCleanupService>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BankLiteDbContext>("database");

builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .Validate(settings =>
            !string.IsNullOrWhiteSpace(settings.Secret) &&
            settings.Secret.Length >= 32 &&
            !settings.Secret.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase),
        "JWT secret must be configured with a non-placeholder value of at least 32 characters.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer), "JWT issuer is required.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.Audience), "JWT audience is required.")
    .Validate(settings => settings.ExpiryMinutes is > 0 and <= 1440, "JWT expiry must be between 1 and 1440 minutes.")
    .ValidateOnStart();

builder.Services.AddOptions<AllowedOriginsSettings>()
    .Bind(builder.Configuration.GetSection(AllowedOriginsSettings.SectionName))
    .Validate(settings => Uri.TryCreate(settings.Frontend, UriKind.Absolute, out _),
        "AllowedOrigins:Frontend must be a valid absolute URI.")
    .ValidateOnStart();

builder.Services.AddOptions<FrontendSettings>()
    .Bind(builder.Configuration.GetSection(FrontendSettings.SectionName))
    .Validate(settings => Uri.TryCreate(settings.ResetPasswordUrl, UriKind.Absolute, out _),
        "Frontend:ResetPasswordUrl must be a valid absolute URI.")
    .ValidateOnStart();

builder.Services.AddOptions<GroqSettings>()
    .Bind(builder.Configuration.GetSection(GroqSettings.SectionName))
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.ApiKey), "Groq API key is required.")
    .ValidateOnStart();

builder.Services.AddOptions<SendGridSettings>()
    .Bind(builder.Configuration.GetSection(SendGridSettings.SectionName))
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.ApiKey), "SendGrid API key is required.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.FromEmail), "SendGrid from email is required.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.FromName), "SendGrid from name is required.")
    .ValidateOnStart();

builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        string frontendOrigin = builder.Configuration["AllowedOrigins:Frontend"]
                                ?? throw new InvalidOperationException("AllowedOrigins:Frontend is not configured.");
        List<string> allowedOrigins = [frontendOrigin];

        if (builder.Environment.IsDevelopment())
        {
            allowedOrigins.Add("http://127.0.0.1:5500");
            allowedOrigins.Add("https://localhost:3000");
        }

        policy.WithOrigins(allowedOrigins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    bool isTest = builder.Environment.IsEnvironment("Testing");

    options.AddPolicy("fixed", context => CreateFixedWindowPartition(context, "fixed", isTest ? 10000 : 30,
        TimeSpan.FromMinutes(1), isTest ? 0 : 5));
    options.AddPolicy("login", context => CreateFixedWindowPartition(context, "login", isTest ? 10000 : 5,
        TimeSpan.FromMinutes(1), 0));
    options.AddPolicy("register", context => CreateFixedWindowPartition(context, "register", isTest ? 10000 : 3,
        TimeSpan.FromMinutes(1), 0));
    options.AddPolicy("chat", context => CreateFixedWindowPartition(context, "chat", isTest ? 10000 : 10,
        TimeSpan.FromMinutes(1), 0));
    options.AddPolicy("refresh", context => CreateFixedWindowPartition(context, "refresh", isTest ? 10000 : 10,
        TimeSpan.FromMinutes(1), 0));
    options.AddPolicy("forgotpassword", context => CreateFixedWindowPartition(context, "forgotpassword",
        isTest ? 10000 : 3, TimeSpan.FromMinutes(1), 0));
    options.AddPolicy("changepassword", context => CreateFixedWindowPartition(context, "changepassword",
        isTest ? 10000 : 5, TimeSpan.FromMinutes(1), 0));
    options.RejectionStatusCode = 429;
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BankLite API v1");
        c.RoutePrefix = "swagger";
    });
}

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    using IServiceScope scope = app.Services.CreateScope();
    BankLiteDbContext context = scope.ServiceProvider.GetRequiredService<BankLiteDbContext>();
    await context.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
    {
        await SeedData.SeedAsync(context);
    }

    return;
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseResponseCompression();

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseMiddleware<CsrfProtectionMiddleware>();

app.UseAuthentication();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        object response = app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")
            ? new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description
                })
            }
            : new { status = report.Status.ToString() };

        string result = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(result);
    }
});

app.MapHub<BankHub>("/hubs/bank");

app.Lifetime.ApplicationStopping.Register(() =>
    Log.Information("Application is shutting down..."));

await app.RunAsync();

static RateLimitPartition<string> CreateFixedWindowPartition(
    HttpContext context,
    string policyName,
    int permitLimit,
    TimeSpan window,
    int queueLimit)
{
    string partitionKey = GetRateLimitPartitionKey(context, policyName);
    return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = queueLimit
        });
}

static string GetRateLimitPartitionKey(HttpContext context, string policyName)
{
    string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!string.IsNullOrWhiteSpace(userId))
    {
        return $"{policyName}:user:{userId}";
    }

    string remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return $"{policyName}:ip:{remoteIp}";
}

public partial class Program;
