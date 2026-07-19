using System;
using System.Threading.Tasks;
using ServiceLayer.Dtos;

namespace ServiceLayer.Services
{
    public interface IUsageReportService
    {
        Task<UsageReportDto> GetUsageReportAsync(DateTime startDate, DateTime endDate, bool useDemoData = false);
    }
}
