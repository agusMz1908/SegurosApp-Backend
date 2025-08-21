using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SegurosApp.API.Data;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
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

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var userName = context.Principal?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
                Console.WriteLine($"✅ JWT Token Validated - User: {userName} (ID: {userId})");
                Console.WriteLine($"✅ Request Path: {context.Request.Path}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"⚠️ JWT Challenge triggered");
                Console.WriteLine($"⚠️ Error: {context.Error}");
                Console.WriteLine($"⚠️ Error Description: {context.ErrorDescription}");
                Console.WriteLine($"⚠️ Request Path: {context.Request.Path}");
                Console.WriteLine($"⚠️ Auth Header: {context.Request.Headers.Authorization.FirstOrDefault()}");
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers.Authorization.FirstOrDefault();
                if (!string.IsNullOrEmpty(token))
                {
                    Console.WriteLine($"📨 JWT Message Received for: {context.Request.Path}");
                    Console.WriteLine($"📨 Token starts with: {token.Substring(0, Math.Min(20, token.Length))}...");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddMemoryCache();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAzureDocumentService, AzureDocumentService>();
builder.Services.AddScoped<IVelneoMasterDataService, VelneoMasterDataService>();
builder.Services.AddScoped<DocumentFieldParser>();
builder.Services.AddScoped<PolizaMapperService>();
builder.Services.AddScoped<PricingService>();

builder.Services.AddHttpClient<VelneoMasterDataService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SegurosApp API",
        Version = "v1",
        Description = "API para procesamiento de documentos de seguros con Azure Document Intelligence"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConfiguration(builder.Configuration.GetSection("Logging"));
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

var app = builder.Build();

app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var contextFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (contextFeature != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(contextFeature.Error, "❌ Error no manejado: {Message}", contextFeature.Error.Message);
        }

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
        c.DocumentTitle = "SegurosApp API Documentation";
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

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

            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            if (adminUser == null)
            {
                dbLogger.LogWarning("⚠️ Usuario admin no encontrado. Creándolo...");

                var admin = new SegurosApp.API.Models.User
                {
                    Username = "admin",
                    Email = "admin@segurosapp.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(admin);
                await context.SaveChangesAsync();
                dbLogger.LogInformation("✅ Usuario admin creado exitosamente");
            }
            else
            {
                dbLogger.LogInformation("✅ Usuario admin encontrado");
            }
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

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 SegurosApp API iniciando...");
logger.LogInformation("🔧 Entorno: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("🌐 CORS: Permitir cualquier origen");
logger.LogInformation("🔐 JWT Authentication: Habilitado");
logger.LogInformation("📊 Swagger: Disponible en /swagger con autenticación JWT");
logger.LogInformation("🗄️ Base de datos: MySQL configurada");
logger.LogInformation("💾 Memory Cache: Configurado para datos maestros Velneo");

var azureEndpoint = builder.Configuration["AzureDocumentIntelligence:Endpoint"];
if (string.IsNullOrEmpty(azureEndpoint))
    logger.LogWarning("⚠️ Azure Document Intelligence endpoint no configurado");
else
    logger.LogInformation("🤖 Azure Document Intelligence: Configurado");

app.Run();