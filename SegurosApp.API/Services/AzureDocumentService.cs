using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo;
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
        private readonly IAzureModelMappingService _modelMappingService;
        private readonly BillingService _billingService;

        public AzureDocumentService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<AzureDocumentService> logger,
            DocumentFieldParser fieldParser,
            IAzureModelMappingService modelMappingService,
            BillingService billingService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _fieldParser = fieldParser;
            _modelMappingService = modelMappingService;
            _billingService = billingService;

            var endpoint = _configuration["AzureDocumentIntelligence:Endpoint"];
            var apiKey = _configuration["AzureDocumentIntelligence:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || endpoint.Contains("PLACEHOLDER"))
            {
                _logger.LogError("Azure Endpoint es placeholder o vacío. Usando modo mock.");
                _documentClient = null;
                return;
            }

            if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("PLACEHOLDER"))
            {
                _logger.LogError("Azure ApiKey es placeholder o vacío. Usando modo mock.");
                _documentClient = null;
                return;
            }

            try
            {
                _documentClient = new DocumentIntelligenceClient(
                    new Uri(endpoint),
                    new AzureKeyCredential(apiKey)
                );

                _logger.LogInformation("Azure Document Intelligence Service inicializado correctamente");
            }
            catch (UriFormatException ex)
            {
                _logger.LogError(ex, "Error con formato de URI de Azure: {Endpoint}", endpoint);
                _documentClient = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inicializando Azure Document Intelligence");
                _documentClient = null;
            }
        }

        public async Task<DocumentScanResponse> ProcessDocumentAsync(IFormFile file, int userId, int? companiaId = null)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Procesando documento: {FileName} para usuario: {UserId}, compañía: {CompaniaId}",
                    file.FileName, userId, companiaId ?? 0);

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

                var fileHash = await CalculateFileHashAsync(file);
                var existingScan = await _context.DocumentScans
                    .Where(d => d.FileMd5Hash == fileHash && d.UserId == userId)
                    .FirstOrDefaultAsync();

                if (existingScan != null)
                {
                    _logger.LogInformation("Documento duplicado detectado: {FileName}", file.FileName);

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
                        CompletedAt = existingScan.CompletedAt,
                        AzureModelUsed = existingScan.AzureModelId,
                        CompaniaId = existingScan.CompaniaId
                    };
                }

                string modelId;

                if (companiaId.HasValue)
                {
                    var modelInfo = await _modelMappingService.GetModelByCompaniaIdAsync(companiaId.Value);
                    modelId = modelInfo.ModelId;

                    _logger.LogInformation("Modelo seleccionado para compañía {CompaniaId}: {ModelId} - {Description}",
                        companiaId, modelId, modelInfo.Description);
                }
                else
                {
                    modelId = _configuration["AzureDocumentIntelligence:ModelId"] ?? "poliza_vehiculos_bse";

                    _logger.LogInformation("Usando modelo por defecto (sin compañía especificada): {ModelId}", modelId);
                }

                var azureResult = await ProcessWithAzureAsync(file, modelId);
                stopwatch.Stop();

                var documentScan = new DocumentScan
                {
                    UserId = userId,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    FileMd5Hash = fileHash,
                    AzureModelId = modelId, 
                    CompaniaId = companiaId, 
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
                    CompletedAt = DateTime.UtcNow,
                    AzureModelUsed = modelId,
                    CompaniaId = companiaId
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error procesando documento: {FileName}", file.FileName);

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

            int pageSize;
            if (filters.PageSize.HasValue)
            {
                pageSize = filters.PageSize.Value;
            }
            else
            {
                pageSize = filters.Limit;
            }

            int page = filters.Page; 

            var scans = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
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

        private (bool IsValid, string? ErrorMessage) ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (false, "Archivo requerido");

            if (file.Length > 10 * 1024 * 1024) 
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

        private async Task<AzureDocumentResult> ProcessWithAzureAsync(IFormFile file, string modelId)
        {
            _logger.LogInformation("Procesando con Azure modelo: {ModelId}", modelId);

            if (_documentClient == null)
            {
                throw new InvalidOperationException("Azure Document Intelligence no está configurado correctamente");
            }

            using var stream = file.OpenReadStream();
            var binaryData = BinaryData.FromStream(stream);

            _logger.LogInformation("Enviando {FileSize} bytes a Azure con modelo {ModelId}...", file.Length, modelId);

            try
            {
                var operation = await _documentClient.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    modelId,
                    binaryData);

                var analyzeResult = operation.Value;

                var confidence = (analyzeResult.Documents?.FirstOrDefault()?.Confidence ?? 0.5f);
                _logger.LogInformation("Azure procesó el documento. Confidence: {Confidence:P1}", confidence);

                var extractedFields = ExtractRealFieldsFromAzureResult(analyzeResult);

                return new AzureDocumentResult
                {
                    AzureOperationId = operation.Id ?? "azure-" + Guid.NewGuid().ToString("N")[..8],
                    SuccessRate = (decimal)(confidence * 100),
                    FieldsExtracted = extractedFields.Count,
                    TotalFieldsAttempted = Math.Max(8, extractedFields.Count),
                    ExtractedFields = extractedFields
                };
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError("Azure RequestFailedException: Status={Status}, ErrorCode={ErrorCode}, Message={Message}",
                    ex.Status, ex.ErrorCode, ex.Message);

                _logger.LogError("Full exception: {Exception}", ex.ToString());

                throw new InvalidOperationException($"Azure Document Intelligence error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado procesando con Azure");
                throw;
            }
        }

        private Dictionary<string, object> ExtractRealFieldsFromAzureResult(AnalyzeResult analyzeResult)
        {
            var extractedFields = new Dictionary<string, object>();

            try
            {
                if (analyzeResult?.Documents == null)
                {
                    _logger.LogWarning("AnalyzeResult o Documents es null");
                    return extractedFields;
                }

                foreach (var document in analyzeResult.Documents)
                {
                    _logger.LogInformation("Documento procesado con confianza: {Confidence:P1}",
                        document.Confidence);

                    if (document.Fields == null)
                    {
                        _logger.LogWarning("Document.Fields es null");
                        continue;
                    }

                    foreach (var field in document.Fields)
                    {
                        try
                        {
                            var value = ExtractFieldValue(field.Value);
                            if (!string.IsNullOrEmpty(value))
                            {
                                extractedFields[field.Key] = value;
                                _logger.LogInformation("Campo: {Key} = {Value} (Confidence: {Confidence:P1})",
                                    field.Key, value, field.Value.Confidence ?? 0);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Error extrayendo campo {FieldName}: {Error}", field.Key, ex.Message);
                        }
                    }
                }

                if (analyzeResult.KeyValuePairs != null)
                {
                    _logger.LogInformation("Extrayendo {Count} pares clave-valor", analyzeResult.KeyValuePairs.Count);

                    foreach (var kvp in analyzeResult.KeyValuePairs)
                    {
                        try
                        {
                            var key = kvp.Key?.Content?.ToLowerInvariant()
                                .Replace(" ", "_")
                                .Replace(":", "")
                                .Replace(".", "")
                                .Replace("-", "_") ?? "";

                            var value = kvp.Value?.Content?.Trim() ?? "";

                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value) && value.Length > 1)
                            {
                                if (!extractedFields.ContainsKey(key))
                                {
                                    extractedFields[key] = value;
                                    _logger.LogInformation("KVP: {Key} = {Value}", key, value);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Error extrayendo KVP: {Error}", ex.Message);
                        }
                    }
                }

                if (analyzeResult.Tables?.Count > 0)
                {
                    _logger.LogInformation("Encontradas {TableCount} tablas", analyzeResult.Tables.Count);
                    var tableData = ExtractTableData(analyzeResult.Tables);
                    foreach (var kvp in tableData)
                    {
                        if (!extractedFields.ContainsKey(kvp.Key))
                        {
                            extractedFields[kvp.Key] = kvp.Value;
                        }
                    }
                }

                _logger.LogInformation("Total campos extraídos: {Count}", extractedFields.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo campos de Azure result");
            }

            return extractedFields;
        }

        private Dictionary<string, object> ExtractTableData(IReadOnlyList<DocumentTable> tables)
        {
            var tableFields = new Dictionary<string, object>();

            try
            {
                foreach (var table in tables.Take(3)) 
                {
                    _logger.LogInformation("Procesando tabla con {Rows} filas y {Cols} columnas",
                        table.RowCount, table.ColumnCount);

                    for (int row = 0; row < table.RowCount && row < 10; row++) 
                    {
                        for (int col = 0; col < table.ColumnCount && col < 5; col++) 
                        {
                            var cell = table.Cells.FirstOrDefault(c => c.RowIndex == row && c.ColumnIndex == col);
                            if (cell != null && !string.IsNullOrEmpty(cell.Content?.Trim()))
                            {
                                var key = $"tabla_{row}_{col}";
                                tableFields[key] = cell.Content.Trim();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo datos de tablas");
            }

            return tableFields;
        }

        private string ExtractFieldValue(DocumentField field)
        {
            try
            {
                if (field == null)
                    return string.Empty;

                if (!string.IsNullOrEmpty(field.Content))
                    return field.Content.Trim();

                if (!string.IsNullOrEmpty(field.ValueString))
                    return field.ValueString.Trim();

                if (field.ValueDouble.HasValue)
                    return field.ValueDouble.Value.ToString();

                if (field.ValueInt64.HasValue)
                    return field.ValueInt64.Value.ToString();

                if (field.ValueDate.HasValue)
                    return field.ValueDate.Value.ToString("yyyy-MM-dd");

                if (field.ValueBoolean.HasValue)
                    return field.ValueBoolean.Value.ToString();

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error extrayendo valor del campo: {Error}", ex.Message);
                return field?.Content ?? string.Empty;
            }
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

            if (metrics.TotalScans == 1)
            {
                metrics.AvgProcessingTimeMs = processingTime;
                metrics.AvgSuccessRate = successRate;
            }
            else
            {
                var totalScansDecimal = (decimal)metrics.TotalScans;
                var processingTimeDecimal = (decimal)processingTime;


                metrics.AvgProcessingTimeMs =
                    (metrics.AvgProcessingTimeMs * (totalScansDecimal - 1) + processingTimeDecimal) / totalScansDecimal;

                metrics.AvgSuccessRate =
                    (metrics.AvgSuccessRate * (totalScansDecimal - 1) + successRate) / totalScansDecimal;
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateScanWithVelneoInfoAsync(int scanId, string velneoPolizaNumber, bool velneoCreated)
        {
            try
            {
                _logger.LogInformation("Actualizando scan {ScanId} con info Velneo: {PolizaNumber}, Creado: {Created}",
                    scanId, velneoPolizaNumber, velneoCreated);

                var scan = await _context.DocumentScans.FindAsync(scanId);
                if (scan == null)
                {
                    _logger.LogWarning("Scan {ScanId} no encontrado", scanId);
                    return;
                }

                scan.VelneoPolizaNumber = velneoPolizaNumber;
                scan.VelneoCreated = velneoCreated;

                if (velneoCreated && scan.IsBillable && !scan.IsBilled)
                {
                    try
                    {
                        await _billingService.AddToCurrentMonthBillingAsync(scanId, scan.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error en facturación automática para scan {ScanId}", scanId);
                    }
                }

                await _context.SaveChangesAsync();

                if (velneoCreated && scan.IsBillable && !scan.IsBilled)
                {
                    try
                    {
                        _logger.LogInformation("Agregando a factura mensual automática para scan {ScanId}", scanId);

                        var billingItem = await _billingService.AddToCurrentMonthBillingAsync(scanId, scan.UserId);

                        if (billingItem != null)
                        {
                            _logger.LogInformation("Scan {ScanId} agregado a factura mensual exitosamente", scanId);
                        }
                    }
                    catch (Exception billingEx)
                    {
                        _logger.LogError(billingEx, "Error agregando scan {ScanId} a factura mensual, pero Velneo fue exitoso", scanId);
                    }
                }

                _logger.LogInformation("Scan {ScanId} actualizado exitosamente", scanId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando scan {ScanId} con info Velneo", scanId);
                throw;
            }
        }

        public async Task<List<DocumentHistoryDto>> GetPendingVelneoScansAsync(int userId, int limit = 50)
        {
            try
            {
                _logger.LogInformation("Obteniendo escaneos pendientes Velneo para usuario {UserId}", userId);

                var pendingScans = await _context.DocumentScans
                    .Where(d => d.UserId == userId &&
                               d.Status == "Completed" &&  
                               !d.VelneoCreated &&         
                               d.SuccessRate >= 70)       
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                var result = pendingScans.Select(MapToHistoryDto).ToList();

                _logger.LogInformation("Encontrados {Count} escaneos pendientes para Velneo", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo escaneos pendientes Velneo para usuario {UserId}", userId);
                return new List<DocumentHistoryDto>();
            }
        }

        public async Task<VelneoIntegrationMetricsDto> GetVelneoIntegrationMetricsAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                fromDate ??= DateTime.UtcNow.AddDays(-30);
                toDate ??= DateTime.UtcNow;

                _logger.LogInformation("Calculando métricas Velneo para usuario {UserId} desde {FromDate} hasta {ToDate}",
                    userId, fromDate, toDate);

                var scans = await _context.DocumentScans
                    .Where(d => d.UserId == userId &&
                               d.CreatedAt >= fromDate &&
                               d.CreatedAt <= toDate &&
                               d.Status == "Completed")
                    .ToListAsync();

                var metrics = new VelneoIntegrationMetricsDto
                {
                    TotalScans = scans.Count,
                    SuccessfulScans = scans.Count(s => s.Status == "Completed"),
                    PendingVelneoCreation = scans.Count(s => !s.VelneoCreated && s.IsBillable),
                    VelneoCreatedSuccessfully = scans.Count(s => s.VelneoCreated),
                    VelneoCreationFailed = scans.Count(s => !s.VelneoCreated && s.IsBillable && s.CreatedAt < DateTime.UtcNow.AddHours(-1)), 

                    PeriodStart = fromDate.Value,
                    PeriodEnd = toDate.Value,

                    VelneoSuccessRate = scans.Count > 0 ?
                        (decimal)scans.Count(s => s.VelneoCreated) / scans.Count * 100 : 0,

                    ProcessingEfficiency = scans.Count > 0 ?
                        (decimal)scans.Count(s => s.SuccessRate >= 80) / scans.Count * 100 : 0,

                    AverageProcessingTimeMs = scans.Count > 0 ?
                        (int)scans.Average(s => s.ProcessingTimeMs) : 0,

                    AverageVelneoCreationTimeMs = 2000,

                    DailyMetrics = await CalculateDailyVelneoMetricsAsync(userId, fromDate.Value, toDate.Value),

                    ProblematicDocuments = await GetProblematicVelneoDocumentsAsync(userId, fromDate.Value, toDate.Value),

                    Quality = await CalculateQualityMetricsAsync(scans)
                };

                _logger.LogInformation("Métricas Velneo calculadas: {TotalScans} scans, {VelneoSuccessRate:F1}% éxito Velneo",
                    metrics.TotalScans, metrics.VelneoSuccessRate);

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculando métricas Velneo para usuario {UserId}", userId);
                return new VelneoIntegrationMetricsDto
                {
                    PeriodStart = fromDate ?? DateTime.UtcNow.AddDays(-30),
                    PeriodEnd = toDate ?? DateTime.UtcNow
                };
            }
        }
        private async Task<List<DailyVelneoMetric>> CalculateDailyVelneoMetricsAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var dailyMetrics = new List<DailyVelneoMetric>();

                for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
                {
                    var dayScans = await _context.DocumentScans
                        .Where(d => d.UserId == userId &&
                                   d.CreatedAt.Date == date &&
                                   d.Status == "Completed")
                        .ToListAsync();

                    if (dayScans.Any())
                    {
                        var metric = new DailyVelneoMetric
                        {
                            Date = date,
                            TotalScans = dayScans.Count,
                            VelneoCreated = dayScans.Count(s => s.VelneoCreated),
                            VelneoFailed = dayScans.Count(s => !s.VelneoCreated && s.IsBillable),
                            PendingVelneo = dayScans.Count(s => !s.VelneoCreated && s.IsBillable),
                            SuccessRate = dayScans.Count > 0 ?
                                (decimal)dayScans.Count(s => s.VelneoCreated) / dayScans.Count * 100 : 0,
                            AverageProcessingTimeMs = (int)dayScans.Average(s => s.ProcessingTimeMs)
                        };

                        dailyMetrics.Add(metric);
                    }
                }

                return dailyMetrics.OrderBy(m => m.Date).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculando métricas diarias Velneo");
                return new List<DailyVelneoMetric>();
            }
        }

        private async Task<List<ProblematicVelneoDocumentDto>> GetProblematicVelneoDocumentsAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var problematicScans = await _context.DocumentScans
                    .Where(d => d.UserId == userId &&
                               d.CreatedAt >= fromDate &&
                               d.CreatedAt <= toDate &&
                               d.Status == "Completed" &&
                               !d.VelneoCreated &&
                               d.IsBillable &&
                               d.CreatedAt < DateTime.UtcNow.AddHours(-1)) 
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(20) 
                    .ToListAsync();

                return problematicScans.Select(scan => new ProblematicVelneoDocumentDto
                {
                    ScanId = scan.Id,
                    FileName = scan.FileName,
                    CreatedAt = scan.CreatedAt,
                    ErrorType = "VelneoCreationPending",
                    ErrorMessage = "Documento no enviado a Velneo después de procesamiento exitoso",
                    RetryCount = 0, 
                    Status = "PendingVelneo",

                    HasClienteId = false, 
                    HasCompaniaId = false,
                    HasSeccionId = false,
                    HasPolicyNumber = !string.IsNullOrEmpty(GetPolicyNumberFromExtractedData(scan.ExtractedData)),
                    DataCompleteness = scan.SuccessRate
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo documentos problemáticos Velneo");
                return new List<ProblematicVelneoDocumentDto>();
            }
        }

        private async Task<QualityMetrics> CalculateQualityMetricsAsync(List<DocumentScan> scans)
        {
            try
            {
                var qualityMetrics = new QualityMetrics
                {
                    AverageDataCompleteness = scans.Count > 0 ? scans.Average(s => s.SuccessRate) : 0,
                    AverageMappingConfidence = scans.Count > 0 ? scans.Average(s => s.SuccessRate) : 0,
                    DocumentsRequiringManualReview = scans.Count(s => s.SuccessRate < 70),
                    AutoProcessedDocuments = scans.Count(s => s.SuccessRate >= 80),

                    ProblematicFields = new List<FieldQualityMetric>(),
                    BestPerformingFields = new List<FieldQualityMetric>()
                };

                return qualityMetrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculando métricas de calidad");
                return new QualityMetrics();
            }
        }

        private string GetPolicyNumberFromExtractedData(string extractedDataJson)
        {
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(extractedDataJson);
                if (data == null) return "";

                var possibleFields = new[] { "poliza.numero", "numeroPoliza", "datos_poliza" };

                foreach (var field in possibleFields)
                {
                    if (data.TryGetValue(field, out var value) && value != null)
                    {
                        var text = value.ToString() ?? "";
                        var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d{7,9})");
                        if (match.Success)
                        {
                            return match.Groups[1].Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error extrayendo número de póliza de datos guardados: {Error}", ex.Message);
            }

            return "";
        }

        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                var modelInfos = await _modelMappingService.GetAllAvailableModelsAsync();
                return modelInfos.Select(m => m.ModelId).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo modelos disponibles");
                return new List<string> { "poliza_vehiculos_bse" }; 
            }
        }

    }
}