using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs
{
    public class ValidatedPreSelection
    {
        public ClienteItem Cliente { get; set; } = null!;
        public CompaniaItem Compania { get; set; } = null!;
        public SeccionItem Seccion { get; set; } = null!;

        public string ClienteDisplayName => Cliente.DisplayName;
        public string ClienteDocumentNumber => Cliente.DocumentNumber;
        public string CompaniaDisplayName => Compania.DisplayName;
        public string SeccionDisplayName => Seccion.DisplayName;
    }
}
