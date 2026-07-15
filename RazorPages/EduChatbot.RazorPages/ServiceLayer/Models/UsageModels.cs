using System;
using System.Collections.Generic;

namespace ServiceLayer.Models
{
    public class TokenUsageRecordInput
    {
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
    }

    public class UsageReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalInputTokens { get; set; }
        public int TotalOutputTokens { get; set; }
        public int TotalTokens { get; set; }

        public List<DailyUsageDto> DailyUsages { get; set; } = new();
        public List<UserUsageDto> UserUsages { get; set; } = new();
        public List<SubjectUsageDto> SubjectUsages { get; set; } = new();
    }

    public class DailyUsageDto
    {
        public DateTime Date { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    public class UserUsageDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    public class SubjectUsageDto
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
