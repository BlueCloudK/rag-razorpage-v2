using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    public class ChatSession
    {
        [Key]
        public int Id { get; set; }

        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ChatMessage>? Messages { get; set; }
    }
}
