using Microsoft.Extensions.Caching.Memory;
using SegurosApp.API.Converters;
using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.DTOs.Velneo.Response;
using SegurosApp.API.Interfaces;
using System.Text;
using System.Text.Json;

namespace SegurosApp.API.Services
{
    /// <summary>
    /// Servicio Multi-Tenant para Velneo que reemplaza VelneoMasterDataService
    /// Cada usuario usa su propia API Key configurada en TenantConfigurations
    /// </summary>
    public class MultiTenantVelneoService : IVelneoMasterDataService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantService _tenantService;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MultiTenantVelneoService> _logger;

        // URL default desde configuración (fallback)
        private string DefaultBaseUrl => _configuration["VelneoAPI:BaseUrl"];

        public MultiTenantVelneoService(
            IHttpClientFactory httpClientFactory,
            ITenantService tenantService,
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<MultiTenantVelneoService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _tenantService = tenantService;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        // ===============================================
        // Métodos auxiliares para Multi-Tenant
        // ===============================================

        private async Task<(string baseUrl, string apiKey)> GetTenantConfigAsync()
        {
            var userId = _tenantService.GetCurrentTenantUserId();
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Usuario no autenticado o UserId no encontrado");
            }

            var apiKey = await _tenantService.GetTenantApiKeyAsync(userId.Value);
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new UnauthorizedAccessException($"API Key de Velneo no configurada para el usuario {userId}");
            }

            var baseUrl = await _tenantService.GetTenantBaseUrlAsync(userId.Value) ?? DefaultBaseUrl;
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new InvalidOperationException("URL base de Velneo no configurada");
            }

            _logger.LogDebug("🏢 Usando configuración de tenant - UserId: {UserId}, BaseUrl: {BaseUrl}", userId, baseUrl);

            return (baseUrl, apiKey);
        }

        private async Task<HttpClient> CreateTenantHttpClientAsync()
        {
            var (baseUrl, apiKey) = await GetTenantConfigAsync();

            var client = _httpClientFactory.CreateClient("MultiTenantVelneo");
            client.BaseAddress = new Uri(baseUrl);

            // Limpiar headers existentes y agregar los del tenant específico
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("ApiKey", apiKey);
            client.DefaultRequestHeaders.Add("User-Agent", "SegurosApp-MultiTenant/1.0");

            // Timeout desde configuración
            var timeoutSeconds = _configuration.GetValue<int>("VelneoAPI:TimeoutSeconds", 30);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            return client;
        }

        private string GetTenantCacheKey(string baseKey)
        {
            var userId = _tenantService.GetCurrentTenantUserId();
            return $"{baseKey}_tenant_{userId}";
        }

        // ===============================================
        // Implementación de IVelneoMasterDataService
        // ===============================================

        public async Task<List<DepartamentoItem>> GetDepartamentosAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_departamentos");

            if (_cache.TryGetValue(cacheKey, out List<DepartamentoItem>? cached) && cached != null)
            {
                _logger.LogDebug("💾 Departamentos desde cache para tenant");
                return cached;
            }

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                // 🔧 URL CORREGIDA: usar client.GetAsync directamente con ruta relativa
                var response = await client.GetAsync($"departamentos?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoDepartamentoResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var departamentos = velneoResponse?.departamentos ?? new List<DepartamentoItem>();
                    _cache.Set(cacheKey, departamentos, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Departamentos obtenidos para tenant: {Count}", departamentos.Count);
                    return departamentos;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo departamentos para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción obteniendo departamentos para tenant");
            }

            return new List<DepartamentoItem>();
        }

        public async Task<List<CombustibleItem>> GetCombustiblesAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_combustibles");

            if (_cache.TryGetValue(cacheKey, out List<CombustibleItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"combustibles?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCombustibleResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var combustibles = velneoResponse?.combustibles ?? new List<CombustibleItem>();
                    _cache.Set(cacheKey, combustibles, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Combustibles obtenidos para tenant: {Count}", combustibles.Count);
                    return combustibles;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo combustibles para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo combustibles para tenant");
            }

            return new List<CombustibleItem>();
        }

        public async Task<List<CorredorItem>> GetCorredoresAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_corredores");

            if (_cache.TryGetValue(cacheKey, out List<CorredorItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"corredores?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCorredorResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var corredores = velneoResponse?.corredores ?? new List<CorredorItem>();
                    _cache.Set(cacheKey, corredores, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Corredores obtenidos para tenant: {Count}", corredores.Count);
                    return corredores;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo corredores para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo corredores para tenant");
            }

            return new List<CorredorItem>();
        }

        public async Task<List<CategoriaItem>> GetCategoriasAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_categorias");

            if (_cache.TryGetValue(cacheKey, out List<CategoriaItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"categorias?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCategoriaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var categorias = velneoResponse?.categorias ?? new List<CategoriaItem>();
                    _cache.Set(cacheKey, categorias, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Categorías obtenidas para tenant: {Count}", categorias.Count);
                    return categorias;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo categorías para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo categorías para tenant");
            }

            return new List<CategoriaItem>();
        }

        public async Task<List<DestinoItem>> GetDestinosAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_destinos");

            if (_cache.TryGetValue(cacheKey, out List<DestinoItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"destinos?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoDestinoResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var destinos = velneoResponse?.destinos ?? new List<DestinoItem>();
                    _cache.Set(cacheKey, destinos, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Destinos obtenidos para tenant: {Count}", destinos.Count);
                    return destinos;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo destinos para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo destinos para tenant");
            }

            return new List<DestinoItem>();
        }

        public async Task<List<CalidadItem>> GetCalidadesAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_calidades");

            if (_cache.TryGetValue(cacheKey, out List<CalidadItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"calidades?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCalidadResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var calidades = velneoResponse?.calidades ?? new List<CalidadItem>();
                    _cache.Set(cacheKey, calidades, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Calidades obtenidas para tenant: {Count}", calidades.Count);
                    return calidades;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo calidades para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo calidades para tenant");
            }

            return new List<CalidadItem>();
        }

        public async Task<List<TarifaItem>> GetTarifasAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_tarifas");

            if (_cache.TryGetValue(cacheKey, out List<TarifaItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"tarifas?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoTarifaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var tarifas = velneoResponse?.tarifas ?? new List<TarifaItem>();
                    _cache.Set(cacheKey, tarifas, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Tarifas obtenidas para tenant: {Count}", tarifas.Count);
                    return tarifas;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo tarifas para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo tarifas para tenant");
            }

            return new List<TarifaItem>();
        }

        public async Task<List<MonedaItem>> GetMonedasAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_monedas");

            if (_cache.TryGetValue(cacheKey, out List<MonedaItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"monedas?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoMonedaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var monedas = velneoResponse?.monedas ?? new List<MonedaItem>();
                    _cache.Set(cacheKey, monedas, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Monedas obtenidas para tenant: {Count}", monedas.Count);
                    return monedas;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo monedas para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo monedas para tenant");
            }

            return new List<MonedaItem>();
        }

        public async Task<CompleteMasterDataResponse> GetAllMasterDataAsync()
        {
            try
            {
                _logger.LogInformation("🔄 Obteniendo master data completo para tenant");

                // Ejecutar todas las consultas en paralelo con tipos específicos
                var departamentosTask = GetDepartamentosAsync();
                var combustiblesTask = GetCombustiblesAsync();
                var corredoresTask = GetCorredoresAsync();
                var categoriasTask = GetCategoriasAsync();
                var destinosTask = GetDestinosAsync();
                var calidadesTask = GetCalidadesAsync();
                var tarifasTask = GetTarifasAsync();
                var monedasTask = GetMonedasAsync();
                var companiasTask = GetCompaniasAsync();
                var seccionesTask = GetSeccionesAsync();

                // Esperar a que todas terminen
                await Task.WhenAll(
                    departamentosTask,
                    combustiblesTask,
                    corredoresTask,
                    categoriasTask,
                    destinosTask,
                    calidadesTask,
                    tarifasTask,
                    monedasTask,
                    companiasTask,
                    seccionesTask
                );

                // Obtener los resultados
                var response = new CompleteMasterDataResponse
                {
                    Departamentos = await departamentosTask,
                    Combustibles = await combustiblesTask,
                    Corredores = await corredoresTask,
                    Categorias = await categoriasTask,
                    Destinos = await destinosTask,
                    Calidades = await calidadesTask,
                    Tarifas = await tarifasTask,
                    Monedas = await monedasTask,
                    Companias = await companiasTask,
                    Secciones = await seccionesTask
                };

                _logger.LogInformation("✅ Master data completo obtenido para tenant: {Departamentos} dept, {Combustibles} comb, {Corredores} corr",
                    response.Departamentos.Count, response.Combustibles.Count, response.Corredores.Count);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo master data completo para tenant");
                throw;
            }
        }

        public async Task<List<ClienteItem>> SearchClientesAsync(string query, int limit = 20)
        {
            var cacheKey = GetTenantCacheKey($"velneo_clientes_search_{query}_{limit}");

            if (_cache.TryGetValue(cacheKey, out List<ClienteItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"clientes?filter%5Bnombre%5D={encodedQuery}&api_key={apiKey}";

                _logger.LogInformation("🔍 Buscando clientes para tenant con query: '{Query}' (limit: {Limit})", query, limit);

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = {
                            new NullableDateTimeConverter(),
                            new DateTimeConverter()
                        }
                    };

                    var velneoResponse = JsonSerializer.Deserialize<VelneoClienteDetalleResponse>(json, jsonOptions);

                    if (velneoResponse?.clientes != null)
                    {
                        var clientesFiltrados = velneoResponse.clientes
                            .Where(c => c.activo)
                            .Take(limit)
                            .ToList();

                        _cache.Set(cacheKey, clientesFiltrados, TimeSpan.FromMinutes(5));

                        _logger.LogInformation("✅ Clientes encontrados para tenant: {Count}/{Total} activos para '{Query}'",
                            clientesFiltrados.Count, velneoResponse.clientes.Count, query);

                        return clientesFiltrados;
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error buscando clientes para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción buscando clientes para tenant con query '{Query}'", query);
            }

            return new List<ClienteItem>();
        }

        public async Task<ClienteItem?> GetClienteDetalleAsync(int clienteId)
        {
            var cacheKey = GetTenantCacheKey($"velneo_cliente_detalle_{clienteId}");

            if (_cache.TryGetValue(cacheKey, out ClienteItem? cached) && cached != null)
            {
                _logger.LogInformation("💾 Cliente {ClienteId} obtenido desde cache para tenant", clienteId);
                return cached;
            }

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                _logger.LogInformation("👤 Obteniendo detalle cliente para tenant: {ClienteId}", clienteId);

                var response = await client.GetAsync($"clientes/{clienteId}?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = {
                                new NullableDateTimeConverter(),
                                new DateTimeConverter()
                            }
                        };

                        var cliente = JsonSerializer.Deserialize<ClienteItem>(json, jsonOptions);

                        if (cliente != null)
                        {
                            _cache.Set(cacheKey, cliente, TimeSpan.FromMinutes(30));
                            _logger.LogInformation("✅ Cliente {ClienteId} obtenido para tenant: {Nombre}",
                                clienteId, cliente.clinom);
                            return cliente;
                        }
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo cliente {ClienteId} para tenant: {StatusCode} - {Error}",
                        clienteId, response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción obteniendo cliente {ClienteId} para tenant", clienteId);
            }

            return null;
        }

        public async Task<List<CompaniaItem>> GetCompaniasAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_companias");

            if (_cache.TryGetValue(cacheKey, out List<CompaniaItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"companias?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCompaniaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var companias = velneoResponse?.companias ?? new List<CompaniaItem>();
                    _cache.Set(cacheKey, companias, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Compañías obtenidas para tenant: {Count}", companias.Count);
                    return companias;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo compañías para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo compañías para tenant");
            }

            return new List<CompaniaItem>();
        }

        public async Task<List<SeccionItem>> GetSeccionesAsync(int? companiaId = null)
        {
            var cacheKey = GetTenantCacheKey($"velneo_secciones_{companiaId}");

            if (_cache.TryGetValue(cacheKey, out List<SeccionItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var url = companiaId.HasValue
                    ? $"secciones?compania_id={companiaId}&api_key={apiKey}"
                    : $"secciones?api_key={apiKey}";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoSeccionResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var secciones = velneoResponse?.secciones ?? new List<SeccionItem>();
                    _cache.Set(cacheKey, secciones, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Secciones obtenidas para tenant (CompañíaId: {CompaniaId}): {Count}",
                        companiaId, secciones.Count);
                    return secciones;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo secciones para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo secciones para tenant");
            }

            return new List<SeccionItem>();
        }

        public async Task<List<ClienteItem>> AdvancedSearchClientesAsync(ClienteSearchFilters filters)
        {
            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                // Construir URL con filtros
                var queryParams = new List<string> { $"api_key={apiKey}" };

                if (!string.IsNullOrEmpty(filters.Nombre))
                    queryParams.Add($"filter[nombre]={Uri.EscapeDataString(filters.Nombre)}");
                if (!string.IsNullOrEmpty(filters.Cliced))
                    queryParams.Add($"filter[documento]={Uri.EscapeDataString(filters.Cliced)}");

                var url = $"clientes?{string.Join("&", queryParams)}";

                _logger.LogInformation("🔍 Búsqueda avanzada de clientes para tenant con filtros: {Filters}",
                    System.Text.Json.JsonSerializer.Serialize(filters));

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoClienteDetalleResponse>(json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = {
                                new NullableDateTimeConverter(),
                                new DateTimeConverter()
                            }
                        });

                    var clientes = velneoResponse?.clientes ?? new List<ClienteItem>();
                    _logger.LogInformation("✅ Búsqueda avanzada completada para tenant: {Count} clientes encontrados",
                        clientes.Count);

                    return clientes;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error en búsqueda avanzada para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción en búsqueda avanzada de clientes para tenant");
            }

            return new List<ClienteItem>();
        }

        public async Task<FieldMappingSuggestion> SuggestMappingAsync(string fieldName, string scannedValue)
        {
            // TODO: Implementar lógica de sugerencia basada en datos del tenant
            // Por ahora retorna sugerencia básica
            return new FieldMappingSuggestion
            {
                FieldName = fieldName,
                ScannedValue = scannedValue,
                SuggestedValue = scannedValue,
                Confidence = (double)0.5m,
            };
        }

        public async Task SaveMappingAsync(int userId, string fieldName, string scannedValue, string velneoValue)
        {
            // TODO: Implementar guardado de mappings personalizados por tenant
            _logger.LogInformation("💾 Guardando mapping para tenant UserId {UserId}: {FieldName} = {VelneoValue}",
                userId, fieldName, velneoValue);
        }

        public async Task<CreatePolizaResponse> CreatePolizaAsync(VelneoPolizaRequest request)
        {
            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"polizas?api_key={apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var createResponse = JsonSerializer.Deserialize<CreatePolizaResponse>(responseJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _logger.LogInformation("✅ Póliza creada exitosamente para tenant: {PolizaId}",
                        createResponse?.PolizaId);

                    return createResponse ?? new CreatePolizaResponse { Success = false, Message = "Respuesta inválida" };
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error creando póliza para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);

                    return new CreatePolizaResponse
                    {
                        Success = false,
                        Message = $"Error HTTP {response.StatusCode}: {error}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción creando póliza para tenant");
                return new CreatePolizaResponse
                {
                    Success = false,
                    Message = $"Excepción: {ex.Message}"
                };
            }
        }
    }
}