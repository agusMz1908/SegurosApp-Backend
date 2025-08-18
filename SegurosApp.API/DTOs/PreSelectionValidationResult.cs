namespace SegurosApp.API.DTOs
{
    public class PreSelectionValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public ValidatedPreSelection? ValidatedData { get; set; }

        public static PreSelectionValidationResult Success(ValidatedPreSelection data)
        {
            return new PreSelectionValidationResult
            {
                IsValid = true,
                ValidatedData = data
            };
        }

        public static PreSelectionValidationResult Error(string message)
        {
            return new PreSelectionValidationResult
            {
                IsValid = false,
                ErrorMessage = message,
                Errors = new List<string> { message }
            };
        }
    }
}
