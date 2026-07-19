using System.Threading.Tasks;
using ServiceLayer.Dtos;

namespace ServiceLayer.Services
{
    public interface IRagLabService
    {
        Task<RagLabDashboardDto> GetDashboardAsync();
    }
}
