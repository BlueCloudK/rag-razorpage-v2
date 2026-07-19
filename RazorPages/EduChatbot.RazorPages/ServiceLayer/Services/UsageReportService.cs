using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ServiceLayer.Dtos;
using ServiceLayer.Options;

namespace ServiceLayer.Services
{
    public class UsageReportService : IUsageReportService
    {
        private readonly IDataRepository _repository;
        private readonly ICurrentUserService _currentUser;
        private readonly ChatTokenLimitOptions _tokenLimits;

        public UsageReportService(IDataRepository context, ICurrentUserService currentUser, IConfiguration configuration)
        {
            _repository = context;
            _currentUser = currentUser;
            _tokenLimits = ChatTokenLimitOptions.FromConfiguration(configuration);
        }

        public async Task<UsageReportDto> GetUsageReportAsync(DateTime startDate, DateTime endDate, bool useDemoData = false)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
                return new UsageReportDto();

            var isAdmin = await _currentUser.IsInRoleAsync(AuthConstants.Admin);
            var isLecturer = await _currentUser.IsInRoleAsync(AuthConstants.Lecturer);
            
            var orgId = await _repository.OrganizationMembers
                .Where(m => m.UserId == userId && m.Organization!.IsActive)
                .Select(m => (int?)m.OrganizationId)
                .FirstOrDefaultAsync();

            var scopedSubjectIds = new List<int>();
            if (isAdmin && orgId.HasValue)
            {
                scopedSubjectIds = await _repository.Subjects
                    .Where(subject => subject.OrganizationId == orgId.Value)
                    .Select(subject => subject.Id)
                    .ToListAsync();
            }
            else if (isLecturer)
            {
                scopedSubjectIds = await _repository.SubjectMemberships
                    .Where(m => m.UserId == userId && (m.RoleInSubject == AuthConstants.Lecturer || m.RoleInSubject == AuthConstants.SubjectLead))
                    .Select(m => m.SubjectId)
                    .ToListAsync();
            }

            var query = _repository.TokenUsages
                .Where(u => u.Timestamp >= startDate && u.Timestamp <= endDate);

            if (isAdmin && orgId.HasValue)
            {
                query = query.Where(u => u.OrganizationId == orgId.Value);
            }
            else if (isLecturer)
            {
                query = query.Where(usage =>
                    usage.SubjectId.HasValue
                    && scopedSubjectIds.Contains(usage.SubjectId.Value)
                    && _repository.SubjectMemberships.Any(membership =>
                        membership.SubjectId == usage.SubjectId.Value
                        && membership.UserId == usage.UserId
                        && membership.RoleInSubject == AuthConstants.Student));
            }
            else
            {
                // The report endpoints deny students, but keep the service safe if reused elsewhere.
                query = query.Where(u => u.UserId == userId);
            }

            var usages = await query
                .Include(u => u.User)
                .Include(u => u.Subject)
                .ToListAsync();

            var usageUserIds = usages.Select(u => u.UserId).Distinct().ToList();
            var rolePairs = usageUserIds.Count == 0
                ? new List<(string UserId, string RoleName)>()
                : (await (from userRole in _repository.UserRoles
                          join role in _repository.Roles on userRole.RoleId equals role.Id
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

            var documentScopeSubjectIds = scopedSubjectIds.Count > 0
                ? scopedSubjectIds
                : usages.Where(u => u.SubjectId.HasValue).Select(u => u.SubjectId!.Value).Distinct().ToList();
            var indexedDocumentCounts = documentScopeSubjectIds.Count == 0
                ? new List<(int SubjectId, int Count)>()
                : (await _repository.Documents
                    .Where(document => documentScopeSubjectIds.Contains(document.SubjectId) && document.IsIndexed)
                    .GroupBy(document => document.SubjectId)
                    .Select(group => new { SubjectId = group.Key, Count = group.Count() })
                    .ToListAsync())
                    .Select(item => (item.SubjectId, item.Count))
                    .ToList();
            var indexedDocumentsBySubject = indexedDocumentCounts
                .ToDictionary(item => item.SubjectId, item => item.Count);

            var report = new UsageReportDto
            {
                ScopeKind = isAdmin ? "organization" : "teaching",
                ScopeLabel = isAdmin ? "Organization usage" : isLecturer ? "Teaching analytics" : "My learning usage",
                ScopeDescription = isAdmin
                    ? "Privacy-safe usage totals across your active organization."
                    : isLecturer
                        ? "Usage totals for subjects where you are a lecturer or subject lead."
                        : "Only your own completed AI answers are included.",
                StartDate = startDate,
                EndDate = endDate,
                TotalInputTokens = usages.Sum(u => u.InputTokens),
                TotalRetrievedContextTokens = usages.Sum(u => u.RetrievedContextTokens),
                TotalOutputTokens = usages.Sum(u => u.OutputTokens),
                TotalTokens = usages.Sum(u => u.TotalTokens),
                QuestionCount = usages.Count,
                CompletedQuestionCount = usages.Count,
                ActiveUserCount = usageUserIds.Count,
                AccessibleSubjectCount = scopedSubjectIds.Count,
                IndexedDocumentCount = indexedDocumentCounts.Sum(item => item.Count),
                TokensAreEstimated = usages.Any(u => u.IsEstimated),
                TokenLimits = _tokenLimits
            };

            report.DailyUsages = usages
                .GroupBy(u => u.Timestamp.Date)
                .Select(g => new DailyTokenUsageDto
                {
                    Date = g.Key,
                    InputTokens = g.Sum(x => x.InputTokens),
                    RetrievedContextTokens = g.Sum(x => x.RetrievedContextTokens),
                    OutputTokens = g.Sum(x => x.OutputTokens),
                    TotalTokens = g.Sum(x => x.TotalTokens),
                    QuestionCount = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            if (isAdmin)
            {
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
            }

            if (isLecturer && scopedSubjectIds.Count > 0)
            {
                var learnerMemberships = await _repository.SubjectMemberships
                    .Include(membership => membership.User)
                    .Include(membership => membership.Subject)
                    .Where(membership => scopedSubjectIds.Contains(membership.SubjectId))
                    .Where(membership => membership.RoleInSubject == AuthConstants.Student)
                    .ToListAsync();

                var usageByLearnerAndSubject = usages
                    .Where(usage => usage.SubjectId.HasValue)
                    .GroupBy(usage => new { usage.UserId, SubjectId = usage.SubjectId!.Value })
                    .ToDictionary(group => (group.Key.UserId, group.Key.SubjectId));

                report.LearnerUsages = learnerMemberships
                    .Select(membership =>
                    {
                        usageByLearnerAndSubject.TryGetValue((membership.UserId, membership.SubjectId), out var learnerUsage);
                        return new LearnerSubjectUsageDto
                        {
                            UserId = membership.UserId,
                            UserName = string.IsNullOrWhiteSpace(membership.User?.FullName)
                                ? membership.User?.Email ?? "Unknown"
                                : membership.User.FullName,
                            Email = membership.User?.Email ?? "Unknown",
                            SubjectId = membership.SubjectId,
                            SubjectName = membership.Subject?.Name ?? "Unknown",
                            SubjectCode = membership.Subject?.Code ?? string.Empty,
                            QuestionCount = learnerUsage?.Count() ?? 0,
                            TotalTokens = learnerUsage?.Sum(usage => usage.TotalTokens) ?? 0,
                            LastActivityAt = learnerUsage?.Max(usage => (DateTime?)usage.Timestamp)
                        };
                    })
                    .OrderByDescending(usage => usage.TotalTokens)
                    .ThenBy(usage => usage.UserName)
                    .ToList();
            }

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
                    RetrievedContextTokens = g.Sum(x => x.RetrievedContextTokens),
                    OutputTokens = g.Sum(x => x.OutputTokens),
                    TotalTokens = g.Sum(x => x.TotalTokens),
                    IndexedDocumentCount = indexedDocumentsBySubject.GetValueOrDefault(g.Key.SubjectId)
                })
                .OrderByDescending(s => s.TotalTokens)
                .ToList();

            if (useDemoData)
            {
                await ApplyPresentationDemoAsync(report, scopedSubjectIds, startDate, endDate, isAdmin, isLecturer);
            }

            return report;
        }

        private async Task ApplyPresentationDemoAsync(
            UsageReportDto report,
            List<int> scopedSubjectIds,
            DateTime startDate,
            DateTime endDate,
            bool isAdmin,
            bool isLecturer)
        {
            var subjects = scopedSubjectIds.Count == 0
                ? new List<DataAccessLayer.Entities.Subject>()
                : await _repository.Subjects
                    .Where(subject => scopedSubjectIds.Contains(subject.Id))
                    .OrderBy(subject => subject.Name)
                    .ToListAsync();

            // Keep demo records visibly plausible: weekday study activity is higher than weekends.
            var daily = new List<DailyTokenUsageDto>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var weekdayWeight = date.DayOfWeek switch
                {
                    DayOfWeek.Monday => 5,
                    DayOfWeek.Tuesday => 7,
                    DayOfWeek.Wednesday => 6,
                    DayOfWeek.Thursday => 8,
                    DayOfWeek.Friday => 6,
                    DayOfWeek.Saturday => 3,
                    _ => 2
                };
                var questions = Math.Max(1, weekdayWeight + (date.DayOfYear % 3) - 1);
                var input = questions * (118 + (date.DayOfYear % 4) * 13);
                var context = questions * (245 + (date.DayOfYear % 5) * 19);
                var output = questions * (82 + (date.DayOfYear % 3) * 11);
                daily.Add(new DailyTokenUsageDto
                {
                    Date = date,
                    QuestionCount = questions,
                    InputTokens = input,
                    RetrievedContextTokens = context,
                    OutputTokens = output,
                    TotalTokens = input + context + output
                });
            }

            report.IsDemoData = true;
            report.ScopeDescription += " Demo values are generated in memory and are not saved to the database.";
            report.DailyUsages = daily;
            report.TotalInputTokens = daily.Sum(day => day.InputTokens);
            report.TotalRetrievedContextTokens = daily.Sum(day => day.RetrievedContextTokens);
            report.TotalOutputTokens = daily.Sum(day => day.OutputTokens);
            report.TotalTokens = daily.Sum(day => day.TotalTokens);
            report.QuestionCount = daily.Sum(day => day.QuestionCount);
            report.CompletedQuestionCount = report.QuestionCount;
            report.ActiveUserCount = Math.Max(report.ActiveUserCount, isLecturer ? report.LearnerUsages.Count : 4);
            report.AccessibleSubjectCount = Math.Max(report.AccessibleSubjectCount, subjects.Count);
            report.IndexedDocumentCount = Math.Max(report.IndexedDocumentCount, report.SubjectUsages.Sum(subject => subject.IndexedDocumentCount));
            report.TokensAreEstimated = true;

            if (subjects.Count > 0)
            {
                var perSubjectQuestions = report.QuestionCount / subjects.Count;
                report.SubjectUsages = subjects.Select((subject, index) =>
                {
                    var questions = perSubjectQuestions + (index == 0 ? report.QuestionCount % subjects.Count : 0);
                    var total = questions * (118 + 264 + 93);
                    return new SubjectTokenUsageDto
                    {
                        SubjectId = subject.Id,
                        SubjectName = subject.Name,
                        SubjectCode = subject.Code,
                        QuestionCount = questions,
                        InputTokens = questions * 118,
                        RetrievedContextTokens = questions * 264,
                        OutputTokens = questions * 93,
                        TotalTokens = total,
                        IndexedDocumentCount = Math.Max(1, report.SubjectUsages.FirstOrDefault(item => item.SubjectId == subject.Id)?.IndexedDocumentCount ?? 0)
                    };
                }).OrderByDescending(subject => subject.TotalTokens).ToList();
            }

            if (isLecturer && report.LearnerUsages.Count > 0)
            {
                report.LearnerUsages = report.LearnerUsages.Select((learner, index) => new LearnerSubjectUsageDto
                {
                    UserId = learner.UserId,
                    UserName = learner.UserName,
                    Email = learner.Email,
                    SubjectId = learner.SubjectId,
                    SubjectName = learner.SubjectName,
                    SubjectCode = learner.SubjectCode,
                    QuestionCount = 4 + (index % 4) * 3,
                    TotalTokens = (4 + (index % 4) * 3) * 475,
                    LastActivityAt = endDate.Date.AddDays(-(index % 6)).AddHours(9 + index)
                }).OrderByDescending(learner => learner.TotalTokens).ToList();
            }
        }
    }
}

