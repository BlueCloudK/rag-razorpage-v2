using System;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Models;

namespace ServiceLayer.Services
{
    public class UsageService : IUsageService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UsageService(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<int> GetTodayQuestionCountAsync()
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
                return 0;

            var today = DateTime.UtcNow.Date;
            var organizationId = await GetCurrentOrganizationIdAsync();
            return await _context.QuestionUsages
                .Where(u => u.UsageDate == today && (organizationId.HasValue ? u.OrganizationId == organizationId.Value : u.UserId == userId))
                .SumAsync(u => u.QuestionCount);
        }

        public async Task IncrementQuestionCountAsync()
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
                return;

            var today = DateTime.UtcNow.Date;
            var organizationId = await GetCurrentOrganizationIdAsync();
            var usage = await _context.QuestionUsages.FirstOrDefaultAsync(u =>
                u.UserId == userId &&
                u.OrganizationId == organizationId &&
                u.UsageDate == today);
            if (usage == null)
            {
                usage = new QuestionUsage
                {
                    UserId = userId,
                    OrganizationId = organizationId,
                    UsageDate = today,
                    QuestionCount = 0
                };
                _context.QuestionUsages.Add(usage);
            }

            usage.QuestionCount++;
            await _context.SaveChangesAsync();
        }

        public async Task RecordTokenUsageAsync(TokenUsageRecordInput input)
        {
            var usage = new TokenUsage
            {
                UserId = input.UserId,
                OrganizationId = input.OrganizationId,
                SubjectId = input.SubjectId,
                ChatSessionId = input.ChatSessionId,
                InputTokens = input.InputTokens,
                RetrievedContextTokens = input.RetrievedContextTokens,
                OutputTokens = input.OutputTokens,
                TotalTokens = input.TotalTokens,
                ModelName = input.ModelName,
                IsEstimated = input.IsEstimated,
                Timestamp = DateTime.UtcNow
            };

            _context.TokenUsages.Add(usage);
            await _context.SaveChangesAsync();
        }

        private async Task<int?> GetCurrentOrganizationIdAsync()
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _context.OrganizationMembers
                .Where(m => m.UserId == userId && m.Organization!.IsActive)
                .OrderBy(m => m.OrganizationId)
                .Select(m => (int?)m.OrganizationId)
                .FirstOrDefaultAsync();
        }
    }
}
