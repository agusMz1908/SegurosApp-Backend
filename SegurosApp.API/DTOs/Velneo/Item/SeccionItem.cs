namespace SegurosApp.API.DTOs.Velneo.Item
{
    public class SeccionItem
    {
        public int id { get; set; }
        public string seccion { get; set; } = string.Empty;       
        public string icono { get; set; } = string.Empty;        
        public string DisplayName => seccion;
        public string Code => seccion.Replace(" ", "_").ToUpperInvariant();
        public bool IsActive => !string.IsNullOrEmpty(seccion);
    }
}
