using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Models;
using ServiceLayer.Services;

namespace PresentationLayer.Pages.Reports;

[Authorize]
public class UsageModel : PageModel
{
    private static readonly int[] AllowedDayRanges = [7, 30, 90];
    private readonly IUsageReportService _usageReportService;

    public UsageModel(IUsageReportService usageReportService)
    {
        _usageReportService = usageReportService;
    }

    [BindProperty(SupportsGet = true)]
    public int Days { get; set; } = 30;

    public UsageReportDto Report { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Days = AllowedDayRanges.Contains(Days) ? Days : 30;
        var endDate = DateTime.UtcNow;
        var startDate = endDate.Date.AddDays(-(Days - 1));
        Report = await _usageReportService.GetUsageReportAsync(startDate, endDate);
    }
}
