using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SegurosApp.API.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? CompanyName { get; set; }

        [MaxLength(255)]
        public string? CompanyRUC { get; set; }

        [MaxLength(255)]
        public string? CompanyAddress { get; set; }

        [MaxLength(255)]
        public string? ContactPerson { get; set; }

        [MaxLength(255)]
        public string? ContactPhone { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}