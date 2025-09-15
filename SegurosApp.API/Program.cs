using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SegurosApp.API.Data;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Services;
using SegurosApp.API.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 33)));
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key no está configurada");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantService, TenantService>();

builder.Services.AddHttpClient("MultiTenantVelneo", (serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var timeoutSeconds = configuration.GetValue<int>("VelneoAPI:TimeoutSeconds", 60);

    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    client.DefaultRequestHeaders.Add("User-Agent", "SegurosApp-MultiTenant/1.0");

    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("DEBUG: Configurando HttpClient timeout: {Timeout} segundos", timeoutSeconds);
});

builder.Services.AddScoped<IVelneoMasterDataService, MultiTenantVelneoService>();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAzureDocumentService, AzureDocumentService>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<DocumentFieldParser>();
builder.Services.AddScoped<IAzureModelMappingService, AzureModelMappingService>();
builder.Services.AddScoped<PolizaMapperService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IVelneoMetricsService, VelneoMetricsService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<PricingService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:3001",
                "https://localhost:3001"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SegurosApp API Multi-Tenant",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SegurosApp API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");  
app.UseAuthentication();
app.UseTenantResolution();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (await context.Database.CanConnectAsync())
        {
            var userCount = await context.Users.CountAsync();
            var tenantCount = await context.TenantConfigurations.CountAsync();

            logger.LogInformation("DB conectada - {UserCount} usuarios, {TenantCount} tenants",
                userCount, tenantCount);
        }
        else
        {
            logger.LogError("No se puede conectar a la base de datos");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error de conexión: {Message}", ex.Message);
    }
}

app.Run();