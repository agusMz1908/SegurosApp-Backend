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

        public async Task<List<MonedaItem>> GetMonedasAsync()
        {
            const string cacheKey = "velneo_monedas";

            if (_cache.TryGetValue(cacheKey, out List<MonedaItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/monedas?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoMonedaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var monedas = velneoResponse?.monedas ?? new List<MonedaItem>();

                    _cache.Set(cacheKey, monedas, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Monedas obtenidas: {Count}", monedas.Count);
                    return monedas;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener monedas: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener monedas");
            }

            return new List<MonedaItem>();
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
            const string cacheKey = "velneo_complete_master_data";

            if (_cache.TryGetValue(cacheKey, out CompleteMasterDataResponse? cached) && cached != null)
                return cached;

            _logger.LogInformation("🔄 Obteniendo master data completo desde Velneo...");

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
                var monedasTask = GetMonedasAsync();  

                await Task.WhenAll(
                    departamentosTask, combustiblesTask, corredoresTask,
                    categoriasTask, destinosTask, calidadesTask, tarifasTask,
                    companiasTask, seccionesTask, monedasTask  
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
                    Monedas = await monedasTask,  

                    EstadosGestion = GetEstadosGestion(),
                    Tramites = GetTramites(),
                    EstadosPoliza = GetEstadosPoliza(),
                    FormasPago = GetFormasPago()
                };

                _cache.Set(cacheKey, result, TimeSpan.FromHours(1));

                _logger.LogInformation("✅ Master data completo obtenido: {CompaniasCount} compañías, {SeccionesCount} secciones, {MonedasCount} monedas",
                    result.Companias.Count, result.Secciones.Count, result.Monedas.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo master data completo");
                throw;
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
                _logger.LogInformation("🔄 Creando póliza en Velneo: Póliza={PolicyNumber}, Cliente={ClienteId}, Compañía={CompaniaId}, Sección={SeccionId}",
                    request.conpol, request.clinro, request.comcod, request.seccod);
                var url = $"{BaseUrl}/contratos?api_key={ApiKey}";

                var velneoPayload = CreateVelneoPayload(request);

                var jsonPayload = JsonSerializer.Serialize(velneoPayload, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                _logger.LogDebug("📤 Enviando a Velneo: {Url}", url);
                _logger.LogInformation("📦 Payload completo enviado a Velneo:");
                _logger.LogInformation("{Payload}", jsonPayload);

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var response = await _httpClient.PostAsync(url, content);

                _logger.LogInformation("🔧 DEBUG Velneo Response:");
                _logger.LogInformation("  - Status: {StatusCode} ({StatusName})", (int)response.StatusCode, response.StatusCode);
                _logger.LogInformation("  - Content-Type: {ContentType}", response.Content.Headers.ContentType?.ToString() ?? "null");

                var responseJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("  - Response Length: {Length}", responseJson?.Length ?? 0);
                _logger.LogInformation("  - Response Content: '{Response}'", responseJson ?? "NULL");

                if (response.IsSuccessStatusCode)
                {
                    if (string.IsNullOrWhiteSpace(responseJson))
                    {
                        _logger.LogError("❌ Velneo respondió con contenido vacío pero status 200");

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

                    var velneoResponse = ParseVelneoContratosResponse(responseJson);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creando póliza en Velneo");
                return new CreatePolizaResponse
                {
                    Success = false,
                    Message = $"Error interno: {ex.Message}",
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

        private VelneoCreateResponse ParseVelneoContratosResponse(string responseJson)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(responseJson);
                var root = jsonDoc.RootElement;

                // La respuesta de /contratos tiene esta estructura:
                // {
                //   "count": 1,
                //   "total_count": 1,
                //   "contratos": [
                //     {
                //       "id": 7646,
                //       "conpol": "numero_poliza",
                //       ...
                //     }
                //   ]
                // }

                if (root.TryGetProperty("contratos", out var contratosArray) &&
                    contratosArray.ValueKind == JsonValueKind.Array &&
                    contratosArray.GetArrayLength() > 0)
                {
                    var firstContrato = contratosArray[0];

                    return new VelneoCreateResponse
                    {
                        VelneoPolizaId = firstContrato.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : null,
                        PolizaNumber = firstContrato.TryGetProperty("conpol", out var polProp) ? polProp.GetString() : "",
                        Success = true
                    };
                }
                else
                {
                    _logger.LogWarning("⚠️ No se encontró array 'contratos' en la respuesta de Velneo");
                    return new VelneoCreateResponse
                    {
                        Success = false,
                        PolizaNumber = "ERROR_PARSING"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Error parseando respuesta de contratos de Velneo: {Error}", ex.Message);
                return new VelneoCreateResponse
                {
                    Success = false,
                    PolizaNumber = "ERROR_PARSING"
                };
            }
        }

        private object CreateVelneoPayload(VelneoPolizaRequest request)
        {
            // ✅ Basado en el curl que funcionó + correcciones identificadas
            return new
            {
                // ✅ ID auto-generado por Velneo
                id = 0,

                // ✅ IDS PRINCIPALES - OBLIGATORIOS
                comcod = request.comcod,          // Compañía
                seccod = request.seccod,          // Sección  
                clinro = request.clinro,          // Cliente

                // ✅ DATOS BÁSICOS DE PÓLIZA - CORREGIDOS
                condom = request.condom ?? "",                    // ✅ Dirección (faltaba mapear)
                conmaraut = ConcatenateBrandModel(request.conmaraut, request.conmodaut), // ✅ Marca + Modelo concatenados
                conanioaut = request.conanioaut,                  // ✅ Año del vehículo
                concodrev = 0,
                conmataut = request.conmataut ?? "",
                conficto = 0,
                conmotor = ExtractMotorCode(request.conmotor),    // ✅ Solo código motor sin "MOTOR"
                conpadaut = "",
                conchasis = ExtractChassisCode(request.conchasis), // ✅ Solo código chasis sin "CHASIS"
                conclaaut = 0,
                condedaut = 0,
                conresciv = 0,
                conbonnsin = 0,
                conbonant = 0,
                concaraut = 0,
                concesnom = "",
                concestel = "",
                concapaut = 0,

                // ✅ MONTOS - CORREGIDOS
                conpremio = request.conpremio,
                contot = request.contot,
                moncod = request.moncod > 0 ? request.moncod : 858,  // ✅ Por defecto UYU (858)
                concuo = request.concuo > 0 ? request.concuo : 0,    // ✅ Número de cuotas
                concomcorr = 0,

                // ✅ MASTER DATA IDS - CORREGIDOS
                catdsc = request.catdsc,
                desdsc = request.desdsc,
                caldsc = request.caldsc,
                flocod = 0,

                // ✅ DATOS DE PÓLIZA
                concar = "",
                conpol = request.conpol,          // Número de póliza
                conend = request.conend ?? request.conpol,  // Endoso (por defecto igual que póliza)
                confchdes = request.confchdes,    // Fecha desde
                confchhas = request.confchhas,    // Fecha hasta

                // ✅ OTROS CAMPOS REQUERIDOS
                conimp = 0,
                connroser = 0,
                rieres = "",
                conges = "",
                congesti = request.congesti ?? "1",
                congesfi = DateTime.Now.ToString("yyyy-MM-dd"),
                congeses = MapEstadoGestion(request.congeses),   // ✅ Estado gestión por número (1=Pendiente, 2=siguiente, etc.)
                convig = request.convig ?? "1",
                concan = 0,
                congrucon = "",
                contipoemp = "",
                conmatpar = "",
                conmatte = "",
                concapla = 0,
                conflota = 0,
                condednum = 0,
                consta = request.consta ?? "T",    // ✅ Forma de pago
                contra = request.contra ?? "1",    // ✅ Trámite
                conconf = "",
                conpadre = 0,
                confchcan = DateTime.Now.ToString("yyyy-MM-dd"),
                concaucan = "",
                conobjtot = 0,
                contpoact = "",
                conesp = "",
                convalacr = 0,
                convallet = 0,
                condecram = "",
                conmedtra = "",
                conviades = "",
                conviaa = "",
                conviaenb = "",
                conviakb = 0,
                conviakn = 0,
                conviatra = "",
                conviacos = 0,
                conviafle = 0,
                dptnom = request.dptnom,
                conedaret = 0,
                congar = 0,
                condecpri = 0,
                condecpro = 0,
                condecptj = 0,
                conubi = "",
                concaudsc = "",
                conincuno = "",
                conviagas = 0,
                conviarec = 0,
                conviapri = 0,
                linobs = 0,
                concomdes = DateTime.Now.ToString("yyyy-MM-dd"),
                concalcom = "",
                tpoconcod = 0,
                tpovivcod = 0,
                tporiecod = 0,
                modcod = 0,
                concapase = 0,
                conpricap = 0,
                tposegdsc = "",
                conriecod = 0,
                conriedsc = "",
                conrecfin = 0,
                conimprf = 0,
                conafesin = 0,
                conautcor = 0,
                conlinrie = 0,
                conconesp = 0,
                conlimnav = "",
                contpocob = "",
                connomemb = "",
                contpoemb = "",
                lincarta = 0,
                cancecod = 0,
                concomotr = 0,
                conautcome = "",
                conviafac = "",
                conviamon = request.conviamon ?? 0,
                conviatpo = "",
                connrorc = "",
                condedurc = "",
                lininclu = 0,
                linexclu = 0,
                concapret = 0,
                forpagvid = "",
                clinom = request.clinom ?? "",                   // ✅ Nombre del cliente
                tarcod = request.tarcod,
                corrnom = request.corrnom,
                connroint = 0,
                conautnd = "",
                conpadend = 0,
                contotpri = 0,
                padreaux = 0,
                conlinflot = 0,
                conflotimp = 0,
                conflottotal = 0,
                conflotsaldo = 0,
                conaccicer = "",
                concerfin = DateTime.Now.ToString("yyyy-MM-dd"),
                condetemb = "",
                conclaemb = "",
                confabemb = "",
                conbanemb = "",
                conmatemb = "",
                convelemb = "",
                conmatriemb = "",
                conptoemb = "",
                otrcorrcod = 0,
                condeta = "",
                observaciones = request.observaciones ?? "",
                clipcupfia = 0,
                conclieda = "",
                condecrea = "",
                condecaju = "",
                conviatot = 0,
                contpoemp = "",
                congaran = "",
                congarantel = "",
                mot_no_ren = "",
                condetrc = "",
                conautcort = true,
                condetail = "",
                clinro1 = request.clinro1 > 0 ? request.clinro1 : request.clinro,  // ✅ Por defecto mismo cliente, override si hay tomador diferente
                consumsal = 0,
                conespbon = "",
                leer = true,
                enviado = true,
                sob_recib = true,
                leer_obs = true,
                sublistas = "",
                com_sub_corr = 0,
                tipos_de_alarma = 0,
                tiene_alarma = true,
                coberturas_bicicleta = 0,
                com_bro = 0,
                com_bo = 0,
                contotant = 0,
                cotizacion = "",
                motivos_no_renovacion = 0,
                com_alias = request.com_alias ?? "",             // ✅ Nombre de la compañía
                ramo = request.ramo ?? "",                        // ✅ Nombre de la sección
                clausula = "",
                aereo = true,
                maritimo = true,
                terrestre = true,
                max_aereo = 0,
                max_mar = 0,
                max_terrestre = 0,
                tasa = 0,
                facturacion = "",
                importacion = true,
                exportacion = true,
                offshore = true,
                transito_interno = true,
                coning = "",
                cat_cli = 0,
                llamar = true,
                granizo = true,
                idorden = "",
                var_ubi = true,
                mis_rie = true,

                // ✅ METADATOS - TIMESTAMPS
                ingresado = request.ingresado.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                last_update = request.last_update.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                comcod1 = 0,
                comcod2 = 0,
                pagos_efectivo = 0,
                productos_de_vida = 0,
                app_id = request.app_id,
                update_date = DateTime.Now.ToString("yyyy-MM-dd"),
                gestion = "",
                asignado = 0,
                combustibles = request.combustibles ?? "",
                conidpad = 0
            };
        }

        private string ConcatenateBrandModel(string? brand, string? model)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(brand))
            {
                // Limpiar "MARCA" del texto
                var cleanBrand = brand.Replace("MARCA", "").Replace("marca", "").Trim();
                if (!string.IsNullOrWhiteSpace(cleanBrand))
                    parts.Add(cleanBrand);
            }

            if (!string.IsNullOrWhiteSpace(model))
            {
                // Limpiar "MODELO" del texto
                var cleanModel = model.Replace("MODELO", "").Replace("modelo", "").Trim();
                if (!string.IsNullOrWhiteSpace(cleanModel))
                    parts.Add(cleanModel);
            }

            return string.Join(" ", parts);
        }

        private string MapEstadoGestion(string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado)) return "1"; // Por defecto "Pendiente"

            var estadoNormalizado = estado.ToLower().Trim();

            return estadoNormalizado switch
            {
                "pendiente" => "1",
                "pendiente c/plazo" => "2",
                "pendiente s/plazo" => "3",
                "terminado" => "4",
                "en proceso" => "5",
                "modificaciones" => "6",
                "en emisión" => "7",
                "enviado a cía" => "8",
                "enviado a cía x mail" => "9",
                "devuelto a ejecutivo" => "10",
                "declinado" => "11",
                _ => "1" // Por defecto "Pendiente"
            };
        }

        private string ExtractMotorCode(string? motorFull)
        {
            if (string.IsNullOrWhiteSpace(motorFull)) return "";

            // Quitar "MOTOR" del inicio y espacios
            return motorFull.Replace("MOTOR", "").Replace("motor", "").Trim();
        }

        private string ExtractChassisCode(string? chassisFull)
        {
            if (string.IsNullOrWhiteSpace(chassisFull)) return "";

            // Quitar "CHASIS" del inicio y espacios
            return chassisFull.Replace("CHASIS", "").Replace("chasis", "").Trim();
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
