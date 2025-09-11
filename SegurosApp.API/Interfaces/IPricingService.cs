using SegurosApp.API.DTOs;

namespace SegurosApp.API.Services
{
    public interface IPricingService
    {
        Task<List<PricingTierDto>> GetActivePricingTiersAsync();
        Task<PricingTierDto> GetApplicableTierAsync(int totalPolizas);
        Task<PricingTierDto> CreatePricingTierAsync(CreatePricingTierDto dto);
        Task<PricingTierDto> UpdatePricingTierAsync(int id, UpdatePricingTierDto dto);
        Task<bool> DeactivatePricingTierAsync(int id);
    }
}