using CurrencyConverterApi.Providers;
using CurrencyConverterApi.Services;
using CurrencyConverterApi.Middleware;  // Add this line for the custom middleware
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using AspNetCoreRateLimit;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging and Seq integration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()  // Log to console for local debugging
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"])  // Log to Seq
    .Enrich.FromLogContext()
    .CreateLogger();

// Register Serilog for dependency injection
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// Register the rate limiting services
builder.Services.AddOptions();
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();

// Register the IP address rate limiting middleware
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Register providers
builder.Services.AddScoped<FrankfurterCurrencyProvider>();
builder.Services.AddScoped<AnotherCurrencyProvider>();

// Register the factory
builder.Services.AddScoped<ICurrencyProviderFactory, CurrencyProviderFactory>();

// Register the authentication service
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Add services to the container
builder.Services.AddControllers();

// Register HttpClient and FrankfurterCurrencyProvider with memory cache
builder.Services.AddHttpClient<ICurrencyProvider, FrankfurterCurrencyProvider>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
builder.Services.AddMemoryCache();  // Add memory caching service

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],

        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"],

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"])),

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero  // Set to zero to remove delay in token expiration
    };
});

// Register Swagger for API documentation
builder.Services.AddSwaggerGen(c =>
{
    // Define the Bearer token security scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid JWT token",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    // Apply the security requirement globally to all endpoints
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Enable middleware to serve generated Swagger as a JSON endpoint
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply rate limiting middleware
app.UseIpRateLimiting();

// Add the custom request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();  // Register the logging middleware here

app.UseHttpsRedirection();

// Use Authentication and Authorization middleware
app.UseAuthentication();  // Add this line before UseAuthorization()
app.UseAuthorization();

app.MapControllers();

app.Run();

// Policy methods for retry and circuit breaker
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
