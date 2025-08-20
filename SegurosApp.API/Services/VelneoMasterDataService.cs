using Microsoft.Extensions.Caching.Memory;
using SegurosApp.API.Converters;
using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.DTOs.Velneo.Response;
using SegurosApp.API.Converters;
using SegurosApp.API.Interfaces;
using System.Text;
using System.Text.Json;
using SegurosApp.API.DTOs;

namespace SegurosApp.API.Services
{
    public class VelneoMasterDataService : IVelneoMasterDataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<VelneoMasterDataService> _logger;

        private string ApiKey => _configuration["VelneoAPI:ApiKey"];
        private string BaseUrl => _configuration["VelneoAPI:BaseUrl"];

        public VelneoMasterDataService(
            HttpClient httpClient,
            IConfiguration configuration,
            IMemoryCache cache,
            ILogger<VelneoMasterDataService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<CombustibleItem>> GetCombustiblesAsync()
        {
            const string cacheKey = "velneo_combustibles";

            if (_cache.TryGetValue(cacheKey, out List<CombustibleItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/combustibles?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCombustibleResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var combustibles = velneoResponse?.combustibles ?? new List<CombustibleItem>();
                    _cache.Set(cacheKey, combustibles, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Combustibles obtenidos: {Count}", combustibles.Count);
                    return combustibles;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener combustibles: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener combustibles");
            }

            return new List<CombustibleItem>();
        }

        public async Task<List<DepartamentoItem>> GetDepartamentosAsync()
        {
            const string cacheKey = "velneo_departamentos";

            if (_cache.TryGetValue(cacheKey, out List<DepartamentoItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/departamentos?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoDepartamentoResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var departamentos = velneoResponse?.departamentos ?? new List<DepartamentoItem>();

                    _cache.Set(cacheKey, departamentos, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Departamentos obtenidos: {Count}", departamentos.Count);
                    return departamentos;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener departamentos: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener departamentos");
            }

            return new List<DepartamentoItem>();
        }

        public async Task<List<CorredorItem>> GetCorredoresAsync()
        {
            const string cacheKey = "velneo_corredores";

            if (_cache.TryGetValue(cacheKey, out List<CorredorItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/corredores?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCorredorResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var corredores = velneoResponse?.corredores ?? new List<CorredorItem>();

                    _cache.Set(cacheKey, corredores, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Corredores obtenidos: {Count}", corredores.Count);
                    return corredores;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener corredores: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener corredores");
            }

            return new List<CorredorItem>();
        }

        public async Task<List<CategoriaItem>> GetCategoriasAsync()
        {
            const string cacheKey = "velneo_categorias";

            if (_cache.TryGetValue(cacheKey, out List<CategoriaItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/categorias?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCategoriaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var categorias = velneoResponse?.categorias ?? new List<CategoriaItem>();

                    _cache.Set(cacheKey, categorias, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Categorías obtenidas: {Count}", categorias.Count);
                    return categorias;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener categorías: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener categorías");
            }

            return new List<CategoriaItem>();
        }

        public async Task<List<DestinoItem>> GetDestinosAsync()
        {
            const string cacheKey = "velneo_destinos";

            if (_cache.TryGetValue(cacheKey, out List<DestinoItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/destinos?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoDestinoResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var destinos = velneoResponse?.destinos ?? new List<DestinoItem>();

                    _cache.Set(cacheKey, destinos, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Destinos obtenidos: {Count}", destinos.Count);
                    return destinos;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener destinos: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener destinos");
            }

            return new List<DestinoItem>();
        }

        public async Task<List<CalidadItem>> GetCalidadesAsync()
        {
            const string cacheKey = "velneo_calidades";

            if (_cache.TryGetValue(cacheKey, out List<CalidadItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/calidades?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCalidadResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var calidades = velneoResponse?.calidades ?? new List<CalidadItem>();

                    _cache.Set(cacheKey, calidades, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Calidades obtenidas: {Count}", calidades.Count);
                    return calidades;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener calidades: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener calidades");
            }

            return new List<CalidadItem>();
        }

        public async Task<List<TarifaItem>> GetTarifasAsync()
        {
            const string cacheKey = "velneo_tarifas";

            if (_cache.TryGetValue(cacheKey, out List<TarifaItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/tarifas?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoTarifaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var tarifas = velneoResponse?.tarifas ?? new List<TarifaItem>();

                    _cache.Set(cacheKey, tarifas, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Tarifas obtenidas: {Count}", tarifas.Count);
                    return tarifas;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener tarifas: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener tarifas");
            }

            return new List<TarifaItem>();
        }

        public async Task<List<CompaniaItem>> GetCompaniasAsync()
        {
            const string cacheKey = "velneo_companias";

            if (_cache.TryGetValue(cacheKey, out List<CompaniaItem>? cached) && cached != null)
                return cached;

            try
            {
                _logger.LogInformation("🏢 Obteniendo compañías desde Velneo...");

                var url = $"{BaseUrl}/companias?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("🏢 Raw response from Velneo companias: {Json}",
                        json.Length > 500 ? json.Substring(0, 500) + "..." : json);

                    var velneoResponse = JsonSerializer.Deserialize<VelneoCompaniaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (velneoResponse?.companias != null)
                    {
                        var companiasActivas = velneoResponse.companias
                            .Where(c => c.IsActive)
                            .OrderBy(c => c.DisplayName)
                            .ToList();

                        _cache.Set(cacheKey, companiasActivas, TimeSpan.FromHours(4));

                        _logger.LogInformation("✅ Compañías obtenidas desde Velneo: {Count}/{Total} activas",
                            companiasActivas.Count, velneoResponse.companias.Count);
                        return companiasActivas;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Respuesta Velneo compañías vacía o malformada");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error en Velneo companias: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conexión obteniendo compañías desde Velneo");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Error parseando JSON compañías de Velneo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado obteniendo compañías desde Velneo");
            }

            return new List<CompaniaItem>();
        }

        public async Task<List<SeccionItem>> GetSeccionesAsync(int? companiaId = null)
        {
            var cacheKey = companiaId.HasValue
                ? $"velneo_secciones_compania_{companiaId}"
                : "velneo_secciones_all";

            if (_cache.TryGetValue(cacheKey, out List<SeccionItem>? cached) && cached != null)
                return cached;

            try
            {
                _logger.LogInformation("📋 Obteniendo secciones desde Velneo (compañía: {CompaniaId})...",
                    companiaId?.ToString() ?? "todas");

                var url = $"{BaseUrl}/secciones?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("📋 Raw response from Velneo secciones: {Json}",
                        json.Length > 500 ? json.Substring(0, 500) + "..." : json);

                    var velneoResponse = JsonSerializer.Deserialize<VelneoSeccionResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (velneoResponse?.secciones != null)
                    {
                        var seccionesActivas = velneoResponse.secciones
                            .Where(s => s.IsActive)
                            .OrderBy(s => s.DisplayName)
                            .ToList();

                        _cache.Set(cacheKey, seccionesActivas, TimeSpan.FromHours(4));

                        _logger.LogInformation("✅ Secciones obtenidas desde Velneo: {Count}/{Total} activas",
                            seccionesActivas.Count, velneoResponse.secciones.Count);
                        return seccionesActivas;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Respuesta Velneo secciones vacía o malformada");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error en Velneo secciones: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conexión obteniendo secciones desde Velneo");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Error parseando JSON secciones de Velneo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado obteniendo secciones desde Velneo");
            }

            return new List<SeccionItem>();
        }

        public async Task<List<ClienteItem>> SearchClientesAsync(string query, int limit = 20)
        {
            var cacheKey = $"velneo_clientes_search_{query.ToLowerInvariant()}_{limit}";

            if (_cache.TryGetValue(cacheKey, out List<ClienteItem>? cached) && cached != null)
                return cached;

            try
            {
                _logger.LogInformation("🔍 Buscando clientes en Velneo con filtro: '{Query}' (limit: {Limit})", query, limit);

                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"{BaseUrl}/clientes?filter%5Bnombre%5D={encodedQuery}&api_key={ApiKey}";

                var response = await _httpClient.GetAsync(url);

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

                        _logger.LogInformation("✅ Clientes encontrados en Velneo: {Count}/{Total} activos para '{Query}'",
                            clientesFiltrados.Count, velneoResponse.clientes.Count, query);

                        return clientesFiltrados;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Respuesta Velneo clientes vacía o malformada para query '{Query}'", query);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error en Velneo clientes con filtro: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Error parseando JSON de clientes Velneo para query '{Query}' - Path: {Path}",
                    query, ex.Path ?? "desconocido");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conexión buscando clientes en Velneo con query '{Query}'", query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado buscando clientes en Velneo con query '{Query}'", query);
            }

            return new List<ClienteItem>();
        }

        public async Task<List<ClienteItem>> AdvancedSearchClientesAsync(ClienteSearchFilters filters)
        {
            var cacheKey = $"velneo_clientes_advanced_{filters.GetCacheKey()}";

            if (_cache.TryGetValue(cacheKey, out List<ClienteItem>? cached) && cached != null)
                return cached;

            try
            {
                _logger.LogInformation("🔍 Búsqueda avanzada clientes en Velneo: {Filters}", filters.ToString());

                var urlBuilder = new StringBuilder($"{BaseUrl}/clientes?");
                var hasFilters = false;
                if (!string.IsNullOrWhiteSpace(filters.Nombre))
                {
                    urlBuilder.Append($"filter%5Bnombre%5D={Uri.EscapeDataString(filters.Nombre)}&");
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(filters.Direcciones))
                {
                    urlBuilder.Append($"filter%5Bdirecciones%5D={Uri.EscapeDataString(filters.Direcciones)}&");
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(filters.Clitel))
                {
                    urlBuilder.Append($"filter%5Bclitel%5D={Uri.EscapeDataString(filters.Clitel)}&");
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(filters.Clicel))
                {
                    urlBuilder.Append($"filter%5Bclicel%5D={Uri.EscapeDataString(filters.Clicel)}&");
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(filters.Mail))
                {
                    urlBuilder.Append($"filter%5Bmail%5D={Uri.EscapeDataString(filters.Mail)}&");
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(filters.Cliruc))
                {
                    urlBuilder.Append($"filter%5Bcliruc%5D={Uri.EscapeDataString(filters.Cliruc)}&");
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(filters.Cliced))
                {
                    urlBuilder.Append($"filter%5Bcliced%5D={Uri.EscapeDataString(filters.Cliced)}&");
                    hasFilters = true;
                }

                urlBuilder.Append($"api_key={ApiKey}");

                if (!hasFilters)
                {
                    _logger.LogWarning("⚠️ Búsqueda avanzada sin filtros - esto puede devolver muchos resultados");
                }

                var url = urlBuilder.ToString();
                _logger.LogDebug("🔍 URL Velneo búsqueda avanzada: {Url}", url);

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("🔍 Raw response Velneo búsqueda avanzada (primeros 300 chars): {Json}",
                        json.Length > 300 ? json.Substring(0, 300) + "..." : json);

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
                        var clientesFiltrados = velneoResponse.clientes.AsEnumerable();
                        if (filters.SoloActivos)
                        {
                            clientesFiltrados = clientesFiltrados.Where(c => c.activo);
                        }

                        var resultados = clientesFiltrados.Take(filters.Limit).ToList();

                        _cache.Set(cacheKey, resultados, TimeSpan.FromMinutes(3));

                        _logger.LogInformation("✅ Búsqueda avanzada clientes completada: {Count}/{Total} resultados con {ActiveFilters} filtros activos",
                            resultados.Count, velneoResponse.clientes.Count, filters.GetActiveFiltersCount());

                        if (resultados.Count > 0)
                        {
                            var ejemplo = resultados.First();
                            _logger.LogDebug("📋 Ejemplo resultado: ID={Id}, Nombre='{Nombre}', RUC='{Ruc}', Cédula='{Cedula}'",
                                ejemplo.id, ejemplo.DisplayName, ejemplo.cliruc, ejemplo.cliced);
                        }

                        return resultados;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Respuesta Velneo búsqueda avanzada vacía o malformada");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error en Velneo búsqueda avanzada: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (UriFormatException ex)
            {
                _logger.LogError(ex, "❌ Error construyendo URL para búsqueda avanzada");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conexión en búsqueda avanzada clientes");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Error parseando JSON búsqueda avanzada clientes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado en búsqueda avanzada clientes");
            }

            return new List<ClienteItem>();
        }

        public async Task<ClienteItem?> GetClienteDetalleAsync(int clienteId)
        {
            var cacheKey = $"velneo_cliente_detalle_{clienteId}";

            if (_cache.TryGetValue(cacheKey, out ClienteItem? cached) && cached != null)
            {
                _logger.LogInformation("💾 Cliente {ClienteId} obtenido desde cache", clienteId);
                return cached;
            }

            try
            {
                _logger.LogInformation("👤 Obteniendo detalle cliente desde Velneo: {ClienteId}", clienteId);

                var url = $"{BaseUrl}/clientes/{clienteId}?api_key={ApiKey}";
                _logger.LogDebug("🔗 URL Velneo: {Url}", url);

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("📄 Raw JSON response length: {Length} characters", json.Length);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        _logger.LogWarning("⚠️ Velneo devolvió respuesta vacía para cliente {ClienteId}", clienteId);
                        return null;
                    }

                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = {
                    new NullableDateTimeConverter(),
                    new DateTimeConverter()
                        }
                    };

                    var velneoResponse = JsonSerializer.Deserialize<VelneoClienteDetalleResponse>(json, jsonOptions);

                    if (velneoResponse?.clientes != null && velneoResponse.clientes.Count > 0)
                    {
                        var cliente = velneoResponse.clientes.First();

                        _logger.LogInformation("✅ Cliente deserializado exitosamente: ID={Id}, Nombre='{Nombre}', RUC='{Ruc}', Activo={Activo}",
                            cliente.id, cliente.clinom, cliente.cliruc, cliente.activo);

                        LogDateTimeFields(cliente, clienteId);

                        if (cliente.id > 0 && !string.IsNullOrEmpty(cliente.clinom))
                        {
                            _cache.Set(cacheKey, cliente, TimeSpan.FromHours(1));

                            _logger.LogInformation("✅ Cliente {ClienteId} cacheado exitosamente - {DisplayName}",
                                clienteId, cliente.DisplayName);

                            return cliente;
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Cliente {ClienteId} encontrado pero sin datos válidos (ID={Id}, Nombre='{Nombre}')",
                                clienteId, cliente.id, cliente.clinom);
                        }
                    }
                    else if (velneoResponse?.count == 0)
                    {
                        _logger.LogInformation("🔍 Cliente {ClienteId} no existe en Velneo (count=0)", clienteId);
                        return null;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Respuesta Velneo malformada para cliente {ClienteId}: count={Count}, clientes={ClientesCount}",
                            clienteId, velneoResponse?.count ?? -1, velneoResponse?.clientes?.Count ?? -1);
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("🔍 Cliente {ClienteId} no existe en Velneo (404)", clienteId);
                    return null;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error en Velneo cliente/{id}: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conexión obteniendo cliente {ClienteId} desde Velneo", clienteId);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Error deserializando JSON para cliente {ClienteId} desde Velneo", clienteId);

                _logger.LogDebug("🔍 JSON problemático para cliente {ClienteId}: {JsonSnippet}",
                    clienteId, ex.Path ?? "path desconocido");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "❌ Timeout obteniendo cliente {ClienteId} desde Velneo", clienteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado obteniendo cliente {ClienteId} desde Velneo", clienteId);
            }

            return null;
        }

        private void LogDateTimeFields(ClienteItem cliente, int clienteId)
        {
            try
            {
                var dateFields = new[]
                {
            (nameof(cliente.clifchnac), cliente.clifchnac),
            (nameof(cliente.clifching), cliente.clifching),
            (nameof(cliente.clifchegr), cliente.clifchegr),
            (nameof(cliente.clivtoced), cliente.clivtoced),
            (nameof(cliente.clivtolib), cliente.clivtolib),
            (nameof(cliente.clifchnac1), cliente.clifchnac1),
            (nameof(cliente.fch_ingreso), cliente.fch_ingreso),
            (nameof(cliente.last_update), cliente.last_update)
        };

                var validDates = dateFields.Where(d => d.Item2.HasValue).Count();
                var totalDates = dateFields.Length;

                _logger.LogDebug("📅 Cliente {ClienteId} fechas procesadas: {ValidDates}/{TotalDates} válidas",
                    clienteId, validDates, totalDates);

                foreach (var (fieldName, value) in dateFields.Where(d => d.Item2.HasValue))
                {
                    _logger.LogDebug("📅 {FieldName}: {Value}", fieldName, value!.Value.ToString("yyyy-MM-dd"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Error logging fechas para cliente {ClienteId}: {Error}", clienteId, ex.Message);
            }
        }

        public async Task<CompleteMasterDataResponse> GetAllMasterDataAsync()
        {
            const string cacheKey = "velneo_all_master_data_v3"; 

            if (_cache.TryGetValue(cacheKey, out CompleteMasterDataResponse? cached) && cached != null)
                return cached;

            _logger.LogInformation("🔄 Obteniendo master data completo desde Velneo (incluye clientes/compañías/secciones reales)...");

            try
            {
                var departamentosTask = GetDepartamentosAsync();
                var combustiblesTask = GetCombustiblesAsync();
                var corredoresTask = GetCorredoresAsync();
                var categoriasTask = GetCategoriasAsync();
                var destinosTask = GetDestinosAsync();
                var calidadesTask = GetCalidadesAsync();
                var tarifasTask = GetTarifasAsync();
                var companiasTask = GetCompaniasAsync();
                var seccionesTask = GetSeccionesAsync();

                await Task.WhenAll(
                    departamentosTask, combustiblesTask, corredoresTask,
                    categoriasTask, destinosTask, calidadesTask, tarifasTask,
                    companiasTask, seccionesTask
                );

                var result = new CompleteMasterDataResponse
                {
                    Departamentos = await departamentosTask,
                    Combustibles = await combustiblesTask,
                    Corredores = await corredoresTask,
                    Categorias = await categoriasTask,
                    Destinos = await destinosTask,
                    Calidades = await calidadesTask,
                    Tarifas = await tarifasTask,
                    Companias = await companiasTask,
                    Secciones = await seccionesTask,

                    EstadosGestion = GetEstadosGestion(),
                    Tramites = GetTramites(),
                    EstadosPoliza = GetEstadosPoliza(),
                    FormasPago = GetFormasPago()
                };

                _cache.Set(cacheKey, result, TimeSpan.FromHours(1));

                _logger.LogInformation("✅ Master data completo obtenido desde Velneo: {CompaniasCount} compañías, {SeccionesCount} secciones",
                    result.Companias.Count, result.Secciones.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo master data completo desde Velneo");

                return new CompleteMasterDataResponse
                {
                    Departamentos = new(),
                    Combustibles = new(),
                    Corredores = new(),
                    Categorias = new(),
                    Destinos = new(),
                    Calidades = new(),
                    Tarifas = new(),
                    Companias = new(),
                    Secciones = new(),
                    EstadosGestion = GetEstadosGestion(),
                    Tramites = GetTramites(),
                    EstadosPoliza = GetEstadosPoliza(),
                    FormasPago = GetFormasPago()
                };
            }
        }
   
        private List<StaticOption> GetEstadosGestion()
        {
            return new List<StaticOption>
            {
                new() { Value = "1", Label = "En emisión" },
                new() { Value = "2", Label = "Pendiente" },
                new() { Value = "3", Label = "En proceso" },
                new() { Value = "4", Label = "Modificaciones" },
                new() { Value = "5", Label = "Terminado" }
            };
        }

        private List<StaticOption> GetTramites()
        {
            return new List<StaticOption>
            {
                new() { Value = "1", Label = "Nuevo" },
                new() { Value = "2", Label = "Renovación" },
                new() { Value = "3", Label = "Cambio" },
                new() { Value = "4", Label = "Endoso" },
                new() { Value = "5", Label = "No Renueva" },
                new() { Value = "6", Label = "Cancelación" }
            };
        }

        private List<StaticOption> GetEstadosPoliza()
        {
            return new List<StaticOption>
            {
                new() { Value = "1", Label = "Vigente" },
                new() { Value = "2", Label = "Anulada" },
                new() { Value = "3", Label = "Vencida" },
                new() { Value = "4", Label = "Cancelada" }
            };
        }

        private List<StaticOption> GetFormasPago()
        {
            return new List<StaticOption>
            {
                new() { Value = "1", Label = "Contado" },
                new() { Value = "T", Label = "Tarjeta de Crédito" },
                new() { Value = "E", Label = "Efectivo" },
                new() { Value = "B", Label = "Transferencia Bancaria" },
                new() { Value = "C", Label = "Crédito" }
            };
        }

        public async Task<FieldMappingSuggestion> SuggestMappingAsync(string fieldName, string scannedValue)
        {
            try
            {
                _logger.LogInformation("🧠 Sugiriendo mapeo para {FieldName}: {ScannedValue}",
                    fieldName, scannedValue);

                var masterData = await GetAllMasterDataAsync();

                return fieldName.ToLower() switch
                {
                    "departamento" or "asegurado.departamento" =>
                        FindBestMatch(scannedValue, masterData.Departamentos.Cast<object>().ToList(),
                            item => ((DepartamentoItem)item).dptnom,
                            item => ((DepartamentoItem)item).id.ToString()),

                    "combustible" or "vehiculo.combustible" =>
                        FindBestMatch(scannedValue, masterData.Combustibles.Cast<object>().ToList(),
                            item => ((CombustibleItem)item).name,
                            item => ((CombustibleItem)item).id),

                    "destino" or "vehiculo.destino" =>
                        FindBestMatch(scannedValue, masterData.Destinos.Cast<object>().ToList(),
                            item => ((DestinoItem)item).desnom,
                            item => ((DestinoItem)item).id.ToString()),

                    "categoria" or "vehiculo.categoria" =>
                        FindBestMatch(scannedValue, masterData.Categorias.Cast<object>().ToList(),
                            item => ((CategoriaItem)item).catdsc,
                            item => ((CategoriaItem)item).id.ToString()),

                    "calidad" or "vehiculo.calidad" =>
                        FindBestMatch(scannedValue, masterData.Calidades.Cast<object>().ToList(),
                            item => ((CalidadItem)item).caldsc,
                            item => ((CalidadItem)item).id.ToString()),

                    "corredor" or "corredor.nombre" =>
                        FindBestMatch(scannedValue, masterData.Corredores.Cast<object>().ToList(),
                            item => ((CorredorItem)item).corrnom,
                            item => ((CorredorItem)item).id.ToString()),

                    "forma_pago" or "pago.medio" =>
                        MapFormaPago(scannedValue),

                    _ => new FieldMappingSuggestion
                    {
                        FieldName = fieldName,
                        ScannedValue = scannedValue,
                        SuggestedValue = null,
                        Confidence = 0.0,
                        Source = "NoMatch"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sugiriendo mapeo para {FieldName}", fieldName);
                return new FieldMappingSuggestion
                {
                    FieldName = fieldName,
                    ScannedValue = scannedValue,
                    Confidence = 0.0,
                    Source = "Error"
                };
            }
        }

        private FieldMappingSuggestion FindBestMatch(string scannedValue, List<object> items,
            Func<object, string> getName, Func<object, string> getId)
        {
            var bestMatch = items
                .Select(item => new
                {
                    Item = item,
                    Name = getName(item),
                    Id = getId(item),
                    Similarity = CalculateSimilarity(scannedValue, getName(item))
                })
                .Where(x => x.Similarity >= 0.6) 
                .OrderByDescending(x => x.Similarity)
                .FirstOrDefault();

            if (bestMatch != null)
            {
                return new FieldMappingSuggestion
                {
                    ScannedValue = scannedValue,
                    SuggestedValue = bestMatch.Id,
                    SuggestedLabel = bestMatch.Name,
                    Confidence = bestMatch.Similarity,
                    Source = "TextSimilarity"
                };
            }

            return new FieldMappingSuggestion
            {
                ScannedValue = scannedValue,
                SuggestedValue = null,
                Confidence = 0.0,
                Source = "NoMatch"
            };
        }

        private FieldMappingSuggestion MapFormaPago(string scannedValue)
        {
            var formaPago = scannedValue.ToUpper();

            if (formaPago.Contains("TARJETA") || formaPago.Contains("CREDITO"))
                return new FieldMappingSuggestion
                {
                    SuggestedValue = "T",
                    SuggestedLabel = "Tarjeta de Crédito",
                    Confidence = 0.90,
                    Source = "RuleMatch"
                };

            if (formaPago.Contains("CONTADO") || formaPago.Contains("EFECTIVO"))
                return new FieldMappingSuggestion
                {
                    SuggestedValue = "1",
                    SuggestedLabel = "Contado",
                    Confidence = 0.90,
                    Source = "RuleMatch"
                };

            if (formaPago.Contains("TRANSFERENCIA"))
                return new FieldMappingSuggestion
                {
                    SuggestedValue = "B",
                    SuggestedLabel = "Transferencia Bancaria",
                    Confidence = 0.85,
                    Source = "RuleMatch"
                };

            return new FieldMappingSuggestion
            {
                SuggestedValue = "1",
                SuggestedLabel = "Contado",
                Confidence = 0.50,
                Source = "DefaultValue"
            };
        }

        private double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0.0;

            text1 = text1.ToLower().Trim();
            text2 = text2.ToLower().Trim();

            if (text1 == text2) return 1.0;
            if (text2.Contains(text1) || text1.Contains(text2)) return 0.85;

            var distance = ComputeLevenshteinDistance(text1, text2);
            var maxLength = Math.Max(text1.Length, text2.Length);

            return maxLength == 0 ? 1.0 : 1.0 - (double)distance / maxLength;
        }

        private int ComputeLevenshteinDistance(string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (var i = 0; i <= n; d[i, 0] = i++) { }
            for (var j = 0; j <= m; d[0, j] = j++) { }

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        public Task SaveMappingAsync(int userId, string fieldName, string scannedValue, string velneoValue)
        {
            _logger.LogInformation("💾 Guardando mapeo: {FieldName} {ScannedValue} -> {VelneoValue}",
                fieldName, scannedValue, velneoValue);
            return Task.CompletedTask;
        }

        public async Task<CreatePolizaResponse> CreatePolizaAsync(VelneoPolizaRequest request)
        {
            try
            {
                _logger.LogInformation("🚀 Creando póliza en Velneo: Póliza={PolicyNumber}, Cliente={ClienteId}, Compañía={CompaniaId}, Sección={SeccionId}",
                    request.conpol, request.clinro, request.comcod, request.seccod);

                // ✅ VALIDAR REQUEST ANTES DE ENVIAR
                ValidateVelneoPolizaRequest(request);

                // ✅ PREPARAR DATOS PARA VELNEO API
                var velneoPayload = MapToVelneoApiFormat(request);

                // ✅ LLAMADA A VELNEO API
                var url = $"{BaseUrl}/polizas?api_key={ApiKey}";

                var jsonPayload = JsonSerializer.Serialize(velneoPayload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                _logger.LogDebug("📤 Enviando a Velneo: {Url}", url);

                // ✅ DEBUG COMPLETO DEL PAYLOAD
                _logger.LogInformation("📦 Payload completo enviado a Velneo:");
                _logger.LogInformation("{Payload}", jsonPayload);

                // ✅ DEBUG DE CAMPOS CRÍTICOS
                _logger.LogInformation("🔧 Verificando campos críticos del payload:");
                try
                {
                    var payloadDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonPayload);
                    if (payloadDict != null)
                    {
                        _logger.LogInformation("  - clienteId: {ClienteId}", payloadDict.GetValueOrDefault("clienteId", "NOT_FOUND"));
                        _logger.LogInformation("  - numeroPoliza: '{NumeroPoliza}'", payloadDict.GetValueOrDefault("numeroPoliza", "NOT_FOUND"));
                        _logger.LogInformation("  - fechaDesde: '{FechaDesde}'", payloadDict.GetValueOrDefault("fechaDesde", "NOT_FOUND"));
                        _logger.LogInformation("  - fechaHasta: '{FechaHasta}'", payloadDict.GetValueOrDefault("fechaHasta", "NOT_FOUND"));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ Error parseando payload para debug: {Error}", ex.Message);
                }

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // ✅ AGREGAR HEADERS ADICIONALES
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var response = await _httpClient.PostAsync(url, content);

                // ✅ DEBUG COMPLETO DE LA RESPUESTA
                _logger.LogInformation("🔧 DEBUG Velneo Response:");
                _logger.LogInformation("  - Status: {StatusCode} ({StatusName})", (int)response.StatusCode, response.StatusCode);
                _logger.LogInformation("  - Content-Type: {ContentType}", response.Content.Headers.ContentType?.ToString() ?? "null");
                _logger.LogInformation("  - Content-Length: {ContentLength}", response.Content.Headers.ContentLength?.ToString() ?? "null");

                // ✅ LEER Y DEBUGGEAR EL CONTENIDO
                var responseJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("  - Response Length: {Length}", responseJson?.Length ?? 0);
                _logger.LogInformation("  - Response Content: '{Response}'", responseJson ?? "NULL");

                // ✅ DEBUG DE HEADERS DE RESPUESTA
                _logger.LogInformation("🔧 Response Headers:");
                foreach (var header in response.Headers)
                {
                    _logger.LogInformation("  - {HeaderName}: {HeaderValue}", header.Key, string.Join(", ", header.Value));
                }

                if (response.IsSuccessStatusCode)
                {
                    if (string.IsNullOrWhiteSpace(responseJson))
                    {
                        _logger.LogError("❌ Velneo respondió con contenido vacío pero status 200");
                        _logger.LogError("❌ Esto indica que el API de Velneo no está funcionando correctamente");

                        return new CreatePolizaResponse
                        {
                            Success = false,
                            Message = "Velneo respondió con contenido vacío (status 200)",
                            VelneoPolizaId = null,
                            PolizaId = null,
                            PolizaNumber = "",
                            Validation = new ValidationResult
                            {
                                IsValid = false,
                                Errors = new List<string> {
                            "Respuesta vacía de Velneo",
                            "El API de Velneo puede estar mal configurado o con problemas"
                        }
                            }
                        };
                    }

                    // ✅ INTENTAR PARSEAR LA RESPUESTA
                    var velneoResponse = ParseVelneoResponse(responseJson);

                    _logger.LogInformation("✅ Póliza creada exitosamente en Velneo: ID={VelneoId}, Número={PolicyNumber}",
                        velneoResponse.VelneoPolizaId, velneoResponse.PolizaNumber);

                    return new CreatePolizaResponse
                    {
                        Success = true,
                        Message = "Póliza creada exitosamente en Velneo",
                        VelneoPolizaId = velneoResponse.VelneoPolizaId,
                        PolizaId = velneoResponse.VelneoPolizaId,
                        PolizaNumber = velneoResponse.PolizaNumber,
                        CreatedAt = DateTime.UtcNow,
                        VelneoUrl = GenerateVelneoUrl(velneoResponse.VelneoPolizaId),
                        Validation = new ValidationResult
                        {
                            IsValid = true,
                            FieldsValidated = GetValidatedFields(request)
                        }
                    };
                }
                else
                {
                    _logger.LogError("❌ Error HTTP de Velneo: {StatusCode}", response.StatusCode);
                    _logger.LogError("❌ Response Content: {Content}", responseJson);

                    return new CreatePolizaResponse
                    {
                        Success = false,
                        Message = $"Error HTTP en Velneo: {response.StatusCode}",
                        VelneoPolizaId = null,
                        PolizaId = null,
                        PolizaNumber = "",
                        Validation = new ValidationResult
                        {
                            IsValid = false,
                            Errors = new List<string> { $"HTTP {response.StatusCode}: {responseJson}" }
                        }
                    };
                }
            }
            catch (ValidationException ex)
            {
                _logger.LogError(ex, "❌ Error de validación creando póliza en Velneo");
                return new CreatePolizaResponse
                {
                    Success = false,
                    Message = $"Error de validación: {ex.Message}",
                    VelneoPolizaId = null,
                    PolizaId = null,
                    PolizaNumber = "",
                    Validation = new ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { ex.Message }
                    }
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conectividad con Velneo");
                return new CreatePolizaResponse
                {
                    Success = false,
                    Message = "Error de conectividad con Velneo",
                    VelneoPolizaId = null,
                    PolizaId = null,
                    PolizaNumber = "",
                    Validation = new ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "No se pudo conectar con el servicio Velneo" }
                    }
                };
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "❌ Timeout conectando con Velneo");
                return new CreatePolizaResponse
                {
                    Success = false,
                    Message = "Timeout conectando con Velneo - Servicio no responde",
                    VelneoPolizaId = null,
                    PolizaId = null,
                    PolizaNumber = "",
                    Validation = new ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "Timeout: El servicio Velneo no responde en el tiempo esperado" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado creando póliza en Velneo");
                return new CreatePolizaResponse
                {
                    Success = false,
                    Message = $"Error inesperado: {ex.Message}",
                    VelneoPolizaId = null,
                    PolizaId = null,
                    PolizaNumber = "",
                    Validation = new ValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { ex.Message }
                    }
                };
            }
        }

        private void ValidateVelneoPolizaRequest(VelneoPolizaRequest request)
        {
            var errors = new List<string>();

            if (request.clinro <= 0)
                errors.Add("Cliente ID es requerido y debe ser mayor a 0");

            if (request.comcod <= 0)
                errors.Add("Compañía ID es requerido y debe ser mayor a 0");

            if (request.seccod <= 0)
                errors.Add("Sección ID es requerido y debe ser mayor a 0");

            if (string.IsNullOrWhiteSpace(request.conpol))
                errors.Add("Número de póliza es requerido");
            else if (request.conpol.Length < 6)
                errors.Add("Número de póliza debe tener al menos 6 caracteres");

            if (string.IsNullOrWhiteSpace(request.confchdes))
                errors.Add("Fecha de inicio es requerida");

            if (string.IsNullOrWhiteSpace(request.confchhas))
                errors.Add("Fecha de fin es requerida");

            if (!string.IsNullOrWhiteSpace(request.confchdes) && !DateTime.TryParse(request.confchdes, out _))
                errors.Add("Fecha de inicio tiene formato inválido");

            if (!string.IsNullOrWhiteSpace(request.confchhas) && !DateTime.TryParse(request.confchhas, out _))
                errors.Add("Fecha de fin tiene formato inválido");

            if (DateTime.TryParse(request.confchdes, out var startDate) &&
                DateTime.TryParse(request.confchhas, out var endDate))
            {
                if (endDate <= startDate)
                    errors.Add("Fecha de fin debe ser posterior a fecha de inicio");
            }

            if (request.conpremio < 0)
                errors.Add("Premio no puede ser negativo");

            if (request.contot < 0)
                errors.Add("Total no puede ser negativo");

            if (errors.Any())
            {
                var errorMessage = string.Join("; ", errors);
                throw new ValidationException($"Errores de validación: {errorMessage}");
            }
        }
        private object MapToVelneoApiFormat(VelneoPolizaRequest request)
        {
            return new
            {
                // ✅ IDS PRINCIPALES - NOMBRES CORRECTOS DEL SCHEMA VELNEO
                clinro = request.clinro,
                comcod = request.comcod,
                seccod = request.seccod,

                // ✅ DATOS DE PÓLIZA - NOMBRES CORRECTOS
                conpol = request.conpol,
                conend = request.conend,
                confchdes = request.confchdes,
                confchhas = request.confchhas,
                conpremio = request.conpremio,
                contot = request.contot,

                // ✅ DATOS VEHÍCULO - NOMBRES CORRECTOS
                conmaraut = request.conmaraut,
                conmodaut = request.conmodaut,
                conanioaut = request.conanioaut,
                conmotor = request.conmotor,
                conchasis = request.conchasis,

                // ✅ MASTER DATA IDS - NOMBRES CORRECTOS
                dptnom = request.dptnom,
                combustibles = request.combustibles,
                desdsc = request.desdsc,
                catdsc = request.catdsc,
                caldsc = request.caldsc,
                tarcod = request.tarcod,

                // ✅ FORMA DE PAGO - NOMBRES CORRECTOS
                consta = request.consta,
                concuo = request.concuo,

                // ✅ ESTADOS - NOMBRES CORRECTOS
                congesti = request.congesti,
                contra = request.contra,
                convig = request.convig,
                moncod = request.moncod,

                // ✅ METADATOS - NOMBRES CORRECTOS
                ingresado = request.ingresado.ToString("yyyy-MM-dd HH:mm:ss"),
                last_update = request.last_update.ToString("yyyy-MM-dd HH:mm:ss"),
                app_id = request.app_id,
                observaciones = request.observaciones
            };
        }

        private VelneoCreateResponse ParseVelneoResponse(string responseJson)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(responseJson);
                var root = jsonDoc.RootElement;

                return new VelneoCreateResponse
                {
                    VelneoPolizaId = root.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : null,
                    PolizaNumber = root.TryGetProperty("numero_poliza", out var numProp) ? numProp.GetString() : "",
                    Success = root.TryGetProperty("success", out var successProp) ? successProp.GetBoolean() : true
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Error parseando respuesta Velneo: {Error}", ex.Message);
                return new VelneoCreateResponse
                {
                    Success = true,
                    PolizaNumber = "UNKNOWN"
                };
            }
        }

        private string? GenerateVelneoUrl(int? polizaId)
        {
            if (!polizaId.HasValue) return null;
            return $"{BaseUrl}/polizas/{polizaId}";
        }
        private List<string> GetValidatedFields(VelneoPolizaRequest request)
        {
            var fields = new List<string>();

            if (request.clinro > 0) fields.Add("cliente_id");
            if (request.comcod > 0) fields.Add("compania_id");
            if (request.seccod > 0) fields.Add("seccion_id");
            if (!string.IsNullOrEmpty(request.conpol)) fields.Add("numero_poliza");
            if (!string.IsNullOrEmpty(request.confchdes)) fields.Add("fecha_desde");
            if (!string.IsNullOrEmpty(request.confchhas)) fields.Add("fecha_hasta");
            if (request.conpremio > 0) fields.Add("premio");
            if (!string.IsNullOrEmpty(request.conmaraut)) fields.Add("marca_vehiculo");

            return fields;
        }
        private class VelneoCreateResponse
        {
            public int? VelneoPolizaId { get; set; }
            public string PolizaNumber { get; set; } = "";
            public bool Success { get; set; }
        }
    }
}
