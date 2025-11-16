using System.IdentityModel.Tokens.Jwt;
using System.Text;
using BlockchainAidTracker.Blockchain;
using BlockchainAidTracker.Cryptography;
using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.Services;
using BlockchainAidTracker.Services.Configuration;
using BlockchainAidTracker.SmartContracts;
using BlockchainAidTracker.SmartContracts.Engine;
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

// Configure Consensus settings
var consensusSettings = new ConsensusSettings
{
    BlockCreationIntervalSeconds = builder.Configuration.GetValue<int>("ConsensusSettings:BlockCreationIntervalSeconds", 30),
    MinimumTransactionsPerBlock = builder.Configuration.GetValue<int>("ConsensusSettings:MinimumTransactionsPerBlock", 1),
    MaximumTransactionsPerBlock = builder.Configuration.GetValue<int>("ConsensusSettings:MaximumTransactionsPerBlock", 100),
    ValidatorPassword = builder.Configuration["ConsensusSettings:ValidatorPassword"] ?? "ValidatorPassword123!",
    EnableAutomatedBlockCreation = builder.Configuration.GetValue<bool>("ConsensusSettings:EnableAutomatedBlockCreation", true)
};

// Configure Blockchain Persistence settings
var persistenceSettings = new BlockchainAidTracker.Blockchain.Configuration.BlockchainPersistenceSettings
{
    Enabled = builder.Configuration.GetValue<bool>("BlockchainPersistenceSettings:Enabled", false),
    FilePath = builder.Configuration["BlockchainPersistenceSettings:FilePath"] ?? "blockchain-data.json",
    AutoSaveAfterBlockCreation = builder.Configuration.GetValue<bool>("BlockchainPersistenceSettings:AutoSaveAfterBlockCreation", true),
    AutoLoadOnStartup = builder.Configuration.GetValue<bool>("BlockchainPersistenceSettings:AutoLoadOnStartup", true),
    CreateBackup = builder.Configuration.GetValue<bool>("BlockchainPersistenceSettings:CreateBackup", true),
    MaxBackupFiles = builder.Configuration.GetValue<int>("BlockchainPersistenceSettings:MaxBackupFiles", 5)
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
            ?? new[] { "http://localhost:5000", "https://localhost:5001", "http://localhost:5002", "https://localhost:5003" };

        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Token-Expired", "Authorization")
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
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

// Register blockchain with persistence support
var hashService = new HashService();
var digitalSignatureService = new DigitalSignatureService();

// Use persistence if enabled, otherwise use standard blockchain
if (persistenceSettings.Enabled && !builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddBlockchainWithPersistence(hashService, digitalSignatureService, persistenceSettings);
}
else
{
    builder.Services.AddSingleton(sp => new Blockchain(hashService, digitalSignatureService));
}

// Register DataAccess layer - skip in Testing environment (will be configured by tests)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDataAccess(builder.Configuration);
}

// Register Services layer - blockchain instance will be resolved from DI
builder.Services.AddServices(jwtSettings);

// Register consensus settings as singleton
builder.Services.AddSingleton(consensusSettings);

// Register Proof-of-Authority consensus engine
builder.Services.AddProofOfAuthorityConsensus();

// Register SmartContracts with auto-deployment
builder.Services.AddSmartContractsWithAutoDeployment();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

// Register background service for automated block creation
builder.Services.AddHostedService<BlockchainAidTracker.Services.BackgroundServices.BlockCreationBackgroundService>();

var app = builder.Build();

// Get the blockchain instance from the actual service provider and configure it
var blockchain = app.Services.GetRequiredService<Blockchain>();
// Disable signature validation in Development and Testing until private key management is fully set up
blockchain.ValidateTransactionSignatures = false; // TODO: Enable when private keys are properly managed
blockchain.ValidateBlockSignatures = false; // Block validator signatures not yet implemented

// Deploy smart contracts after building the app
app.Services.DeployContracts();

// Apply database migrations in development (but not in testing)
if (app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Delete existing database for demo purposes (as per CLAUDE.md - "Reset database state on application start")
    //dbContext.Database.EnsureDeleted();
    dbContext.Database.Migrate();

    app.Logger.LogInformation("Database migrations applied successfully");
}

// Load blockchain from persistence if configured
if (persistenceSettings.Enabled && persistenceSettings.AutoLoadOnStartup && !app.Environment.IsEnvironment("Testing"))
{
    try
    {
        var loaded = await blockchain.LoadFromPersistenceAsync();
        if (loaded)
        {
            app.Logger.LogInformation("Blockchain loaded from persistence: {BlockCount} blocks", blockchain.Chain.Count);
        }
        else
        {
            app.Logger.LogInformation("No persisted blockchain found, starting with genesis block");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to load blockchain from persistence on startup");
        // Continue without persisted data
    }
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

// Log deployed smart contracts
var contractEngine = app.Services.GetRequiredService<SmartContractEngine>();
var deployedContracts = contractEngine.GetAllContracts();
app.Logger.LogInformation("Smart contract engine initialized with {ContractCount} deployed contracts", deployedContracts.Count);

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
