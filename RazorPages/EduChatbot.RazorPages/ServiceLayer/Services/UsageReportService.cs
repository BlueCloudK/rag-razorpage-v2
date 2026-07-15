using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Models;

namespace ServiceLayer.Services
{
    public class UsageReportService : IUsageReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UsageReportService(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<UsageReportDto> GetUsageReportAsync(DateTime startDate, DateTime endDate)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
                return new UsageReportDto();

            var isAdmin = await _currentUser.IsInRoleAsync(AuthConstants.Admin);
            var isLecturer = await _currentUser.IsInRoleAsync(AuthConstants.Lecturer);
            
            var orgId = await _context.OrganizationMembers
                .Where(m => m.UserId == userId && m.Organization!.IsActive)
                .Select(m => (int?)m.OrganizationId)
                .FirstOrDefaultAsync();

            var query = _context.TokenUsages
                .Where(u => u.Timestamp >= startDate && u.Timestamp <= endDate);

            if (isAdmin && orgId.HasValue)
            {
                query = query.Where(u => u.OrganizationId == orgId.Value);
            }
            else if (isLecturer)
            {
                var subjectIds = await _context.SubjectMemberships
                    .Where(m => m.UserId == userId && (m.RoleInSubject == AuthConstants.Lecturer || m.RoleInSubject == AuthConstants.SubjectLead))
                    .Select(m => m.SubjectId)
                    .ToListAsync();

                query = query.Where(u => u.SubjectId.HasValue && subjectIds.Contains(u.SubjectId.Value));
            }
            else
            {
                query = query.Where(u => u.UserId == userId);
            }

            var usages = await query
                .Include(u => u.User)
                .Include(u => u.Subject)
                .ToListAsync();

            var usageUserIds = usages.Select(u => u.UserId).Distinct().ToList();
            var rolePairs = usageUserIds.Count == 0
                ? new List<(string UserId, string RoleName)>()
                : (await (from userRole in _context.UserRoles
                          join role in _context.Roles on userRole.RoleId equals role.Id
                          where usageUserIds.Contains(userRole.UserId)
                          select new { userRole.UserId, RoleName = role.Name ?? "Unknown" })
                    .ToListAsync())
                    .Select(pair => (pair.UserId, pair.RoleName))
                    .ToList();
            var rolesByUser = rolePairs
                .GroupBy(pair => pair.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => string.Join(", ", group.Select(pair => pair.RoleName).Distinct().OrderBy(role => role)));

            var usageSubjectIds = usages
                .Where(u => u.SubjectId.HasValue)
                .Select(u => u.SubjectId!.Value)
                .Distinct()
                .ToList();
            var indexedDocumentCounts = usageSubjectIds.Count == 0
                ? new List<(int SubjectId, int Count)>()
                : (await _context.Documents
                    .Where(document => usageSubjectIds.Contains(document.SubjectId) && document.IsIndexed)
                    .GroupBy(document => document.SubjectId)
                    .Select(group => new { SubjectId = group.Key, Count = group.Count() })
                    .ToListAsync())
                    .Select(item => (item.SubjectId, item.Count))
                    .ToList();
            var indexedDocumentsBySubject = indexedDocumentCounts
                .ToDictionary(item => item.SubjectId, item => item.Count);

            var report = new UsageReportDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalInputTokens = usages.Sum(u => u.InputTokens),
                TotalRetrievedContextTokens = usages.Sum(u => u.RetrievedContextTokens),
                TotalOutputTokens = usages.Sum(u => u.OutputTokens),
                TotalTokens = usages.Sum(u => u.TotalTokens),
                CompletedQuestionCount = usages.Count,
                ActiveUserCount = usageUserIds.Count,
                IndexedDocumentCount = indexedDocumentCounts.Sum(item => item.Count),
                TokensAreEstimated = usages.Any(u => u.IsEstimated)
            };

            report.DailyUsages = usages
                .GroupBy(u => u.Timestamp.Date)
                .Select(g => new DailyTokenUsageDto
                {
                    Date = g.Key,
                    InputTokens = g.Sum(x => x.InputTokens),
                    RetrievedContextTokens = g.Sum(x => x.RetrievedContextTokens),
                    OutputTokens = g.Sum(x => x.OutputTokens),
                    TotalTokens = g.Sum(x => x.TotalTokens)
                })
                .OrderBy(d => d.Date)
                .ToList();

            report.UserUsages = usages
                .GroupBy(u => new
                {
                    u.UserId,
                    UserName = u.User?.FullName ?? string.Empty,
                    Email = u.User?.Email ?? "Unknown"
                })
                .Select(g => new UserTokenUsageDto
                {
                    UserId = g.Key.UserId,
                    UserName = string.IsNullOrWhiteSpace(g.Key.UserName) ? g.Key.Email : g.Key.UserName,
                    Email = g.Key.Email,
                    SystemRole = rolesByUser.GetValueOrDefault(g.Key.UserId, "Unknown"),
                    QuestionCount = g.Count(),
                    InputTokens = g.Sum(x => x.InputTokens),
                    RetrievedContextTokens = g.Sum(x => x.RetrievedContextTokens),
                    OutputTokens = g.Sum(x => x.OutputTokens),
                    TotalTokens = g.Sum(x => x.TotalTokens)
                })
                .OrderByDescending(u => u.TotalTokens)
                .ToList();

            report.SubjectUsages = usages
                .Where(u => u.SubjectId.HasValue)
                .GroupBy(u => new
                {
                    SubjectId = u.SubjectId!.Value,
                    SubjectName = u.Subject?.Name ?? "Unknown",
                    SubjectCode = u.Subject?.Code ?? string.Empty
                })
                .Select(g => new SubjectTokenUsageDto
                {
                    SubjectId = g.Key.SubjectId,
                    SubjectName = g.Key.SubjectName,
                    SubjectCode = g.Key.SubjectCode,
                    QuestionCount = g.Count(),
                    InputTokens = g.Sum(x => x.InputTokens),
                    OutputTokens = g.Sum(x => x.OutputTokens),
                    TotalTokens = g.Sum(x => x.TotalTokens),
                    IndexedDocumentCount = indexedDocumentsBySubject.GetValueOrDefault(g.Key.SubjectId)
                })
                .OrderByDescending(s => s.TotalTokens)
                .ToList();

            return report;
        }
    }
}
