# Task: HungUniverse - Detailed Usage Report UI

## Required branch

Create and work only on this branch:

```powershell
git checkout main
git pull origin main
git checkout -b hung-universe
```

Wait until the `tecookie` backend branch has been merged into `main`, then create this branch from the updated `main`.

## Goal

Create the detailed report page at `/Reports/Usage`. It complements the overview dashboard and lets users inspect aggregate usage without exposing any chat message content.

## Scope

- Create `PresentationLayer/Pages/Reports/Usage.cshtml` and `Usage.cshtml.cs`.
- Use only `IUsageReportService` from `ServiceLayer`; no direct EF/DbContext usage in `PresentationLayer`.
- Add a 7 / 30 / 90-day filter using query string `?days=`.
- Show a table of token usage by user:
  - user name/email;
  - system role;
  - question count;
  - input/context/output/total tokens.
- Show a table of activity by subject:
  - subject name/code;
  - question count;
  - total tokens;
  - indexed document count.
- Add a CSV export action using data returned by `IUsageReportService`.
- Add a visible link back to `/Reports`.
- Create scoped styling in `Pages/Reports/Usage.cshtml.css`.

## Permission expectations

- Student sees only their own row and subjects they can access.
- Lecturer sees only subjects they teach or lead, plus associated aggregate users.
- Admin sees organization-wide aggregate data.
- Never show question text, AI answer text, full prompt, or source chunk content.

## Do not change

- Token tracking schema and calculation logic.
- `UsageReportService` access rules.
- Existing admin activity logs.
- Billing/subscription behavior.

## Verification

```powershell
dotnet build D:\Project\rag-razorpage-v2\RazorPages\EduChatbot.RazorPages\EduChatbot.RazorPages.slnx
```

Manually check a table with no data, one row, and long email values on desktop/mobile widths.

## Deliverable

- Push branch `hung-universe`.
- Open a PR into `main`.
- PR description: changed files, screenshots, role-scope checks, build result.
