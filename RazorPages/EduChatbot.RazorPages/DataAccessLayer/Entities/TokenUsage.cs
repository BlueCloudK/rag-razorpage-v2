using System;

namespace DataAccessLayer.Entities
{
    public class TokenUsage
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int? OrganizationId { get; set; }
        public int? SubjectId { get; set; }
        public int? ChatSessionId { get; set; }

        public int InputTokens { get; set; }
        public int RetrievedContextTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }

        public string ModelName { get; set; } = string.Empty;
        public bool IsEstimated { get; set; }
        public DateTime Timestamp { get; set; }

        public virtual ApplicationUser? User { get; set; }
        public virtual Organization? Organization { get; set; }
        public virtual Subject? Subject { get; set; }
        public virtual ChatSession? ChatSession { get; set; }
    }
}
