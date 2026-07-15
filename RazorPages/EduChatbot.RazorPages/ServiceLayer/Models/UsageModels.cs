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
        public int TotalRetrievedContextTokens { get; set; }
        public int TotalOutputTokens { get; set; }
        public int TotalTokens { get; set; }
        public int QuestionCount { get; set; }
        public int CompletedQuestionCount { get; set; }
        public int ActiveUserCount { get; set; }
        public int IndexedDocumentCount { get; set; }
        public bool TokensAreEstimated { get; set; }

        public List<DailyTokenUsageDto> DailyUsages { get; set; } = new();
        public List<UserTokenUsageDto> UserUsages { get; set; } = new();
        public List<SubjectTokenUsageDto> SubjectUsages { get; set; } = new();
    }

    public class DailyTokenUsageDto
    {
        public DateTime Date { get; set; }
        public int InputTokens { get; set; }
        public int RetrievedContextTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
        public int QuestionCount { get; set; }
    }

    public class UserTokenUsageDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SystemRole { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public int InputTokens { get; set; }
        public int RetrievedContextTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    public class SubjectTokenUsageDto
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public int InputTokens { get; set; }
        public int RetrievedContextTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
        public int IndexedDocumentCount { get; set; }
    }
}
