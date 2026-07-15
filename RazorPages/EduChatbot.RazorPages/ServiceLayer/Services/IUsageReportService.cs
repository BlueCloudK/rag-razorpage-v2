using System;
using System.Threading.Tasks;
using ServiceLayer.Models;

namespace ServiceLayer.Services
{
    public interface IUsageReportService
    {
        Task<UsageReportDto> GetUsageReportAsync(DateTime startDate, DateTime endDate);
    }
}
