using SegurosApp.API.Interfaces;
using System.Security.Claims;

namespace SegurosApp.API.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            try
            {
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = context.User.FindFirst("userId")?.Value
                                   ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? context.User.FindFirst("sub")?.Value; 

                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                    {
                        tenantService.SetCurrentTenantUserId(userId);
                        context.Items["TenantUserId"] = userId;

                        _logger.LogDebug("Tenant establecido - UserId: {UserId}, Path: {Path}",
                            userId, context.Request.Path);
                    }
                    else
                    {
                        _logger.LogWarning("UserId no encontrado en JWT claims. Path: {Path}, Claims: {Claims}",
                            context.Request.Path,
                            string.Join(", ", context.User.Claims.Select(c => $"{c.Type}={c.Value}")));
                    }
                }
                else
                {
                    _logger.LogDebug("Usuario no autenticado - Path: {Path}", context.Request.Path);
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en TenantMiddleware para path: {Path}", context.Request.Path);
                throw; 
            }
        }
    }

    public static class TenantMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantMiddleware>();
        }
    }
}