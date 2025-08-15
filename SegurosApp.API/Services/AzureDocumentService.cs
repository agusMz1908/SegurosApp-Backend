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

        public AzureDocumentService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<AzureDocumentService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;

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
                    .Where(d => d.UserId == userId && d.FileMd5Hash == fileHash)
                    .OrderByDescending(d => d.CreatedAt)
                    .FirstOrDefaultAsync();

                if (existingScan != null)
                {
                    _logger.LogInformation("🔄 Documento duplicado detectado: {Hash}", fileHash);

                    return new DocumentScanResponse
                    {
                        Success = true,
                        IsDuplicate = true,
                        ExistingScanId = existingScan.Id,
                        ScanId = existingScan.Id,
                        FileName = existingScan.FileName,
                        FileSize = existingScan.FileSize,
                        FileMd5Hash = existingScan.FileMd5Hash,
                        ProcessingTimeMs = existingScan.ProcessingTimeMs,
                        SuccessRate = existingScan.SuccessRate,
                        FieldsExtracted = existingScan.FieldsExtracted,
                        TotalFieldsAttempted = existingScan.TotalFieldsAttempted,
                        Status = existingScan.Status,
                        ExtractedData = JsonSerializer.Deserialize<Dictionary<string, object>>(existingScan.ExtractedData) ?? new(),
                        CreatedAt = existingScan.CreatedAt,
                        CompletedAt = existingScan.CompletedAt
                    };
                }

                // Crear registro inicial en BD
                var documentScan = new DocumentScan
                {
                    UserId = userId,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    FileMd5Hash = fileHash,
                    Status = "Processing",
                    CreatedAt = startTime,
                    ProcessingTimeMs = 0,
                    SuccessRate = 0,
                    FieldsExtracted = 0,
                    TotalFieldsAttempted = 0,
                    ExtractedData = "{}"
                };

                _context.DocumentScans.Add(documentScan);
                await _context.SaveChangesAsync();

                // Procesar con Azure Document Intelligence
                var azureResult = await ProcessWithAzureAsync(file);

                stopwatch.Stop();
                var processingTime = (int)stopwatch.ElapsedMilliseconds;

                // Actualizar registro con resultados
                documentScan.AzureOperationId = azureResult.AzureOperationId;
                documentScan.ProcessingTimeMs = processingTime;
                documentScan.SuccessRate = azureResult.SuccessRate;
                documentScan.FieldsExtracted = azureResult.FieldsExtracted;
                documentScan.TotalFieldsAttempted = azureResult.TotalFieldsAttempted;
                documentScan.ExtractedData = JsonSerializer.Serialize(azureResult.ExtractedFields);
                documentScan.Status = "Completed";
                documentScan.CompletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Actualizar métricas diarias
                await UpdateDailyMetricsAsync(userId, startTime.Date, true, processingTime, azureResult.SuccessRate);

                _logger.LogInformation("✅ Documento procesado exitosamente: {ScanId} en {Time}ms",
                    documentScan.Id, processingTime);

                return new DocumentScanResponse
                {
                    Success = true,
                    ScanId = documentScan.Id,
                    FileName = documentScan.FileName,
                    FileSize = documentScan.FileSize,
                    FileMd5Hash = documentScan.FileMd5Hash,
                    ProcessingTimeMs = processingTime,
                    SuccessRate = azureResult.SuccessRate,
                    FieldsExtracted = azureResult.FieldsExtracted,
                    TotalFieldsAttempted = azureResult.TotalFieldsAttempted,
                    Status = "Completed",
                    ExtractedData = azureResult.ExtractedFields,
                    CreatedAt = documentScan.CreatedAt,
                    CompletedAt = documentScan.CompletedAt
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var processingTime = (int)stopwatch.ElapsedMilliseconds;

                _logger.LogError(ex, "❌ Error procesando documento: {FileName}", file.FileName);

                // Guardar error en BD
                var errorScan = new DocumentScan
                {
                    UserId = userId,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    FileMd5Hash = await CalculateFileHashAsync(file),
                    Status = "Error",
                    ErrorMessage = ex.Message,
                    CreatedAt = startTime,
                    CompletedAt = DateTime.UtcNow,
                    ProcessingTimeMs = processingTime,
                    ExtractedData = "{}"
                };

                _context.DocumentScans.Add(errorScan);
                await UpdateDailyMetricsAsync(userId, startTime.Date, false, processingTime, 0);
                await _context.SaveChangesAsync();

                return new DocumentScanResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    ProcessingTimeMs = processingTime,
                    Status = "Error",
                    CreatedAt = startTime
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

        public async Task<List<DocumentHistoryDto>> GetScanHistoryAsync(int userId, DocumentSearchFilters filters)
        {
            var query = _context.DocumentScans
                .Where(d => d.UserId == userId);

            // Aplicar filtros
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

            var scans = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((filters.Page - 1) * filters.Limit)
                .Take(filters.Limit)
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
            var failedScans = scans.Count(s => s.Status == "Error");
            var billableScans = scans.Count(s => s.IsBillable);

            return new DocumentMetricsDto
            {
                TotalScans = totalScans,
                SuccessfulScans = successfulScans,
                FailedScans = failedScans,
                BillableScans = billableScans,
                AverageSuccessRate = totalScans > 0 ? scans.Average(s => s.SuccessRate) : 0,
                AverageProcessingTimeMs = totalScans > 0 ? (int)scans.Average(s => s.ProcessingTimeMs) : 0,
                PeriodStart = fromDate.Value,
                PeriodEnd = toDate.Value
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

            // Aquí implementarías la lógica de reprocesamiento
            // Por ahora, solo devolver los datos existentes
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
            var modelId = _configuration["AzureDocumentIntelligence:ModelId"] ?? "prebuilt-document";

            _logger.LogInformation("🤖 Enviando a Azure Document Intelligence con modelo: {ModelId}", modelId);

            using var stream = file.OpenReadStream();

            // Aquí va la llamada real a Azure Document Intelligence
            // Por ahora, simulamos el procesamiento
            await Task.Delay(2000); // Simular procesamiento

            // Datos simulados (en implementación real, estos vendrían de Azure)
            var extractedFields = new Dictionary<string, object>
            {
                ["numeroPoliza"] = "POL-2025-001",
                ["asegurado"] = "Juan Pérez",
                ["vehiculo"] = "Toyota Corolla 2020",
                ["vigenciaDesde"] = "2025-01-01",
                ["vigenciaHasta"] = "2026-01-01",
                ["premio"] = "15000"
            };

            return new AzureDocumentResult
            {
                AzureOperationId = Guid.NewGuid().ToString(),
                SuccessRate = 85.5m,
                FieldsExtracted = extractedFields.Count,
                TotalFieldsAttempted = 8,
                ExtractedFields = extractedFields
            };
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

            // Calcular promedios
            metrics.AvgProcessingTimeMs = (metrics.AvgProcessingTimeMs + processingTime) / 2;
            metrics.AvgSuccessRate = (metrics.AvgSuccessRate + successRate) / 2;
        }

        private static DocumentHistoryDto MapToHistoryDto(DocumentScan scan)
        {
            return new DocumentHistoryDto
            {
                Id = scan.Id,
                FileName = scan.FileName,
                FileSize = scan.FileSize,
                Status = scan.Status,
                SuccessRate = scan.SuccessRate,
                FieldsExtracted = scan.FieldsExtracted,
                ProcessingTimeMs = scan.ProcessingTimeMs,
                CreatedAt = scan.CreatedAt,
                CompletedAt = scan.CompletedAt,
                VelneoPolizaNumber = scan.VelneoPolizaNumber,
                VelneoCreated = scan.VelneoCreated,
                IsBillable = scan.IsBillable,
                IsBilled = scan.IsBilled,
                BilledAt = scan.BilledAt
            };
        }
    }
}

