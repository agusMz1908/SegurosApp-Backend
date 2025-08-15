using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Models;
using System.Security.Cryptography;
using System.Text.Json;

namespace SegurosApp.API.Services
{
    public class AzureDocumentService : IAzureDocumentService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureDocumentService> _logger;
        private readonly DocumentIntelligenceClient _documentClient;
        private readonly DocumentFieldParser _fieldParser;

        public AzureDocumentService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<AzureDocumentService> logger,
            DocumentFieldParser fieldParser)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _fieldParser = fieldParser;

            // Configurar cliente de Azure
            var endpoint = _configuration["AzureDocumentIntelligence:Endpoint"];
            var apiKey = _configuration["AzureDocumentIntelligence:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Azure Document Intelligence no está configurado correctamente");
            }

            _documentClient = new DocumentIntelligenceClient(
                new Uri(endpoint),
                new AzureKeyCredential(apiKey)
            );

            _logger.LogInformation("🤖 Azure Document Intelligence Service inicializado");
        }

        public async Task<DocumentScanResponse> ProcessDocumentAsync(IFormFile file, int userId)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("📄 Procesando documento: {FileName} para usuario: {UserId}",
                    file.FileName, userId);

                // Validaciones básicas
                var validationResult = ValidateFile(file);
                if (!validationResult.IsValid)
                {
                    return new DocumentScanResponse
                    {
                        Success = false,
                        ErrorMessage = validationResult.ErrorMessage,
                        FileName = file.FileName,
                        FileSize = file.Length,
                        Status = "Error"
                    };
                }

                // Calcular hash MD5 para detectar duplicados
                var fileHash = await CalculateFileHashAsync(file);

                // Verificar duplicados
                var existingScan = await _context.DocumentScans
                    .Where(d => d.FileMd5Hash == fileHash && d.UserId == userId)
                    .FirstOrDefaultAsync();

                if (existingScan != null)
                {
                    _logger.LogInformation("🔄 Documento duplicado detectado: {FileName}", file.FileName);

                    stopwatch.Stop();
                    return new DocumentScanResponse
                    {
                        Success = true,
                        ScanId = existingScan.Id,
                        FileName = file.FileName,
                        FileSize = file.Length,
                        FileMd5Hash = fileHash,
                        ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds,
                        SuccessRate = existingScan.SuccessRate,
                        FieldsExtracted = existingScan.FieldsExtracted,
                        TotalFieldsAttempted = existingScan.TotalFieldsAttempted,
                        Status = existingScan.Status,
                        ExtractedData = JsonSerializer.Deserialize<Dictionary<string, object>>(existingScan.ExtractedData) ?? new(),
                        IsDuplicate = true,
                        ExistingScanId = existingScan.Id,
                        CreatedAt = existingScan.CreatedAt,
                        CompletedAt = existingScan.CompletedAt
                    };
                }

                // Procesar con Azure Document Intelligence
                var azureResult = await ProcessWithAzureAsync(file);
                stopwatch.Stop();

                // Guardar en base de datos
                var documentScan = new DocumentScan
                {
                    UserId = userId,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    FileMd5Hash = fileHash,
                    AzureOperationId = azureResult.AzureOperationId,
                    ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    SuccessRate = azureResult.SuccessRate,
                    FieldsExtracted = azureResult.FieldsExtracted,
                    TotalFieldsAttempted = azureResult.TotalFieldsAttempted,
                    ExtractedData = JsonSerializer.Serialize(azureResult.ExtractedFields),
                    Status = "Completed",
                    CreatedAt = startTime,
                    CompletedAt = DateTime.UtcNow
                };

                _context.DocumentScans.Add(documentScan);
                await _context.SaveChangesAsync();

                // Actualizar métricas diarias
                await UpdateDailyMetricsAsync(userId, startTime.Date, true, (int)stopwatch.ElapsedMilliseconds, azureResult.SuccessRate);

                return new DocumentScanResponse
                {
                    Success = true,
                    ScanId = documentScan.Id,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    FileMd5Hash = fileHash,
                    ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    SuccessRate = azureResult.SuccessRate,
                    FieldsExtracted = azureResult.FieldsExtracted,
                    TotalFieldsAttempted = azureResult.TotalFieldsAttempted,
                    Status = "Completed",
                    ExtractedData = azureResult.ExtractedFields,
                    IsDuplicate = false,
                    CreatedAt = startTime,
                    CompletedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "❌ Error procesando documento: {FileName}", file.FileName);

                return new DocumentScanResponse
                {
                    Success = false,
                    ErrorMessage = "Error procesando documento: " + ex.Message,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    Status = "Failed"
                };
            }
        }

        public async Task<DocumentHistoryDto?> GetScanByIdAsync(int scanId, int userId)
        {
            var scan = await _context.DocumentScans
                .Where(d => d.Id == scanId && d.UserId == userId)
                .FirstOrDefaultAsync();

            if (scan == null) return null;

            return MapToHistoryDto(scan);
        }

        private DocumentHistoryDto MapToHistoryDto(DocumentScan scan)
        {
            return new DocumentHistoryDto
            {
                Id = scan.Id,
                FileName = scan.FileName,
                FileSize = scan.FileSize,
                ProcessingTimeMs = scan.ProcessingTimeMs,
                SuccessRate = scan.SuccessRate,
                FieldsExtracted = scan.FieldsExtracted,
                TotalFieldsAttempted = scan.TotalFieldsAttempted,
                Status = scan.Status,
                CreatedAt = scan.CreatedAt,
                CompletedAt = scan.CompletedAt,
                ExtractedData = JsonSerializer.Deserialize<Dictionary<string, object>>(scan.ExtractedData) ?? new(),
                VelneoPolizaNumber = scan.VelneoPolizaNumber,
                VelneoCreated = scan.VelneoCreated,
                IsBillable = scan.IsBillable,
                IsBilled = scan.IsBilled,
                BilledAt = scan.BilledAt
            };
        }

        // ✅ CORREGIDO: Manejo correcto de tipos en filtros
        public async Task<List<DocumentHistoryDto>> GetScanHistoryAsync(int userId, DocumentSearchFilters filters)
        {
            var query = _context.DocumentScans.Where(d => d.UserId == userId);

            if (!string.IsNullOrEmpty(filters.FileName))
                query = query.Where(d => d.FileName.Contains(filters.FileName));

            if (!string.IsNullOrEmpty(filters.Status))
                query = query.Where(d => d.Status == filters.Status);

            if (filters.FromDate.HasValue)
                query = query.Where(d => d.CreatedAt >= filters.FromDate.Value);

            if (filters.ToDate.HasValue)
                query = query.Where(d => d.CreatedAt <= filters.ToDate.Value);

            if (filters.IsBillable.HasValue)
                query = query.Where(d => d.IsBillable == filters.IsBillable.Value);

            if (filters.VelneoCreated.HasValue)
                query = query.Where(d => d.VelneoCreated == filters.VelneoCreated.Value);

            if (filters.MinSuccessRate.HasValue)
                query = query.Where(d => d.SuccessRate >= filters.MinSuccessRate.Value);

            // ✅ CORREGIDO: Verificación explícita de tipos
            int pageSize;
            if (filters.PageSize.HasValue)
            {
                pageSize = filters.PageSize.Value;
            }
            else
            {
                pageSize = filters.Limit;
            }

            int page = filters.Page; // Ya es int, no nullable

            var scans = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return scans.Select(MapToHistoryDto).ToList();
        }

        public async Task<DocumentMetricsDto> GetDocumentMetricsAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            fromDate ??= DateTime.UtcNow.AddDays(-30);
            toDate ??= DateTime.UtcNow;

            var scans = await _context.DocumentScans
                .Where(d => d.UserId == userId && d.CreatedAt >= fromDate && d.CreatedAt <= toDate)
                .ToListAsync();

            var totalScans = scans.Count;
            var successfulScans = scans.Count(s => s.Status == "Completed");
            var failedScans = scans.Count(s => s.Status == "Failed");
            var billableScans = scans.Count(s => s.IsBillable);

            return new DocumentMetricsDto
            {
                TotalScans = totalScans,
                SuccessfulScans = successfulScans,
                FailedScans = failedScans,
                BillableScans = billableScans,
                SuccessRate = totalScans > 0 ? (decimal)successfulScans / totalScans * 100 : 0,
                TotalProcessingTimeMs = scans.Sum(s => s.ProcessingTimeMs),
                AverageSuccessRate = totalScans > 0 ? scans.Average(s => s.SuccessRate) : 0,
                AverageProcessingTimeMs = totalScans > 0 ? (int)scans.Average(s => s.ProcessingTimeMs) : 0,
                PeriodStart = fromDate.Value,
                PeriodEnd = toDate.Value,
                ProblematicDocuments = new List<ProblematicDocumentDto>()
            };
        }

        public async Task<DocumentScanResponse> ReprocessDocumentAsync(int scanId, int userId, bool forceReprocess = false)
        {
            var existingScan = await _context.DocumentScans
                .Where(d => d.Id == scanId && d.UserId == userId)
                .FirstOrDefaultAsync();

            if (existingScan == null)
            {
                return new DocumentScanResponse
                {
                    Success = false,
                    ErrorMessage = "Documento no encontrado"
                };
            }

            if (existingScan.Status == "Completed" && !forceReprocess)
            {
                return new DocumentScanResponse
                {
                    Success = false,
                    ErrorMessage = "El documento ya fue procesado exitosamente. Use forceReprocess=true para reprocesar."
                };
            }

            return new DocumentScanResponse
            {
                Success = true,
                ScanId = existingScan.Id,
                FileName = existingScan.FileName,
                Status = existingScan.Status,
                ExtractedData = JsonSerializer.Deserialize<Dictionary<string, object>>(existingScan.ExtractedData) ?? new()
            };
        }

        // ===============================
        // MÉTODOS PRIVADOS
        // ===============================

        private (bool IsValid, string? ErrorMessage) ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (false, "Archivo requerido");

            if (file.Length > 10 * 1024 * 1024) // 10MB
                return (false, "El archivo es demasiado grande (máximo 10MB)");

            if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return (false, "Solo se permiten archivos PDF");

            return (true, null);
        }

        private async Task<string> CalculateFileHashAsync(IFormFile file)
        {
            using var md5 = MD5.Create();
            using var stream = file.OpenReadStream();
            var hash = await Task.Run(() => md5.ComputeHash(stream));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private async Task<AzureDocumentResult> ProcessWithAzureAsync(IFormFile file)
        {
            var modelId = _configuration["AzureDocumentIntelligence:ModelId"];
            if (string.IsNullOrEmpty(modelId))
            {
                modelId = "prebuilt-document";
            }

            _logger.LogInformation("🤖 Enviando a Azure Document Intelligence con modelo: {ModelId}", modelId);

            try
            {
                using var stream = file.OpenReadStream();
                var content = BinaryData.FromStream(stream);

                var operation = await _documentClient.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    modelId,
                    content);

                var analyzeResult = operation.Value;

                // Extraer campos del resultado
                var extractedFields = new Dictionary<string, object>();
                int fieldsExtracted = 0;
                int totalFieldsAttempted = 0;

                if (analyzeResult.Documents != null && analyzeResult.Documents.Count > 0)
                {
                    var document = analyzeResult.Documents[0];

                    if (document.Fields != null)
                    {
                        totalFieldsAttempted = document.Fields.Count;

                        foreach (var field in document.Fields)
                        {
                            try
                            {
                                var value = ExtractFieldValue(field.Value);
                                if (!string.IsNullOrEmpty(value))
                                {
                                    extractedFields[field.Key] = value;
                                    fieldsExtracted++;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("⚠️ Error extrayendo campo {FieldName}: {Error}", field.Key, ex.Message);
                            }
                        }
                    }
                }

                // Si no hay campos específicos del modelo, extraer de key-value pairs y tables
                if (extractedFields.Count == 0)
                {
                    ExtractFromKeyValuePairs(analyzeResult, extractedFields, ref fieldsExtracted, ref totalFieldsAttempted);
                    ExtractFromTables(analyzeResult, extractedFields, ref fieldsExtracted, ref totalFieldsAttempted);
                }

                // Mapear campos específicos del modelo personalizado a nombres estándar
                var mappedFields = MapFieldsToStandardNames(extractedFields);

                // Procesar con parser avanzado
                var processedFields = _fieldParser.ProcessExtractedData(mappedFields);

                // Calcular tasa de éxito
                var successRate = totalFieldsAttempted > 0 ? (decimal)fieldsExtracted / totalFieldsAttempted * 100 : 0;

                _logger.LogInformation("✅ Azure procesó el documento: {FieldsExtracted}/{TotalFields} campos, {SuccessRate}% éxito",
                    fieldsExtracted, totalFieldsAttempted, successRate);

                return new AzureDocumentResult
                {
                    AzureOperationId = Guid.NewGuid().ToString(),
                    SuccessRate = successRate,
                    FieldsExtracted = fieldsExtracted,
                    TotalFieldsAttempted = totalFieldsAttempted,
                    ExtractedFields = processedFields
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en Azure Document Intelligence: {Message}", ex.Message);
                throw;
            }
        }

        // ✅ CORREGIDO: Método simplificado para evitar errores de tipos
        private string ExtractFieldValue(DocumentField field)
        {
            try
            {
                if (field == null)
                    return string.Empty;

                // ✅ Verificar campos de texto
                if (!string.IsNullOrEmpty(field.ValueString))
                    return field.ValueString;

                // ✅ Verificar números decimales
                if (field.ValueDouble.HasValue)
                    return field.ValueDouble.Value.ToString();

                // ✅ Verificar números enteros
                if (field.ValueInt64.HasValue)
                    return field.ValueInt64.Value.ToString();

                // ✅ Verificar fechas
                if (field.ValueDate.HasValue)
                    return field.ValueDate.Value.ToString("yyyy-MM-dd");

                // ✅ Verificar booleanos
                if (field.ValueBoolean.HasValue)
                    return field.ValueBoolean.Value.ToString();

                // ✅ Verificar país/región
                if (!string.IsNullOrEmpty(field.ValueCountryRegion))
                    return field.ValueCountryRegion;

                // ✅ Verificar número de teléfono
                if (!string.IsNullOrEmpty(field.ValuePhoneNumber))
                    return field.ValuePhoneNumber;

                // ✅ Contenido por defecto
                return field.Content ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error extrayendo valor del campo: {Error}", ex.Message);
                return field?.Content ?? string.Empty;
            }
        }

        private void ExtractFromKeyValuePairs(AnalyzeResult result, Dictionary<string, object> extractedFields,
            ref int fieldsExtracted, ref int totalFieldsAttempted)
        {
            if (result.KeyValuePairs != null)
            {
                foreach (var kvp in result.KeyValuePairs)
                {
                    totalFieldsAttempted++;

                    var key = kvp.Key?.Content?.Trim() ?? "";
                    var value = kvp.Value?.Content?.Trim() ?? "";

                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        var cleanKey = key.Replace(":", "").Replace(" ", "_").ToLowerInvariant();
                        extractedFields[cleanKey] = value;
                        fieldsExtracted++;
                    }
                }
            }
        }

        private void ExtractFromTables(AnalyzeResult result, Dictionary<string, object> extractedFields,
            ref int fieldsExtracted, ref int totalFieldsAttempted)
        {
            if (result.Tables != null)
            {
                foreach (var table in result.Tables)
                {
                    if (table.Cells != null)
                    {
                        for (int i = 0; i < table.Cells.Count - 1; i++)
                        {
                            var cell1 = table.Cells[i];
                            var cell2 = table.Cells[i + 1];

                            if (cell1.RowIndex == cell2.RowIndex &&
                                cell1.ColumnIndex + 1 == cell2.ColumnIndex)
                            {
                                totalFieldsAttempted++;

                                var key = cell1.Content?.Trim() ?? "";
                                var value = cell2.Content?.Trim() ?? "";

                                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                                {
                                    var cleanKey = key.Replace(":", "").Replace(" ", "_").ToLowerInvariant();
                                    extractedFields[cleanKey] = value;
                                    fieldsExtracted++;
                                }
                            }
                        }
                    }
                }
            }
        }

        private Dictionary<string, object> MapFieldsToStandardNames(Dictionary<string, object> azureFields)
        {
            var mappedFields = new Dictionary<string, object>();

            var fieldMappings = new Dictionary<string, string>
            {
                ["numero_poliza"] = "numeroPoliza",
                ["poliza_numero"] = "numeroPoliza",
                ["policy_number"] = "numeroPoliza",
                ["asegurado_nombre"] = "asegurado",
                ["cliente_nombre"] = "asegurado",
                ["vehiculo_descripcion"] = "vehiculo",
                ["vigencia_desde"] = "vigenciaDesde",
                ["vigencia_hasta"] = "vigenciaHasta",
                ["premio_total"] = "premio"
            };

            foreach (var azureField in azureFields)
            {
                var key = azureField.Key.ToLowerInvariant();
                var mappedKey = fieldMappings.ContainsKey(key) ? fieldMappings[key] : azureField.Key;
                mappedFields[mappedKey] = azureField.Value;
            }

            foreach (var azureField in azureFields)
            {
                if (!mappedFields.ContainsKey(azureField.Key))
                {
                    mappedFields[azureField.Key] = azureField.Value;
                }
            }

            return mappedFields;
        }

        private async Task UpdateDailyMetricsAsync(int userId, DateTime date, bool success, int processingTime, decimal successRate)
        {
            var metrics = await _context.DailyMetrics
                .FirstOrDefaultAsync(m => m.UserId == userId && m.Date == date.Date);

            if (metrics == null)
            {
                metrics = new DailyMetrics
                {
                    UserId = userId,
                    Date = date.Date
                };
                _context.DailyMetrics.Add(metrics);
            }

            metrics.TotalScans++;
            if (success)
            {
                metrics.SuccessfulScans++;
                metrics.BillableScans++;
            }
            else
            {
                metrics.FailedScans++;
            }

            // ✅ CORREGIDO: Cálculo simplificado para evitar errores de tipos
            if (metrics.TotalScans == 1)
            {
                metrics.AverageProcessingTimeMs = processingTime;
                metrics.AverageSuccessRate = successRate;
            }
            else
            {
                var totalScansDecimal = (decimal)metrics.TotalScans;
                var processingTimeDecimal = (decimal)processingTime;

                metrics.AverageProcessingTimeMs =
                    (metrics.AverageProcessingTimeMs * (totalScansDecimal - 1) + processingTimeDecimal) / totalScansDecimal;

                metrics.AverageSuccessRate =
                    (metrics.AverageSuccessRate * (totalScansDecimal - 1) + successRate) / totalScansDecimal;
            }

            await _context.SaveChangesAsync();
        }
    }

    public class AzureDocumentResult
    {
        public string AzureOperationId { get; set; } = string.Empty;
        public decimal SuccessRate { get; set; }
        public int FieldsExtracted { get; set; }
        public int TotalFieldsAttempted { get; set; }
        public Dictionary<string, object> ExtractedFields { get; set; } = new();
    }
}