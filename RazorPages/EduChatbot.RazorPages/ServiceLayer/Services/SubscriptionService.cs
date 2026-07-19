using System;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Repositories;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Dtos;

namespace ServiceLayer.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IDataRepository _repository;
        private readonly IAccessControlService _accessControl;
        private readonly ICurrentUserService _currentUser;

        public SubscriptionService(IDataRepository context, IAccessControlService accessControl, ICurrentUserService currentUser)
        {
            _repository = context;
            _accessControl = accessControl;
            _currentUser = currentUser;
        }

        public async Task<SubscriptionStatusDto> GetCurrentStatusAsync()
        {
            var plan = await GetPlanAsync();
            var userId = _currentUser.UserId ?? string.Empty;
            var organizationId = await GetCurrentOrganizationIdAsync();
            var today = DateTime.UtcNow.Date;

            var usedQuestions = organizationId.HasValue
                ? await _repository.QuestionUsages
                    .Where(u => u.OrganizationId == organizationId.Value && u.UsageDate == today)
                    .SumAsync(u => u.QuestionCount)
                : string.IsNullOrEmpty(userId)
                ? 0
                : await _repository.QuestionUsages
                    .Where(u => u.UserId == userId && u.UsageDate == today)
                    .Select(u => u.QuestionCount)
                    .FirstOrDefaultAsync();

            var documentsUsed = await CountAccessibleDocumentsAsync(userId);
            var subjectsUsed = await CountAccessibleSubjectsAsync(userId);
            var membersUsed = organizationId.HasValue
                ? await _repository.OrganizationMembers.CountAsync(m => m.OrganizationId == organizationId.Value)
                : 0;

            var bypassesQuota = await _accessControl.IsAdminAsync();

            return new SubscriptionStatusDto
            {
                PlanName = plan.Name,
                QuestionsUsedToday = usedQuestions,
                MaxQuestionsPerDay = plan.MaxQuestionsPerDay,
                DocumentsUsed = documentsUsed,
                MaxDocuments = plan.MaxDocuments,
                SubjectsUsed = subjectsUsed,
                MaxSubjects = plan.MaxSubjects,
                MembersUsed = membersUsed,
                MaxMembers = plan.MaxMembers,
                MaxFileSizeMb = plan.MaxFileSizeMb,
                AllowGemini = plan.AllowGemini,
                IsUnlimited = plan.IsUnlimited,
                BypassesQuota = bypassesQuota
            };
        }

        public async Task<bool> CanCreateSubjectAsync()
        {
            return await _accessControl.IsAdminAsync();
        }

        public async Task<bool> CanUploadDocumentAsync(int subjectId, long fileSizeBytes)
        {
            if (!await _accessControl.CanUploadDocumentAsync(subjectId))
                return false;

            var status = await GetCurrentStatusAsync();
            var fileSizeMb = fileSizeBytes / 1024m / 1024m;
            return (status.IsUnlimited || status.DocumentsUsed < status.MaxDocuments) &&
                   fileSizeMb <= status.MaxFileSizeMb;
        }

        public async Task<bool> CanAskQuestionAsync()
        {
            if (await _accessControl.IsAdminAsync())
                return true;

            var status = await GetCurrentStatusAsync();
            return status.IsUnlimited || status.QuestionsUsedToday < status.MaxQuestionsPerDay;
        }

        public async Task<bool> CanUseGeminiAsync()
        {
            if (await _accessControl.IsAdminAsync())
                return true;

            var plan = await GetPlanAsync();
            return plan.IsUnlimited || plan.AllowGemini;
        }

        private async Task<SubscriptionPlan> GetPlanAsync()
        {
            var organizationId = await GetCurrentOrganizationIdAsync();
            if (organizationId.HasValue)
            {
                var orgPlan = await _repository.OrganizationSubscriptions
                    .Include(s => s.Plan)
                    .Where(s => s.OrganizationId == organizationId.Value && s.IsActive && (s.EndDate == null || s.EndDate > DateTime.UtcNow))
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => s.Plan)
                    .FirstOrDefaultAsync();

                if (orgPlan != null)
                    return orgPlan;
            }

            return await _repository.SubscriptionPlans.FirstAsync(p => p.Name == AuthConstants.Free);
        }

        private async Task<int> CountAccessibleDocumentsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return 0;

            var organizationId = await GetCurrentOrganizationIdAsync();
            if (organizationId.HasValue)
                return await _repository.Documents.CountAsync(d => d.Subject != null && d.Subject.OrganizationId == organizationId.Value);

            if (await _accessControl.IsAdminAsync())
                return await _repository.Documents.CountAsync();

            return await _repository.Documents.CountAsync(d => d.UploadedByUserId == userId);
        }

        private async Task<int> CountAccessibleSubjectsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return 0;

            var organizationId = await GetCurrentOrganizationIdAsync();
            if (organizationId.HasValue)
                return await _repository.Subjects.CountAsync(s => s.OrganizationId == organizationId.Value);

            if (await _accessControl.IsAdminAsync())
                return await _repository.Subjects.CountAsync();

            return await _repository.SubjectMemberships
                .Where(m => m.UserId == userId &&
                    (m.RoleInSubject == AuthConstants.Lecturer ||
                     m.RoleInSubject == AuthConstants.SubjectLead))
                .Select(m => m.SubjectId)
                .Distinct()
                .CountAsync();
        }

        private async Task<int?> GetCurrentOrganizationIdAsync()
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
                return null;

            if (await _accessControl.IsAdminAsync())
            {
                return await _repository.Organizations
                    .Where(o => o.IsActive)
                    .OrderBy(o => o.Id)
                    .Select(o => (int?)o.Id)
                    .FirstOrDefaultAsync();
            }

            return await _repository.OrganizationMembers
                .Where(m => m.UserId == userId && m.Organization!.IsActive)
                .OrderBy(m => m.OrganizationId)
                .Select(m => (int?)m.OrganizationId)
                .FirstOrDefaultAsync();
        }
    }
}

