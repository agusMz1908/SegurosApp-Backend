using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SegurosApp.API.DTOs;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SegurosApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BillingController : ControllerBase
    {
        private readonly BillingService _billingService;
        private readonly IPdfService _pdfService;
        private readonly ILogger<BillingController> _logger;

        public BillingController(BillingService billingService, ILogger<BillingController> logger, IPdfService pdfService)
        {
            _billingService = billingService;
            _logger = logger;
            _pdfService = pdfService;
        }

        [HttpGet("current-month-stats")]
        [ProducesResponseType(typeof(BillingStatsDto), 200)]
        public async Task<ActionResult<BillingStatsDto>> GetCurrentMonthStats()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} consultando estadísticas del mes actual", userId);

                var stats = await _billingService.GetCurrentMonthStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas del mes actual");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("company-bills")]
        [ProducesResponseType(typeof(List<MonthlyBillingDto>), 200)]
        public async Task<ActionResult<List<MonthlyBillingDto>>> GetCompanyBills()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} consultando facturas de la empresa", userId);

                var bills = await _billingService.GetCompanyBillsAsync();
                return Ok(bills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo facturas de la empresa");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("generate-monthly-bill")]
        [ProducesResponseType(typeof(MonthlyBillingDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<MonthlyBillingDto>> GenerateMonthlyBill([FromBody] GenerateBillRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} generando factura para {Month}/{Year}",
                    userId, request.Month, request.Year);

                // TODO: Agregar validación de rol admin cuando esté implementado
                // if (!IsAdmin()) return Forbid();

                var bill = await _billingService.GenerateMonthlyBillAsync(
                    request.Year,
                    request.Month,
                    request.CompanyName,
                    request.CompanyAddress,
                    request.CompanyRUC);

                return CreatedAtAction(nameof(GetCompanyBills), new { id = bill.Id }, bill);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Error de validación generando factura: {Error}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando factura mensual");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPut("company-bills/{id}/mark-paid")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> MarkBillAsPaid(int id, [FromBody] MarkAsPaidRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} marcando factura {BillId} como pagada", userId, id);

                // TODO: Agregar validación de rol admin cuando esté implementado
                // if (!IsAdmin()) return Forbid();

                var success = await _billingService.MarkBillAsPaidAsync(id, request.PaymentMethod, request.PaymentReference);

                if (!success)
                {
                    return NotFound(new { message = $"Factura con ID {id} no encontrada" });
                }

                return Ok(new { message = "Factura marcada como pagada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando factura {BillId} como pagada", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("generate-previous-month")]
        [ProducesResponseType(typeof(MonthlyBillingDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<MonthlyBillingDto>> GeneratePreviousMonthBill([FromBody] GenerateCompanyInfoRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var now = DateTime.UtcNow;
                var previousMonth = now.AddMonths(-1);

                _logger.LogInformation("Usuario {UserId} generando factura automática para mes anterior: {Month}/{Year}",
                    userId, previousMonth.Month, previousMonth.Year);

                // TODO: Agregar validación de rol admin cuando esté implementado
                // if (!IsAdmin()) return Forbid();

                var bill = await _billingService.GenerateMonthlyBillAsync(
                    previousMonth.Year,
                    previousMonth.Month,
                    request.CompanyName,
                    request.CompanyAddress,
                    request.CompanyRUC);

                return CreatedAtAction(nameof(GetCompanyBills), new { id = bill.Id }, bill);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("⚠Error de validación generando factura del mes anterior: {Error}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando factura del mes anterior");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(userIdClaim) ? null : int.Parse(userIdClaim);
        }

        [HttpGet("company-bills/{id}")]
        [ProducesResponseType(typeof(BillDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<BillDetailDto>> GetCompanyBill(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} consultando detalle de factura {BillId}", userId, id);

                var bill = await _billingService.GetBillDetailAsync(id);

                if (bill == null)
                {
                    return NotFound(new { message = $"Factura con ID {id} no encontrada" });
                }

                return Ok(bill);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo detalle de factura {BillId}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("company-bills/{id}/pdf")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DownloadBillPdf(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} descargando PDF de factura {BillId}", userId, id);

                var billDetail = await _billingService.GetBillDetailAsync(id);

                if (billDetail == null)
                {
                    return NotFound(new { message = $"Factura con ID {id} no encontrada" });
                }

                var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(billDetail);
                var fileName = $"Factura_{billDetail.Id:D6}_{billDetail.BillingPeriod.Replace("/", "-")}.pdf";

                _logger.LogInformation("PDF generado exitosamente para factura {BillId}, archivo: {FileName}",
                    id, fileName);

                return File(
                    pdfBytes,
                    "application/pdf",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando PDF para factura {BillId}", id);
                return StatusCode(500, new { message = "Error interno del servidor generando PDF" });
            }
        }

        [HttpGet("monthly-summary/{year}/{month}")]
        [ProducesResponseType(typeof(MonthlyBillingSummaryDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<MonthlyBillingSummaryDto>> GetMonthlySummary(int year, int month)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} consultando resumen mensual {Month}/{Year}", userId, month, year);

                // TODO: Agregar validación de rol admin
                // if (!IsAdmin()) return Forbid();

                var summary = await _billingService.GetMonthlySummaryAsync(year, month);

                if (summary == null)
                {
                    return NotFound(new { message = $"No se encontraron datos para {month}/{year}" });
                }

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen mensual {Month}/{Year}", month, year);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("revenue-analytics")]
        [ProducesResponseType(typeof(RevenueAnalyticsDto), 200)]
        public async Task<ActionResult<RevenueAnalyticsDto>> GetRevenueAnalytics(
            [FromQuery] int? months = 12)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} consultando analytics de ingresos últimos {Months} meses", userId, months);

                // TODO: Agregar validación de rol admin
                // if (!IsAdmin()) return Forbid();

                var analytics = await _billingService.GetRevenueAnalyticsAsync(months ?? 12);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo analytics de ingresos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }

    public class GenerateCompanyInfoRequest
    {
        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? CompanyAddress { get; set; }

        [MaxLength(50)]
        public string? CompanyRUC { get; set; }
    }
}