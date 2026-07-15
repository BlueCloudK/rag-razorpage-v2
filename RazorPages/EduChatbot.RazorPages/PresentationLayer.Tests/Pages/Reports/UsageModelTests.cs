using System.Text;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Pages.Reports;
using ServiceLayer.Models;
using ServiceLayer.Services;

namespace PresentationLayer.Tests.Pages.Reports;

public class UsageModelTests
{
    [Fact]
    public async Task OnGetAsync_UsesSelectedSevenDayRangeAndReturnsReport()
    {
        var expected = CreateReport();
        var service = new RecordingUsageReportService(expected);
        var model = new UsageModel(service) { Days = 7 };

        await model.OnGetAsync();

        Assert.Equal(7, model.Days);
        Assert.Same(expected, model.Report);
        Assert.Equal(service.EndDate.Date.AddDays(-6), service.StartDate.Date);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(365)]
    public async Task OnGetAsync_InvalidRangeFallsBackToThirtyDays(int days)
    {
        var service = new RecordingUsageReportService(CreateReport());
        var model = new UsageModel(service) { Days = days };

        await model.OnGetAsync();

        Assert.Equal(30, model.Days);
        Assert.Equal(service.EndDate.Date.AddDays(-29), service.StartDate.Date);
    }

    [Fact]
    public async Task OnGetExportCsvAsync_ExportsPrivacySafeUserAndSubjectAggregates()
    {
        var service = new RecordingUsageReportService(CreateReport());
        var model = new UsageModel(service) { Days = 90 };

        var result = Assert.IsType<FileContentResult>(await model.OnGetExportCsvAsync());
        var csv = Encoding.UTF8.GetString(result.FileContents).TrimStart('\uFEFF');

        Assert.Equal("text/csv; charset=utf-8", result.ContentType);
        Assert.Matches(@"^usage-report-90-days-\d{8}\.csv$", result.FileDownloadName);
        Assert.Contains("\"User\",\"Nguyen, \"\"Hung\"\"\",\"hung@example.com\",\"Student\",\"3\",\"120\",\"240\",\"80\",\"440\",\"\"", csv);
        Assert.Contains("\"Subject\",\"Software Modeling\",\"PRN222\",\"\",\"4\",\"\",\"\",\"\",\"950\",\"2\"", csv);
        Assert.DoesNotContain("question text", csv, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(service.EndDate.Date.AddDays(-89), service.StartDate.Date);
    }

    private static UsageReportDto CreateReport()
    {
        return new UsageReportDto
        {
            ScopeKind = "organization",
            UserUsages =
            {
                new UserTokenUsageDto
                {
                    UserId = "student-1",
                    UserName = "Nguyen, \"Hung\"",
                    Email = "hung@example.com",
                    SystemRole = "Student",
                    QuestionCount = 3,
                    InputTokens = 120,
                    RetrievedContextTokens = 240,
                    OutputTokens = 80,
                    TotalTokens = 440
                }
            },
            SubjectUsages =
            {
                new SubjectTokenUsageDto
                {
                    SubjectId = 1,
                    SubjectName = "Software Modeling",
                    SubjectCode = "PRN222",
                    QuestionCount = 4,
                    TotalTokens = 950,
                    IndexedDocumentCount = 2
                }
            }
        };
    }

    private sealed class RecordingUsageReportService : IUsageReportService
    {
        private readonly UsageReportDto _report;

        public RecordingUsageReportService(UsageReportDto report)
        {
            _report = report;
        }

        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        public Task<UsageReportDto> GetUsageReportAsync(DateTime startDate, DateTime endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
            return Task.FromResult(_report);
        }
    }
}
