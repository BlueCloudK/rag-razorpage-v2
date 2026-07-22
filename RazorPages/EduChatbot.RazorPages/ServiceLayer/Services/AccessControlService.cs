using System.Threading.Tasks;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.Services
{
    public class AccessControlService : IAccessControlService
    {
        private readonly IDataRepository _repository;
        private readonly ICurrentUserService _currentUser;

        public AccessControlService(IDataRepository context, ICurrentUserService currentUser)
        {
            _repository = context;
            _currentUser = currentUser;
        }

        public Task<bool> IsAdminAsync()
        {
            return _currentUser.IsInRoleAsync(AuthConstants.Admin);
        }

        public async Task<bool> CanViewSubjectAsync(int subjectId)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
                return false;

            if (await IsAdminAsync())
                return true;

            return await _repository.SubjectMemberships.AnyAsync(m =>
                m.SubjectId == subjectId &&
                m.UserId == _currentUser.UserId);
        }

        public async Task<bool> CanManageSubjectAsync(int subjectId)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
                return false;

            if (await IsAdminAsync())
                return true;

            return await _repository.SubjectMemberships.AnyAsync(m =>
                m.SubjectId == subjectId &&
                m.UserId == _currentUser.UserId &&
                m.RoleInSubject == AuthConstants.SubjectLead);
        }

        public async Task<bool> CanUploadDocumentAsync(int subjectId)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
                return false;

            if (await IsAdminAsync())
                return false;

            return await _repository.SubjectMemberships.AnyAsync(m =>
                m.SubjectId == subjectId &&
                m.UserId == _currentUser.UserId &&
                m.RoleInSubject == AuthConstants.SubjectLead);
        }

        public async Task<bool> CanDeleteDocumentAsync(int subjectId)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
                return false;

            if (await IsAdminAsync())
                return false;

            return await _repository.SubjectMemberships.AnyAsync(m =>
                m.SubjectId == subjectId &&
                m.UserId == _currentUser.UserId &&
                m.RoleInSubject == AuthConstants.SubjectLead);
        }
    }
}

