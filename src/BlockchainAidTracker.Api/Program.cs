using System.IdentityModel.Tokens.Jwt;
using System.Text;
using BlockchainAidTracker.Blockchain;
using BlockchainAidTracker.Cryptography;
using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.Services;
using BlockchainAidTracker.Services.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// Clear default JWT claim type mappings to preserve original claim types
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Configure JWT settings
var jwtSettings = new JwtSettings
{
    SecretKey = builder.Configuration["JwtSettings:SecretKey"]
        ?? throw new InvalidOperationException("JWT SecretKey not configured"),
    Issuer = builder.Configuration["JwtSettings:Issuer"] ?? "BlockchainAidTracker.Api",
    Audience = builder.Configuration["JwtSettings:Audience"] ?? "BlockchainAidTracker.Web",
    AccessTokenExpirationMinutes = builder.Configuration.GetValue<int>("JwtSettings:ExpirationMinutes", 60),
    RefreshTokenExpirationDays = builder.Configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 7)
};

// Add controllers
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Blockchain Aid Tracker API",
        Version = "v1",
        Description = "API for blockchain-based humanitarian aid supply chain tracking system"
    });

    // Configure JWT authentication in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5000", "https://localhost:5001" };

        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero // Remove default 5 minute tolerance
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Register cryptography services
builder.Services.AddSingleton<IHashService, HashService>();
builder.Services.AddSingleton<IDigitalSignatureService, DigitalSignatureService>();

// Register blockchain with signature validation enabled
var hashService = new HashService();
var digitalSignatureService = new DigitalSignatureService();
var blockchain = new Blockchain(hashService, digitalSignatureService)
{
    // Transaction signatures are now validated with real cryptographic keys
    ValidateTransactionSignatures = true,
    // Block validator signatures not yet implemented
    ValidateBlockSignatures = false
};
builder.Services.AddSingleton(blockchain);

// Register DataAccess layer - skip in Testing environment (will be configured by tests)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDataAccess(builder.Configuration);
}

// Register Services layer with blockchain
builder.Services.AddServicesWithBlockchain(jwtSettings, blockchain);

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

// Apply database migrations in development (but not in testing)
if (app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Delete existing database for demo purposes (as per CLAUDE.md - "Reset database state on application start")
    dbContext.Database.EnsureDeleted();
    dbContext.Database.Migrate();

    app.Logger.LogInformation("Database migrations applied successfully");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Blockchain Aid Tracker API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Logger.LogInformation("Blockchain Aid Tracker API starting...");
app.Logger.LogInformation("Blockchain initialized with {BlockCount} blocks", blockchain.Chain.Count);

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
