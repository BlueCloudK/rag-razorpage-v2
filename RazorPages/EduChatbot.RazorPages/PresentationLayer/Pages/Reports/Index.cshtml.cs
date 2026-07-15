using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Models;
using ServiceLayer.Services;

namespace PresentationLayer.Pages.Reports;

[Authorize(Roles = AuthConstants.Admin + "," + AuthConstants.Lecturer)]
public class IndexModel : PageModel
{
    private readonly IUsageReportService _usageReportService;

    public IndexModel(IUsageReportService usageReportService)
    {
        _usageReportService = usageReportService;
    }

    public UsageReportDto Report { get; private set; } = new();

    public int MaxDailyTokens => Math.Max(Report.DailyUsages.DefaultIfEmpty().Max(day => day?.TotalTokens ?? 0), 1);

    public bool HasUsage => Report.CompletedQuestionCount > 0 || Report.TotalTokens > 0;

    public async Task OnGetAsync()
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.Date.AddDays(-29);
        Report = await _usageReportService.GetUsageReportAsync(startDate, endDate);
    }
}
