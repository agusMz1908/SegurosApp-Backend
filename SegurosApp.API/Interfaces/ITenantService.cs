using SegurosApp.API.Models;

namespace SegurosApp.API.Interfaces
{
    public interface ITenantService
    {
        Task<TenantConfiguration?> GetCurrentTenantConfigurationAsync();
        Task<TenantConfiguration?> GetTenantConfigurationByUserIdAsync(int userId);
        void SetCurrentTenantUserId(int userId);
        int? GetCurrentTenantUserId();
        Task<string?> GetVelneoBaseUrlAsync();
        Task<string?> GetVelneoApiKeyAsync();
        Task<string?> GetTenantBaseUrlAsync(int userId);
        Task<string?> GetTenantApiKeyAsync(int userId);
        Task<bool> IsTenantActiveAsync();
    }
}