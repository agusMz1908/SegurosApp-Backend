using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SegurosApp.API.DTOs
{
    public class ClienteSearchFilters
    {
        [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
        public string? Nombre { get; set; }
        [StringLength(200, ErrorMessage = "La dirección no puede tener más de 200 caracteres")]
        public string? Direcciones { get; set; }
        [StringLength(50, ErrorMessage = "El teléfono fijo no puede tener más de 50 caracteres")]
        public string? Clitel { get; set; }
        [StringLength(50, ErrorMessage = "El celular no puede tener más de 50 caracteres")]
        public string? Clicel { get; set; }
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede tener más de 100 caracteres")]
        public string? Mail { get; set; }
        [StringLength(20, ErrorMessage = "El RUC no puede tener más de 20 caracteres")]
        public string? Cliruc { get; set; }
        [StringLength(20, ErrorMessage = "La cédula no puede tener más de 20 caracteres")]
        public string? Cliced { get; set; }
        [Range(1, 100, ErrorMessage = "El límite debe estar entre 1 y 100")]
        public int Limit { get; set; } = 20;
        public bool SoloActivos { get; set; } = true;
        public bool HasAnyFilter()
        {
            return !string.IsNullOrWhiteSpace(Nombre) ||
                   !string.IsNullOrWhiteSpace(Direcciones) ||
                   !string.IsNullOrWhiteSpace(Clitel) ||
                   !string.IsNullOrWhiteSpace(Clicel) ||
                   !string.IsNullOrWhiteSpace(Mail) ||
                   !string.IsNullOrWhiteSpace(Cliruc) ||
                   !string.IsNullOrWhiteSpace(Cliced);
        }

        public string GetCacheKey()
        {
            var keyBuilder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(Nombre))
                keyBuilder.Append($"n:{Nombre.ToLowerInvariant()}_");
            if (!string.IsNullOrWhiteSpace(Direcciones))
                keyBuilder.Append($"d:{Direcciones.ToLowerInvariant()}_");
            if (!string.IsNullOrWhiteSpace(Clitel))
                keyBuilder.Append($"t:{Clitel}_");
            if (!string.IsNullOrWhiteSpace(Clicel))
                keyBuilder.Append($"c:{Clicel}_");
            if (!string.IsNullOrWhiteSpace(Mail))
                keyBuilder.Append($"m:{Mail.ToLowerInvariant()}_");
            if (!string.IsNullOrWhiteSpace(Cliruc))
                keyBuilder.Append($"r:{Cliruc}_");
            if (!string.IsNullOrWhiteSpace(Cliced))
                keyBuilder.Append($"ci:{Cliced}_");

            keyBuilder.Append($"l:{Limit}_a:{SoloActivos}");

            return keyBuilder.ToString();
        }

        public override string ToString()
        {
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(Nombre))
                filters.Add($"Nombre='{Nombre}'");
            if (!string.IsNullOrWhiteSpace(Direcciones))
                filters.Add($"Direcciones='{Direcciones}'");
            if (!string.IsNullOrWhiteSpace(Clitel))
                filters.Add($"Clitel='{Clitel}'");
            if (!string.IsNullOrWhiteSpace(Clicel))
                filters.Add($"Clicel='{Clicel}'");
            if (!string.IsNullOrWhiteSpace(Mail))
                filters.Add($"Mail='{Mail}'");
            if (!string.IsNullOrWhiteSpace(Cliruc))
                filters.Add($"Cliruc='{Cliruc}'");
            if (!string.IsNullOrWhiteSpace(Cliced))
                filters.Add($"Cliced='{Cliced}'");

            filters.Add($"Limit={Limit}");
            filters.Add($"SoloActivos={SoloActivos}");

            return string.Join(", ", filters);
        }

        public int GetActiveFiltersCount()
        {
            int count = 0;

            if (!string.IsNullOrWhiteSpace(Nombre)) count++;
            if (!string.IsNullOrWhiteSpace(Direcciones)) count++;
            if (!string.IsNullOrWhiteSpace(Clitel)) count++;
            if (!string.IsNullOrWhiteSpace(Clicel)) count++;
            if (!string.IsNullOrWhiteSpace(Mail)) count++;
            if (!string.IsNullOrWhiteSpace(Cliruc)) count++;
            if (!string.IsNullOrWhiteSpace(Cliced)) count++;

            return count;
        }
        public List<string> GetActiveFiltersList()
        {
            var activeFilters = new List<string>();

            if (!string.IsNullOrWhiteSpace(Nombre)) activeFilters.Add($"nombre: {Nombre}");
            if (!string.IsNullOrWhiteSpace(Direcciones)) activeFilters.Add($"direcciones: {Direcciones}");
            if (!string.IsNullOrWhiteSpace(Clitel)) activeFilters.Add($"clitel: {Clitel}");
            if (!string.IsNullOrWhiteSpace(Clicel)) activeFilters.Add($"clicel: {Clicel}");
            if (!string.IsNullOrWhiteSpace(Mail)) activeFilters.Add($"mail: {Mail}");
            if (!string.IsNullOrWhiteSpace(Cliruc)) activeFilters.Add($"cliruc: {Cliruc}");
            if (!string.IsNullOrWhiteSpace(Cliced)) activeFilters.Add($"cliced: {Cliced}");

            return activeFilters;
        }
        public void TrimAndCleanFilters()
        {
            Nombre = string.IsNullOrWhiteSpace(Nombre) ? null : Nombre.Trim();
            Direcciones = string.IsNullOrWhiteSpace(Direcciones) ? null : Direcciones.Trim();
            Clitel = string.IsNullOrWhiteSpace(Clitel) ? null : Clitel.Trim();
            Clicel = string.IsNullOrWhiteSpace(Clicel) ? null : Clicel.Trim();
            Mail = string.IsNullOrWhiteSpace(Mail) ? null : Mail.Trim();
            Cliruc = string.IsNullOrWhiteSpace(Cliruc) ? null : Cliruc.Trim();
            Cliced = string.IsNullOrWhiteSpace(Cliced) ? null : Cliced.Trim();

            if (Limit < 1) Limit = 1;
            if (Limit > 100) Limit = 100;
        }
    }
}