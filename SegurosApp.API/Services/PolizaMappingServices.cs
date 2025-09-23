using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo.Response;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Services.Poliza.Shared;

namespace SegurosApp.API.Services.Poliza
{
    /// <summary>
    /// Servicio especializado para el mapeo de datos escaneados con contexto de pre-selección
    /// </summary>
    public class PolizaMappingService
    {
        private readonly IVelneoMasterDataService _masterDataService;
        private readonly PolizaDataExtractor _dataExtractor;
        private readonly ILogger<PolizaMappingService> _logger;

        public PolizaMappingService(
            IVelneoMasterDataService masterDataService,
            PolizaDataExtractor dataExtractor,
            ILogger<PolizaMappingService> logger)
        {
            _masterDataService = masterDataService;
            _dataExtractor = dataExtractor;
            _logger = logger;
        }

        /// <summary>
        /// Mapea los datos escaneados con el contexto de pre-selección
        /// </summary>
        public async Task<PolizaMappingWithContextResponse> MapToPolizaWithContextAsync(
            Dictionary<string, object> extractedData,
            PreSelectionContext context)
        {
            _logger.LogInformation("Iniciando mapeo con contexto para scan {ScanId} - Cliente:{ClienteId}, Compañía:{CompaniaId}, Sección:{SeccionId}",
                context.ScanId, context.ClienteId, context.CompaniaId, context.SeccionId);

            var response = new PolizaMappingWithContextResponse();

            try
            {
                // Normalizar datos usando el extractor compartido
                var normalizedData = await NormalizeExtractedDataAsync(extractedData, context.CompaniaId);

                // Mapear datos básicos
                var mappedData = await MapBasicPolizaDataAsync(normalizedData, context);
                response.MappedData = mappedData;
                response.NormalizedData = normalizedData;

                // Generar sugerencias automáticas
                var suggestions = await GenerateAutoSuggestionsAsync(normalizedData, mappedData);
                response.AutoSuggestions = suggestions;

                // Identificar campos que requieren atención
                var requiresAttention = IdentifyFieldsRequiringAttention(mappedData, normalizedData);
                response.RequiresAttention = requiresAttention;

                // Calcular métricas de mapeo
                var metrics = CalculateMappingMetrics(mappedData, normalizedData);
                response.MappingMetrics = metrics;
                response.CompletionPercentage = metrics.OverallCompletionPercentage;

                // Determinar si está completo
                response.IsComplete = DetermineIfComplete(mappedData, requiresAttention);
                response.OverallCompletionPercentage = response.CompletionPercentage;
                response.ConfirmedByPreSelection = new List<string> { "cliente", "compania", "seccion" };

                _logger.LogInformation("Mapeo con contexto completado: {CompletionPercentage:F1}% - Listo: {IsComplete}",
                    response.CompletionPercentage, response.IsComplete);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en mapeo con contexto para scan {ScanId}", context.ScanId);

                return new PolizaMappingWithContextResponse
                {
                    IsComplete = false,
                    CompletionPercentage = 0,
                    NormalizedData = new Dictionary<string, object>(),
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

        #region Métodos privados (extraídos del PolizaMapperService original)

        private async Task<PolizaDataMapped> MapBasicPolizaDataAsync(
            Dictionary<string, object> extractedData,
            PreSelectionContext context)
        {
            return new PolizaDataMapped
            {
                NumeroPoliza = _dataExtractor.ExtractPolicyNumber(extractedData),
                Endoso = _dataExtractor.ExtractEndorsement(extractedData),
                FechaDesde = _dataExtractor.ExtractStartDate(extractedData),
                FechaHasta = _dataExtractor.ExtractEndDate(extractedData),
                Premio = _dataExtractor.ExtractPremium(extractedData),
                MontoTotal = _dataExtractor.ExtractTotalAmount(extractedData),
                VehiculoMarca = _dataExtractor.ExtractVehicleBrand(extractedData),
                VehiculoModelo = _dataExtractor.ExtractVehicleModel(extractedData),
                VehiculoAño = _dataExtractor.ExtractVehicleYear(extractedData),
                VehiculoMotor = _dataExtractor.ExtractMotorNumber(extractedData),
                VehiculoChasis = _dataExtractor.ExtractChassisNumber(extractedData),
                VehiculoCombustible = ExtractFuelType(extractedData),       
                VehiculoDestino = ExtractDestination(extractedData),        
                VehiculoCategoria = ExtractCategory(extractedData),         
                MedioPago = _dataExtractor.ExtractPaymentMethod(extractedData),
                CantidadCuotas = _dataExtractor.ExtractInstallmentCount(extractedData),
                TipoMovimiento = ExtractMovementType(extractedData),         
                AseguradoNombre = "[Confirmado por selección]",
                AseguradoDocumento = "[Confirmado por selección]"
            };
        }

        private async Task<List<FieldSuggestion>> GenerateAutoSuggestionsAsync(
            Dictionary<string, object> extractedData,
            PolizaDataMapped mappedData)
        {
            var suggestions = new List<FieldSuggestion>();

            try
            {
                // Sugerencia para combustible
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

                // Más sugerencias según sea necesario...
                _logger.LogInformation("Generadas {Count} sugerencias automáticas", suggestions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando sugerencias automáticas");
            }

            return suggestions;
        }

        private List<FieldMappingIssue> IdentifyFieldsRequiringAttention(
            PolizaDataMapped mappedData,
            Dictionary<string, object> extractedData)
        {
            var issues = new List<FieldMappingIssue>();

            if (string.IsNullOrEmpty(mappedData.NumeroPoliza))
            {
                issues.Add(new FieldMappingIssue
                {
                    FieldName = "numeroPoliza",
                    DisplayName = "Número de Póliza",
                    IssueType = "MissingCritical",
                    Description = "No se pudo extraer el número de póliza del documento",
                    Severity = "Error",
                    IsRequired = true
                });
            }

            // Más validaciones...
            return issues;
        }

        private MappingMetrics CalculateMappingMetrics(
            PolizaDataMapped mappedData,
            Dictionary<string, object> extractedData)
        {
            var mappedFields = CountMappedFields(mappedData);
            var totalExpected = 20;
            var completionPercentage = (decimal)mappedFields / totalExpected * 100;

            return new MappingMetrics
            {
                TotalFieldsScanned = extractedData.Count,
                FieldsMappedSuccessfully = mappedFields,
                OverallCompletionPercentage = completionPercentage,
                // Más métricas según sea necesario...
            };
        }

        private bool DetermineIfComplete(
            PolizaDataMapped mappedData,
            List<FieldMappingIssue> issues)
        {
            var hasEssentials = !string.IsNullOrEmpty(mappedData.NumeroPoliza) &&
                               mappedData.NumeroPoliza.Length >= 7 &&
                               !string.IsNullOrEmpty(mappedData.FechaDesde) &&
                               !string.IsNullOrEmpty(mappedData.FechaHasta);

            var hasNoErrors = !issues.Any(i => i.Severity == "Error");

            return hasEssentials && hasNoErrors;
        }

        private int CountMappedFields(PolizaDataMapped data)
        {
            int count = 0;
            if (!string.IsNullOrEmpty(data.NumeroPoliza)) count++;
            if (!string.IsNullOrEmpty(data.FechaDesde)) count++;
            if (!string.IsNullOrEmpty(data.FechaHasta)) count++;
            if (data.Premio > 0) count++;
            if (data.MontoTotal > 0) count++;
            // Contar más campos...
            return count;
        }

        #endregion

        #region Métodos de extracción locales (que faltan en PolizaDataExtractor)

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

        private string ExtractMovementType(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "tipoMovimiento", "tipo_movimiento", "movement_type",
                "operacion", "operation", "tipo_operacion"
            };
            return GetFirstValidValue(data, possibleFields);
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

        #endregion

        #region Normalización de datos

        private async Task<Dictionary<string, object>> NormalizeExtractedDataAsync(Dictionary<string, object> extractedData, int? companiaId = null)
        {
            // Por ahora usar los datos tal como vienen
            // En el futuro aquí se pueden aplicar normalizaciones específicas por compañía
            return extractedData;
        }

        #endregion
    }
}