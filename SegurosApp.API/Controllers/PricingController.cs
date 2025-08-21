using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SegurosApp.API.DTOs;
using SegurosApp.API.Services;
using System.Security.Claims;

namespace SegurosApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PricingController : ControllerBase
    {
        private readonly PricingService _pricingService;
        private readonly ILogger<PricingController> _logger;

        public PricingController(PricingService pricingService, ILogger<PricingController> logger)
        {
            _pricingService = pricingService;
            _logger = logger;
        }

        [HttpGet("tiers")]
        [ProducesResponseType(typeof(List<PricingTierDto>), 200)]
        public async Task<ActionResult<List<PricingTierDto>>> GetPricingTiers()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("📊 Usuario {UserId} consultando tiers de precios", userId);

                var tiers = await _pricingService.GetActivePricingTiersAsync();

                return Ok(tiers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo tiers de precios");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("calculate/{polizasCount}")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult> CalculatePrice(int polizasCount)
        {
            try
            {
                if (polizasCount <= 0)
                {
                    return BadRequest(new { message = "La cantidad de pólizas debe ser mayor a 0" });
                }

                var userId = GetCurrentUserId();
                _logger.LogInformation("💰 Usuario {UserId} calculando precio para {PolizasCount} pólizas", userId, polizasCount);

                var tier = await _pricingService.GetApplicableTierAsync(polizasCount);
                var totalCost = tier.PricePerPoliza * polizasCount;

                var result = new
                {
                    polizasCount = polizasCount,
                    applicableTier = tier,
                    pricePerPoliza = tier.PricePerPoliza,
                    totalCost = totalCost,
                    calculatedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calculando precio para {PolizasCount} pólizas", polizasCount);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("tiers")]
        [ProducesResponseType(typeof(PricingTierDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PricingTierDto>> CreatePricingTier([FromBody] CreatePricingTierDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("➕ Usuario {UserId} creando nuevo tier: {TierName}", userId, dto.TierName);

                var tier = await _pricingService.CreatePricingTierAsync(dto);

                return CreatedAtAction(nameof(GetPricingTiers), new { id = tier.Id }, tier);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("⚠️ Error de validación creando tier: {Error}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creando tier de precios");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPut("tiers/{id}")]
        [ProducesResponseType(typeof(PricingTierDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PricingTierDto>> UpdatePricingTier(int id, [FromBody] UpdatePricingTierDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("✏️ Usuario {UserId} actualizando tier {TierId}: {TierName}", userId, id, dto.TierName);

                var tier = await _pricingService.UpdatePricingTierAsync(id, dto);

                return Ok(tier);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Tier {TierId} no encontrado", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("⚠️ Error de validación actualizando tier: {Error}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error actualizando tier de precios {TierId}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpDelete("tiers/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeactivatePricingTier(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("🗑️ Usuario {UserId} desactivando tier {TierId}", userId, id);

                var success = await _pricingService.DeactivatePricingTierAsync(id);

                if (!success)
                {
                    return NotFound(new { message = $"Tier con ID {id} no encontrado" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error desactivando tier de precios {TierId}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(userIdClaim) ? null : int.Parse(userIdClaim);
        }
    }
}