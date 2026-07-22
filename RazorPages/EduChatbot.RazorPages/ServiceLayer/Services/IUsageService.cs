using System.Threading.Tasks;
using ServiceLayer.Dtos;

namespace ServiceLayer.Services
{
    public interface IUsageService
    {
        Task<int> GetTodayQuestionCountAsync();
        Task IncrementQuestionCountAsync();
        Task RecordTokenUsageAsync(TokenUsageRecordInput input);
    }
}
