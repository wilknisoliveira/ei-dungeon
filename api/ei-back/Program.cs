using ei_back.Infrastructure.Context;
using ei_back.Infrastructure.Exceptions;
using ei_back.Infrastructure.Mappings;
using ei_back.Infrastructure.Swagger;
using ei_back.Infrastructure.Token;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Globalization;
using System.Text;
using ei_back.UserInterface.Hubs;
using ei_back.Infrastructure.Extensions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTrustedReverseProxy(builder.Configuration);

//Logs
builder.Logging.ClearProviders();
var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "Infrastructure/Logs/logs.txt", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7, 
        fileSizeLimitBytes: 10_000_000, // 10 MB
        rollOnFileSizeLimit: true)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

//Deploy
//var port = Environment.GetEnvironmentVariable("PORT") ?? "8081";
//builder.WebHost.UseUrls($"http://*:{port}");

//Exception Handler
builder.Services.AddExceptionHandler<AppExceptionHandler>();

//Token Configurations
var tokenConfigurations = new TokenConfiguration();

new ConfigureFromConfigurationOptions<TokenConfiguration>(
    builder.Configuration.GetSection("TokenConfigurations")
    ).Configure(tokenConfigurations);

builder.Services.AddSingleton(tokenConfigurations);

//Token Configurations -> Define the authentication parameters
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = tokenConfigurations.Issuer,
        ValidAudience = tokenConfigurations.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenConfigurations.Secret))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
                //context.HttpContext.Request.Headers.Add("Authorization", "Bearer " + accessToken);
            }
            return Task.CompletedTask;
        }
    };
});

//Token Configurations -> Authorize
builder.Services.AddAuthorization(auth =>
{
    auth.AddPolicy("Bearer", new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser().Build()
        );
});

//Cors
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"] ?? "";
var origins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries);

builder.Services.AddCors(options => options.AddDefaultPolicy(builder =>
{
    builder.WithOrigins(origins.Length > 0 ? origins : ["http://localhost:4200", "http://localhost:8000"])
        .AllowAnyMethod()
        .WithHeaders(["Content-Type", "Accept-Language", "Authorization", "X-Requested-With", "Accept", "Origin", "Cookie"])
        .WithExposedHeaders("Retry-After")
        .AllowCredentials()
        .SetPreflightMaxAge(TimeSpan.FromHours(1));
}));

//Native
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Support for requests with enum description
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddInfrastructureSwagger();

//Database
var connection = builder.Configuration["PostgresConnection:PostgresConnectionString"];
builder.Services.AddDbContext<EIContext>(options => options.UseNpgsql(
    connection,
    assembly => assembly.MigrationsAssembly(typeof(EIContext).Assembly.FullName))
);

//HealthChecks
builder.Services.AddHealthChecks()
    .AddNpgSql(connection, name: "Postgres Check", tags: new string[] { "db", "data" });

//AutoMapper
builder.Services.AddAutoMapper(typeof(MappingsProfile));

//SignalR
builder.Services.AddSignalR();

//Resources
builder.Services.AddApplicationLocalization();

builder.Services.AddRateLimitingPolicies();
builder.Services.AddRepositories();
builder.Services.AddServices();
builder.Services.AddUseCases(builder.Configuration);
builder.Services.AddInfraHttpClients();
builder.Services.AddGenAiClient();

var app = builder.Build();

// Restore the original scheme and client IP before request processing.
app.UseForwardedHeaders();
app.UseRequestLocalization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//Health Check
app.UseHealthChecks("/health");

//Map Web Socket
app.MapHub<ExampleHub>("/hubs");

//Exception Handler
app.UseExceptionHandler(_ => { });

app.UseHttpsRedirection();

app.UseCors();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();



using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EIContext>();
    dbContext.Database.Migrate();
}

app.Run();
