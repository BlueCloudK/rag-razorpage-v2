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

            var report = new UsageReportDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalInputTokens = usages.Sum(u => u.InputTokens),
                TotalOutputTokens = usages.Sum(u => u.OutputTokens),
                TotalTokens = usages.Sum(u => u.TotalTokens)
            };

            report.DailyUsages = usages
                .GroupBy(u => u.Timestamp.Date)
                .Select(g => new DailyTokenUsageDto
                {
                    Date = g.Key,
                    InputTokens = g.Sum(x => x.InputTokens),
                    OutputTokens = g.Sum(x => x.OutputTokens),
                    TotalTokens = g.Sum(x => x.TotalTokens)
                })
                .OrderBy(d => d.Date)
                .ToList();

            report.UserUsages = usages
                .GroupBy(u => new { u.UserId, Email = u.User?.Email ?? "Unknown" })
                .Select(g => new UserTokenUsageDto
                {
                    UserId = g.Key.UserId,
                    Email = g.Key.Email,
                    InputTokens = g.Sum(x => x.InputTokens),
                    OutputTokens = g.Sum(x => x.OutputTokens),
                    TotalTokens = g.Sum(x => x.TotalTokens)
                })
                .OrderByDescending(u => u.TotalTokens)
                .ToList();

            report.SubjectUsages = usages
                .Where(u => u.SubjectId.HasValue)
                .GroupBy(u => new { SubjectId = u.SubjectId!.Value, SubjectName = u.Subject?.Name ?? "Unknown" })
                .Select(g => new SubjectTokenUsageDto
                {
                    SubjectId = g.Key.SubjectId,
                    SubjectName = g.Key.SubjectName,
                    InputTokens = g.Sum(x => x.InputTokens),
                    OutputTokens = g.Sum(x => x.OutputTokens),
                    TotalTokens = g.Sum(x => x.TotalTokens)
                })
                .OrderByDescending(s => s.TotalTokens)
                .ToList();

            return report;
        }
    }
}
