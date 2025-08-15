using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SegurosApp.API.Data;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SegurosApp API",
        Version = "v1",
        Description = "API para procesamiento de pólizas con IA"
    });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingresa el token JWT en el formato: Bearer {tu_token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found");
var key = Encoding.UTF8.GetBytes(jwtKey);

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
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var authLogger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                authLogger.LogWarning("❌ JWT Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var authLogger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var userName = context.Principal?.Identity?.Name ?? "Unknown";
                authLogger.LogInformation("✅ JWT Token validated for user: {User}", userName);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAzureDocumentService, AzureDocumentService>();

builder.Services.AddHttpClient("VelneoApi", client =>
{
    var baseUrl = builder.Configuration["VelneoAPI:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        var errorLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

        if (feature?.Error != null)
        {
            errorLogger.LogError(feature.Error, "❌ Error no manejado: {Message}", feature.Error.Message);
        }

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            success = false,
            message = "Error interno del servidor",
            timestamp = DateTime.UtcNow
        }));
    });
});

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
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ===============================
// 🗄️ VERIFICACIÓN DE BASE DE DATOS
// ===============================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var dbLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        dbLogger.LogInformation("🗄️ Verificando conexión a base de datos...");

        if (await context.Database.CanConnectAsync())
        {
            dbLogger.LogInformation("✅ Conexión a base de datos exitosa");
            var userCount = await context.Users.CountAsync();
            dbLogger.LogInformation("👥 Usuarios en base de datos: {Count}", userCount);
        }
        else
        {
            dbLogger.LogError("❌ No se puede conectar a la base de datos");
        }
    }
    catch (Exception ex)
    {
        dbLogger.LogError(ex, "❌ Error verificando base de datos: {Message}", ex.Message);
    }
}

// ===============================
// 📊 LOGGING DE STARTUP
// ===============================
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 SegurosApp API iniciando...");
logger.LogInformation("🔧 Entorno: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("🌐 CORS: Permitir cualquier origen");
logger.LogInformation("🔐 JWT Authentication: Habilitado");
logger.LogInformation("📊 Swagger: Disponible en /swagger");
logger.LogInformation("🗄️ Base de datos: MySQL configurada");

var azureEndpoint = builder.Configuration["AzureDocumentIntelligence:Endpoint"];
var velneoBaseUrl = builder.Configuration["VelneoAPI:BaseUrl"];

if (string.IsNullOrEmpty(azureEndpoint))
    logger.LogWarning("⚠️ Azure Document Intelligence endpoint no configurado");
else
    logger.LogInformation("🤖 Azure Document Intelligence: Configurado");

if (string.IsNullOrEmpty(velneoBaseUrl))
    logger.LogWarning("⚠️ Velneo base URL no configurada");
else
    logger.LogInformation("🔗 Velneo API: Configurada");

logger.LogInformation("🎯 API lista para recibir requests");

app.Run();