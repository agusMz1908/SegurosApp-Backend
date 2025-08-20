using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.DTOs.Velneo.Response;
using SegurosApp.API.Interfaces;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SegurosApp.API.Services
{
    public class PolizaMapperService
    {
        private readonly IVelneoMasterDataService _masterDataService;
        private readonly AppDbContext _context;
        private readonly ILogger<PolizaMapperService> _logger;

        public PolizaMapperService(
            IVelneoMasterDataService masterDataService,
            AppDbContext context,
            ILogger<PolizaMapperService> logger)
        {
            _masterDataService = masterDataService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 🎯 NUEVO: Mapear con contexto de pre-selección
        /// </summary>
        public async Task<PolizaMappingWithContextResponse> MapToPolizaWithContextAsync(
            Dictionary<string, object> extractedData,
            PreSelectionContext context)
        {
            _logger.LogInformation("🔄 Iniciando mapeo con contexto para scan {ScanId} - Cliente:{ClienteId}, Compañía:{CompaniaId}, Sección:{SeccionId}",
                context.ScanId, context.ClienteId, context.CompaniaId, context.SeccionId);

            var response = new PolizaMappingWithContextResponse();

            try
            {
                // ✅ PASO 1: MAPEAR DATOS BÁSICOS
                var mappedData = await MapBasicPolizaDataAsync(extractedData, context);
                response.MappedData = mappedData;

                // ✅ PASO 2: VALIDAR CAMPOS CRÍTICOS
                var criticalValidation = ValidateCriticalFields(mappedData, extractedData);

                // ✅ PASO 3: GENERAR SUGERENCIAS AUTOMÁTICAS
                var suggestions = await GenerateAutoSuggestionsAsync(extractedData, mappedData);
                response.AutoSuggestions = suggestions;

                // ✅ PASO 4: IDENTIFICAR CAMPOS QUE REQUIEREN ATENCIÓN
                var requiresAttention = IdentifyFieldsRequiringAttention(mappedData, extractedData);
                response.RequiresAttention = requiresAttention;

                // ✅ PASO 5: CALCULAR COMPLETITUD
                var metrics = CalculateMappingMetrics(mappedData, extractedData, criticalValidation);
                response.MappingMetrics = metrics;
                response.CompletionPercentage = metrics.OverallCompletionPercentage;

                // ✅ PASO 6: DETERMINAR SI ESTÁ LISTO
                response.IsComplete = DetermineIfComplete(mappedData, criticalValidation, requiresAttention);

                // ✅ PASO 7: CAMPOS CONFIRMADOS POR PRE-SELECCIÓN
                response.ConfirmedByPreSelection = new List<string>
                {
                    "cliente", "compania", "seccion"
                };

                _logger.LogInformation("✅ Mapeo con contexto completado: {CompletionPercentage:F1}% - Listo: {IsComplete}",
                    response.CompletionPercentage, response.IsComplete);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en mapeo con contexto para scan {ScanId}", context.ScanId);

                return new PolizaMappingWithContextResponse
                {
                    IsComplete = false,
                    CompletionPercentage = 0,
                    RequiresAttention = new List<FieldMappingIssue>
                    {
                        new FieldMappingIssue
                        {
                            FieldName = "general",
                            IssueType = "ProcessingError",
                            Description = $"Error procesando mapeo: {ex.Message}",
                            Severity = "Error"
                        }
                    }
                };
            }
        }

        public async Task<VelneoPolizaRequest> CreateVelneoRequestFromScanAsync(
            int scanId,
            int userId,
            CreatePolizaVelneoRequest? overrides = null)
        {
            _logger.LogInformation("🚀 Creando request Velneo para scan {ScanId}, usuario {UserId}", scanId, userId);

            // ✅ OBTENER DATOS DEL SCAN CON CONTEXTO
            var scan = await _context.DocumentScans
                .FirstOrDefaultAsync(s => s.Id == scanId && s.UserId == userId);

            if (scan == null)
            {
                _logger.LogError("❌ Scan {ScanId} no encontrado para usuario {UserId}", scanId, userId);
                throw new ArgumentException($"Scan {scanId} no encontrado para usuario {userId}");
            }

            var extractedData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(scan.ExtractedData)
                ?? new Dictionary<string, object>();

            // ✅ OBTENER CONTEXTO
            var contextClienteId = GetValueWithOverride(overrides?.ClienteId, scan.ClienteId, "ClienteId");
            var contextCompaniaId = GetValueWithOverride(overrides?.CompaniaId, scan.CompaniaId, "CompaniaId");
            var contextSeccionId = GetValueWithOverride(overrides?.SeccionId, scan.SeccionId, "SeccionId");

            // ✅ OBTENER DATOS DEL CLIENTE DESDE VELNEO
            ClienteItem? clienteInfo = null;
            CompaniaItem? companiaInfo = null;
            SeccionItem? seccionInfo = null;

            try
            {
                clienteInfo = await _masterDataService.GetClienteDetalleAsync(contextClienteId);

                // Obtener información de la compañía
                var companias = await _masterDataService.GetCompaniasAsync();
                companiaInfo = companias.FirstOrDefault(c => c.id == contextCompaniaId);

                // Obtener información de la sección
                var secciones = await _masterDataService.GetSeccionesAsync(contextCompaniaId);
                seccionInfo = secciones.FirstOrDefault(s => s.id == contextSeccionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Error obteniendo información de maestros: {Error}", ex.Message);
            }

            // ✅ DEBUG: EXTRAER Y DEBUGGEAR FECHAS
            var rawStartDate = ExtractStartDate(extractedData);
            var rawEndDate = ExtractEndDate(extractedData);
            var formattedStartDate = ConvertToVelneoDateFormat(rawStartDate);
            var formattedEndDate = ConvertToVelneoDateFormat(rawEndDate);

            // ✅ DEBUG: EXTRAER MONTOS Y CUOTAS DIRECTAMENTE (SIN OVERRIDES)
            _logger.LogInformation("🔍 DEBUG: Extrayendo montos y cuotas directamente...");

            var extractedPremium = ExtractPremium(extractedData);
            var extractedTotal = ExtractTotalAmount(extractedData);
            var extractedCuotas = ExtractInstallmentCount(extractedData);
            var extractedPaymentMethod = ExtractPaymentMethod(extractedData);

            _logger.LogInformation("💰 DEBUG EXTRAÍDOS DIRECTAMENTE:");
            _logger.LogInformation("  - Premio extraído: {Premium}", extractedPremium);
            _logger.LogInformation("  - Total extraído: {Total}", extractedTotal);
            _logger.LogInformation("  - Cuotas extraídas: {Cuotas}", extractedCuotas);
            _logger.LogInformation("  - Forma de pago extraída: '{PaymentMethod}'", extractedPaymentMethod);

            var request = new VelneoPolizaRequest
            {
                // ✅ IDs DEL CONTEXTO GUARDADO
                clinro = contextClienteId,
                comcod = contextCompaniaId,
                seccod = contextSeccionId,

                // ✅ DATOS DE PÓLIZA
                conpol = ExtractPolicyNumber(extractedData),
                conend = ExtractEndorsement(extractedData),
                confchdes = GetStringValueWithOverride(overrides?.StartDateOverride, formattedStartDate, "FechaInicio"),
                confchhas = GetStringValueWithOverride(overrides?.EndDateOverride, formattedEndDate, "FechaFin"),

                // ✅ MONTOS - DIRECTOS SIN OVERRIDES
                conpremio = (int)Math.Round(extractedPremium),
                contot = (int)Math.Round(extractedTotal),

                // ✅ DATOS DEL VEHÍCULO - MANTENER OVERRIDES PARA ESTOS
                conmaraut = GetStringValueWithOverride(overrides?.VehicleBrandOverride, ExtractVehicleBrand(extractedData), "MarcaVehiculo"),
                conmodaut = GetStringValueWithOverride(overrides?.VehicleModelOverride, ExtractVehicleModel(extractedData), "ModeloVehiculo"),
                conanioaut = overrides?.VehicleYearOverride ?? ExtractVehicleYear(extractedData),
                conmotor = GetStringValueWithOverride(overrides?.MotorNumberOverride, ExtractMotorNumber(extractedData), "NumeroMotor"),
                conchasis = GetStringValueWithOverride(overrides?.ChassisNumberOverride, ExtractChassisNumber(extractedData), "NumeroChasis"),
                conmataut = ExtractVehiclePlate(extractedData),

                // ✅ DATOS DEL CLIENTE - NUEVOS CAMPOS
                clinom = clienteInfo?.clinom ?? "",
                condom = clienteInfo?.clidir ?? ExtractClientAddress(extractedData),
                clinro1 = ExtractBeneficiaryId(extractedData),

                // ✅ MASTER DATA IDS - MANTENER OVERRIDES PARA ESTOS
                dptnom = overrides?.DepartmentIdOverride ?? await FindDepartmentIdAsync(extractedData),
                combustibles = overrides?.FuelCodeOverride ?? await FindFuelCodeAsync(extractedData),
                desdsc = overrides?.DestinationIdOverride ?? await FindDestinationIdAsync(extractedData),
                catdsc = overrides?.CategoryIdOverride ?? await FindCategoryIdAsync(extractedData),
                caldsc = overrides?.QualityIdOverride ?? await FindQualityIdAsync(extractedData),
                tarcod = overrides?.TariffIdOverride ?? await FindTariffIdAsync(extractedData),
                corrnom = ExtractBrokerId(extractedData),

                // ✅ CONDICIONES DE PAGO - DIRECTAS SIN OVERRIDES
                consta = MapPaymentMethodCode(extractedPaymentMethod),
                concuo = extractedCuotas,
                moncod = ExtractCurrencyCode(extractedData),

                // ✅ ESTADOS - CON VALORES POR DEFECTO CORRECTOS
                congesti = "1",
                congeses = "2",
                contra = "1",
                convig = "1",

                // ✅ DATOS ADICIONALES - NOMBRES DESDE VELNEO
                com_alias = companiaInfo?.comnom ?? "",
                ramo = seccionInfo?.seccion ?? "",

                // ✅ METADATOS
                ingresado = DateTime.UtcNow,
                last_update = DateTime.UtcNow,
                app_id = scanId,
                observaciones = FormatObservations(overrides?.Notes, overrides?.UserComments)
            };

            // ✅ DEBUG: VERIFICAR VALORES FINALES EN EL REQUEST
            _logger.LogInformation("🔧 DEBUG: Verificando valores FINALES en el request:");
            _logger.LogInformation("  - request.conpremio: {Value}", request.conpremio);
            _logger.LogInformation("  - request.contot: {Value}", request.contot);
            _logger.LogInformation("  - request.concuo: {Value}", request.concuo);
            _logger.LogInformation("  - request.consta: '{Value}'", request.consta);
            _logger.LogInformation("  - request.conmaraut: '{Value}'", request.conmaraut);
            _logger.LogInformation("  - request.conmodaut: '{Value}'", request.conmodaut);
            _logger.LogInformation("  - request.conanioaut: {Value}", request.conanioaut);
            _logger.LogInformation("  - request.clinom: '{Value}'", request.clinom);
            _logger.LogInformation("  - request.condom: '{Value}'", request.condom);
            _logger.LogInformation("  - request.moncod: {Value}", request.moncod);
            _logger.LogInformation("  - request.com_alias: '{Value}'", request.com_alias);
            _logger.LogInformation("  - request.ramo: '{Value}'", request.ramo);
            _logger.LogInformation("  - request.congeses: '{Value}'", request.congeses);

            _logger.LogInformation("🔧 DEBUG REQUEST - Póliza: '{Policy}', Fechas: '{StartDate}' a '{EndDate}'",
                request.conpol, request.confchdes, request.confchhas);

            await ValidateVelneoRequest(request);
            _logger.LogInformation("✅ Request Velneo validado exitosamente");
            _logger.LogInformation("✅ Request Velneo creado: Póliza={Policy}, Cliente={ClienteId}, Compañía={CompaniaId}, Sección={SeccionId}",
                request.conpol, request.clinro, request.comcod, request.seccod);

            return request;
        }

        private string ExtractVehiclePlate(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
        "vehiculo.matricula", "matricula", "MATRICULA", "placa", "patente"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        private string ExtractClientAddress(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
        "cliente.direccion", "direccion", "DIRECCION", "domicilio", "address"
    };
            return GetFirstValidValue(data, possibleFields);
        }

        private int ExtractBeneficiaryId(Dictionary<string, object> data)
        {
            // Por ahora retornar 0, se puede implementar lógica específica
            return 0;
        }

        private int ExtractBrokerId(Dictionary<string, object> data)
        {
            // Por ahora retornar 0, se puede implementar lógica específica
            return 0;
        }

        private int ExtractCurrencyCode(Dictionary<string, object> data)
        {
            var possibleFields = new[] { "moneda", "currency", "divisa" };
            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var normalized = value.ToUpper();
                    if (normalized.Contains("USD") || normalized.Contains("DOLAR"))
                        return 840; // USD
                    if (normalized.Contains("UYU") || normalized.Contains("PESO"))
                        return 858; // UYU
                }
            }
            return 858; // Por defecto UYU
        }

        private int GetValueWithOverride(int? overrideValue, int? dbValue, string fieldName)
        {
            if (overrideValue.HasValue && overrideValue.Value > 0)
            {
                _logger.LogDebug("🔧 Usando override para {FieldName}: {Value}", fieldName, overrideValue.Value);
                return overrideValue.Value;
            }

            var result = dbValue ?? 0;
            _logger.LogDebug("🔧 Usando BD para {FieldName}: {Value}", fieldName, result);
            return result;
        }

        private string ExtractCompanyAlias(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
        "compania.alias", "alias_compania", "company_alias"
    };
            return GetFirstValidValue(data, possibleFields);
        }

        private string FormatObservations(string? notes, string? userComments)
        {
            var parts = new List<string> { "Generado desde escaneo automático." };

            if (!string.IsNullOrWhiteSpace(notes))
                parts.Add($"Notas: {notes}");

            if (!string.IsNullOrWhiteSpace(userComments))
                parts.Add($"Comentarios: {userComments}");

            return string.Join(" ", parts);
        }

        private string GetStringValueWithOverride(string? overrideValue, string defaultValue, string fieldName)
        {
            // ✅ SI OVERRIDE ES VÁLIDO (no null, no empty, no literal "string")
            if (!string.IsNullOrEmpty(overrideValue) &&
                overrideValue != "string" &&
                overrideValue.Trim() != "string")
            {
                _logger.LogDebug("🔧 Usando override para {FieldName}: '{Value}'", fieldName, overrideValue);
                return overrideValue.Trim();
            }

            _logger.LogDebug("🔧 Usando valor por defecto para {FieldName}: '{Value}'", fieldName, defaultValue);
            return defaultValue;
        }

        #region Mapeo de Datos Básicos

        private string ConvertToVelneoDateFormat(string dateStr)
        {
            _logger.LogDebug("🔧 ConvertToVelneoDateFormat input: '{DateStr}'", dateStr);

            if (string.IsNullOrEmpty(dateStr))
            {
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                _logger.LogWarning("⚠️ Fecha vacía, usando hoy: {Today}", today);
                return today;
            }

            try
            {
                // ✅ LIMPIAR FECHA PRIMERO
                var cleanDate = dateStr.Trim();

                // ✅ SI YA ESTÁ EN FORMATO CORRECTO
                if (DateTime.TryParseExact(cleanDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var alreadyFormatted))
                {
                    _logger.LogDebug("✅ Fecha ya en formato correcto: {Date}", cleanDate);
                    return cleanDate;
                }

                // ✅ INTENTAR FORMATOS COMUNES
                var formats = new[]
                {
            "dd/MM/yyyy",   // 22/03/2024
            "MM/dd/yyyy",   // 03/22/2024
            "dd-MM-yyyy",   // 22-03-2024
            "yyyy/MM/dd",   // 2024/03/22
            "dd/MM/yy",     // 22/03/24
            "MM/dd/yy",     // 03/22/24
            "yyyyMMdd",     // 20240322
            "dd.MM.yyyy",   // 22.03.2024
            "yyyy-M-d",     // 2024-3-22
            "d/M/yyyy"      // 22/3/2024
        };

                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(cleanDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                    {
                        var result = parsedDate.ToString("yyyy-MM-dd");
                        _logger.LogDebug("✅ Fecha convertida de '{Format}': {Input} -> {Output}", format, cleanDate, result);
                        return result;
                    }
                }

                // ✅ FALLBACK: PARSEO FLEXIBLE
                if (DateTime.TryParse(cleanDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var flexibleDate))
                {
                    var result = flexibleDate.ToString("yyyy-MM-dd");
                    _logger.LogDebug("✅ Fecha parseada flexible: {Input} -> {Output}", cleanDate, result);
                    return result;
                }

                // ✅ INTENTAR EXTRAER NÚMEROS
                var numbers = System.Text.RegularExpressions.Regex.Matches(cleanDate, @"\d+");
                if (numbers.Count >= 3)
                {
                    var day = int.Parse(numbers[0].Value);
                    var month = int.Parse(numbers[1].Value);
                    var year = int.Parse(numbers[2].Value);

                    // ✅ AJUSTAR AÑO SI ES DE 2 DÍGITOS
                    if (year < 100)
                    {
                        year += (year < 30) ? 2000 : 1900;
                    }

                    // ✅ INTERCAMBIAR SI EL DÍA ES > 12 (probablemente formato MM/dd)
                    if (day > 12 && month <= 12)
                    {
                        (day, month) = (month, day);
                    }

                    if (month >= 1 && month <= 12 && day >= 1 && day <= 31)
                    {
                        var extractedDate = new DateTime(year, month, day);
                        var result = extractedDate.ToString("yyyy-MM-dd");
                        _logger.LogDebug("✅ Fecha extraída por números: {Input} -> {Output}", cleanDate, result);
                        return result;
                    }
                }

                _logger.LogError("❌ No se pudo parsear fecha: '{DateStr}'", dateStr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error parseando fecha '{DateStr}'", dateStr);
            }

            // ✅ ÚLTIMO RECURSO: FECHA ACTUAL
            var fallback = DateTime.Today.ToString("yyyy-MM-dd");
            _logger.LogWarning("⚠️ Usando fecha fallback: {Fallback}", fallback);
            return fallback;
        }

        private async Task<PolizaDataMapped> MapBasicPolizaDataAsync(
            Dictionary<string, object> extractedData,
            PreSelectionContext context)
        {
            return new PolizaDataMapped
            {
                // ✅ DATOS DE PÓLIZA
                NumeroPoliza = ExtractPolicyNumber(extractedData),
                Endoso = ExtractEndorsement(extractedData),
                FechaDesde = ExtractStartDate(extractedData),
                FechaHasta = ExtractEndDate(extractedData),
                Premio = ExtractPremium(extractedData),
                MontoTotal = ExtractTotalAmount(extractedData),

                // ✅ DATOS VEHÍCULO
                VehiculoMarca = ExtractVehicleBrand(extractedData),
                VehiculoModelo = ExtractVehicleModel(extractedData),
                VehiculoAño = ExtractVehicleYear(extractedData),
                VehiculoMotor = ExtractMotorNumber(extractedData),
                VehiculoChasis = ExtractChassisNumber(extractedData),
                VehiculoCombustible = ExtractFuelType(extractedData),
                VehiculoDestino = ExtractDestination(extractedData),
                VehiculoCategoria = ExtractCategory(extractedData),

                // ✅ FORMA DE PAGO
                MedioPago = ExtractPaymentMethod(extractedData),
                CantidadCuotas = ExtractInstallmentCount(extractedData),
                TipoMovimiento = ExtractMovementType(extractedData),

                // ✅ DATOS DEL CONTEXTO (ya confirmados)
                AseguradoNombre = "[Confirmado por selección]",
                AseguradoDocumento = "[Confirmado por selección]"
            };
        }

        #endregion

        #region Validación de Campos Críticos

        private CriticalFieldsStatus ValidateCriticalFields(
            PolizaDataMapped mappedData,
            Dictionary<string, object> extractedData)
        {
            var status = new CriticalFieldsStatus();

            // ✅ NÚMERO DE PÓLIZA
            if (!string.IsNullOrEmpty(mappedData.NumeroPoliza) && mappedData.NumeroPoliza.Length >= 7)
            {
                status.HasPolicyNumber = true;
                status.FoundCritical.Add("Número de Póliza");
            }
            else
            {
                status.MissingCritical.Add("Número de Póliza");
            }

            // ✅ INFO VEHÍCULO
            if (!string.IsNullOrEmpty(mappedData.VehiculoMarca) || !string.IsNullOrEmpty(mappedData.VehiculoModelo))
            {
                status.HasVehicleInfo = true;
                status.FoundCritical.Add("Información del Vehículo");
            }
            else
            {
                status.MissingCritical.Add("Información del Vehículo");
            }

            // ✅ RANGO DE FECHAS
            if (!string.IsNullOrEmpty(mappedData.FechaDesde) && !string.IsNullOrEmpty(mappedData.FechaHasta))
            {
                status.HasDateRange = true;
                status.FoundCritical.Add("Rango de Fechas");
            }
            else
            {
                status.MissingCritical.Add("Rango de Fechas");
            }

            // ✅ INFO DE PREMIO
            if (mappedData.Premio > 0 || mappedData.MontoTotal > 0)
            {
                status.HasPremiumInfo = true;
                status.FoundCritical.Add("Información de Premio");
            }
            else
            {
                status.MissingCritical.Add("Información de Premio");
            }

            return status;
        }

        #endregion

        #region Sugerencias Automáticas

        private async Task<List<FieldSuggestion>> GenerateAutoSuggestionsAsync(
            Dictionary<string, object> extractedData,
            PolizaDataMapped mappedData)
        {
            var suggestions = new List<FieldSuggestion>();

            try
            {
                // ✅ SUGERENCIA PARA COMBUSTIBLE
                if (!string.IsNullOrEmpty(mappedData.VehiculoCombustible))
                {
                    var fuelSuggestion = await _masterDataService.SuggestMappingAsync("combustible", mappedData.VehiculoCombustible);
                    if (fuelSuggestion.Confidence >= 0.7)
                    {
                        suggestions.Add(new FieldSuggestion
                        {
                            FieldName = "vehiculo.combustible",
                            DisplayName = "Combustible",
                            ScannedValue = mappedData.VehiculoCombustible,
                            SuggestedValue = fuelSuggestion.SuggestedValue!,
                            SuggestedLabel = fuelSuggestion.SuggestedLabel!,
                            Confidence = fuelSuggestion.Confidence,
                            Source = "AutoMapping"
                        });
                    }
                }

                // ✅ SUGERENCIA PARA DESTINO
                if (!string.IsNullOrEmpty(mappedData.VehiculoDestino))
                {
                    var destinoSuggestion = await _masterDataService.SuggestMappingAsync("destino", mappedData.VehiculoDestino);
                    if (destinoSuggestion.Confidence >= 0.7)
                    {
                        suggestions.Add(new FieldSuggestion
                        {
                            FieldName = "vehiculo.destino",
                            DisplayName = "Destino del Vehículo",
                            ScannedValue = mappedData.VehiculoDestino,
                            SuggestedValue = destinoSuggestion.SuggestedValue!,
                            SuggestedLabel = destinoSuggestion.SuggestedLabel!,
                            Confidence = destinoSuggestion.Confidence,
                            Source = "AutoMapping"
                        });
                    }
                }

                // ✅ SUGERENCIA PARA CATEGORÍA
                if (!string.IsNullOrEmpty(mappedData.VehiculoCategoria))
                {
                    var categoriaSuggestion = await _masterDataService.SuggestMappingAsync("categoria", mappedData.VehiculoCategoria);
                    if (categoriaSuggestion.Confidence >= 0.7)
                    {
                        suggestions.Add(new FieldSuggestion
                        {
                            FieldName = "vehiculo.categoria",
                            DisplayName = "Categoría del Vehículo",
                            ScannedValue = mappedData.VehiculoCategoria,
                            SuggestedValue = categoriaSuggestion.SuggestedValue!,
                            SuggestedLabel = categoriaSuggestion.SuggestedLabel!,
                            Confidence = categoriaSuggestion.Confidence,
                            Source = "AutoMapping"
                        });
                    }
                }

                _logger.LogInformation("💡 Generadas {Count} sugerencias automáticas", suggestions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando sugerencias automáticas");
            }

            return suggestions;
        }

        #endregion

        #region Campos que Requieren Atención

        private List<FieldMappingIssue> IdentifyFieldsRequiringAttention(
            PolizaDataMapped mappedData,
            Dictionary<string, object> extractedData)
        {
            var issues = new List<FieldMappingIssue>();

            // ✅ NÚMERO DE PÓLIZA
            if (string.IsNullOrEmpty(mappedData.NumeroPoliza))
            {
                issues.Add(new FieldMappingIssue
                {
                    FieldName = "numeroPoliza",
                    DisplayName = "Número de Póliza",
                    ScannedValue = GetValue(extractedData, "poliza.numero", "datos_poliza"),
                    IssueType = "MissingCritical",
                    Description = "No se pudo extraer el número de póliza del documento",
                    Severity = "Error",
                    IsRequired = true
                });
            }
            else if (mappedData.NumeroPoliza.Length < 7)
            {
                issues.Add(new FieldMappingIssue
                {
                    FieldName = "numeroPoliza",
                    DisplayName = "Número de Póliza",
                    ScannedValue = mappedData.NumeroPoliza,
                    IssueType = "InvalidFormat",
                    Description = "El número de póliza parece incompleto (menos de 7 dígitos)",
                    Severity = "Warning",
                    IsRequired = true
                });
            }

            // ✅ FECHAS
            if (string.IsNullOrEmpty(mappedData.FechaDesde) || string.IsNullOrEmpty(mappedData.FechaHasta))
            {
                issues.Add(new FieldMappingIssue
                {
                    FieldName = "vigencia",
                    DisplayName = "Fechas de Vigencia",
                    ScannedValue = $"Desde: {mappedData.FechaDesde}, Hasta: {mappedData.FechaHasta}",
                    IssueType = "MissingCritical",
                    Description = "No se pudieron extraer las fechas de vigencia completamente",
                    Severity = "Error",
                    IsRequired = true
                });
            }

            // ✅ INFORMACIÓN VEHÍCULO
            if (string.IsNullOrEmpty(mappedData.VehiculoMarca) && string.IsNullOrEmpty(mappedData.VehiculoModelo))
            {
                issues.Add(new FieldMappingIssue
                {
                    FieldName = "vehiculo",
                    DisplayName = "Información del Vehículo",
                    ScannedValue = GetValue(extractedData, "vehiculo.marca", "vehiculo.modelo"),
                    IssueType = "MissingCritical",
                    Description = "No se pudo extraer información básica del vehículo (marca/modelo)",
                    Severity = "Warning",
                    IsRequired = false
                });
            }

            // ✅ PREMIO/MONTO
            if (mappedData.Premio <= 0 && mappedData.MontoTotal <= 0)
            {
                issues.Add(new FieldMappingIssue
                {
                    FieldName = "premio",
                    DisplayName = "Información de Premio",
                    ScannedValue = GetValue(extractedData, "poliza.prima_comercial", "financiero.premio_total"),
                    IssueType = "MissingCritical",
                    Description = "No se pudo extraer información de premio/monto de la póliza",
                    Severity = "Warning",
                    IsRequired = false
                });
            }

            return issues;
        }

        #endregion

        #region Métricas de Mapeo

        private MappingMetrics CalculateMappingMetrics(
            PolizaDataMapped mappedData,
            Dictionary<string, object> extractedData,
            CriticalFieldsStatus criticalStatus)
        {
            // ✅ CATEGORIZAR CAMPOS POR TIPO
            var policyFields = CategorizePolicyFields(mappedData, extractedData);
            var vehicleFields = CategorizeVehicleFields(mappedData, extractedData);
            var financialFields = CategorizeFinancialFields(mappedData, extractedData);
            var clientFields = CategorizeClientFields(mappedData, extractedData);
            var masterDataFields = CategorizeMasterDataFields(mappedData, extractedData);
            var optionalFields = CategorizeOptionalFields(mappedData, extractedData);

            // ✅ CALCULAR MÉTRICAS GENERALES
            var totalFieldsExpected = 20; // Campos críticos esperados
            var mappedFields = CountMappedFields(mappedData);
            var overallCompletionPercentage = (decimal)mappedFields / totalFieldsExpected * 100;

            // ✅ CALCULAR MÉTRICAS DE PERFORMANCE
            var performanceMetrics = new PerformanceMetrics
            {
                ProcessingTimeMs = 1000, // Placeholder - obtener del contexto real
                ValidationTimeMs = 200,
                MasterDataLookupTimeMs = 300,
                TotalMappingTimeMs = 1500,
                FieldsPerSecond = mappedFields > 0 ? (decimal)mappedFields / 1.5m : 0,
                AutoMappingSuccessRate = (decimal)mappedFields / extractedData.Count * 100,
                ManualReviewRequired = 0,
            };

            // ✅ CALCULAR BREAKDOWN DE CONFIANZA
            var confidenceBreakdown = CalculateConfidenceBreakdown(mappedData, extractedData);

            return new MappingMetrics
            {
                TotalFieldsScanned = extractedData.Count,
                FieldsMappedSuccessfully = mappedFields,
                FieldsWithIssues = 0, // Calculado en IdentifyFieldsRequiringAttention
                FieldsRequireAttention = 0,
                OverallConfidence = criticalStatus.CriticalFieldsCompleteness,
                MappingQuality = DetermineMappingQuality(criticalStatus.CriticalFieldsCompleteness),
                MissingCriticalFields = criticalStatus.MissingCritical,
                OverallCompletionPercentage = overallCompletionPercentage,

                // ✅ BREAKDOWN DETALLADO POR CATEGORÍAS
                FieldsByCategory = new CategoryBreakdown
                {
                    PolicyFields = policyFields,
                    VehicleFields = vehicleFields,
                    FinancialFields = financialFields,
                    ClientFields = clientFields,
                    MasterDataFields = masterDataFields,
                    OptionalFields = optionalFields
                },

                Performance = performanceMetrics,
                Confidence = confidenceBreakdown,
                Suggestions = GenerateImprovementSuggestions(mappedData, extractedData)
            };
        }

        private CategoryMetric CategorizePolicyFields(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var policyFieldsMap = new Dictionary<string, (bool IsMapped, bool IsCritical)>
    {
        { "NumeroPoliza", (!string.IsNullOrEmpty(mappedData.NumeroPoliza), true) },
        { "Endoso", (!string.IsNullOrEmpty(mappedData.Endoso), false) },
        { "FechaDesde", (!string.IsNullOrEmpty(mappedData.FechaDesde), true) },
        { "FechaHasta", (!string.IsNullOrEmpty(mappedData.FechaHasta), true) },
        { "TipoMovimiento", (!string.IsNullOrEmpty(mappedData.TipoMovimiento), false) }
    };

            return CalculateCategoryMetric(policyFieldsMap, "Póliza");
        }

        /// <summary>
        /// 🚗 Categorizar campos de vehículo
        /// </summary>
        private CategoryMetric CategorizeVehicleFields(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var vehicleFieldsMap = new Dictionary<string, (bool IsMapped, bool IsCritical)>
    {
        { "VehiculoMarca", (!string.IsNullOrEmpty(mappedData.VehiculoMarca), true) },
        { "VehiculoModelo", (!string.IsNullOrEmpty(mappedData.VehiculoModelo), true) },
        { "VehiculoAño", (mappedData.VehiculoAño > 0, true) },
        { "VehiculoMotor", (!string.IsNullOrEmpty(mappedData.VehiculoMotor), false) },
        { "VehiculoChasis", (!string.IsNullOrEmpty(mappedData.VehiculoChasis), false) },
        { "VehiculoCombustible", (!string.IsNullOrEmpty(mappedData.VehiculoCombustible), false) },
        { "VehiculoDestino", (!string.IsNullOrEmpty(mappedData.VehiculoDestino), false) },
        { "VehiculoCategoria", (!string.IsNullOrEmpty(mappedData.VehiculoCategoria), false) }
    };

            return CalculateCategoryMetric(vehicleFieldsMap, "Vehículo");
        }

        /// <summary>
        /// 💰 Categorizar campos financieros
        /// </summary>
        private CategoryMetric CategorizeFinancialFields(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var financialFieldsMap = new Dictionary<string, (bool IsMapped, bool IsCritical)>
    {
        { "Premio", (mappedData.Premio > 0, true) },
        { "MontoTotal", (mappedData.MontoTotal > 0, false) },
        { "MedioPago", (!string.IsNullOrEmpty(mappedData.MedioPago), false) },
        { "CantidadCuotas", (mappedData.CantidadCuotas > 0, false) }
    };

            return CalculateCategoryMetric(financialFieldsMap, "Financiero");
        }

        /// <summary>
        /// 👤 Categorizar campos de cliente
        /// </summary>
        private CategoryMetric CategorizeClientFields(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var clientFieldsMap = new Dictionary<string, (bool IsMapped, bool IsCritical)>
    {
        { "AseguradoNombre", (!string.IsNullOrEmpty(mappedData.AseguradoNombre), true) },
        { "AseguradoDocumento", (!string.IsNullOrEmpty(mappedData.AseguradoDocumento), true) },
        { "AseguradoDepartamento", (!string.IsNullOrEmpty(mappedData.AseguradoDepartamento), false) },
        { "AseguradoDireccion", (!string.IsNullOrEmpty(mappedData.AseguradoDireccion), false) }
    };

            return CalculateCategoryMetric(clientFieldsMap, "Cliente");
        }

        /// <summary>
        /// 🗂️ Categorizar campos de master data
        /// </summary>
        private CategoryMetric CategorizeMasterDataFields(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            // Estos campos requieren mapeo a IDs específicos de Velneo
            var masterDataFieldsMap = new Dictionary<string, (bool IsMapped, bool IsCritical)>
    {
        { "Departamento", (extractedData.ContainsKey("asegurado.departamento"), false) },
        { "Combustible", (!string.IsNullOrEmpty(mappedData.VehiculoCombustible), false) },
        { "Destino", (!string.IsNullOrEmpty(mappedData.VehiculoDestino), false) },
        { "Categoria", (!string.IsNullOrEmpty(mappedData.VehiculoCategoria), false) }
    };

            return CalculateCategoryMetric(masterDataFieldsMap, "Master Data");
        }

        /// <summary>
        /// 📄 Categorizar campos opcionales
        /// </summary>
        private CategoryMetric CategorizeOptionalFields(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var optionalFieldsMap = new Dictionary<string, (bool IsMapped, bool IsCritical)>
    {
        { "CorredorNombre", (!string.IsNullOrEmpty(mappedData.CorredorNombre), false) },
        { "CorredorNumero", (!string.IsNullOrEmpty(mappedData.CorredorNumero), false) },
        { "Moneda", (!string.IsNullOrEmpty(mappedData.Moneda), false) }
    };

            return CalculateCategoryMetric(optionalFieldsMap, "Opcional");
        }

        /// <summary>
        /// 📊 Calcular métrica de categoría
        /// </summary>
        private CategoryMetric CalculateCategoryMetric(Dictionary<string, (bool IsMapped, bool IsCritical)> fieldsMap, string categoryName)
        {
            var totalFields = fieldsMap.Count;
            var mappedFields = fieldsMap.Count(f => f.Value.IsMapped);
            var criticalFields = fieldsMap.Where(f => f.Value.IsCritical);
            var criticalMissing = criticalFields.Where(f => !f.Value.IsMapped).Select(f => f.Key).ToList();
            var successfullyMapped = fieldsMap.Where(f => f.Value.IsMapped).Select(f => f.Key).ToList();

            var completionPercentage = totalFields > 0 ? (decimal)mappedFields / totalFields * 100 : 0;

            return new CategoryMetric
            {
                TotalFields = totalFields,
                MappedFields = mappedFields,
                MissingFields = totalFields - mappedFields,
                CompletionPercentage = completionPercentage,
                AverageConfidence = mappedFields > 0 ? 85.0m : 0, // Placeholder - calcular real
                CriticalMissing = criticalMissing,
                SuccessfullyMapped = successfullyMapped
            };
        }

        /// <summary>
        /// 🔍 Calcular breakdown de confianza
        /// </summary>
        private ConfidenceBreakdown CalculateConfidenceBreakdown(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var allFields = new[]
            {
        ("NumeroPoliza", !string.IsNullOrEmpty(mappedData.NumeroPoliza), 95m),
        ("FechaDesde", !string.IsNullOrEmpty(mappedData.FechaDesde), 90m),
        ("FechaHasta", !string.IsNullOrEmpty(mappedData.FechaHasta), 90m),
        ("VehiculoMarca", !string.IsNullOrEmpty(mappedData.VehiculoMarca), 88m),
        ("VehiculoModelo", !string.IsNullOrEmpty(mappedData.VehiculoModelo), 85m),
        ("Premio", mappedData.Premio > 0, 92m),
        ("VehiculoAño", mappedData.VehiculoAño > 0, 95m)
    };

            var mappedFieldsWithConfidence = allFields.Where(f => f.Item2).ToList();

            var exactMatches = mappedFieldsWithConfidence.Where(f => f.Item3 >= 90m).ToList();
            var highConfidence = mappedFieldsWithConfidence.Where(f => f.Item3 >= 75m && f.Item3 < 90m).ToList();
            var mediumConfidence = mappedFieldsWithConfidence.Where(f => f.Item3 >= 50m && f.Item3 < 75m).ToList();
            var lowConfidence = mappedFieldsWithConfidence.Where(f => f.Item3 >= 25m && f.Item3 < 50m).ToList();
            var veryLowConfidence = mappedFieldsWithConfidence.Where(f => f.Item3 < 25m).ToList();

            var totalMapped = mappedFieldsWithConfidence.Count;
            var weightedAverage = totalMapped > 0 ? mappedFieldsWithConfidence.Average(f => f.Item3) : 0;

            return new ConfidenceBreakdown
            {
                ExactMatches = new ConfidenceLevel
                {
                    FieldCount = exactMatches.Count,
                    Percentage = totalMapped > 0 ? (decimal)exactMatches.Count / totalMapped * 100 : 0,
                    FieldNames = exactMatches.Select(f => f.Item1).ToList(),
                    AverageConfidence = exactMatches.Any() ? exactMatches.Average(f => f.Item3) : 0
                },
                HighConfidence = new ConfidenceLevel
                {
                    FieldCount = highConfidence.Count,
                    Percentage = totalMapped > 0 ? (decimal)highConfidence.Count / totalMapped * 100 : 0,
                    FieldNames = highConfidence.Select(f => f.Item1).ToList(),
                    AverageConfidence = highConfidence.Any() ? highConfidence.Average(f => f.Item3) : 0
                },
                MediumConfidence = new ConfidenceLevel
                {
                    FieldCount = mediumConfidence.Count,
                    Percentage = totalMapped > 0 ? (decimal)mediumConfidence.Count / totalMapped * 100 : 0,
                    FieldNames = mediumConfidence.Select(f => f.Item1).ToList(),
                    AverageConfidence = mediumConfidence.Any() ? mediumConfidence.Average(f => f.Item3) : 0
                },
                LowConfidence = new ConfidenceLevel
                {
                    FieldCount = lowConfidence.Count,
                    Percentage = totalMapped > 0 ? (decimal)lowConfidence.Count / totalMapped * 100 : 0,
                    FieldNames = lowConfidence.Select(f => f.Item1).ToList(),
                    AverageConfidence = lowConfidence.Any() ? lowConfidence.Average(f => f.Item3) : 0
                },
                VeryLowConfidence = new ConfidenceLevel
                {
                    FieldCount = veryLowConfidence.Count,
                    Percentage = totalMapped > 0 ? (decimal)veryLowConfidence.Count / totalMapped * 100 : 0,
                    FieldNames = veryLowConfidence.Select(f => f.Item1).ToList(),
                    AverageConfidence = veryLowConfidence.Any() ? veryLowConfidence.Average(f => f.Item3) : 0
                },
                WeightedAverageConfidence = weightedAverage,
                OverallConfidenceLevel = DetermineOverallConfidenceLevel(weightedAverage)
            };
        }

        private List<ImprovementSuggestion> GenerateImprovementSuggestions(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var suggestions = new List<ImprovementSuggestion>();

            if (string.IsNullOrEmpty(mappedData.VehiculoMotor))
            {
                suggestions.Add(new ImprovementSuggestion
                {
                    Category = "Vehículo",
                    Title = "Mejorar extracción de número de motor",
                    Description = "El número de motor no se está extrayendo consistentemente",
                    Priority = "Medium",
                    ActionType = "DocumentQuality",
                    SpecificFields = new List<string> { "VehiculoMotor" },
                    PotentialImprovement = 15m
                });
            }

            if (string.IsNullOrEmpty(mappedData.VehiculoCombustible))
            {
                suggestions.Add(new ImprovementSuggestion
                {
                    Category = "Master Data",
                    Title = "Mapeo automático de combustible",
                    Description = "Implementar mapeo automático de tipos de combustible a códigos Velneo",
                    Priority = "High",
                    ActionType = "Configuration",
                    SpecificFields = new List<string> { "VehiculoCombustible" },
                    PotentialImprovement = 20m
                });
            }

            return suggestions;
        }

        private string DetermineOverallConfidenceLevel(decimal averageConfidence)
        {
            return averageConfidence switch
            {
                >= 90m => "Muy Alta",
                >= 75m => "Alta",
                >= 60m => "Media",
                >= 40m => "Baja",
                _ => "Muy Baja"
            };
        }

        private int CountMappedFields(PolizaDataMapped data)
        {
            int count = 0;

            if (!string.IsNullOrEmpty(data.NumeroPoliza)) count++;
            if (!string.IsNullOrEmpty(data.Endoso) && data.Endoso != "0") count++;
            if (!string.IsNullOrEmpty(data.FechaDesde)) count++;
            if (!string.IsNullOrEmpty(data.FechaHasta)) count++;
            if (data.Premio > 0) count++;
            if (data.MontoTotal > 0) count++;
            if (!string.IsNullOrEmpty(data.VehiculoMarca)) count++;
            if (!string.IsNullOrEmpty(data.VehiculoModelo)) count++;
            if (data.VehiculoAño > 0) count++;
            if (!string.IsNullOrEmpty(data.VehiculoMotor)) count++;
            if (!string.IsNullOrEmpty(data.VehiculoChasis)) count++;
            if (!string.IsNullOrEmpty(data.VehiculoCombustible)) count++;
            if (!string.IsNullOrEmpty(data.VehiculoDestino)) count++;
            if (!string.IsNullOrEmpty(data.VehiculoCategoria)) count++;
            if (!string.IsNullOrEmpty(data.MedioPago)) count++;
            if (data.CantidadCuotas > 0) count++;
            if (!string.IsNullOrEmpty(data.TipoMovimiento)) count++;

            return count;
        }

        private string DetermineMappingQuality(decimal completeness)
        {
            return completeness switch
            {
                >= 90 => "Excelente",
                >= 75 => "Buena",
                >= 50 => "Aceptable",
                >= 25 => "Básica",
                _ => "Insuficiente"
            };
        }

        #endregion

        #region Determinación de Completitud

        private bool DetermineIfComplete(
            PolizaDataMapped mappedData,
            CriticalFieldsStatus criticalStatus,
            List<FieldMappingIssue> issues)
        {
            // ✅ CRITERIOS MÍNIMOS PARA ESTAR "COMPLETO"
            var hasEssentials = !string.IsNullOrEmpty(mappedData.NumeroPoliza) &&
                               mappedData.NumeroPoliza.Length >= 7 &&
                               !string.IsNullOrEmpty(mappedData.FechaDesde) &&
                               !string.IsNullOrEmpty(mappedData.FechaHasta);

            var hasNoErrors = !issues.Any(i => i.Severity == "Error");

            var criticalFieldsOk = criticalStatus.CriticalFieldsCompleteness >= 75;

            return hasEssentials && hasNoErrors && criticalFieldsOk;
        }

        #endregion

        #region Métodos de Extracción (reutilizados del service original)

        private string ExtractPolicyNumber(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "poliza.numero", "datos_poliza", "Nº de Póliza",
                "poliza_numero", "numero_poliza"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var match = Regex.Match(value, @"(\d{7,9})");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            return "";
        }

        private string ExtractEndorsement(Dictionary<string, object> data)
        {
            var possibleFields = new[] { "poliza.endoso", "endoso", "datos_poliza" };
            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var match = Regex.Match(value, @"Endoso:\s*(\d+)");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            return "0";
        }

        private string ExtractStartDate(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "poliza.vigencia.desde", "vigencia_desde", "confchdes",
                "datos_poliza", "vigencia.desde"
            };
            return ExtractDateFromFields(data, possibleFields);
        }

        private string ExtractEndDate(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "poliza.vigencia.hasta", "vigencia_hasta", "confchhas",
                "datos_poliza", "vigencia.hasta"
            };
            return ExtractDateFromFields(data, possibleFields);
        }

        private decimal ExtractPremium(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
        // ✅ CAMPOS REALES QUE LLEGAN DEL ESCANEO
        "poliza.prima_comercial",           // "Prima Comercial:\n$ 123.584,47"
        "financiero.prima_comercial",       // "Prima Comercial:\n$ 123.584,47"
        "pago.cuotas[0].prima",            // "Prima:\n$ 15.379,00"
        
        // Campos adicionales por si acaso
        "prima_comercial", "conpremio", "premio", "datos_financiero"
    };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    _logger.LogInformation("🎯 ExtractPremium - Campo encontrado: '{Field}' = '{Value}'", field, value);
                    var amount = ExtractAmountFromString(value);
                    if (amount > 0)
                    {
                        _logger.LogInformation("✅ ExtractPremium - Premio extraído: {Amount} desde campo '{Field}'", amount, field);
                        return amount;
                    }
                }
            }

            _logger.LogWarning("⚠️ ExtractPremium - No se encontró valor de premio en ningún campo");
            return 0;
        }

        private decimal ExtractAmountFromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            try
            {
                // ✅ LIMPIAR EL STRING: quitar texto descriptivo y símbolos
                var cleanValue = value
                    .Replace("Prima Comercial:", "")
                    .Replace("Premio Total a Pagar:", "")
                    .Replace("Prima:", "")
                    .Replace("$", "")
                    .Replace("€", "")
                    .Replace("UYU", "")
                    .Replace("USD", "")
                    .Replace("PESOS", "")
                    .Replace("PESO URUGUAYO", "")
                    .Replace("\n", " ")
                    .Replace("\r", " ")
                    .Trim();

                // ✅ BUSCAR PATRÓN DE NÚMERO CON FORMATO URUGUAYO: 123.584,47
                var uruguayanMatch = Regex.Match(cleanValue, @"(\d{1,3}(?:\.\d{3})*,\d{2})");
                if (uruguayanMatch.Success)
                {
                    var uruguayanNumber = uruguayanMatch.Groups[1].Value
                        .Replace(".", "")  // Quitar separadores de miles
                        .Replace(",", "."); // Cambiar coma decimal por punto

                    if (decimal.TryParse(uruguayanNumber, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                    {
                        _logger.LogInformation("✅ Monto extraído (formato uruguayo): '{Input}' -> {Amount}", value, amount);
                        return amount;
                    }
                }

                // ✅ BUSCAR PATRÓN DE NÚMERO ESTÁNDAR: 123,584.47
                var standardMatch = Regex.Match(cleanValue, @"(\d{1,3}(?:,\d{3})*\.\d{2})");
                if (standardMatch.Success)
                {
                    var standardNumber = standardMatch.Groups[1].Value.Replace(",", ""); // Quitar separadores de miles

                    if (decimal.TryParse(standardNumber, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                    {
                        _logger.LogInformation("✅ Monto extraído (formato estándar): '{Input}' -> {Amount}", value, amount);
                        return amount;
                    }
                }

                // ✅ BUSCAR CUALQUIER NÚMERO
                var anyNumberMatch = Regex.Match(cleanValue, @"(\d+(?:[.,]\d+)?)");
                if (anyNumberMatch.Success)
                {
                    var numberStr = anyNumberMatch.Groups[1].Value.Replace(",", ".");

                    if (decimal.TryParse(numberStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                    {
                        _logger.LogInformation("✅ Monto extraído (número simple): '{Input}' -> {Amount}", value, amount);
                        return amount;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Error extrayendo monto de '{Value}': {Error}", value, ex.Message);
            }

            _logger.LogWarning("⚠️ No se pudo extraer monto de: '{Value}'", value);
            return 0;
        }

        private decimal ExtractTotalAmount(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
        // ✅ CAMPOS REALES QUE LLEGAN DEL ESCANEO
        "financiero.premio_total",          // "Premio Total a Pagar:\n$ 153.790,00"
        "datos_financiero",                 // Contiene "Premio Total a Pagar:\n$ 153.790,00"
        
        // Campos adicionales por si acaso
        "premio_total", "contot", "total", "PREMIO TOTAL A PAGAR"
    };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    _logger.LogInformation("🎯 ExtractTotalAmount - Campo encontrado: '{Field}' = '{Value}'", field, value);

                    // Para el campo datos_financiero, buscar específicamente "Premio Total a Pagar"
                    if (field == "datos_financiero" && value.Contains("Premio Total a Pagar"))
                    {
                        var match = Regex.Match(value, @"Premio Total a Pagar:\s*\$?\s*([\d.,]+)");
                        if (match.Success)
                        {
                            var amount = ExtractAmountFromString(match.Groups[1].Value);
                            if (amount > 0)
                            {
                                _logger.LogInformation("✅ ExtractTotalAmount - Total extraído: {Amount} desde datos_financiero", amount);
                                return amount;
                            }
                        }
                    }
                    else
                    {
                        var amount = ExtractAmountFromString(value);
                        if (amount > 0)
                        {
                            _logger.LogInformation("✅ ExtractTotalAmount - Total extraído: {Amount} desde campo '{Field}'", amount, field);
                            return amount;
                        }
                    }
                }
            }

            _logger.LogWarning("⚠️ ExtractTotalAmount - No se encontró valor total en ningún campo");
            return 0;
        }

        private string ExtractVehicleBrand(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.marca", "marca", "MARCA", "conmaraut"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        private string ExtractVehicleModel(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.modelo", "modelo", "MODELO", "conmodaut"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        private int ExtractVehicleYear(Dictionary<string, object> data)
        {
            var possibleFields = new[] { "vehiculo.anio", "vehiculo.año", "año", "AÑO", "conanioaut" };
            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var match = Regex.Match(value, @"\b(20\d{2}|19\d{2})\b");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var year))
                    {
                        return year;
                    }
                }
            }
            return 0;
        }

        private string ExtractMotorNumber(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
        "vehiculo.motor", "motor", "MOTOR", "numero_motor"
    };
            var motorFull = GetFirstValidValue(data, possibleFields);

            return motorFull.Replace("MOTOR", "").Replace("motor", "").Trim();
        }

        private string ExtractChassisNumber(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
        "vehiculo.chasis", "chasis", "CHASIS", "numero_chasis"
    };
            var chassisFull = GetFirstValidValue(data, possibleFields);

            return chassisFull.Replace("CHASIS", "").Replace("chasis", "").Trim();
        }

        private string ExtractFuelType(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.combustible", "combustible", "COMBUSTIBLE"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        private string ExtractDestination(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.destino_del_vehiculo", "destino", "DESTINO DEL VEHÍCULO"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        private string ExtractCategory(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.tipo_vehiculo", "categoria", "TIPO DE VEHÍCULO"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        private string ExtractPaymentMethod(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
        "pago.forma", "forma_pago", "payment_method", "metodo_pago"
    };
            return GetFirstValidValue(data, possibleFields);
        }

        private int ExtractInstallmentCount(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
        // ✅ CAMPOS REALES QUE LLEGAN DEL ESCANEO
        "pago.modo_facturacion",           // "Modo de facturación: 10 cuotas."
        
        // Campos adicionales por si acaso
        "cantidadCuotas", "cantidad_cuotas", "cuotas", "concuo"
    };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    _logger.LogInformation("🎯 ExtractInstallmentCount - Campo encontrado: '{Field}' = '{Value}'", field, value);

                    // Buscar patrón "X cuotas" o números seguidos de "cuotas"
                    var match = Regex.Match(value, @"(\d+)\s*cuotas?", RegexOptions.IgnoreCase);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var cuotas) && cuotas > 0)
                    {
                        _logger.LogInformation("✅ ExtractInstallmentCount - Cuotas extraídas: {Count} desde campo '{Field}'", cuotas, field);
                        return cuotas;
                    }

                    // Buscar cualquier número en el texto
                    var numberMatch = Regex.Match(value, @"(\d+)");
                    if (numberMatch.Success && int.TryParse(numberMatch.Groups[1].Value, out var number) && number > 0 && number <= 60)
                    {
                        _logger.LogInformation("✅ ExtractInstallmentCount - Número extraído: {Count} desde campo '{Field}'", number, field);
                        return number;
                    }
                }
            }

            _logger.LogWarning("⚠️ ExtractInstallmentCount - No se encontró número de cuotas, usando valor por defecto: 1");
            return 1; // Por defecto 1 cuota
        }

        private string ExtractMovementType(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "tipoMovimiento", "tipo_movimiento", "movement_type",
                "operacion", "operation", "tipo_operacion"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        #endregion

        #region Mapeos a IDs de Velneo (Master Data)

        private async Task<int> FindDepartmentIdAsync(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "asegurado.departamento", "departamento", "depto", "dptnom"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var suggestion = await _masterDataService.SuggestMappingAsync("departamento", value);
                    if (suggestion.Confidence >= 0.8 && int.TryParse(suggestion.SuggestedValue, out var deptId))
                    {
                        return deptId;
                    }
                }
            }
            return 1; // Default: Montevideo
        }

        private async Task<string> FindFuelCodeAsync(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.combustible", "combustible", "COMBUSTIBLE"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var suggestion = await _masterDataService.SuggestMappingAsync("combustible", value);
                    if (suggestion.Confidence >= 0.7)
                    {
                        return suggestion.SuggestedValue ?? "1";
                    }
                }
            }
            return "1"; // Default
        }

        private async Task<int> FindDestinationIdAsync(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.destino_del_vehiculo", "destino", "DESTINO DEL VEHÍCULO"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var suggestion = await _masterDataService.SuggestMappingAsync("destino", value);
                    if (suggestion.Confidence >= 0.7 && int.TryParse(suggestion.SuggestedValue, out var destId))
                    {
                        return destId;
                    }
                }
            }
            return 1; // Default
        }

        private async Task<int> FindCategoryIdAsync(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.tipo_vehiculo", "categoria", "TIPO DE VEHÍCULO"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var suggestion = await _masterDataService.SuggestMappingAsync("categoria", value);
                    if (suggestion.Confidence >= 0.7 && int.TryParse(suggestion.SuggestedValue, out var catId))
                    {
                        return catId;
                    }
                }
            }
            return 1; // Default
        }

        private async Task<int> FindQualityIdAsync(Dictionary<string, object> data)
        {
            // Por ahora valor por defecto, se puede implementar lógica específica
            return 1;
        }

        private async Task<int> FindTariffIdAsync(Dictionary<string, object> data)
        {
            // Por ahora valor por defecto, se puede implementar lógica específica
            return 1;
        }

        #endregion

        #region Mapeo de Códigos

        private string MapPaymentMethodCode(string paymentMethod)
        {
            if (string.IsNullOrEmpty(paymentMethod)) return "1";

            var normalized = paymentMethod.ToUpper();
            return normalized switch
            {
                var x when x.Contains("TARJETA") || x.Contains("CREDITO") => "T",
                var x when x.Contains("CONTADO") || x.Contains("EFECTIVO") => "1",
                var x when x.Contains("DEBITO") || x.Contains("BANCARIO") => "B",
                var x when x.Contains("TRANSFERENCIA") => "B",
                _ => "1" // Default: Contado
            };
        }

        #endregion

        #region Validación del Request Velneo

        private async Task ValidateVelneoRequest(VelneoPolizaRequest request)
        {
            var errors = new List<string>();

            // ✅ VALIDAR IDs REQUERIDOS
            if (request.clinro <= 0)
                errors.Add("Cliente ID es requerido");

            if (request.comcod <= 0)
                errors.Add("Compañía ID es requerido");

            if (request.seccod <= 0)
                errors.Add("Sección ID es requerido");

            // ✅ VALIDAR DATOS CRÍTICOS
            if (string.IsNullOrEmpty(request.conpol))
                errors.Add("Número de póliza es requerido");

            if (string.IsNullOrEmpty(request.confchdes))
                errors.Add("Fecha de inicio es requerida");

            if (string.IsNullOrEmpty(request.confchhas))
                errors.Add("Fecha de fin es requerida");

            // ✅ VALIDAR FECHAS
            if (!string.IsNullOrEmpty(request.confchdes) && !DateTime.TryParse(request.confchdes, out _))
                errors.Add("Fecha de inicio tiene formato inválido");

            if (!string.IsNullOrEmpty(request.confchhas) && !DateTime.TryParse(request.confchhas, out _))
                errors.Add("Fecha de fin tiene formato inválido");

            if (errors.Any())
            {
                throw new ValidationException($"Errores de validación: {string.Join(", ", errors)}");
            }

            _logger.LogInformation("✅ Request Velneo validado exitosamente");
        }

        #endregion

        #region Métodos Auxiliares

        private bool TryGetValue(Dictionary<string, object> data, string key, out string value)
        {
            value = "";
            if (data.TryGetValue(key, out var obj) && obj != null)
            {
                value = obj.ToString()?.Trim() ?? "";
                return !string.IsNullOrEmpty(value);
            }
            return false;
        }

        private string GetFirstValidValue(Dictionary<string, object> data, string[] possibleFields)
        {
            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var cleaned = CleanText(value);
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        return cleaned;
                    }
                }
            }
            return "";
        }

        private string ExtractDateFromFields(Dictionary<string, object> data, string[] fields)
        {
            foreach (var field in fields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var date = ParseDateFromText(value);
                    if (!string.IsNullOrEmpty(date))
                    {
                        return date;
                    }
                }
            }
            return DateTime.Today.ToString("yyyy-MM-dd");
        }

        private string GetValue(Dictionary<string, object> data, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (data.TryGetValue(key, out var value) && value != null)
                {
                    var text = value.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(text))
                        return text;
                }
            }
            return "";
        }

        private string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            return input
                .Trim()
                .Replace("  ", " ")
                .Replace("\n", " ")
                .Replace("\r", "")
                .Replace("\t", " ");
        }

        private string ParseDateFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var patterns = new[]
            {
                @"(\d{1,2})/(\d{1,2})/(\d{4})",  // dd/MM/yyyy
                @"(\d{4})-(\d{1,2})-(\d{1,2})",  // yyyy-MM-dd
                @"(\d{1,2})-(\d{1,2})-(\d{4})"   // dd-MM-yyyy
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    try
                    {
                        var day = int.Parse(match.Groups[1].Value);
                        var month = int.Parse(match.Groups[2].Value);
                        var year = int.Parse(match.Groups[3].Value);

                        if (year < 100) year += 2000;
                        if (day > 31) (day, year) = (year, day);

                        var date = new DateTime(year, month, day);
                        return date.ToString("yyyy-MM-dd");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("⚠️ Error parseando fecha {Text}: {Error}", text, ex.Message);
                    }
                }
            }
            return "";
        }

        private decimal ExtractAmountFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var match = Regex.Match(text, @"[\$\s]*([0-9]+(?:[.,][0-9]{3})*(?:[.,][0-9]{1,2})?)");
            if (match.Success)
            {
                var amountStr = match.Groups[1].Value
                    .Replace(".", "")  // Remover separadores de miles
                    .Replace(",", "."); // Convertir coma decimal a punto

                if (decimal.TryParse(amountStr, NumberStyles.Currency, CultureInfo.InvariantCulture, out var amount))
                {
                    return amount;
                }
            }
            return 0;
        }

        #endregion
    }

    /// <summary>
    /// 🚨 Excepción para errores de validación
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}