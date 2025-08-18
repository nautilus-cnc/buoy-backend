using System.ComponentModel.DataAnnotations;

namespace BuoySystem.Models
{
    public class BuoyCommandRequest
    {
        [Required(ErrorMessage = "IMEI is required")]
        [StringLength(15, MinimumLength = 15, ErrorMessage = "IMEI must be exactly 15 digits")]
        [RegularExpression(@"^\d{15}$", ErrorMessage = "IMEI must contain only digits")]
        public string Imei { get; set; } = string.Empty;

        [Required(ErrorMessage = "Command is required")]
        [StringLength(500, ErrorMessage = "Command cannot exceed 500 characters")]
        public string Command { get; set; } = string.Empty;

        [Required(ErrorMessage = "Recipient email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        public string RecipientEmail { get; set; } = string.Empty;

        public string? RecipientDisplayName { get; set; }
    }

    public class BuoyCommandResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorDetails { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? TransactionId { get; set; }
    }

    public class EmailConfiguration
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
    }
}