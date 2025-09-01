using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SegurosApp.API.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SegurosApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AzureController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureController> _logger;

        public AzureController(
            IConfiguration configuration,
            ILogger<AzureController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("config")]
        [ProducesResponseType(typeof(object), 200)]
        public ActionResult GetAzureConfig()
        {
            try
            {
                var endpoint = _configuration["AzureDocumentIntelligence:Endpoint"];
                var apiKey = _configuration["AzureDocumentIntelligence:ApiKey"];
                var modelId = _configuration["AzureDocumentIntelligence:ModelId"];

                return Ok(new
                {
                    endpoint = endpoint,
                    hasApiKey = !string.IsNullOrEmpty(apiKey),
                    apiKeyLength = apiKey?.Length ?? 0,
                    apiKeyPrefix = apiKey?.Substring(0, Math.Min(8, apiKey?.Length ?? 0)) + "...",
                    modelId = modelId,
                    timestamp = DateTime.UtcNow,
                    status = "configured"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo configuración Azure");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("health")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult> TestAzureHealth()
        {
            try
            {
                var endpoint = _configuration["AzureDocumentIntelligence:Endpoint"];
                var apiKey = _configuration["AzureDocumentIntelligence:ApiKey"];

                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Azure configuration missing",
                        hasEndpoint = !string.IsNullOrEmpty(endpoint),
                        hasApiKey = !string.IsNullOrEmpty(apiKey)
                    });
                }

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

                var testUrl = $"{endpoint.TrimEnd('/')}/formrecognizer/info?api-version=2023-07-31";
                _logger.LogInformation("🧪 Testing connectivity: {Url}", testUrl);

                var response = await httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(new
                    {
                        success = true,
                        message = "Azure connectivity successful",
                        statusCode = response.StatusCode,
                        endpoint = endpoint,
                        apiVersion = "2023-07-31",
                        response = content
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return BadRequest(new
                    {
                        success = false,
                        error = $"Azure returned: {response.StatusCode}",
                        details = errorContent,
                        endpoint = endpoint
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en health check de Azure");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("model-info")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult> GetModelInfo()
        {
            try
            {
                var endpoint = _configuration["AzureDocumentIntelligence:Endpoint"];
                var apiKey = _configuration["AzureDocumentIntelligence:ApiKey"];
                var modelId = _configuration["AzureDocumentIntelligence:ModelId"];

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

                var endpointsToTry = new[]
                {
                    ($"{endpoint.TrimEnd('/')}/formrecognizer/documentModels/{modelId}?api-version=2024-11-30", "2024-11-30"),
                    ($"{endpoint.TrimEnd('/')}/formrecognizer/documentModels/{modelId}?api-version=2023-07-31", "2023-07-31"),
                    ($"{endpoint.TrimEnd('/')}/documentintelligence/documentModels/{modelId}?api-version=2024-02-29-preview", "2024-02-29-preview"),
                    ($"{endpoint.TrimEnd('/')}/formrecognizer/documentModels/{modelId}?api-version=2022-08-31", "2022-08-31")
                };

                var results = new List<object>();

                foreach (var (url, version) in endpointsToTry)
                {
                    try
                    {
                        _logger.LogInformation("🔍 Testing model endpoint: {Url}", url);
                        var response = await httpClient.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            _logger.LogInformation("✅ SUCCESS with API version: {Version}", version);

                            return Ok(new
                            {
                                success = true,
                                modelId = modelId,
                                endpoint = endpoint,
                                apiVersion = version,
                                workingUrl = url,
                                status = "Model found and accessible",
                                modelDetails = JsonSerializer.Deserialize<object>(content),
                                timestamp = DateTime.UtcNow
                            });
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            results.Add(new
                            {
                                apiVersion = version,
                                statusCode = response.StatusCode,
                                error = errorContent
                            });
                            _logger.LogWarning("❌ Failed with API version {Version}: {StatusCode}", version, response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add(new { apiVersion = version, error = ex.Message });
                        _logger.LogError(ex, "Error testing API version: {Version}", version);
                    }
                }

                return BadRequest(new
                {
                    success = false,
                    error = "Model not accessible with any API version",
                    modelId = modelId,
                    endpoint = endpoint,
                    attempts = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo información del modelo");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("models")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult> ListAvailableModels()
        {
            try
            {
                var endpoint = _configuration["AzureDocumentIntelligence:Endpoint"];
                var apiKey = _configuration["AzureDocumentIntelligence:ApiKey"];

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

                var listEndpoints = new[]
                {
                    ($"{endpoint.TrimEnd('/')}/formrecognizer/documentModels?api-version=2024-11-30", "Form Recognizer 2024-11-30"),
                    ($"{endpoint.TrimEnd('/')}/formrecognizer/documentModels?api-version=2023-07-31", "Form Recognizer 2023-07-31"),
                    ($"{endpoint.TrimEnd('/')}/documentintelligence/documentModels?api-version=2024-02-29-preview", "Document Intelligence Preview"),
                    ($"{endpoint.TrimEnd('/')}/formrecognizer/documentModels?api-version=2022-08-31", "Form Recognizer 2022-08-31")
                };

                foreach (var (url, name) in listEndpoints)
                {
                    try
                    {
                        _logger.LogInformation("🔍 Listing models with: {Name}", name);
                        var response = await httpClient.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            _logger.LogInformation("✅ SUCCESS listing models with: {Name}", name);

                            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);
                            var models = new List<object>();

                            if (jsonResponse.TryGetProperty("value", out var valueArray))
                            {
                                foreach (var model in valueArray.EnumerateArray())
                                {
                                    var modelInfo = new
                                    {
                                        modelId = model.TryGetProperty("modelId", out var id) ? id.GetString() : "N/A",
                                        description = model.TryGetProperty("description", out var desc) ? desc.GetString() : "N/A",
                                        createdDateTime = model.TryGetProperty("createdDateTime", out var created) ? created.GetString() : "N/A",
                                        status = model.TryGetProperty("status", out var status) ? status.GetString() : "N/A"
                                    };
                                    models.Add(modelInfo);
                                }
                            }

                            return Ok(new
                            {
                                success = true,
                                apiVersion = name,
                                endpoint = endpoint,
                                totalModels = models.Count,
                                models = models,
                                timestamp = DateTime.UtcNow
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error listing models with: {Name}", name);
                    }
                }

                return BadRequest(new { success = false, error = "Could not list models with any API version" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error listando modelos");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("test-process")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult> TestProcessDocument([Required] IFormFile file)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, error = "Archivo requerido" });
                }

                var endpoint = _configuration["AzureDocumentIntelligence:Endpoint"];
                var apiKey = _configuration["AzureDocumentIntelligence:ApiKey"];
                var modelId = _configuration["AzureDocumentIntelligence:ModelId"];

                _logger.LogInformation("🧪 Test processing: {FileName} ({FileSize} bytes)", file.FileName, file.Length);

                try
                {
                    var client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

                    using var stream = file.OpenReadStream();
                    var binaryData = BinaryData.FromStream(stream);

                    var operation = await client.AnalyzeDocumentAsync(
                        WaitUntil.Completed,
                        modelId,
                        binaryData);

                    var result = operation.Value;
                    stopwatch.Stop();

                    var fieldsFound = new List<object>();
                    if (result.Documents?.Count > 0)
                    {
                        var document = result.Documents[0];
                        if (document.Fields != null)
                        {
                            foreach (var field in document.Fields)
                            {
                                fieldsFound.Add(new
                                {
                                    name = field.Key,
                                    value = field.Value.Content ?? field.Value.ValueString,
                                    confidence = field.Value.Confidence,
                                    type = field.Value.ToString()
                                });
                            }
                        }
                    }

                    return Ok(new
                    {
                        success = true,
                        fileName = file.FileName,
                        fileSize = file.Length,
                        processingTimeMs = stopwatch.ElapsedMilliseconds,
                        modelId = modelId,
                        operationId = operation.Id,
                        confidence = result.Documents?.FirstOrDefault()?.Confidence ?? 0,
                        fieldsExtracted = fieldsFound.Count,
                        fields = fieldsFound,
                        hasKeyValuePairs = result.KeyValuePairs?.Count > 0,
                        keyValuePairsCount = result.KeyValuePairs?.Count ?? 0,
                        hasTables = result.Tables?.Count > 0,
                        tablesCount = result.Tables?.Count ?? 0,
                        contentLength = result.Content?.Length ?? 0,
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (RequestFailedException ex)
                {
                    stopwatch.Stop();
                    _logger.LogError("❌ Azure RequestFailedException: {Status} - {ErrorCode} - {Message}",
                        ex.Status, ex.ErrorCode, ex.Message);

                    return BadRequest(new
                    {
                        success = false,
                        error = "Azure Document Intelligence error",
                        status = ex.Status,
                        errorCode = ex.ErrorCode,
                        message = ex.Message,
                        modelId = modelId,
                        endpoint = endpoint,
                        processingTimeMs = stopwatch.ElapsedMilliseconds,
                        suggestion = ex.Status == 401 ? "Check API key" :
                                   ex.Status == 404 ? "Check model ID" : "Check configuration"
                    });
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "❌ Error en test de procesamiento");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    processingTimeMs = stopwatch.ElapsedMilliseconds
                });
            }
        }

        [HttpPost("change-model")]
        [ProducesResponseType(typeof(object), 200)]
        public ActionResult ChangeModel([FromBody] ChangeModelRequest request)
        {
            try
            {
                _configuration["AzureDocumentIntelligence:ModelId"] = request.ModelId;

                return Ok(new
                {
                    success = true,
                    message = "Model changed temporarily (session only)",
                    newModelId = request.ModelId,
                    note = "Change is not persistent - restart will revert to configured model",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cambiando modelo");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("diagnostics")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult> GetCompleteDiagnostics()
        {
            try
            {
                var diagnostics = new
                {
                    configuration = await GetConfigurationDiagnostics(),
                    connectivity = await GetConnectivityDiagnostics(),
                    modelAccess = await GetModelAccessDiagnostics(),
                    timestamp = DateTime.UtcNow
                };

                return Ok(diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en diagnósticos completos");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        #region Métodos de apoyo para diagnósticos

        private async Task<object> GetConfigurationDiagnostics()
        {
            var endpoint = _configuration["AzureDocumentIntelligence:Endpoint"];
            var apiKey = _configuration["AzureDocumentIntelligence:ApiKey"];
            var modelId = _configuration["AzureDocumentIntelligence:ModelId"];

            return new
            {
                hasEndpoint = !string.IsNullOrEmpty(endpoint),
                endpoint = endpoint,
                hasApiKey = !string.IsNullOrEmpty(apiKey),
                apiKeyLength = apiKey?.Length ?? 0,
                apiKeyPrefix = apiKey?.Substring(0, Math.Min(8, apiKey?.Length ?? 0)),
                hasModelId = !string.IsNullOrEmpty(modelId),
                modelId = modelId,
                configurationSource = "appsettings.Development.json or user secrets"
            };
        }

        private async Task<object> GetConnectivityDiagnostics()
        {
            try
            {
                var endpoint = _configuration["AzureDocumentIntelligence:Endpoint"];
                var apiKey = _configuration["AzureDocumentIntelligence:ApiKey"];

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

                var testUrl = $"{endpoint.TrimEnd('/')}/formrecognizer/info?api-version=2023-07-31";
                var response = await httpClient.GetAsync(testUrl);

                return new
                {
                    canConnect = response.IsSuccessStatusCode,
                    statusCode = response.StatusCode,
                    responseHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                    testUrl = testUrl
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    canConnect = false,
                    error = ex.Message,
                    errorType = ex.GetType().Name
                };
            }
        }

        private async Task<object> GetModelAccessDiagnostics()
        {
            try
            {
                var endpoint = _configuration["AzureDocumentIntelligence:Endpoint"];
                var apiKey = _configuration["AzureDocumentIntelligence:ApiKey"];
                var modelId = _configuration["AzureDocumentIntelligence:ModelId"];

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

                var modelUrl = $"{endpoint.TrimEnd('/')}/formrecognizer/documentModels/{modelId}?api-version=2024-11-30";
                var response = await httpClient.GetAsync(modelUrl);

                return new
                {
                    canAccessModel = response.IsSuccessStatusCode,
                    statusCode = response.StatusCode,
                    modelId = modelId,
                    testUrl = modelUrl,
                    response = response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : await response.Content.ReadAsStringAsync()
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    canAccessModel = false,
                    error = ex.Message,
                    modelId = _configuration["AzureDocumentIntelligence:ModelId"]
                };
            }
        }

        [HttpGet("models-by-company")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult> GetModelsByCompany()
        {
            try
            {
                _logger.LogInformation("Obteniendo mapeo de modelos por compañía");

                var modelMappingService = HttpContext.RequestServices.GetRequiredService<IAzureModelMappingService>();
                var availableModels = await modelMappingService.GetAllAvailableModelsAsync();

                var response = new
                {
                    success = true,
                    totalModels = availableModels.Count,
                    models = availableModels.Select(m => new
                    {
                        modelId = m.ModelId,
                        modelName = m.ModelName,
                        companiaId = m.CompaniaId,
                        companiaAlias = m.CompaniaAlias,
                        description = m.Description,
                        isActive = m.IsActive
                    }).ToList(),
                    timestamp = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo mapeo de modelos");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("test-model-selection")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult> TestModelSelection([FromForm] int companiaId)
        {
            try
            {
                _logger.LogInformation("Probando selección de modelo para compañía: {CompaniaId}", companiaId);

                var modelMappingService = HttpContext.RequestServices.GetRequiredService<IAzureModelMappingService>();
                var modelInfo = await modelMappingService.GetModelByCompaniaIdAsync(companiaId);
                var hasModel = await modelMappingService.HasModelForCompaniaAsync(companiaId);

                var response = new
                {
                    success = true,
                    companiaId = companiaId,
                    selectedModel = new
                    {
                        modelId = modelInfo.ModelId,
                        modelName = modelInfo.ModelName,
                        description = modelInfo.Description,
                        companiaAlias = modelInfo.CompaniaAlias
                    },
                    hasSpecificModel = hasModel,
                    isDefaultModel = modelInfo.ModelId == "poliza_vehiculos_bse" && companiaId != 1,
                    timestamp = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error probando selección de modelo para compañía {CompaniaId}", companiaId);
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        #endregion
    }

    public class ChangeModelRequest
    {
        public string ModelId { get; set; } = string.Empty;
    }
}