using System.Threading.Tasks;
using ServiceLayer.Dtos;

namespace ServiceLayer.Services
{
    public interface ISubscriptionService
    {
        Task<SubscriptionStatusDto> GetCurrentStatusAsync();
        Task<bool> CanCreateSubjectAsync();
        Task<bool> CanUploadDocumentAsync(int subjectId, long fileSizeBytes);
        Task<bool> CanAskQuestionAsync();
        Task<bool> CanUseGeminiAsync();
    }
}
