using DataAccessLayer.Data;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Services;

namespace PresentationLayer.Tests.Services;

public class UsageReportServiceTests
{
    [Fact]
    public async Task GetUsageReportAsync_ProjectsDetailedUserAndSubjectAggregates()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);

        const string userId = "admin-1";
        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            FullName = "Ada Administrator",
            Email = "ada@example.edu",
            UserName = "ada@example.edu"
        });
        context.Roles.Add(new IdentityRole
        {
            Id = "admin-role",
            Name = AuthConstants.Admin,
            NormalizedName = AuthConstants.Admin.ToUpperInvariant()
        });
        context.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = userId,
            RoleId = "admin-role"
        });
        context.Organizations.Add(new Organization
        {
            Id = 10,
            Name = "Example University",
            Slug = "example-university",
            IsActive = true
        });
        context.OrganizationMembers.Add(new OrganizationMember
        {
            Id = 20,
            OrganizationId = 10,
            UserId = userId,
            RoleInOrganization = AuthConstants.Admin
        });
        context.Subjects.Add(new Subject
        {
            Id = 30,
            OrganizationId = 10,
            Name = "Software Modeling",
            Code = "PRN222"
        });
        context.Documents.AddRange(
            CreateDocument(1, 30, true),
            CreateDocument(2, 30, true),
            CreateDocument(3, 30, false));
        context.TokenUsages.AddRange(
            CreateUsage(1, userId, 10, 30, 100, 300, 40),
            CreateUsage(2, userId, 10, 30, 50, 200, 30));
        await context.SaveChangesAsync();

        var service = new UsageReportService(
            context,
            new FixedCurrentUserService(userId, AuthConstants.Admin));

        var report = await service.GetUsageReportAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        var user = Assert.Single(report.UserUsages);
        Assert.Equal("Ada Administrator", user.UserName);
        Assert.Equal("ada@example.edu", user.Email);
        Assert.Equal(AuthConstants.Admin, user.SystemRole);
        Assert.Equal(2, user.QuestionCount);
        Assert.Equal(150, user.InputTokens);
        Assert.Equal(500, user.RetrievedContextTokens);
        Assert.Equal(70, user.OutputTokens);
        Assert.Equal(720, user.TotalTokens);

        var subject = Assert.Single(report.SubjectUsages);
        Assert.Equal("Software Modeling", subject.SubjectName);
        Assert.Equal("PRN222", subject.SubjectCode);
        Assert.Equal(2, subject.QuestionCount);
        Assert.Equal(720, subject.TotalTokens);
        Assert.Equal(2, subject.IndexedDocumentCount);
    }

    [Fact]
    public async Task GetUsageReportAsync_StudentReceivesOnlyOwnUsage()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);

        context.Users.AddRange(
            CreateUser("student-1", "Student One"),
            CreateUser("student-2", "Student Two"));
        context.TokenUsages.AddRange(
            CreateUsage(1, "student-1", null, null, 10, 20, 5),
            CreateUsage(2, "student-2", null, null, 100, 200, 50));
        await context.SaveChangesAsync();

        var service = new UsageReportService(
            context,
            new FixedCurrentUserService("student-1", AuthConstants.Student));

        var report = await service.GetUsageReportAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        var user = Assert.Single(report.UserUsages);
        Assert.Equal("student-1", user.UserId);
        Assert.Equal(35, report.TotalTokens);
    }

    [Fact]
    public async Task GetUsageReportAsync_LecturerReceivesOnlyAssignedSubjectUsage()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);

        context.Users.AddRange(
            CreateUser("lecturer-1", "Lecturer"),
            CreateUser("student-1", "Student One"),
            CreateUser("student-2", "Student Two"));
        context.Subjects.AddRange(
            new Subject { Id = 1, Name = "Assigned", Code = "ASSIGNED" },
            new Subject { Id = 2, Name = "Not assigned", Code = "PRIVATE" });
        context.SubjectMemberships.Add(new SubjectMembership
        {
            Id = 1,
            SubjectId = 1,
            UserId = "lecturer-1",
            RoleInSubject = AuthConstants.Lecturer
        });
        context.TokenUsages.AddRange(
            CreateUsage(1, "student-1", null, 1, 10, 20, 5),
            CreateUsage(2, "student-2", null, 2, 100, 200, 50));
        await context.SaveChangesAsync();

        var service = new UsageReportService(
            context,
            new FixedCurrentUserService("lecturer-1", AuthConstants.Lecturer));

        var report = await service.GetUsageReportAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        Assert.Equal("student-1", Assert.Single(report.UserUsages).UserId);
        Assert.Equal(1, Assert.Single(report.SubjectUsages).SubjectId);
        Assert.Equal(35, report.TotalTokens);
    }

    private static Document CreateDocument(int id, int subjectId, bool isIndexed)
    {
        return new Document
        {
            Id = id,
            SubjectId = subjectId,
            FileName = $"document-{id}.pdf",
            FilePath = $"/documents/document-{id}.pdf",
            IsIndexed = isIndexed
        };
    }

    private static ApplicationUser CreateUser(string id, string fullName)
    {
        var email = $"{id}@example.edu";
        return new ApplicationUser
        {
            Id = id,
            FullName = fullName,
            Email = email,
            UserName = email
        };
    }

    private static TokenUsage CreateUsage(
        int id,
        string userId,
        int? organizationId,
        int? subjectId,
        int inputTokens,
        int contextTokens,
        int outputTokens)
    {
        return new TokenUsage
        {
            Id = id,
            UserId = userId,
            OrganizationId = organizationId,
            SubjectId = subjectId,
            InputTokens = inputTokens,
            RetrievedContextTokens = contextTokens,
            OutputTokens = outputTokens,
            TotalTokens = inputTokens + contextTokens + outputTokens,
            ModelName = "test-model",
            IsEstimated = true,
            Timestamp = DateTime.UtcNow.AddDays(-1)
        };
    }

    private sealed class FixedCurrentUserService : ICurrentUserService
    {
        private readonly string _role;

        public FixedCurrentUserService(string userId, string role)
        {
            UserId = userId;
            _role = role;
        }

        public string? UserId { get; }
        public bool IsAuthenticated => true;

        public Task<ApplicationUser?> GetCurrentUserAsync()
        {
            return Task.FromResult<ApplicationUser?>(null);
        }

        public Task<bool> IsInRoleAsync(string role)
        {
            return Task.FromResult(string.Equals(role, _role, StringComparison.Ordinal));
        }
    }
}
