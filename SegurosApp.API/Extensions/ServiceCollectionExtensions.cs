using SegurosApp.API.Services;
using SegurosApp.API.Services.CompanyMappers;

namespace SegurosApp.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCompanyMappingServices(this IServiceCollection services)
        {
            services.AddScoped<BSEFieldMapper>();
            services.AddScoped<MapfreFieldMapper>();
            services.AddScoped<SuraFieldMapper>();
            services.AddScoped<CompanyMapperFactory>();

            return services;
        }
    }
}