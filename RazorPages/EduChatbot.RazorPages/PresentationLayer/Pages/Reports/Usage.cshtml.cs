using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Dtos;
using ServiceLayer.Services;

namespace PresentationLayer.Pages.Reports;

[Authorize(Roles = AuthConstants.Admin + "," + AuthConstants.Lecturer)]
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

    [BindProperty(SupportsGet = true)]
    public bool Demo { get; set; }

    public UsageReportDto Report { get; private set; } = new();

    public Task<IActionResult> OnGetAsync()
    {
        Days = AllowedDayRanges.Contains(Days) ? Days : 30;
        return Task.FromResult<IActionResult>(RedirectToPage("/Reports/Index", new { Days, Demo }));
    }

    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        await LoadReportAsync();

        var csv = new StringBuilder();
        AppendCsvRow(csv,
            "Report type",
            "Name",
            "Code or email",
            "Subject",
            "Role",
            "Questions",
            "Input tokens",
            "Context tokens",
            "Output tokens",
            "Total tokens",
            "Indexed documents");

        if (Report.ScopeKind == "organization")
        {
            foreach (var user in Report.UserUsages)
            {
                AppendCsvRow(csv,
                    "User",
                    user.UserName,
                    user.Email,
                    null,
                    user.SystemRole,
                    user.QuestionCount,
                    user.InputTokens,
                    user.RetrievedContextTokens,
                    user.OutputTokens,
                    user.TotalTokens,
                    null);
            }
        }

        if (Report.ScopeKind == "teaching")
        {
            foreach (var learner in Report.LearnerUsages)
            {
                AppendCsvRow(csv,
                    "Learner",
                    learner.UserName,
                    learner.Email,
                    string.IsNullOrWhiteSpace(learner.SubjectCode)
                        ? learner.SubjectName
                        : $"{learner.SubjectName} ({learner.SubjectCode})",
                    "Student",
                    learner.QuestionCount,
                    null,
                    null,
                    null,
                    learner.TotalTokens,
                    null);
            }
        }

        foreach (var subject in Report.SubjectUsages)
        {
            AppendCsvRow(csv,
                "Subject",
                subject.SubjectName,
                subject.SubjectCode,
                null,
                null,
                subject.QuestionCount,
                null,
                null,
                null,
                subject.TotalTokens,
                subject.IndexedDocumentCount);
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(csv.ToString());
        var fileContents = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, fileContents, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, fileContents, preamble.Length, content.Length);

        return File(
            fileContents,
            "text/csv; charset=utf-8",
            $"usage-report-{Days}-days-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private async Task LoadReportAsync()
    {
        Days = AllowedDayRanges.Contains(Days) ? Days : 30;
        var endDate = DateTime.UtcNow;
        var startDate = endDate.Date.AddDays(-(Days - 1));
        Report = await _usageReportService.GetUsageReportAsync(startDate, endDate, Demo);
    }

    private static void AppendCsvRow(StringBuilder csv, params object?[] values)
    {
        csv.AppendLine(string.Join(',', values.Select(ToCsvValue)));
    }

    private static string ToCsvValue(object? value)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }
}
