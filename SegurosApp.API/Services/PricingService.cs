using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.Models;

namespace SegurosApp.API.Services
{
    public class PricingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PricingService> _logger;

        public PricingService(AppDbContext context, ILogger<PricingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<PricingTierDto>> GetActivePricingTiersAsync()
        {
            try
            {
                var tiers = await _context.PricingTiers
                    .Where(pt => pt.IsActive)
                    .OrderBy(pt => pt.MinPolizas)
                    .Select(pt => new PricingTierDto
                    {
                        Id = pt.Id,
                        TierName = pt.TierName,
                        MinPolizas = pt.MinPolizas,
                        MaxPolizas = pt.MaxPolizas,
                        PricePerPoliza = pt.PricePerPoliza,
                        IsActive = pt.IsActive
                    })
                    .ToListAsync();

                _logger.LogInformation("Obtenidos {Count} tiers de precios activos", tiers.Count);
                return tiers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo tiers de precios");
                throw;
            }
        }

        public async Task<PricingTierDto> GetApplicableTierAsync(int totalPolizas)
        {
            try
            {
                var tier = await _context.PricingTiers
                    .Where(pt => pt.IsActive &&
                                pt.MinPolizas <= totalPolizas &&
                                (pt.MaxPolizas == null || pt.MaxPolizas >= totalPolizas))
                    .OrderBy(pt => pt.MinPolizas)
                    .FirstOrDefaultAsync();

                if (tier == null)
                {
                    _logger.LogWarning("No se encontró tier para {TotalPolizas} pólizas", totalPolizas);

                    tier = await _context.PricingTiers
                        .Where(pt => pt.IsActive)
                        .OrderByDescending(pt => pt.MinPolizas)
                        .FirstOrDefaultAsync();
                }

                if (tier == null)
                {
                    throw new InvalidOperationException("No hay tiers de precios configurados");
                }

                var result = new PricingTierDto
                {
                    Id = tier.Id,
                    TierName = tier.TierName,
                    MinPolizas = tier.MinPolizas,
                    MaxPolizas = tier.MaxPolizas,
                    PricePerPoliza = tier.PricePerPoliza,
                    IsActive = tier.IsActive
                };

                _logger.LogInformation("Tier aplicable para {TotalPolizas} pólizas: {TierName} (${PricePerPoliza})",
                    totalPolizas, result.TierName, result.PricePerPoliza);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculando tier aplicable para {TotalPolizas} pólizas", totalPolizas);
                throw;
            }
        }

        public async Task<PricingTierDto> CreatePricingTierAsync(CreatePricingTierDto dto)
        {
            try
            {
                await ValidateNoOverlapAsync(dto.MinPolizas, dto.MaxPolizas);

                var tier = new PricingTiers
                {
                    TierName = dto.TierName,
                    MinPolizas = dto.MinPolizas,
                    MaxPolizas = dto.MaxPolizas,
                    PricePerPoliza = dto.PricePerPoliza,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PricingTiers.Add(tier);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tier de precios creado: {TierName} ({MinPolizas}-{MaxPolizas}) = ${PricePerPoliza}",
                    tier.TierName, tier.MinPolizas, tier.MaxPolizas, tier.PricePerPoliza);

                return new PricingTierDto
                {
                    Id = tier.Id,
                    TierName = tier.TierName,
                    MinPolizas = tier.MinPolizas,
                    MaxPolizas = tier.MaxPolizas,
                    PricePerPoliza = tier.PricePerPoliza,
                    IsActive = tier.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando tier de precios");
                throw;
            }
        }

        public async Task<PricingTierDto> UpdatePricingTierAsync(int id, UpdatePricingTierDto dto)
        {
            try
            {
                var tier = await _context.PricingTiers.FindAsync(id);
                if (tier == null)
                {
                    throw new ArgumentException($"Tier con ID {id} no encontrado");
                }

                await ValidateNoOverlapAsync(dto.MinPolizas, dto.MaxPolizas, id);

                tier.TierName = dto.TierName;
                tier.MinPolizas = dto.MinPolizas;
                tier.MaxPolizas = dto.MaxPolizas;
                tier.PricePerPoliza = dto.PricePerPoliza;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Tier de precios actualizado: {TierName} ({MinPolizas}-{MaxPolizas}) = ${PricePerPoliza}",
                    tier.TierName, tier.MinPolizas, tier.MaxPolizas, tier.PricePerPoliza);

                return new PricingTierDto
                {
                    Id = tier.Id,
                    TierName = tier.TierName,
                    MinPolizas = tier.MinPolizas,
                    MaxPolizas = tier.MaxPolizas,
                    PricePerPoliza = tier.PricePerPoliza,
                    IsActive = tier.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando tier de precios {TierId}", id);
                throw;
            }
        }

        public async Task<bool> DeactivatePricingTierAsync(int id)
        {
            try
            {
                var tier = await _context.PricingTiers.FindAsync(id);
                if (tier == null)
                {
                    return false;
                }

                tier.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tier de precios desactivado: {TierName}", tier.TierName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando tier de precios {TierId}", id);
                throw;
            }
        }

        private async Task ValidateNoOverlapAsync(int minPolizas, int? maxPolizas, int? excludeId = null)
        {
            var query = _context.PricingTiers.Where(pt => pt.IsActive);

            if (excludeId.HasValue)
            {
                query = query.Where(pt => pt.Id != excludeId.Value);
            }

            var existingTiers = await query.ToListAsync();

            foreach (var existing in existingTiers)
            {
                bool overlaps = false;

                if (maxPolizas == null) 
                {
                    overlaps = existing.MaxPolizas == null || existing.MaxPolizas >= minPolizas;
                }
                else if (existing.MaxPolizas == null) 
                {
                    overlaps = existing.MinPolizas <= maxPolizas;
                }
                else 
                {
                    overlaps = !(maxPolizas < existing.MinPolizas || minPolizas > existing.MaxPolizas);
                }

                if (overlaps)
                {
                    throw new InvalidOperationException(
                        $"El rango {minPolizas}-{maxPolizas?.ToString() ?? "∞"} se solapa con el tier existente '{existing.TierName}' ({existing.MinPolizas}-{existing.MaxPolizas?.ToString() ?? "∞"})");
                }
            }
        }
    }
}