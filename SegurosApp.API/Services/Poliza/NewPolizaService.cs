using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Models;
using SegurosApp.API.Services.Poliza.Shared;
using System.Text.Json;

namespace SegurosApp.API.Services.Poliza
{
    public class NewPolizaService
    {
        private readonly IVelneoMasterDataService _masterDataService;
        private readonly PolizaDataExtractor _dataExtractor;
        private readonly ObservationsGenerator _observationsGenerator;
        private readonly AppDbContext _context;
        private readonly ILogger<NewPolizaService> _logger;

        public NewPolizaService(
            IVelneoMasterDataService masterDataService,
            PolizaDataExtractor dataExtractor,
            ObservationsGenerator observationsGenerator,
            AppDbContext context,
            ILogger<NewPolizaService> logger)
        {
            _masterDataService = masterDataService;
            _dataExtractor = dataExtractor;
            _observationsGenerator = observationsGenerator;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Crea un request de Velneo para nueva póliza desde scan
        /// </summary>
        public async Task<VelneoPolizaRequest> CreateVelneoRequestFromScanAsync(
            int scanId,
            int userId,
            CreatePolizaVelneoRequest? overrides = null)
        {
            _logger.LogInformation("Creando request Velneo para nueva póliza - Scan: {ScanId}, Usuario: {UserId}", scanId, userId);

            var scan = await _context.DocumentScans
                .FirstOrDefaultAsync(s => s.Id == scanId && s.UserId == userId);

            if (scan == null)
            {
                throw new ArgumentException($"Scan {scanId} no encontrado para usuario {userId}");
            }

            var extractedData = JsonSerializer.Deserialize<Dictionary<string, object>>(scan.ExtractedData)
                ?? new Dictionary<string, object>();

            // Obtener contexto
            var contextClienteId = GetValueWithOverride(overrides?.ClienteId, scan.ClienteId, "ClienteId");
            var contextCompaniaId = GetValueWithOverride(overrides?.CompaniaId, scan.CompaniaId, "CompaniaId");
            var contextSeccionId = GetValueWithOverride(overrides?.SeccionId, scan.SeccionId, "SeccionId");

            // Obtener información de master data
            ClienteItem? clienteInfo = null;
            CompaniaItem? companiaInfo = null;
            SeccionItem? seccionInfo = null;

            try
            {
                if (contextClienteId.HasValue)
                {
                    clienteInfo = await _masterDataService.GetClienteDetalleAsync(contextClienteId.Value);
                }

                if (contextCompaniaId.HasValue)
                {
                    var companias = await _masterDataService.GetCompaniasAsync();
                    companiaInfo = companias.FirstOrDefault(c => c.id == contextCompaniaId.Value);
                }

                if (contextSeccionId.HasValue)
                {
                    var secciones = await _masterDataService.GetSeccionesAsync(contextCompaniaId);
                    seccionInfo = secciones.FirstOrDefault(s => s.id == contextSeccionId.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error obteniendo información de master data: {Error}", ex.Message);
            }

            // Extraer datos usando PolizaDataExtractor
            var extractedStartDate = _dataExtractor.ExtractStartDate(extractedData);
            var extractedEndDate = _dataExtractor.ExtractEndDate(extractedData);
            var extractedPremium = _dataExtractor.ExtractPremium(extractedData);
            var extractedTotal = _dataExtractor.ExtractTotalAmount(extractedData);
            var extractedCuotas = _dataExtractor.ExtractInstallmentCount(extractedData);
            var extractedPaymentMethod = _dataExtractor.ExtractPaymentMethod(extractedData);

            // Construir request usando las propiedades override del DTO
            var request = new VelneoPolizaRequest
            {
                // IDs principales
                clinro = contextClienteId ?? throw new ArgumentException("Cliente ID requerido"),
                comcod = contextCompaniaId ?? throw new ArgumentException("Compañía ID requerida"),
                seccod = contextSeccionId ?? throw new ArgumentException("Sección ID requerida"),

                conend = _dataExtractor.ExtractEndorsement(extractedData),
                conpol = GetStringValueWithOverride(overrides?.PolicyNumber, _dataExtractor.ExtractPolicyNumber(extractedData), "NumeroPoliza"),
                confchdes = ConvertToVelneoDateFormat(GetStringValueWithOverride(overrides?.StartDateOverride, extractedStartDate, "FechaDesde")),
                confchhas = ConvertToVelneoDateFormat(GetStringValueWithOverride(overrides?.EndDateOverride, extractedEndDate, "FechaHasta")),
                conpremio = (int)Math.Round(GetDecimalValueWithOverride(overrides?.PremiumOverride, extractedPremium, "Premio")),
                contot = (int)Math.Round(GetDecimalValueWithOverride(overrides?.TotalOverride, extractedTotal, "MontoTotal")),

                // Datos del vehículo
                conmaraut = GetStringValueWithOverride(overrides?.VehicleBrandOverride, _dataExtractor.ExtractVehicleBrand(extractedData), "MarcaVehiculo"),
                conmodaut = GetStringValueWithOverride(overrides?.VehicleModelOverride, _dataExtractor.ExtractVehicleModel(extractedData), "ModeloVehiculo"),
                conanioaut = GetIntValueWithOverride(overrides?.VehicleYearOverride, _dataExtractor.ExtractVehicleYear(extractedData), "AnoVehiculo"),
                conmotor = GetStringValueWithOverride(overrides?.MotorNumberOverride, _dataExtractor.ExtractMotorNumber(extractedData), "NumeroMotor"),
                conchasis = GetStringValueWithOverride(overrides?.ChassisNumberOverride, _dataExtractor.ExtractChassisNumber(extractedData), "NumeroChasis"),
                conmataut = _dataExtractor.ExtractVehiclePlate(extractedData),

                // Datos del cliente
                clinom = GetStringValueWithOverride(overrides?.ClientNameOverride, clienteInfo?.clinom, "NombreCliente"),
                condom = GetStringValueWithOverride(overrides?.ClientAddressOverride, clienteInfo?.clidir, "DireccionCliente"),
                clinro1 = 0, // Beneficiario

                // Master data (usando overrides con defaults)
                dptnom = overrides?.DepartmentIdOverride ?? 1,
                combustibles = overrides?.FuelCodeOverride ?? "1",
                desdsc = overrides?.DestinationIdOverride ?? 1,
                catdsc = overrides?.CategoryIdOverride ?? 1,
                caldsc = overrides?.QualityIdOverride ?? 1,
                tarcod = overrides?.TariffIdOverride ?? 1,
                corrnom = overrides?.BrokerIdOverride ?? 0,

                // Condiciones de pago
                consta = MapPaymentMethodCode(extractedPaymentMethod),
                concuo = GetIntValueWithOverride(overrides?.InstallmentCountOverride, extractedCuotas, "CantidadCuotas"),
                moncod = overrides?.CurrencyIdOverride ?? 1, // Default UYU
                conviamon = overrides?.PaymentCurrencyIdOverride ?? 1,

                // Estados para nueva póliza
                congesti = "1",
                congeses = "1",
                contra = "1", // Nueva póliza
                convig = "1",

                // Información adicional
                com_alias = companiaInfo?.comnom ?? "",
                ramo = seccionInfo?.seccion ?? "",

                // Metadatos
                ingresado = DateTime.UtcNow,
                last_update = DateTime.UtcNow,
                app_id = scanId,
                conpadre = 0 // Sin póliza padre para nuevas pólizas
            };

            // Generar observaciones usando ObservationsGenerator
            request.observaciones = _observationsGenerator.GenerateNewPolizaObservations(
                overrides?.Notes,
                overrides?.UserComments,
                request.concuo,
                request.contot,
                extractedData);

            _logger.LogInformation("Request Velneo para nueva póliza creado exitosamente");
            return request;
        }

        #region Métodos auxiliares

        private int? GetValueWithOverride(int? overrideValue, int? extractedValue, string fieldName)
        {
            if (overrideValue.HasValue && overrideValue.Value > 0)
            {
                _logger.LogDebug("Usando override para {FieldName}: {Value}", fieldName, overrideValue.Value);
                return overrideValue.Value;
            }
            return extractedValue;
        }

        private string GetStringValueWithOverride(string? overrideValue, string? extractedValue, string fieldName)
        {
            if (!string.IsNullOrEmpty(overrideValue))
            {
                _logger.LogDebug("Usando override para {FieldName}: {Value}", fieldName, overrideValue);
                return overrideValue;
            }
            return extractedValue ?? "";
        }

        private decimal GetDecimalValueWithOverride(decimal? overrideValue, decimal extractedValue, string fieldName)
        {
            if (overrideValue.HasValue && overrideValue.Value > 0)
            {
                _logger.LogDebug("Usando override para {FieldName}: {Value}", fieldName, overrideValue.Value);
                return overrideValue.Value;
            }
            return extractedValue;
        }

        private int GetIntValueWithOverride(int? overrideValue, int extractedValue, string fieldName)
        {
            if (overrideValue.HasValue && overrideValue.Value > 0)
            {
                _logger.LogDebug("Usando override para {FieldName}: {Value}", fieldName, overrideValue.Value);
                return overrideValue.Value;
            }
            return extractedValue;
        }

        private string MapPaymentMethodCode(string paymentMethod)
        {
            if (string.IsNullOrEmpty(paymentMethod))
                return "1"; // Default contado

            return paymentMethod.ToUpperInvariant() switch
            {
                "TARJETA" or "CREDIT" or "CARD" => "T",
                "CONTADO" or "CASH" => "1",
                "DEBITO" or "DEBIT" => "D",
                _ => "1"
            };
        }

        private string ConvertToVelneoDateFormat(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
            {
                return DateTime.Today.ToString("yyyy-MM-dd");
            }

            try
            {
                var cleanDate = dateStr.Trim();
                if (DateTime.TryParseExact(cleanDate, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var alreadyFormatted))
                {
                    return cleanDate;
                }

                var formats = new[]
                {
                    "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy", "yyyy/MM/dd",
                    "dd/MM/yy", "MM/dd/yy", "yyyyMMdd", "dd.MM.yyyy"
                };

                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(cleanDate, format,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var parsedDate))
                    {
                        return parsedDate.ToString("yyyy-MM-dd");
                    }
                }

                if (DateTime.TryParse(cleanDate, out var flexibleDate))
                {
                    return flexibleDate.ToString("yyyy-MM-dd");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parseando fecha '{DateStr}'", dateStr);
            }

            return DateTime.Today.ToString("yyyy-MM-dd");
        }

        #endregion
    }
}