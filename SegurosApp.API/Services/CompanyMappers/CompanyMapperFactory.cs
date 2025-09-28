using SegurosApp.API.Services.CompanyMappers;

namespace SegurosApp.API.Services
{
    public class CompanyMapperFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CompanyMapperFactory> _logger;

        public CompanyMapperFactory(IServiceProvider serviceProvider, ILogger<CompanyMapperFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public ICompanyFieldMapper GetMapper(int? companiaId)
        {
            _logger.LogInformation("🔍 Solicitando mapper para compañía: {CompaniaId}", companiaId);

            var mapperType = companiaId switch
            {
                1 => typeof(BSEFieldMapper),
                2 => typeof(SuraFieldMapper),
                3 => typeof(MapfreFieldMapper),
                4 => typeof(SuraFieldMapper),
                _ => typeof(BSEFieldMapper)
            };

            _logger.LogInformation("🎯 Tipo de mapper determinado: {MapperType}", mapperType.Name);

            try
            {
                var mapper = (ICompanyFieldMapper)_serviceProvider.GetRequiredService(mapperType);
                _logger.LogInformation("✅ Mapper resuelto correctamente: {ActualMapperName}", mapper.GetCompanyName());
                return mapper;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error resolviendo mapper {MapperType}, usando BSE por defecto", mapperType.Name);
                return _serviceProvider.GetRequiredService<BSEFieldMapper>();
            }
        }

        public Dictionary<int, string> GetAvailableMappers()
        {
            return new Dictionary<int, string>
            {
                { 1, "BSE" },
                { 2, "SURA" }, 
                { 3, "MAPFRE" }, 
                { 4, "SURA" }
            };
        }
    }
}