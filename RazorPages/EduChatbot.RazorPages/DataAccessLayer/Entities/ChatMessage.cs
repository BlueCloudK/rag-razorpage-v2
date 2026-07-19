using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int SessionId { get; set; }

        [ForeignKey("SessionId")]
        public ChatSession? Session { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty; // "User" or "Bot"

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? SourceDocuments { get; set; } = ""; // JSON or comma separated string for citations

        public string? ProcessingTraceJson { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
