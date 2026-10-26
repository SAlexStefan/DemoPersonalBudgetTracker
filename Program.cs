using PersonalBudgetTrackerApi.Repositories.Interfaces;
using PersonalBudgetTrackerApi.Data;
using PersonalBudgetTrackerApi.Repositories;
using PersonalBudgetTrackerApi.Services;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Dapper;
using PersonalBudgetTrackerApi.Interfaces;
using PersonalBudgetTrackerApi;
using PersonalBudgetTrackerApi.Models;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.RateLimiting;
using PersonalBudgetTrackerApi.Healthchecks;
using PersonalBudgetTrackerApi.Middleware;
using System.Threading.RateLimiting;
using PersonalBudgetTrackerApi.Services.Interfaces;


var builder = WebApplication.CreateBuilder(args);


//Services
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

var appVersion = builder.Configuration["Version"];
var appTitle = builder.Configuration["Title"];

const string schemeId = "Bearer";
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    options.SupportNonNullableReferenceTypes();
    options.UseAllOfToExtendReferenceSchemas();

    options.MapType<CategoryType>(() => new OpenApiSchema
    {
        Type = JsonSchemaType.String,
        Enum = [.. Enum.GetNames<CategoryType>().Select(name => (JsonNode)JsonValue.Create(name)!)]
    });

    // 1. Define the scheme
    options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",        // lowercase, per RFC 7235
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token below."
    });

    // 2. Apply it as a requirement — note this now takes the OpenApiDocument
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(schemeId, document)] = []
    });
});

//--- Data access ---
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepositories, CategoryRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();


builder.Services.AddScoped<TokenService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt key is not set.");

    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context =>
        {
            var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
        }
    );
});

//CORS: allow the React dev server and a configurable production frontend URL
var allowOrigins = builder.Configuration.GetSection("AllowOrigins").Get<string[]>() ?? throw new InvalidOperationException("No Allowed Origins set.");
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgres");

SqlMapper.AddTypeHandler(new CategoryTypeEnumHandler());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStatusCodePages();
app.UseExceptionHandler();
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

// Makes the top-level application entry point available to WebApplicationFactory integration tests.
public partial class Program { }
