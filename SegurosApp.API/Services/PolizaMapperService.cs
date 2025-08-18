using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.SegurosApp.API.DTOs;
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

        /// <summary>
        /// 🚀 NUEVO: Crear request Velneo desde scan con contexto
        /// </summary>
        public async Task<VelneoPolizaRequest> CreateVelneoRequestFromScanAsync(
            int scanId,
            int userId,
            CreatePolizaVelneoRequest? overrides = null)
        {
            _logger.LogInformation("🚀 Creando request Velneo para scan {ScanId}", scanId);

            // ✅ OBTENER DATOS DEL SCAN
            var scan = await _context.DocumentScans
                .FirstOrDefaultAsync(s => s.Id == scanId && s.UserId == userId);

            if (scan == null)
            {
                throw new ArgumentException($"Scan {scanId} no encontrado para usuario {userId}");
            }

            var extractedData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(scan.ExtractedData)
                ?? new Dictionary<string, object>();

            // ✅ CREAR REQUEST BASE
            var request = new VelneoPolizaRequest
            {
                // IDs del contexto (deben venir de overrides o estar guardados en scan)
                clinro = overrides?.ClienteId ?? 0,
                comcod = overrides?.CompaniaId ?? 0,
                seccod = overrides?.SeccionId ?? 0,

                // ✅ DATOS DE PÓLIZA (con overrides)
                conpol = overrides?.PolicyNumberOverride ?? ExtractPolicyNumber(extractedData),
                conend = ExtractEndorsement(extractedData),
                confchdes = overrides?.StartDateOverride ?? ExtractStartDate(extractedData),
                confchhas = overrides?.EndDateOverride ?? ExtractEndDate(extractedData),
                conpremio = (int)(overrides?.PremiumOverride ?? ExtractPremium(extractedData)),
                contot = (int)(overrides?.PremiumOverride ?? ExtractTotalAmount(extractedData)),

                // ✅ DATOS VEHÍCULO (con overrides)
                conmaraut = overrides?.VehicleBrandOverride ?? ExtractVehicleBrand(extractedData),
                conmodaut = overrides?.VehicleModelOverride ?? ExtractVehicleModel(extractedData),
                conanioaut = overrides?.VehicleYearOverride ?? ExtractVehicleYear(extractedData),
                conmotor = overrides?.MotorNumberOverride ?? ExtractMotorNumber(extractedData),
                conchasis = overrides?.ChassisNumberOverride ?? ExtractChassisNumber(extractedData),

                // ✅ MAPEOS A MASTER DATA (con overrides o mapeo automático)
                dptnom = overrides?.DepartmentIdOverride ?? await FindDepartmentIdAsync(extractedData),
                combustibles = overrides?.FuelCodeOverride ?? await FindFuelCodeAsync(extractedData),
                desdsc = overrides?.DestinationIdOverride ?? await FindDestinationIdAsync(extractedData),
                catdsc = overrides?.CategoryIdOverride ?? await FindCategoryIdAsync(extractedData),
                caldsc = overrides?.QualityIdOverride ?? await FindQualityIdAsync(extractedData),
                tarcod = overrides?.TariffIdOverride ?? await FindTariffIdAsync(extractedData),

                // ✅ FORMA DE PAGO
                consta = MapPaymentMethodCode(overrides?.PaymentMethodOverride ?? ExtractPaymentMethod(extractedData)),
                concuo = overrides?.InstallmentCountOverride ?? ExtractInstallmentCount(extractedData),

                // ✅ METADATOS
                app_id = scanId,
                observaciones = $"Generado desde escaneo automático. {overrides?.Notes ?? ""}".Trim()
            };

            // ✅ VALIDAR REQUEST
            await ValidateVelneoRequest(request);

            _logger.LogInformation("✅ Request Velneo creado: Póliza={PolicyNumber}, Cliente={ClienteId}, Compañía={CompaniaId}",
                request.conpol, request.clinro, request.comcod);

            return request;
        }

        #region Mapeo de Datos Básicos

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
            var totalFields = 20; 
            var mappedFields = CountMappedFields(mappedData);

            return new MappingMetrics
            {
                TotalFieldsScanned = extractedData.Count,
                FieldsMappedSuccessfully = mappedFields,
                FieldsWithIssues = 0, 
                FieldsRequireAttention = 0,
                OverallConfidence = criticalStatus.CriticalFieldsCompleteness,
                MappingQuality = DetermineMappingQuality(criticalStatus.CriticalFieldsCompleteness),
                MissingCriticalFields = criticalStatus.MissingCritical,
                OverallCompletionPercentage = (decimal)mappedFields / totalFields * 100
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
                "poliza.prima_comercial", "prima_comercial", "conpremio",
                "financiero.prima_comercial", "datos_poliza"
            };
            return ExtractAmountFromFields(data, possibleFields);
        }

        private decimal ExtractTotalAmount(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "financiero.premio_total", "premio_total", "contot",
                "PREMIO TOTAL A PAGAR", "datos_poliza"
            };
            return ExtractAmountFromFields(data, possibleFields);
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
            var possibleFields = new[] { "vehiculo.motor", "motor", "MOTOR", "conmotor" };
            return GetFirstValidValue(data, possibleFields);
        }

        private string ExtractChassisNumber(Dictionary<string, object> data)
        {
            var possibleFields = new[] { "vehiculo.chasis", "chasis", "CHASIS", "conchasis" };
            return GetFirstValidValue(data, possibleFields);
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
            var possibleFields = new[] { "pago.medio", "medio_pago", "forma_pago", "consta" };
            return GetFirstValidValue(data, possibleFields);
        }

        private int ExtractInstallmentCount(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "cantidadCuotas", "cantidad_cuotas", "installments", "cuotas"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var match = Regex.Match(value, @"(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
                    {
                        return count;
                    }
                }
            }
            return 1;
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

        private decimal ExtractAmountFromFields(Dictionary<string, object> data, string[] fields)
        {
            foreach (var field in fields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var amount = ExtractAmountFromText(value);
                    if (amount > 0)
                    {
                        return amount;
                    }
                }
            }
            return 0;
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