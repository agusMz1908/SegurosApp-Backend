using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Data;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Models;
using System.Security.Claims;

namespace SegurosApp.API.Services
{
    public class TenantService : ITenantService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TenantService> _logger;
        private int? _currentTenantUserId;

        public TenantService(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<TenantService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public void SetCurrentTenantUserId(int userId)
        {
            _currentTenantUserId = userId;
            _logger.LogDebug("TenantService - UserId establecido: {UserId}", userId);
        }

        public int? GetCurrentTenantUserId()
        {
            if (_currentTenantUserId.HasValue)
                return _currentTenantUserId.Value;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = httpContext.User.FindFirst("userId")?.Value
                               ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? httpContext.User.FindFirst("sub")?.Value;

                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }
            }

            return null;
        }

        public async Task<TenantConfiguration?> GetCurrentTenantConfigurationAsync()
        {
            var userId = GetCurrentTenantUserId();
            if (!userId.HasValue)
            {
                _logger.LogWarning("No se pudo obtener UserId para tenant");
                return null;
            }

            return await GetTenantConfigurationByUserIdAsync(userId.Value);
        }

        public async Task<TenantConfiguration?> GetTenantConfigurationByUserIdAsync(int userId)
        {
            try
            {
                _logger.LogDebug("Buscando configuración tenant para UserId: {UserId}", userId);
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("Usuario {UserId} no encontrado o inactivo", userId);
                    return null;
                }

                if (!user.TenantId.HasValue)
                {
                    _logger.LogWarning("Usuario {UserId} no tiene TenantId asignado", userId);
                    return null;
                }

                var tenantConfig = await _context.TenantConfigurations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == user.TenantId.Value && t.IsActive);

                if (tenantConfig == null)
                {
                    _logger.LogWarning("TenantConfiguration {TenantId} no encontrado o inactivo", user.TenantId.Value);
                    return null;
                }

                _logger.LogInformation("Configuración tenant obtenida - User: {UserId}, Tenant: {TenantName}, Velneo: {VelneoUrl}",
                    userId, tenantConfig.TenantName, tenantConfig.VelneoBaseUrl);

                return tenantConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo configuración tenant para UserId: {UserId}", userId);
                return null;
            }
        }

        public async Task<string?> GetVelneoBaseUrlAsync()
        {
            var config = await GetCurrentTenantConfigurationAsync();
            return config?.VelneoBaseUrl;
        }

        public async Task<string?> GetVelneoApiKeyAsync()
        {
            var config = await GetCurrentTenantConfigurationAsync();
            return config?.VelneoApiKey;
        }

        public async Task<string?> GetTenantBaseUrlAsync(int userId)
        {
            var config = await GetTenantConfigurationByUserIdAsync(userId);
            return config?.VelneoBaseUrl;
        }

        public async Task<string?> GetTenantApiKeyAsync(int userId)
        {
            var config = await GetTenantConfigurationByUserIdAsync(userId);
            return config?.VelneoApiKey;
        }

        public async Task<bool> IsTenantActiveAsync()
        {
            var config = await GetCurrentTenantConfigurationAsync();
            return config?.IsActive ?? false;
        }
    }
}