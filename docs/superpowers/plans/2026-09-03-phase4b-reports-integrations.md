# Phase 4B — Reports & Integrations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add reporting with export (CSV/Excel/PDF), enhanced management dashboards with charts, and outbound webhook + ERP integration capabilities.

**Architecture:** Real-time EF Core aggregation queries feed MediatR handlers exposed via REST API. Angular frontend uses ngx-charts for visualization. Export service uses ClosedXML (Excel) and QuestPDF (PDF). Outbound webhooks dispatch via MediatR notification handlers with HMAC-SHA256 signing and in-process retry.

**Tech Stack:** .NET 10, EF Core 10, Angular 20, Material 20, MediatR, FluentValidation, ngx-charts, ClosedXML, QuestPDF

**Spec:** `docs/superpowers/specs/2026-09-03-phase4b-reports-integrations-design.md`

## Global Constraints

- .NET 10, EF Core 10, Angular 20, Material 20
- MediatR CQRS: `IRequest<T>` + Handler co-located in same file
- FluentValidation for all command/query validators
- Multi-tenant: all new entities implement `ITenantEntity`; global query filters via AppDbContext
- Bilingual: `Name`/`NameAr` pairs; Angular uses `@ngx-translate/core` with `en.json`/`ar.json`
- Angular patterns: standalone components, `inject()`, `@for`/`@if` control flow, extends `ApiService`
- Auth: JWT Bearer with `[Authorize(Policy = "Permission:xxx")]`
- No new background-job framework — in-process `Task.Delay` for retries
- Charting: `@swimlane/ngx-charts` for all frontend charts
- Export: ClosedXML for Excel, QuestPDF for PDF, StreamWriter for CSV
- Real-time queries: all report data computed live (no materialized views)
- File-scoped namespaces throughout
- Positional `record` types for DTOs and commands/queries

---

### Task 1: Reports Domain Foundation

**Files:**
- Create: `src/CustomerSupport.Domain/Entities/TicketFeedback.cs`
- Create: `src/CustomerSupport.Domain/Interfaces/IReportExportService.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/TicketFeedbackConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Reports/ReportExportService.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs` — add DbSets
- Modify: `src/CustomerSupport.Infrastructure/CustomerSupport.Infrastructure.csproj` — add NuGet packages
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs` — register IReportExportService
- Modify: `src/CustomerSupport.Infrastructure/Persistence/Seeders/PermissionSeeder.cs` — add report/integration permissions
- Modify: `src/CustomerSupport.Infrastructure/Persistence/Seeders/RoleAndUserSeeder.cs` — add reports.view to AgentPermissions

**Interfaces:**
- Consumes: `BaseEntity` from `CustomerSupport.Domain.Entities`, `ITenantEntity` from `CustomerSupport.Domain.Interfaces`
- Produces:
  - `TicketFeedback` entity class (used by Task 2 queries and Task 4 portal UI)
  - `IReportExportService` interface with methods `ExportCsvAsync(ReportExportData)`, `ExportExcelAsync(ReportExportData)`, `ExportPdfAsync(ReportExportData)` — all return `Task<byte[]>`
  - `ReportExportData` record: `(string Title, string TitleAr, string[] ColumnHeaders, string[] ColumnHeadersAr, List<string[]> Rows, DateTime GeneratedAt, string? DateRange = null)`
  - Permissions: `reports.view`, `reports.export`, `integrations.view`, `integrations.manage`

- [ ] **Step 1: Add NuGet packages to Infrastructure project**

Edit `src/CustomerSupport.Infrastructure/CustomerSupport.Infrastructure.csproj` and add after the existing `Azure.AI.OpenAI` package reference:

```xml
    <PackageReference Include="ClosedXML" Version="0.104.2" />
    <PackageReference Include="QuestPDF" Version="2024.12.3" />
```

- [ ] **Step 2: Restore NuGet packages**

Run:
```bash
dotnet restore src/CustomerSupport.Infrastructure/CustomerSupport.Infrastructure.csproj
```

Expected: packages restore successfully.

- [ ] **Step 3: Create TicketFeedback entity**

Create `src/CustomerSupport.Domain/Entities/TicketFeedback.cs`:

```csharp
namespace CustomerSupport.Domain.Entities;

public class TicketFeedback : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TicketId { get; set; }
    public Guid CustomerId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime SubmittedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}
```

- [ ] **Step 4: Create TicketFeedback EF configuration**

Create `src/CustomerSupport.Infrastructure/Persistence/Configurations/TicketFeedbackConfiguration.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class TicketFeedbackConfiguration : IEntityTypeConfiguration<TicketFeedback>
{
    public void Configure(EntityTypeBuilder<TicketFeedback> builder)
    {
        builder.ToTable("TicketFeedbacks");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Rating).IsRequired();
        builder.Property(f => f.Comment).HasMaxLength(1000);
        builder.Property(f => f.SubmittedAt).IsRequired();

        builder.HasOne(f => f.Ticket).WithMany().HasForeignKey(f => f.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(f => f.Customer).WithMany().HasForeignKey(f => f.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.TenantId, f.TicketId }).IsUnique();
        builder.HasIndex(f => new { f.TenantId, f.SubmittedAt });
    }
}
```

- [ ] **Step 5: Add DbSet to AppDbContext**

In `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs`, add after the `AiSuggestions` DbSet line:

```csharp
    public DbSet<TicketFeedback> TicketFeedbacks => Set<TicketFeedback>();
```

- [ ] **Step 6: Create IReportExportService interface**

Create `src/CustomerSupport.Domain/Interfaces/IReportExportService.cs`:

```csharp
namespace CustomerSupport.Domain.Interfaces;

public interface IReportExportService
{
    Task<byte[]> ExportCsvAsync(ReportExportData data);
    Task<byte[]> ExportExcelAsync(ReportExportData data);
    Task<byte[]> ExportPdfAsync(ReportExportData data);
}

public record ReportExportData(
    string Title,
    string TitleAr,
    string[] ColumnHeaders,
    string[] ColumnHeadersAr,
    List<string[]> Rows,
    DateTime GeneratedAt,
    string? DateRange = null);
```

- [ ] **Step 7: Implement ReportExportService**

Create `src/CustomerSupport.Infrastructure/Services/Reports/ReportExportService.cs`:

```csharp
using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CustomerSupport.Domain.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CustomerSupport.Infrastructure.Services.Reports;

public class ReportExportService : IReportExportService
{
    public Task<byte[]> ExportCsvAsync(ReportExportData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", data.ColumnHeaders.Select(EscapeCsvField)));
        foreach (var row in data.Rows)
            sb.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
        return Task.FromResult(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray());
    }

    public Task<byte[]> ExportExcelAsync(ReportExportData data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(Truncate(data.Title, 31));

        for (var col = 0; col < data.ColumnHeaders.Length; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = data.ColumnHeaders[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (var row = 0; row < data.Rows.Count; row++)
        {
            for (var col = 0; col < data.Rows[row].Length; col++)
                worksheet.Cell(row + 2, col + 1).Value = data.Rows[row][col];
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    public Task<byte[]> ExportPdfAsync(ReportExportData data)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);

                page.Header().Column(col =>
                {
                    col.Item().Text(data.Title).FontSize(18).Bold();
                    if (data.DateRange is not null)
                        col.Item().Text($"Period: {data.DateRange}").FontSize(10).Italic();
                    col.Item().Text($"Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm} UTC").FontSize(9);
                    col.Item().PaddingBottom(10);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (var i = 0; i < data.ColumnHeaders.Length; i++)
                            columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var h in data.ColumnHeaders)
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                                .Text(h).Bold().FontSize(9);
                        }
                    });

                    foreach (var row in data.Rows)
                    {
                        foreach (var cell in row)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .Padding(4).Text(cell ?? "").FontSize(9);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });

        using var ms = new MemoryStream();
        document.GeneratePdf(ms);
        return Task.FromResult(ms.ToArray());
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
```

- [ ] **Step 8: Register IReportExportService in DI**

In `src/CustomerSupport.Infrastructure/DependencyInjection.cs`, add after the `services.AddScoped<IAiChatbotService, AiChatbotService>();` line:

```csharp
        // Report Services
        services.AddScoped<IReportExportService, ReportExportService>();
```

Also add this using at the top of the file:

```csharp
using CustomerSupport.Infrastructure.Services.Reports;
```

- [ ] **Step 9: Add new permissions to PermissionSeeder**

In `src/CustomerSupport.Infrastructure/Persistence/Seeders/PermissionSeeder.cs`, add these entries to the `AllPermissions` array after the `("ai.manage", "AI", "Configure AI settings")` entry:

```csharp
        ("reports.export", "Reports", "Export reports to CSV/Excel/PDF"),
        ("integrations.view", "Integrations", "View integration configurations"),
        ("integrations.manage", "Integrations", "Manage webhook subscriptions and ERP settings"),
```

Note: `("reports.view", "Reports", "View reports")` already exists in the seeder.

- [ ] **Step 10: Add reports.view to AgentPermissions**

In `src/CustomerSupport.Infrastructure/Persistence/Seeders/RoleAndUserSeeder.cs`, add `"reports.view"` to the `AgentPermissions` array:

```csharp
    private static readonly string[] AgentPermissions =
    [
        "tickets.view", "tickets.create", "tickets.edit", "tickets.assign",
        "customers.view", "knowledgebase.view", "dashboard.view", "assignment.view",
        "conversations.view", "conversations.manage", "chat.view", "notifications.view",
        "ai.use", "reports.view"
    ];
```

- [ ] **Step 11: Create EF migration for TicketFeedback**

Run:
```bash
dotnet ef migrations add AddTicketFeedback --project src/CustomerSupport.Infrastructure --startup-project src/CustomerSupport.API --output-dir Persistence/Migrations
```

Expected: migration generated with CreateTable for TicketFeedbacks.

- [ ] **Step 12: Apply migration**

Run:
```bash
dotnet ef database update --project src/CustomerSupport.Infrastructure --startup-project src/CustomerSupport.API
```

- [ ] **Step 13: Build and verify**

Run:
```bash
dotnet build src/CustomerSupport.API
```

Expected: build succeeds with no errors.

- [ ] **Step 14: Commit**

```bash
git add -A
git commit -m "feat(reports): add TicketFeedback entity, export service (CSV/Excel/PDF), and report permissions"
```

---

### Task 2: Report Query Handlers and API

**Files:**
- Create: `src/CustomerSupport.Application/Reports/DTOs/DateRangeFilter.cs`
- Create: `src/CustomerSupport.Application/Reports/DTOs/TicketVolumeReportDto.cs`
- Create: `src/CustomerSupport.Application/Reports/DTOs/SlaPerformanceReportDto.cs`
- Create: `src/CustomerSupport.Application/Reports/DTOs/AgentPerformanceReportDto.cs`
- Create: `src/CustomerSupport.Application/Reports/DTOs/ChannelAnalyticsReportDto.cs`
- Create: `src/CustomerSupport.Application/Reports/DTOs/AiUsageReportDto.cs`
- Create: `src/CustomerSupport.Application/Reports/DTOs/CsatReportDto.cs`
- Create: `src/CustomerSupport.Application/Reports/Queries/GetTicketVolumeReportQuery.cs`
- Create: `src/CustomerSupport.Application/Reports/Queries/GetSlaPerformanceReportQuery.cs`
- Create: `src/CustomerSupport.Application/Reports/Queries/GetAgentPerformanceReportQuery.cs`
- Create: `src/CustomerSupport.Application/Reports/Queries/GetChannelAnalyticsReportQuery.cs`
- Create: `src/CustomerSupport.Application/Reports/Queries/GetAiUsageReportQuery.cs`
- Create: `src/CustomerSupport.Application/Reports/Queries/GetCsatReportQuery.cs`
- Create: `src/CustomerSupport.Application/Reports/Validators/TicketVolumeReportValidator.cs`
- Create: `src/CustomerSupport.Application/Portal/Commands/SubmitTicketFeedbackCommand.cs`
- Create: `src/CustomerSupport.Application/Portal/Validators/SubmitTicketFeedbackValidator.cs`
- Create: `src/CustomerSupport.API/Controllers/ReportsController.cs`
- Modify: `src/CustomerSupport.API/Controllers/PortalController.cs` — add feedback endpoint

**Interfaces:**
- Consumes: `TicketFeedback` entity (Task 1), `IReportExportService` + `ReportExportData` (Task 1), `AppDbContext`, `ICurrentUserService`, `IDateTimeService`
- Produces:
  - `DateRangeFilter` record: `(DateTime StartDate, DateTime EndDate)`
  - All 6 report DTO types (used by Task 3-5 Angular services)
  - `SubmitTicketFeedbackCommand(Guid TicketId, Guid CustomerId, Guid TenantId, int Rating, string? Comment)` → `Result`
  - `ReportsController` REST endpoints at `api/v1/reports/*`

- [ ] **Step 1: Create DateRangeFilter shared record**

Create `src/CustomerSupport.Application/Reports/DTOs/DateRangeFilter.cs`:

```csharp
namespace CustomerSupport.Application.Reports.DTOs;

public record DateRangeFilter(DateTime StartDate, DateTime EndDate);
```

- [ ] **Step 2: Create TicketVolumeReportDto**

Create `src/CustomerSupport.Application/Reports/DTOs/TicketVolumeReportDto.cs`:

```csharp
namespace CustomerSupport.Application.Reports.DTOs;

public record TicketVolumeReportDto(
    List<TimeSeriesPoint> TimeSeries,
    List<CategoryBreakdownItem> CategoryBreakdown,
    List<PriorityBreakdownItem> PriorityBreakdown,
    int TotalCreated,
    int TotalResolved);

public record TimeSeriesPoint(string Period, int CreatedCount, int ResolvedCount);
public record CategoryBreakdownItem(string CategoryName, string CategoryNameAr, int Count);
public record PriorityBreakdownItem(string PriorityName, string PriorityNameAr, int Count);
```

- [ ] **Step 3: Create SlaPerformanceReportDto**

Create `src/CustomerSupport.Application/Reports/DTOs/SlaPerformanceReportDto.cs`:

```csharp
namespace CustomerSupport.Application.Reports.DTOs;

public record SlaPerformanceReportDto(
    double OverallFirstResponseCompliance,
    double OverallResolutionCompliance,
    List<SlaTimeSeriesPoint> TimeSeries,
    List<SlaBreachDetailItem> BreachDetails);

public record SlaTimeSeriesPoint(string Period, int FirstResponseOnTime, int FirstResponseBreached, int ResolutionOnTime, int ResolutionBreached);
public record SlaBreachDetailItem(Guid TicketId, string TicketNumber, string BreachType, string PolicyName, DateTime DueAt, DateTime BreachedAt, double MinutesLate);
```

- [ ] **Step 4: Create AgentPerformanceReportDto**

Create `src/CustomerSupport.Application/Reports/DTOs/AgentPerformanceReportDto.cs`:

```csharp
namespace CustomerSupport.Application.Reports.DTOs;

public record AgentPerformanceReportDto(
    List<AgentPerformanceItem> Agents,
    AgentPerformanceTopPerformer? TopPerformer);

public record AgentPerformanceItem(
    Guid AgentId, string AgentName, int TicketsHandled, int TicketsResolved,
    double AvgResolutionMinutes, double AvgFirstResponseMinutes, double SlaCompliancePercent);

public record AgentPerformanceTopPerformer(Guid AgentId, string AgentName);
```

- [ ] **Step 5: Create ChannelAnalyticsReportDto**

Create `src/CustomerSupport.Application/Reports/DTOs/ChannelAnalyticsReportDto.cs`:

```csharp
namespace CustomerSupport.Application.Reports.DTOs;

public record ChannelAnalyticsReportDto(
    List<ChannelBreakdownItem> ChannelBreakdown,
    List<ChannelTimeSeriesPoint> TimeSeries);

public record ChannelBreakdownItem(string Channel, int ConversationCount, int MessageCount, double AvgResponseMinutes);
public record ChannelTimeSeriesPoint(string Period, string Channel, int ConversationCount);
```

- [ ] **Step 6: Create AiUsageReportDto**

Create `src/CustomerSupport.Application/Reports/DTOs/AiUsageReportDto.cs`:

```csharp
namespace CustomerSupport.Application.Reports.DTOs;

public record AiUsageReportDto(
    List<AiSuggestionTypeBreakdown> SuggestionsByType,
    double AvgConfidence,
    int TotalTokensUsed,
    List<AiUsageTimeSeriesPoint> TimeSeries);

public record AiSuggestionTypeBreakdown(
    string Type, int TotalCount, int AcceptedCount, int RejectedCount,
    int PendingCount, double AcceptanceRate);

public record AiUsageTimeSeriesPoint(string Period, int SuggestionCount, double AcceptanceRate);
```

- [ ] **Step 7: Create CsatReportDto**

Create `src/CustomerSupport.Application/Reports/DTOs/CsatReportDto.cs`:

```csharp
namespace CustomerSupport.Application.Reports.DTOs;

public record CsatReportDto(
    double AverageRating,
    int TotalResponses,
    List<RatingDistributionItem> RatingDistribution,
    List<RecentFeedbackItem> RecentFeedback,
    List<CsatTimeSeriesPoint> TimeSeries);

public record RatingDistributionItem(int Rating, int Count, double Percentage);
public record RecentFeedbackItem(Guid TicketId, string TicketNumber, string CustomerName, int Rating, string? Comment, DateTime SubmittedAt);
public record CsatTimeSeriesPoint(string Period, double AverageRating, int ResponseCount);
```

- [ ] **Step 8: Create GetTicketVolumeReportQuery**

Create `src/CustomerSupport.Application/Reports/Queries/GetTicketVolumeReportQuery.cs`:

```csharp
using CustomerSupport.Application.Reports.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Reports.Queries;

public record GetTicketVolumeReportQuery(
    DateTime StartDate, DateTime EndDate, string GroupBy = "Day",
    Guid? CategoryId = null, Guid? PriorityId = null,
    Guid? StatusId = null, Guid? AssignedToId = null) : IRequest<TicketVolumeReportDto>;

public class GetTicketVolumeReportQueryHandler : IRequestHandler<GetTicketVolumeReportQuery, TicketVolumeReportDto>
{
    private readonly AppDbContext _context;

    public GetTicketVolumeReportQueryHandler(AppDbContext context) => _context = context;

    public async Task<TicketVolumeReportDto> Handle(GetTicketVolumeReportQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Tickets
            .Where(t => t.CreatedAt >= request.StartDate && t.CreatedAt <= request.EndDate);

        if (request.CategoryId.HasValue) query = query.Where(t => t.CategoryId == request.CategoryId.Value);
        if (request.PriorityId.HasValue) query = query.Where(t => t.PriorityId == request.PriorityId.Value);
        if (request.StatusId.HasValue) query = query.Where(t => t.StatusId == request.StatusId.Value);
        if (request.AssignedToId.HasValue) query = query.Where(t => t.AssignedToId == request.AssignedToId.Value);

        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        var tickets = await query.Select(t => new
        {
            t.CreatedAt,
            t.StatusId,
            t.CategoryId,
            CategoryName = t.Category!.Name,
            CategoryNameAr = t.Category!.NameAr,
            t.PriorityId,
            PriorityName = t.Priority!.Name,
            PriorityNameAr = t.Priority!.NameAr
        }).ToListAsync(cancellationToken);

        var timeSeries = tickets
            .GroupBy(t => FormatPeriod(t.CreatedAt, request.GroupBy))
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesPoint(
                g.Key,
                g.Count(),
                g.Count(t => finalStatusIds.Contains(t.StatusId))))
            .ToList();

        var categoryBreakdown = tickets
            .GroupBy(t => new { t.CategoryName, t.CategoryNameAr })
            .Select(g => new CategoryBreakdownItem(g.Key.CategoryName, g.Key.CategoryNameAr, g.Count()))
            .OrderByDescending(c => c.Count)
            .ToList();

        var priorityBreakdown = tickets
            .GroupBy(t => new { t.PriorityName, t.PriorityNameAr })
            .Select(g => new PriorityBreakdownItem(g.Key.PriorityName, g.Key.PriorityNameAr, g.Count()))
            .OrderByDescending(p => p.Count)
            .ToList();

        return new TicketVolumeReportDto(
            timeSeries, categoryBreakdown, priorityBreakdown,
            tickets.Count,
            tickets.Count(t => finalStatusIds.Contains(t.StatusId)));
    }

    private static string FormatPeriod(DateTime date, string groupBy) => groupBy switch
    {
        "Week" => $"{date.Year}-W{System.Globalization.ISOWeek.GetWeekOfYear(date):D2}",
        "Month" => date.ToString("yyyy-MM"),
        _ => date.ToString("yyyy-MM-dd")
    };
}
```

- [ ] **Step 9: Create GetSlaPerformanceReportQuery**

Create `src/CustomerSupport.Application/Reports/Queries/GetSlaPerformanceReportQuery.cs`:

```csharp
using CustomerSupport.Application.Reports.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Reports.Queries;

public record GetSlaPerformanceReportQuery(
    DateTime StartDate, DateTime EndDate,
    Guid? PriorityId = null, Guid? CategoryId = null) : IRequest<SlaPerformanceReportDto>;

public class GetSlaPerformanceReportQueryHandler : IRequestHandler<GetSlaPerformanceReportQuery, SlaPerformanceReportDto>
{
    private readonly AppDbContext _context;

    public GetSlaPerformanceReportQueryHandler(AppDbContext context) => _context = context;

    public async Task<SlaPerformanceReportDto> Handle(GetSlaPerformanceReportQuery request, CancellationToken cancellationToken)
    {
        var slaQuery = _context.TicketSlas
            .Include(ts => ts.Ticket)
            .Where(ts => ts.CreatedAt >= request.StartDate && ts.CreatedAt <= request.EndDate);

        if (request.PriorityId.HasValue)
            slaQuery = slaQuery.Where(ts => ts.Ticket.PriorityId == request.PriorityId.Value);
        if (request.CategoryId.HasValue)
            slaQuery = slaQuery.Where(ts => ts.Ticket.CategoryId == request.CategoryId.Value);

        var slaRecords = await slaQuery.Select(ts => new
        {
            ts.CreatedAt,
            ts.FirstResponseBreached,
            ts.ResolutionBreached,
            ts.FirstRespondedAt,
            ts.ResolvedAt
        }).ToListAsync(cancellationToken);

        var frResolved = slaRecords.Where(s => s.FirstRespondedAt.HasValue).ToList();
        var resResolved = slaRecords.Where(s => s.ResolvedAt.HasValue).ToList();

        var frOnTime = frResolved.Count(s => !s.FirstResponseBreached);
        var frBreached = frResolved.Count(s => s.FirstResponseBreached);
        var resOnTime = resResolved.Count(s => !s.ResolutionBreached);
        var resBreached = resResolved.Count(s => s.ResolutionBreached);

        var frTotal = frOnTime + frBreached;
        var resTotal = resOnTime + resBreached;

        var timeSeries = slaRecords
            .GroupBy(s => s.CreatedAt.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g => new SlaTimeSeriesPoint(
                g.Key,
                g.Count(s => s.FirstRespondedAt.HasValue && !s.FirstResponseBreached),
                g.Count(s => s.FirstResponseBreached),
                g.Count(s => s.ResolvedAt.HasValue && !s.ResolutionBreached),
                g.Count(s => s.ResolutionBreached)))
            .ToList();

        var breachDetails = await _context.SlaBreachLogs
            .Include(b => b.Ticket)
            .Include(b => b.SlaPolicy)
            .Where(b => b.CreatedAt >= request.StartDate && b.CreatedAt <= request.EndDate)
            .OrderByDescending(b => b.BreachedAt)
            .Take(50)
            .Select(b => new SlaBreachDetailItem(
                b.TicketId, b.Ticket.TicketNumber, b.BreachType,
                b.SlaPolicy.Name, b.DueAt, b.BreachedAt,
                (b.BreachedAt - b.DueAt).TotalMinutes))
            .ToListAsync(cancellationToken);

        return new SlaPerformanceReportDto(
            frTotal > 0 ? Math.Round(frOnTime * 100.0 / frTotal, 1) : 100,
            resTotal > 0 ? Math.Round(resOnTime * 100.0 / resTotal, 1) : 100,
            timeSeries, breachDetails);
    }
}
```

- [ ] **Step 10: Create GetAgentPerformanceReportQuery**

Create `src/CustomerSupport.Application/Reports/Queries/GetAgentPerformanceReportQuery.cs`:

```csharp
using CustomerSupport.Application.Reports.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Reports.Queries;

public record GetAgentPerformanceReportQuery(
    DateTime StartDate, DateTime EndDate,
    Guid? AgentId = null) : IRequest<AgentPerformanceReportDto>;

public class GetAgentPerformanceReportQueryHandler : IRequestHandler<GetAgentPerformanceReportQuery, AgentPerformanceReportDto>
{
    private readonly AppDbContext _context;

    public GetAgentPerformanceReportQueryHandler(AppDbContext context) => _context = context;

    public async Task<AgentPerformanceReportDto> Handle(GetAgentPerformanceReportQuery request, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        var ticketQuery = _context.Tickets
            .Where(t => t.AssignedToId.HasValue
                && t.CreatedAt >= request.StartDate && t.CreatedAt <= request.EndDate);

        if (request.AgentId.HasValue)
            ticketQuery = ticketQuery.Where(t => t.AssignedToId == request.AgentId.Value);

        var ticketData = await ticketQuery.Select(t => new
        {
            t.AssignedToId,
            AgentName = t.AssignedTo!.FullName,
            t.StatusId,
            t.CreatedAt,
            t.UpdatedAt
        }).ToListAsync(cancellationToken);

        var slaData = await _context.TicketSlas
            .Where(ts => ts.CreatedAt >= request.StartDate && ts.CreatedAt <= request.EndDate)
            .Select(ts => new
            {
                ts.Ticket.AssignedToId,
                ts.FirstResponseBreached,
                ts.ResolutionBreached,
                ts.FirstRespondedAt,
                ts.CreatedAt
            }).ToListAsync(cancellationToken);

        var agents = ticketData
            .GroupBy(t => new { t.AssignedToId, t.AgentName })
            .Select(g =>
            {
                var agentSla = slaData.Where(s => s.AssignedToId == g.Key.AssignedToId).ToList();
                var slaTotal = agentSla.Count;
                var slaOnTime = agentSla.Count(s => !s.FirstResponseBreached && !s.ResolutionBreached);
                var resolved = g.Where(t => finalStatusIds.Contains(t.StatusId)).ToList();
                var avgResolution = resolved.Count > 0
                    ? resolved.Average(t => (t.UpdatedAt - t.CreatedAt).TotalMinutes) : 0;
                var avgFirstResponse = agentSla.Where(s => s.FirstRespondedAt.HasValue).ToList();
                var avgFr = avgFirstResponse.Count > 0
                    ? avgFirstResponse.Average(s => (s.FirstRespondedAt!.Value - s.CreatedAt).TotalMinutes) : 0;

                return new AgentPerformanceItem(
                    g.Key.AssignedToId!.Value, g.Key.AgentName,
                    g.Count(), resolved.Count,
                    Math.Round(avgResolution, 1), Math.Round(avgFr, 1),
                    slaTotal > 0 ? Math.Round(slaOnTime * 100.0 / slaTotal, 1) : 100);
            })
            .OrderByDescending(a => a.SlaCompliancePercent)
            .ToList();

        var top = agents.FirstOrDefault();

        return new AgentPerformanceReportDto(
            agents,
            top is not null ? new AgentPerformanceTopPerformer(top.AgentId, top.AgentName) : null);
    }
}
```

- [ ] **Step 11: Create GetChannelAnalyticsReportQuery**

Create `src/CustomerSupport.Application/Reports/Queries/GetChannelAnalyticsReportQuery.cs`:

```csharp
using CustomerSupport.Application.Reports.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Reports.Queries;

public record GetChannelAnalyticsReportQuery(
    DateTime StartDate, DateTime EndDate) : IRequest<ChannelAnalyticsReportDto>;

public class GetChannelAnalyticsReportQueryHandler : IRequestHandler<GetChannelAnalyticsReportQuery, ChannelAnalyticsReportDto>
{
    private readonly AppDbContext _context;

    public GetChannelAnalyticsReportQueryHandler(AppDbContext context) => _context = context;

    public async Task<ChannelAnalyticsReportDto> Handle(GetChannelAnalyticsReportQuery request, CancellationToken cancellationToken)
    {
        var conversations = await _context.Conversations
            .Where(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate)
            .Select(c => new { c.Id, c.Channel, c.CreatedAt })
            .ToListAsync(cancellationToken);

        var messages = await _context.Messages
            .Where(m => m.CreatedAt >= request.StartDate && m.CreatedAt <= request.EndDate)
            .Select(m => new { m.ConversationId, m.Channel, m.Direction, m.SentAt, m.CreatedAt })
            .ToListAsync(cancellationToken);

        var channelBreakdown = conversations
            .GroupBy(c => c.Channel.ToString())
            .Select(g =>
            {
                var convIds = g.Select(c => c.Id).ToHashSet();
                var channelMessages = messages.Where(m => convIds.Contains(m.ConversationId)).ToList();
                var inbound = channelMessages.Where(m => m.Direction == Domain.Enums.MessageDirection.Inbound).OrderBy(m => m.SentAt).ToList();
                var outbound = channelMessages.Where(m => m.Direction == Domain.Enums.MessageDirection.Outbound).OrderBy(m => m.SentAt).ToList();

                double avgResponse = 0;
                if (inbound.Count > 0 && outbound.Count > 0)
                {
                    var responseTimes = inbound
                        .Select(i => outbound.FirstOrDefault(o => o.ConversationId == i.ConversationId && o.SentAt > i.SentAt))
                        .Where(o => o is not null)
                        .Select(o => (o!.SentAt - inbound.First(i => i.ConversationId == o.ConversationId && i.SentAt < o.SentAt).SentAt).TotalMinutes)
                        .ToList();
                    if (responseTimes.Count > 0) avgResponse = Math.Round(responseTimes.Average(), 1);
                }

                return new ChannelBreakdownItem(g.Key, g.Count(), channelMessages.Count, avgResponse);
            })
            .OrderByDescending(c => c.ConversationCount)
            .ToList();

        var timeSeries = conversations
            .GroupBy(c => new { Period = c.CreatedAt.ToString("yyyy-MM-dd"), Channel = c.Channel.ToString() })
            .OrderBy(g => g.Key.Period)
            .Select(g => new ChannelTimeSeriesPoint(g.Key.Period, g.Key.Channel, g.Count()))
            .ToList();

        return new ChannelAnalyticsReportDto(channelBreakdown, timeSeries);
    }
}
```

- [ ] **Step 12: Create GetAiUsageReportQuery**

Create `src/CustomerSupport.Application/Reports/Queries/GetAiUsageReportQuery.cs`:

```csharp
using CustomerSupport.Application.Reports.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Reports.Queries;

public record GetAiUsageReportQuery(
    DateTime StartDate, DateTime EndDate) : IRequest<AiUsageReportDto>;

public class GetAiUsageReportQueryHandler : IRequestHandler<GetAiUsageReportQuery, AiUsageReportDto>
{
    private readonly AppDbContext _context;

    public GetAiUsageReportQueryHandler(AppDbContext context) => _context = context;

    public async Task<AiUsageReportDto> Handle(GetAiUsageReportQuery request, CancellationToken cancellationToken)
    {
        var suggestions = await _context.AiSuggestions
            .Where(s => s.CreatedAt >= request.StartDate && s.CreatedAt <= request.EndDate)
            .Select(s => new { s.Type, s.Status, s.Confidence, s.TokensUsed, s.CreatedAt })
            .ToListAsync(cancellationToken);

        var byType = suggestions
            .GroupBy(s => s.Type)
            .Select(g =>
            {
                var total = g.Count();
                var accepted = g.Count(s => s.Status == "Accepted" || s.Status == "AutoApplied");
                var rejected = g.Count(s => s.Status == "Rejected");
                var pending = g.Count(s => s.Status == "Pending");
                return new AiSuggestionTypeBreakdown(
                    g.Key, total, accepted, rejected, pending,
                    total > 0 ? Math.Round(accepted * 100.0 / total, 1) : 0);
            })
            .OrderByDescending(t => t.TotalCount)
            .ToList();

        var avgConfidence = suggestions.Where(s => s.Confidence.HasValue).ToList();
        var totalTokens = suggestions.Sum(s => s.TokensUsed);

        var timeSeries = suggestions
            .GroupBy(s => s.CreatedAt.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var total = g.Count();
                var accepted = g.Count(s => s.Status == "Accepted" || s.Status == "AutoApplied");
                return new AiUsageTimeSeriesPoint(
                    g.Key, total,
                    total > 0 ? Math.Round(accepted * 100.0 / total, 1) : 0);
            })
            .ToList();

        return new AiUsageReportDto(
            byType,
            avgConfidence.Count > 0 ? Math.Round((double)avgConfidence.Average(s => s.Confidence!.Value), 2) : 0,
            totalTokens,
            timeSeries);
    }
}
```

- [ ] **Step 13: Create GetCsatReportQuery**

Create `src/CustomerSupport.Application/Reports/Queries/GetCsatReportQuery.cs`:

```csharp
using CustomerSupport.Application.Reports.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Reports.Queries;

public record GetCsatReportQuery(
    DateTime StartDate, DateTime EndDate,
    Guid? CategoryId = null) : IRequest<CsatReportDto>;

public class GetCsatReportQueryHandler : IRequestHandler<GetCsatReportQuery, CsatReportDto>
{
    private readonly AppDbContext _context;

    public GetCsatReportQueryHandler(AppDbContext context) => _context = context;

    public async Task<CsatReportDto> Handle(GetCsatReportQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TicketFeedbacks
            .Include(f => f.Ticket)
            .Include(f => f.Customer)
            .Where(f => f.SubmittedAt >= request.StartDate && f.SubmittedAt <= request.EndDate);

        if (request.CategoryId.HasValue)
            query = query.Where(f => f.Ticket.CategoryId == request.CategoryId.Value);

        var feedbacks = await query.Select(f => new
        {
            f.TicketId,
            f.Ticket.TicketNumber,
            CustomerName = f.Customer.Name,
            f.Rating,
            f.Comment,
            f.SubmittedAt
        }).ToListAsync(cancellationToken);

        if (feedbacks.Count == 0)
            return new CsatReportDto(0, 0, [], [], []);

        var avgRating = Math.Round(feedbacks.Average(f => f.Rating), 2);

        var distribution = Enumerable.Range(1, 5)
            .Select(rating =>
            {
                var count = feedbacks.Count(f => f.Rating == rating);
                return new RatingDistributionItem(rating, count,
                    Math.Round(count * 100.0 / feedbacks.Count, 1));
            })
            .ToList();

        var recent = feedbacks
            .OrderByDescending(f => f.SubmittedAt)
            .Take(20)
            .Select(f => new RecentFeedbackItem(
                f.TicketId, f.TicketNumber, f.CustomerName,
                f.Rating, f.Comment, f.SubmittedAt))
            .ToList();

        var timeSeries = feedbacks
            .GroupBy(f => f.SubmittedAt.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g => new CsatTimeSeriesPoint(
                g.Key, Math.Round(g.Average(f => f.Rating), 2), g.Count()))
            .ToList();

        return new CsatReportDto(avgRating, feedbacks.Count, distribution, recent, timeSeries);
    }
}
```

- [ ] **Step 14: Create report query validator**

Create `src/CustomerSupport.Application/Reports/Validators/TicketVolumeReportValidator.cs`:

```csharp
using CustomerSupport.Application.Reports.Queries;
using FluentValidation;

namespace CustomerSupport.Application.Reports.Validators;

public class TicketVolumeReportValidator : AbstractValidator<GetTicketVolumeReportQuery>
{
    public TicketVolumeReportValidator()
    {
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
        RuleFor(x => x.GroupBy).Must(g => g is "Day" or "Week" or "Month").WithMessage("GroupBy must be Day, Week, or Month.");
    }
}

public class SlaPerformanceReportValidator : AbstractValidator<GetSlaPerformanceReportQuery>
{
    public SlaPerformanceReportValidator()
    {
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public class AgentPerformanceReportValidator : AbstractValidator<GetAgentPerformanceReportQuery>
{
    public AgentPerformanceReportValidator()
    {
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public class ChannelAnalyticsReportValidator : AbstractValidator<GetChannelAnalyticsReportQuery>
{
    public ChannelAnalyticsReportValidator()
    {
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public class AiUsageReportValidator : AbstractValidator<GetAiUsageReportQuery>
{
    public AiUsageReportValidator()
    {
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public class CsatReportValidator : AbstractValidator<GetCsatReportQuery>
{
    public CsatReportValidator()
    {
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}
```

- [ ] **Step 15: Create SubmitTicketFeedbackCommand**

Create `src/CustomerSupport.Application/Portal/Commands/SubmitTicketFeedbackCommand.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Commands;

public record SubmitTicketFeedbackCommand(
    Guid TicketId, Guid CustomerId, Guid TenantId,
    int Rating, string? Comment) : IRequest<Result>;

public class SubmitTicketFeedbackCommandHandler : IRequestHandler<SubmitTicketFeedbackCommand, Result>
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public SubmitTicketFeedbackCommandHandler(AppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result> Handle(SubmitTicketFeedbackCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.CustomerId == request.CustomerId, cancellationToken);

        if (ticket is null)
            return Result.Failure("Ticket not found.");

        if (ticket.Status is null || !ticket.Status.IsFinal)
            return Result.Failure("Feedback can only be submitted for resolved tickets.");

        var existingFeedback = await _context.TicketFeedbacks
            .AnyAsync(f => f.TicketId == request.TicketId, cancellationToken);

        if (existingFeedback)
            return Result.Failure("Feedback has already been submitted for this ticket.");

        _context.TicketFeedbacks.Add(new TicketFeedback
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            TicketId = request.TicketId,
            CustomerId = request.CustomerId,
            Rating = request.Rating,
            Comment = request.Comment,
            SubmittedAt = _dateTimeService.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

- [ ] **Step 16: Create SubmitTicketFeedbackValidator**

Create `src/CustomerSupport.Application/Portal/Validators/SubmitTicketFeedbackValidator.cs`:

```csharp
using CustomerSupport.Application.Portal.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Portal.Validators;

public class SubmitTicketFeedbackValidator : AbstractValidator<SubmitTicketFeedbackCommand>
{
    public SubmitTicketFeedbackValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}
```

- [ ] **Step 17: Create ReportsController**

Create `src/CustomerSupport.API/Controllers/ReportsController.cs`:

```csharp
using CustomerSupport.Application.Reports.DTOs;
using CustomerSupport.Application.Reports.Queries;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IReportExportService _exportService;

    public ReportsController(IMediator mediator, IReportExportService exportService)
    {
        _mediator = mediator;
        _exportService = exportService;
    }

    [HttpGet("ticket-volume")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<TicketVolumeReportDto>> GetTicketVolume(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate,
        [FromQuery] string groupBy = "Day", [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? priorityId = null, [FromQuery] Guid? statusId = null,
        [FromQuery] Guid? assignedToId = null)
        => Ok(await _mediator.Send(new GetTicketVolumeReportQuery(startDate, endDate, groupBy, categoryId, priorityId, statusId, assignedToId)));

    [HttpGet("ticket-volume/export")]
    [Authorize(Policy = "Permission:reports.export")]
    public async Task<IActionResult> ExportTicketVolume(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate,
        [FromQuery] string format = "csv", [FromQuery] string groupBy = "Day",
        [FromQuery] Guid? categoryId = null, [FromQuery] Guid? priorityId = null,
        [FromQuery] Guid? statusId = null, [FromQuery] Guid? assignedToId = null)
    {
        var data = await _mediator.Send(new GetTicketVolumeReportQuery(startDate, endDate, groupBy, categoryId, priorityId, statusId, assignedToId));
        var exportData = new ReportExportData(
            "Ticket Volume Report", "تقرير حجم التذاكر",
            ["Period", "Created", "Resolved"],
            ["الفترة", "المنشأة", "المحلولة"],
            data.TimeSeries.Select(t => new[] { t.Period, t.CreatedCount.ToString(), t.ResolvedCount.ToString() }).ToList(),
            DateTime.UtcNow, $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        return await ExportFile(exportData, format, "ticket-volume");
    }

    [HttpGet("sla-performance")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<SlaPerformanceReportDto>> GetSlaPerformance(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate,
        [FromQuery] Guid? priorityId = null, [FromQuery] Guid? categoryId = null)
        => Ok(await _mediator.Send(new GetSlaPerformanceReportQuery(startDate, endDate, priorityId, categoryId)));

    [HttpGet("sla-performance/export")]
    [Authorize(Policy = "Permission:reports.export")]
    public async Task<IActionResult> ExportSlaPerformance(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate,
        [FromQuery] string format = "csv", [FromQuery] Guid? priorityId = null,
        [FromQuery] Guid? categoryId = null)
    {
        var data = await _mediator.Send(new GetSlaPerformanceReportQuery(startDate, endDate, priorityId, categoryId));
        var exportData = new ReportExportData(
            "SLA Performance Report", "تقرير أداء اتفاقية مستوى الخدمة",
            ["Ticket #", "Breach Type", "Policy", "Due At", "Breached At", "Minutes Late"],
            ["رقم التذكرة", "نوع الخرق", "السياسة", "تاريخ الاستحقاق", "تاريخ الخرق", "الدقائق المتأخرة"],
            data.BreachDetails.Select(b => new[] { b.TicketNumber, b.BreachType, b.PolicyName, b.DueAt.ToString("g"), b.BreachedAt.ToString("g"), b.MinutesLate.ToString("F0") }).ToList(),
            DateTime.UtcNow, $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        return await ExportFile(exportData, format, "sla-performance");
    }

    [HttpGet("agent-performance")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<AgentPerformanceReportDto>> GetAgentPerformance(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate,
        [FromQuery] Guid? agentId = null)
        => Ok(await _mediator.Send(new GetAgentPerformanceReportQuery(startDate, endDate, agentId)));

    [HttpGet("agent-performance/export")]
    [Authorize(Policy = "Permission:reports.export")]
    public async Task<IActionResult> ExportAgentPerformance(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate,
        [FromQuery] string format = "csv", [FromQuery] Guid? agentId = null)
    {
        var data = await _mediator.Send(new GetAgentPerformanceReportQuery(startDate, endDate, agentId));
        var exportData = new ReportExportData(
            "Agent Performance Report", "تقرير أداء الوكلاء",
            ["Agent", "Handled", "Resolved", "Avg Resolution (min)", "Avg First Response (min)", "SLA Compliance %"],
            ["الوكيل", "المعالجة", "المحلولة", "متوسط الحل (دقيقة)", "متوسط الاستجابة الأولى (دقيقة)", "الامتثال %"],
            data.Agents.Select(a => new[] { a.AgentName, a.TicketsHandled.ToString(), a.TicketsResolved.ToString(), a.AvgResolutionMinutes.ToString("F1"), a.AvgFirstResponseMinutes.ToString("F1"), a.SlaCompliancePercent.ToString("F1") }).ToList(),
            DateTime.UtcNow, $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        return await ExportFile(exportData, format, "agent-performance");
    }

    [HttpGet("channel-analytics")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<ChannelAnalyticsReportDto>> GetChannelAnalytics(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        => Ok(await _mediator.Send(new GetChannelAnalyticsReportQuery(startDate, endDate)));

    [HttpGet("channel-analytics/export")]
    [Authorize(Policy = "Permission:reports.export")]
    public async Task<IActionResult> ExportChannelAnalytics(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate,
        [FromQuery] string format = "csv")
    {
        var data = await _mediator.Send(new GetChannelAnalyticsReportQuery(startDate, endDate));
        var exportData = new ReportExportData(
            "Channel Analytics Report", "تقرير تحليل القنوات",
            ["Channel", "Conversations", "Messages", "Avg Response (min)"],
            ["القناة", "المحادثات", "الرسائل", "متوسط الاستجابة (دقيقة)"],
            data.ChannelBreakdown.Select(c => new[] { c.Channel, c.ConversationCount.ToString(), c.MessageCount.ToString(), c.AvgResponseMinutes.ToString("F1") }).ToList(),
            DateTime.UtcNow, $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        return await ExportFile(exportData, format, "channel-analytics");
    }

    [HttpGet("ai-usage")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<AiUsageReportDto>> GetAiUsage(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        => Ok(await _mediator.Send(new GetAiUsageReportQuery(startDate, endDate)));

    [HttpGet("ai-usage/export")]
    [Authorize(Policy = "Permission:reports.export")]
    public async Task<IActionResult> ExportAiUsage(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate,
        [FromQuery] string format = "csv")
    {
        var data = await _mediator.Send(new GetAiUsageReportQuery(startDate, endDate));
        var exportData = new ReportExportData(
            "AI Usage Report", "تقرير استخدام الذكاء الاصطناعي",
            ["Type", "Total", "Accepted", "Rejected", "Pending", "Acceptance Rate %"],
            ["النوع", "الإجمالي", "المقبول", "المرفوض", "معلق", "معدل القبول %"],
            data.SuggestionsByType.Select(s => new[] { s.Type, s.TotalCount.ToString(), s.AcceptedCount.ToString(), s.RejectedCount.ToString(), s.PendingCount.ToString(), s.AcceptanceRate.ToString("F1") }).ToList(),
            DateTime.UtcNow, $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        return await ExportFile(exportData, format, "ai-usage");
    }

    [HttpGet("csat")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<CsatReportDto>> GetCsat(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate,
        [FromQuery] Guid? categoryId = null)
        => Ok(await _mediator.Send(new GetCsatReportQuery(startDate, endDate, categoryId)));

    [HttpGet("csat/export")]
    [Authorize(Policy = "Permission:reports.export")]
    public async Task<IActionResult> ExportCsat(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate,
        [FromQuery] string format = "csv", [FromQuery] Guid? categoryId = null)
    {
        var data = await _mediator.Send(new GetCsatReportQuery(startDate, endDate, categoryId));
        var exportData = new ReportExportData(
            "Customer Satisfaction Report", "تقرير رضا العملاء",
            ["Ticket #", "Customer", "Rating", "Comment", "Submitted At"],
            ["رقم التذكرة", "العميل", "التقييم", "التعليق", "تاريخ التقديم"],
            data.RecentFeedback.Select(f => new[] { f.TicketNumber, f.CustomerName, f.Rating.ToString(), f.Comment ?? "", f.SubmittedAt.ToString("g") }).ToList(),
            DateTime.UtcNow, $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        return await ExportFile(exportData, format, "csat");
    }

    private async Task<IActionResult> ExportFile(ReportExportData data, string format, string fileNameBase)
    {
        return format.ToLower() switch
        {
            "excel" => File(await _exportService.ExportExcelAsync(data),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{fileNameBase}.xlsx"),
            "pdf" => File(await _exportService.ExportPdfAsync(data),
                "application/pdf", $"{fileNameBase}.pdf"),
            _ => File(await _exportService.ExportCsvAsync(data),
                "text/csv", $"{fileNameBase}.csv")
        };
    }
}
```

- [ ] **Step 18: Add feedback endpoint to PortalController**

In `src/CustomerSupport.API/Controllers/PortalController.cs`, add the following using at the top if not present:

```csharp
using CustomerSupport.Application.Common.Models;
```

Add this endpoint method after the existing `GetTicket` endpoint:

```csharp
    [HttpPost("tickets/{ticketId:guid}/feedback")]
    public async Task<ActionResult<Result>> SubmitFeedback(Guid ticketId, [FromBody] PortalFeedbackRequest request)
    {
        var result = await _mediator.Send(new PortalSubmitTicketFeedbackCommand(
            ticketId, GetCustomerId(), GetTenantId(), request.Rating, request.Comment));
        return result.Succeeded ? StatusCode(201, result) : BadRequest(result);
    }
```

Note: Import `PortalSubmitTicketFeedbackCommand` → the actual command class is `SubmitTicketFeedbackCommand` from `CustomerSupport.Application.Portal.Commands`. Use the correct import:

```csharp
using CustomerSupport.Application.Portal.Commands;
```

And the endpoint should use:

```csharp
    [HttpPost("tickets/{ticketId:guid}/feedback")]
    public async Task<ActionResult<Result>> SubmitFeedback(Guid ticketId, [FromBody] PortalFeedbackRequest request)
    {
        var result = await _mediator.Send(new SubmitTicketFeedbackCommand(
            ticketId, GetCustomerId(), GetTenantId(), request.Rating, request.Comment));
        return result.Succeeded ? StatusCode(201, result) : BadRequest(result);
    }
```

Also add at the bottom of the file (near other request records):

```csharp
public record PortalFeedbackRequest(int Rating, string? Comment);
```

- [ ] **Step 19: Build and verify**

Run:
```bash
dotnet build src/CustomerSupport.API
```

Expected: build succeeds.

- [ ] **Step 20: Commit**

```bash
git add -A
git commit -m "feat(reports): add 6 report query handlers, ReportsController, CSAT submission endpoint"
```

---

### Task 3: Reports UI — Shared Components, Service, and Landing Page

**Files:**
- Create: `src/client/src/app/features/reports/reports.service.ts`
- Create: `src/client/src/app/features/reports/reports.routes.ts`
- Create: `src/client/src/app/features/reports/shared/report-date-range/report-date-range.ts`
- Create: `src/client/src/app/features/reports/shared/report-export-bar/report-export-bar.ts`
- Create: `src/client/src/app/features/reports/shared/report-chart-card/report-chart-card.ts`
- Create: `src/client/src/app/features/reports/reports-landing/reports-landing.ts`
- Modify: `src/client/src/app/app.routes.ts` — add reports route
- Modify: `src/client/src/assets/i18n/en.json` — add report i18n keys
- Modify: `src/client/src/assets/i18n/ar.json` — add report i18n keys (Arabic)

**Interfaces:**
- Consumes: `ApiService` base class, report DTO shapes from Task 2 API
- Produces:
  - `ReportsService` with methods: `getTicketVolume(params)`, `getSlaPerformance(params)`, `getAgentPerformance(params)`, `getChannelAnalytics(params)`, `getAiUsage(params)`, `getCsat(params)`, `exportReport(reportType, format, params)`
  - `ReportDateRangeComponent` — emits `(dateRangeChange)` with `{ startDate: string, endDate: string }`
  - `ReportExportBarComponent` — inputs: `exportUrl`, `params`
  - `ReportChartCardComponent` — inputs: `title`, `loading`, content projection
  - Report routes at `/admin/reports/*`

- [ ] **Step 1: Install ngx-charts**

Run from `src/client/`:
```bash
npm install @swimlane/ngx-charts --save
```

- [ ] **Step 2: Create ReportsService**

Create `src/client/src/app/features/reports/reports.service.ts`:

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiService } from '../../core/services/api.service';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface TicketVolumeReportDto {
  timeSeries: { period: string; createdCount: number; resolvedCount: number }[];
  categoryBreakdown: { categoryName: string; categoryNameAr: string; count: number }[];
  priorityBreakdown: { priorityName: string; priorityNameAr: string; count: number }[];
  totalCreated: number;
  totalResolved: number;
}

export interface SlaPerformanceReportDto {
  overallFirstResponseCompliance: number;
  overallResolutionCompliance: number;
  timeSeries: { period: string; firstResponseOnTime: number; firstResponseBreached: number; resolutionOnTime: number; resolutionBreached: number }[];
  breachDetails: { ticketId: string; ticketNumber: string; breachType: string; policyName: string; dueAt: string; breachedAt: string; minutesLate: number }[];
}

export interface AgentPerformanceReportDto {
  agents: { agentId: string; agentName: string; ticketsHandled: number; ticketsResolved: number; avgResolutionMinutes: number; avgFirstResponseMinutes: number; slaCompliancePercent: number }[];
  topPerformer: { agentId: string; agentName: string } | null;
}

export interface ChannelAnalyticsReportDto {
  channelBreakdown: { channel: string; conversationCount: number; messageCount: number; avgResponseMinutes: number }[];
  timeSeries: { period: string; channel: string; conversationCount: number }[];
}

export interface AiUsageReportDto {
  suggestionsByType: { type: string; totalCount: number; acceptedCount: number; rejectedCount: number; pendingCount: number; acceptanceRate: number }[];
  avgConfidence: number;
  totalTokensUsed: number;
  timeSeries: { period: string; suggestionCount: number; acceptanceRate: number }[];
}

export interface CsatReportDto {
  averageRating: number;
  totalResponses: number;
  ratingDistribution: { rating: number; count: number; percentage: number }[];
  recentFeedback: { ticketId: string; ticketNumber: string; customerName: string; rating: number; comment: string | null; submittedAt: string }[];
  timeSeries: { period: string; averageRating: number; responseCount: number }[];
}

@Injectable({ providedIn: 'root' })
export class ReportsService extends ApiService {
  getTicketVolume(params: Record<string, any>): Observable<TicketVolumeReportDto> {
    return this.get<TicketVolumeReportDto>('/v1/Reports/ticket-volume', params);
  }

  getSlaPerformance(params: Record<string, any>): Observable<SlaPerformanceReportDto> {
    return this.get<SlaPerformanceReportDto>('/v1/Reports/sla-performance', params);
  }

  getAgentPerformance(params: Record<string, any>): Observable<AgentPerformanceReportDto> {
    return this.get<AgentPerformanceReportDto>('/v1/Reports/agent-performance', params);
  }

  getChannelAnalytics(params: Record<string, any>): Observable<ChannelAnalyticsReportDto> {
    return this.get<ChannelAnalyticsReportDto>('/v1/Reports/channel-analytics', params);
  }

  getAiUsage(params: Record<string, any>): Observable<AiUsageReportDto> {
    return this.get<AiUsageReportDto>('/v1/Reports/ai-usage', params);
  }

  getCsat(params: Record<string, any>): Observable<CsatReportDto> {
    return this.get<CsatReportDto>('/v1/Reports/csat', params);
  }

  exportReport(reportType: string, format: string, params: Record<string, any>): void {
    const httpClient = inject(HttpClient);
    const queryString = Object.entries({ ...params, format })
      .filter(([, v]) => v != null && v !== '')
      .map(([k, v]) => `${k}=${encodeURIComponent(v)}`)
      .join('&');
    const url = `${environment.apiUrl}/v1/Reports/${reportType}/export?${queryString}`;
    window.open(url, '_blank');
  }
}
```

- [ ] **Step 3: Create ReportDateRangeComponent**

Create `src/client/src/app/features/reports/shared/report-date-range/report-date-range.ts`:

```typescript
import { Component, output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatNativeDateModule } from '@angular/material/core';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'app-report-date-range',
  imports: [
    FormsModule, TranslateModule,
    MatFormFieldModule, MatDatepickerModule, MatInputModule,
    MatButtonModule, MatNativeDateModule, MatChipsModule
  ],
  template: `
    <div class="date-range-container">
      <div class="presets">
        @for (preset of presets; track preset.label) {
          <button mat-stroked-button (click)="applyPreset(preset)">
            {{ preset.label | translate }}
          </button>
        }
      </div>
      <mat-form-field>
        <mat-label>{{ 'reports.dateRange' | translate }}</mat-label>
        <mat-date-range-input [rangePicker]="picker">
          <input matStartDate [(ngModel)]="startDate" (dateChange)="emitChange()">
          <input matEndDate [(ngModel)]="endDate" (dateChange)="emitChange()">
        </mat-date-range-input>
        <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
        <mat-date-range-picker #picker></mat-date-range-picker>
      </mat-form-field>
    </div>
  `,
  styles: [`
    .date-range-container { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; margin-block-end: 16px; }
    .presets { display: flex; gap: 8px; flex-wrap: wrap; }
    .presets button { font-size: 12px; }
  `]
})
export class ReportDateRangeComponent {
  dateRangeChange = output<{ startDate: string; endDate: string }>();

  startDate: Date | null = null;
  endDate: Date | null = null;

  presets = [
    { label: 'reports.presets.today', days: 0 },
    { label: 'reports.presets.last7', days: 7 },
    { label: 'reports.presets.last30', days: 30 },
    { label: 'reports.presets.thisMonth', days: -1 },
    { label: 'reports.presets.thisQuarter', days: -2 },
  ];

  constructor() {
    this.applyPreset(this.presets[2]);
  }

  applyPreset(preset: { label: string; days: number }): void {
    const now = new Date();
    if (preset.days === -1) {
      this.startDate = new Date(now.getFullYear(), now.getMonth(), 1);
      this.endDate = now;
    } else if (preset.days === -2) {
      const qMonth = Math.floor(now.getMonth() / 3) * 3;
      this.startDate = new Date(now.getFullYear(), qMonth, 1);
      this.endDate = now;
    } else if (preset.days === 0) {
      this.startDate = new Date(now.getFullYear(), now.getMonth(), now.getDate());
      this.endDate = now;
    } else {
      this.startDate = new Date(now.getTime() - preset.days * 86400000);
      this.endDate = now;
    }
    this.emitChange();
  }

  emitChange(): void {
    if (this.startDate && this.endDate) {
      this.dateRangeChange.emit({
        startDate: this.startDate.toISOString().split('T')[0],
        endDate: this.endDate.toISOString().split('T')[0]
      });
    }
  }
}
```

- [ ] **Step 4: Create ReportExportBarComponent**

Create `src/client/src/app/features/reports/shared/report-export-bar/report-export-bar.ts`:

```typescript
import { Component, input, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ReportsService } from '../../reports.service';

@Component({
  selector: 'app-report-export-bar',
  imports: [TranslateModule, MatButtonModule, MatIconModule],
  template: `
    <div class="export-bar">
      <button mat-stroked-button (click)="doExport('csv')">
        <mat-icon>description</mat-icon> CSV
      </button>
      <button mat-stroked-button (click)="doExport('excel')">
        <mat-icon>table_chart</mat-icon> Excel
      </button>
      <button mat-stroked-button (click)="doExport('pdf')">
        <mat-icon>picture_as_pdf</mat-icon> PDF
      </button>
    </div>
  `,
  styles: [`
    .export-bar { display: flex; gap: 8px; margin-block: 12px; }
  `]
})
export class ReportExportBarComponent {
  reportType = input.required<string>();
  params = input<Record<string, any>>({});

  private reportsService = inject(ReportsService);

  doExport(format: string): void {
    this.reportsService.exportReport(this.reportType, format, this.params());
  }
}
```

- [ ] **Step 5: Create ReportChartCardComponent**

Create `src/client/src/app/features/reports/shared/report-chart-card/report-chart-card.ts`:

```typescript
import { Component, input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-report-chart-card',
  imports: [TranslateModule, MatCardModule, MatProgressSpinnerModule],
  template: `
    <mat-card class="chart-card">
      <mat-card-header>
        <mat-card-title>{{ title() | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        @if (loading()) {
          <div class="loading-container">
            <mat-spinner diameter="40"></mat-spinner>
          </div>
        } @else {
          <ng-content></ng-content>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .chart-card { margin-block-end: 16px; }
    .loading-container { display: flex; justify-content: center; padding: 40px 0; }
  `]
})
export class ReportChartCardComponent {
  title = input.required<string>();
  loading = input(false);
}
```

- [ ] **Step 6: Create ReportsLandingComponent**

Create `src/client/src/app/features/reports/reports-landing/reports-landing.ts`:

```typescript
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

interface ReportCard {
  icon: string;
  titleKey: string;
  descKey: string;
  route: string;
}

@Component({
  selector: 'app-reports-landing',
  imports: [RouterLink, TranslateModule, MatCardModule, MatIconModule],
  template: `
    <h1>{{ 'reports.title' | translate }}</h1>
    <div class="report-grid">
      @for (card of reportCards; track card.route) {
        <mat-card class="report-card" [routerLink]="card.route">
          <mat-card-content>
            <mat-icon class="report-icon">{{ card.icon }}</mat-icon>
            <h3>{{ card.titleKey | translate }}</h3>
            <p>{{ card.descKey | translate }}</p>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .report-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; }
    .report-card { cursor: pointer; transition: box-shadow 0.2s; }
    .report-card:hover { box-shadow: 0 4px 12px rgba(0,0,0,0.15); }
    .report-card mat-card-content { display: flex; flex-direction: column; align-items: center; text-align: center; padding: 24px 16px; }
    .report-icon { font-size: 48px; width: 48px; height: 48px; color: var(--mat-sys-primary, #1976d2); margin-block-end: 12px; }
    h3 { margin: 0 0 8px; }
    p { color: rgba(0,0,0,0.6); margin: 0; font-size: 14px; }
  `]
})
export class ReportsLandingComponent {
  reportCards: ReportCard[] = [
    { icon: 'bar_chart', titleKey: 'reports.ticketVolume.title', descKey: 'reports.ticketVolume.desc', route: 'ticket-volume' },
    { icon: 'speed', titleKey: 'reports.slaPerformance.title', descKey: 'reports.slaPerformance.desc', route: 'sla-performance' },
    { icon: 'groups', titleKey: 'reports.agentPerformance.title', descKey: 'reports.agentPerformance.desc', route: 'agent-performance' },
    { icon: 'hub', titleKey: 'reports.channelAnalytics.title', descKey: 'reports.channelAnalytics.desc', route: 'channel-analytics' },
    { icon: 'smart_toy', titleKey: 'reports.aiUsage.title', descKey: 'reports.aiUsage.desc', route: 'ai-usage' },
    { icon: 'sentiment_satisfied', titleKey: 'reports.csat.title', descKey: 'reports.csat.desc', route: 'csat' },
  ];
}
```

- [ ] **Step 7: Create reports routes**

Create `src/client/src/app/features/reports/reports.routes.ts`:

```typescript
import { Routes } from '@angular/router';

export const reportsRoutes: Routes = [
  { path: '', loadComponent: () => import('./reports-landing/reports-landing').then(m => m.ReportsLandingComponent) },
  { path: 'ticket-volume', loadComponent: () => import('./ticket-volume-report/ticket-volume-report').then(m => m.TicketVolumeReportComponent) },
  { path: 'sla-performance', loadComponent: () => import('./sla-performance-report/sla-performance-report').then(m => m.SlaPerformanceReportComponent) },
  { path: 'agent-performance', loadComponent: () => import('./agent-performance-report/agent-performance-report').then(m => m.AgentPerformanceReportComponent) },
  { path: 'channel-analytics', loadComponent: () => import('./channel-analytics-report/channel-analytics-report').then(m => m.ChannelAnalyticsReportComponent) },
  { path: 'ai-usage', loadComponent: () => import('./ai-usage-report/ai-usage-report').then(m => m.AiUsageReportComponent) },
  { path: 'csat', loadComponent: () => import('./csat-report/csat-report').then(m => m.CsatReportComponent) },
];
```

- [ ] **Step 8: Add reports route to app.routes.ts**

In `src/client/src/app/app.routes.ts`, add this child route inside the `admin` children array, after the `chat` route:

```typescript
      {
        path: 'reports',
        loadChildren: () => import('./features/reports/reports.routes').then(m => m.reportsRoutes)
      },
```

- [ ] **Step 9: Add i18n keys to en.json**

In `src/client/src/assets/i18n/en.json`, add a `"reports"` section after the `"ai"` section, and extend `"common"`:

```json
  "reports": {
    "title": "Reports & Analytics",
    "dateRange": "Date Range",
    "presets": {
      "today": "Today",
      "last7": "Last 7 Days",
      "last30": "Last 30 Days",
      "thisMonth": "This Month",
      "thisQuarter": "This Quarter"
    },
    "export": "Export",
    "ticketVolume": {
      "title": "Ticket Volume",
      "desc": "Track ticket creation and resolution trends over time",
      "created": "Created",
      "resolved": "Resolved",
      "trend": "Volume Trend",
      "byCategory": "By Category",
      "byPriority": "By Priority"
    },
    "slaPerformance": {
      "title": "SLA Performance",
      "desc": "Monitor first-response and resolution compliance rates",
      "firstResponse": "First Response Compliance",
      "resolution": "Resolution Compliance",
      "trend": "Compliance Trend",
      "breaches": "Breach Details",
      "breachType": "Breach Type",
      "policy": "Policy",
      "dueAt": "Due At",
      "breachedAt": "Breached At",
      "minutesLate": "Minutes Late"
    },
    "agentPerformance": {
      "title": "Agent Performance",
      "desc": "Compare agent productivity and SLA compliance",
      "agent": "Agent",
      "handled": "Tickets Handled",
      "resolved": "Resolved",
      "avgResolution": "Avg Resolution (min)",
      "avgFirstResponse": "Avg First Response (min)",
      "slaCompliance": "SLA Compliance %",
      "topPerformer": "Top Performer",
      "ticketsPerAgent": "Tickets per Agent",
      "timeComparison": "Time Comparison"
    },
    "channelAnalytics": {
      "title": "Channel Analytics",
      "desc": "Analyze conversation volumes and response times by channel",
      "channel": "Channel",
      "conversations": "Conversations",
      "messages": "Messages",
      "avgResponse": "Avg Response (min)",
      "distribution": "Channel Distribution",
      "volumeOverTime": "Volume Over Time"
    },
    "aiUsage": {
      "title": "AI Usage",
      "desc": "Track AI suggestion usage, acceptance rates, and token consumption",
      "type": "Type",
      "total": "Total",
      "accepted": "Accepted",
      "rejected": "Rejected",
      "pending": "Pending",
      "acceptanceRate": "Acceptance Rate",
      "avgConfidence": "Avg Confidence",
      "totalTokens": "Total Tokens Used",
      "byType": "Suggestions by Type",
      "tokenTrend": "Token Usage Trend"
    },
    "csat": {
      "title": "Customer Satisfaction",
      "desc": "View customer satisfaction ratings and feedback",
      "avgRating": "Average Rating",
      "totalResponses": "Total Responses",
      "ratingDistribution": "Rating Distribution",
      "recentFeedback": "Recent Feedback",
      "rating": "Rating",
      "comment": "Comment",
      "submittedAt": "Submitted At",
      "customer": "Customer",
      "submitFeedback": "Rate Your Experience",
      "feedbackSubmitted": "Thank you for your feedback!",
      "feedbackPlaceholder": "Tell us about your experience (optional)"
    },
    "noData": "No data available for the selected period"
  }
```

Also add to `"common"`:

```json
    "export": "Export",
    "csv": "CSV",
    "excel": "Excel",
    "pdf": "PDF"
```

- [ ] **Step 10: Add i18n keys to ar.json**

In `src/client/src/assets/i18n/ar.json`, add the matching `"reports"` section with Arabic translations:

```json
  "reports": {
    "title": "التقارير والتحليلات",
    "dateRange": "النطاق الزمني",
    "presets": {
      "today": "اليوم",
      "last7": "آخر 7 أيام",
      "last30": "آخر 30 يوم",
      "thisMonth": "هذا الشهر",
      "thisQuarter": "هذا الربع"
    },
    "export": "تصدير",
    "ticketVolume": {
      "title": "حجم التذاكر",
      "desc": "تتبع اتجاهات إنشاء التذاكر وحلها عبر الزمن",
      "created": "المنشأة",
      "resolved": "المحلولة",
      "trend": "اتجاه الحجم",
      "byCategory": "حسب الفئة",
      "byPriority": "حسب الأولوية"
    },
    "slaPerformance": {
      "title": "أداء اتفاقية مستوى الخدمة",
      "desc": "مراقبة معدلات الامتثال للاستجابة الأولى والحل",
      "firstResponse": "امتثال الاستجابة الأولى",
      "resolution": "امتثال الحل",
      "trend": "اتجاه الامتثال",
      "breaches": "تفاصيل الخروقات",
      "breachType": "نوع الخرق",
      "policy": "السياسة",
      "dueAt": "تاريخ الاستحقاق",
      "breachedAt": "تاريخ الخرق",
      "minutesLate": "الدقائق المتأخرة"
    },
    "agentPerformance": {
      "title": "أداء الوكلاء",
      "desc": "مقارنة إنتاجية الوكلاء وامتثالهم لاتفاقية مستوى الخدمة",
      "agent": "الوكيل",
      "handled": "التذاكر المعالجة",
      "resolved": "المحلولة",
      "avgResolution": "متوسط الحل (دقيقة)",
      "avgFirstResponse": "متوسط الاستجابة الأولى (دقيقة)",
      "slaCompliance": "الامتثال %",
      "topPerformer": "أفضل أداء",
      "ticketsPerAgent": "التذاكر لكل وكيل",
      "timeComparison": "مقارنة الوقت"
    },
    "channelAnalytics": {
      "title": "تحليل القنوات",
      "desc": "تحليل حجم المحادثات وأوقات الاستجابة حسب القناة",
      "channel": "القناة",
      "conversations": "المحادثات",
      "messages": "الرسائل",
      "avgResponse": "متوسط الاستجابة (دقيقة)",
      "distribution": "توزيع القنوات",
      "volumeOverTime": "الحجم عبر الزمن"
    },
    "aiUsage": {
      "title": "استخدام الذكاء الاصطناعي",
      "desc": "تتبع استخدام اقتراحات الذكاء الاصطناعي ومعدلات القبول واستهلاك الرموز",
      "type": "النوع",
      "total": "الإجمالي",
      "accepted": "المقبول",
      "rejected": "المرفوض",
      "pending": "معلق",
      "acceptanceRate": "معدل القبول",
      "avgConfidence": "متوسط الثقة",
      "totalTokens": "إجمالي الرموز المستخدمة",
      "byType": "الاقتراحات حسب النوع",
      "tokenTrend": "اتجاه استخدام الرموز"
    },
    "csat": {
      "title": "رضا العملاء",
      "desc": "عرض تقييمات رضا العملاء والملاحظات",
      "avgRating": "متوسط التقييم",
      "totalResponses": "إجمالي الردود",
      "ratingDistribution": "توزيع التقييمات",
      "recentFeedback": "الملاحظات الأخيرة",
      "rating": "التقييم",
      "comment": "التعليق",
      "submittedAt": "تاريخ التقديم",
      "customer": "العميل",
      "submitFeedback": "قيّم تجربتك",
      "feedbackSubmitted": "شكراً لملاحظاتك!",
      "feedbackPlaceholder": "أخبرنا عن تجربتك (اختياري)"
    },
    "noData": "لا توجد بيانات للفترة المحددة"
  }
```

Also add to `"common"`:

```json
    "export": "تصدير",
    "csv": "CSV",
    "excel": "Excel",
    "pdf": "PDF"
```

- [ ] **Step 11: Build and verify**

Run from `src/client/`:
```bash
npx ng build --configuration development 2>&1 | head -30
```

Expected: build succeeds (report page components don't exist yet, but routes use lazy loading so they won't fail at build time — only at navigation time).

- [ ] **Step 12: Commit**

```bash
git add -A
git commit -m "feat(reports-ui): add reports service, shared components, landing page, and routes"
```

---

### Task 4: Report Page Components and Portal CSAT Widget

**Files:**
- Create: `src/client/src/app/features/reports/ticket-volume-report/ticket-volume-report.ts`
- Create: `src/client/src/app/features/reports/sla-performance-report/sla-performance-report.ts`
- Create: `src/client/src/app/features/reports/agent-performance-report/agent-performance-report.ts`
- Create: `src/client/src/app/features/reports/channel-analytics-report/channel-analytics-report.ts`
- Create: `src/client/src/app/features/reports/ai-usage-report/ai-usage-report.ts`
- Create: `src/client/src/app/features/reports/csat-report/csat-report.ts`
- Modify: portal ticket detail component — add CSAT feedback widget

**Interfaces:**
- Consumes: `ReportsService` (Task 3), `ReportDateRangeComponent`, `ReportExportBarComponent`, `ReportChartCardComponent` (Task 3), all report DTO types
- Produces: 6 standalone report page components, each with date range filtering, ngx-charts visualizations, data tables, and export bar

- [ ] **Step 1: Create TicketVolumeReportComponent**

Create `src/client/src/app/features/reports/ticket-volume-report/ticket-volume-report.ts`:

```typescript
import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ReportsService, TicketVolumeReportDto } from '../reports.service';
import { ReportDateRangeComponent } from '../shared/report-date-range/report-date-range';
import { ReportExportBarComponent } from '../shared/report-export-bar/report-export-bar';
import { ReportChartCardComponent } from '../shared/report-chart-card/report-chart-card';

@Component({
  selector: 'app-ticket-volume-report',
  imports: [
    TranslateModule, MatTableModule, NgxChartsModule,
    ReportDateRangeComponent, ReportExportBarComponent, ReportChartCardComponent
  ],
  template: `
    <h1>{{ 'reports.ticketVolume.title' | translate }}</h1>
    <app-report-date-range (dateRangeChange)="onDateChange($event)" />
    <app-report-export-bar [reportType]="'ticket-volume'" [params]="currentParams" />

    <div class="charts-grid">
      <app-report-chart-card [title]="'reports.ticketVolume.trend'" [loading]="loading">
        @if (lineData.length > 0) {
          <ngx-charts-line-chart
            [results]="lineData" [xAxis]="true" [yAxis]="true"
            [legend]="true" [showXAxisLabel]="false" [showYAxisLabel]="false"
            [view]="[700, 300]" [autoScale]="true">
          </ngx-charts-line-chart>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.ticketVolume.byCategory'" [loading]="loading">
        @if (categoryData.length > 0) {
          <ngx-charts-bar-vertical
            [results]="categoryData" [xAxis]="true" [yAxis]="true"
            [view]="[500, 300]">
          </ngx-charts-bar-vertical>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.ticketVolume.byPriority'" [loading]="loading">
        @if (priorityData.length > 0) {
          <ngx-charts-pie-chart
            [results]="priorityData" [legend]="true"
            [view]="[500, 300]">
          </ngx-charts-pie-chart>
        }
      </app-report-chart-card>
    </div>

    @if (report) {
      <div class="summary">
        <strong>{{ 'reports.ticketVolume.created' | translate }}: {{ report.totalCreated }}</strong> |
        <strong>{{ 'reports.ticketVolume.resolved' | translate }}: {{ report.totalResolved }}</strong>
      </div>
    }
  `,
  styles: [`
    .charts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(450px, 1fr)); gap: 16px; }
    .summary { margin-block: 16px; font-size: 16px; }
  `]
})
export class TicketVolumeReportComponent {
  private reportsService = inject(ReportsService);

  loading = false;
  report: TicketVolumeReportDto | null = null;
  currentParams: Record<string, any> = {};
  lineData: any[] = [];
  categoryData: any[] = [];
  priorityData: any[] = [];

  onDateChange(range: { startDate: string; endDate: string }): void {
    this.currentParams = { startDate: range.startDate, endDate: range.endDate, groupBy: 'Day' };
    this.loadReport();
  }

  private loadReport(): void {
    this.loading = true;
    this.reportsService.getTicketVolume(this.currentParams).subscribe({
      next: (data) => {
        this.report = data;
        this.lineData = [
          { name: 'Created', series: data.timeSeries.map(t => ({ name: t.period, value: t.createdCount })) },
          { name: 'Resolved', series: data.timeSeries.map(t => ({ name: t.period, value: t.resolvedCount })) }
        ];
        this.categoryData = data.categoryBreakdown.map(c => ({ name: c.categoryName, value: c.count }));
        this.priorityData = data.priorityBreakdown.map(p => ({ name: p.priorityName, value: p.count }));
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }
}
```

- [ ] **Step 2: Create SlaPerformanceReportComponent**

Create `src/client/src/app/features/reports/sla-performance-report/sla-performance-report.ts`:

```typescript
import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { DatePipe } from '@angular/common';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ReportsService, SlaPerformanceReportDto } from '../reports.service';
import { ReportDateRangeComponent } from '../shared/report-date-range/report-date-range';
import { ReportExportBarComponent } from '../shared/report-export-bar/report-export-bar';
import { ReportChartCardComponent } from '../shared/report-chart-card/report-chart-card';

@Component({
  selector: 'app-sla-performance-report',
  imports: [
    TranslateModule, MatTableModule, DatePipe, NgxChartsModule,
    ReportDateRangeComponent, ReportExportBarComponent, ReportChartCardComponent
  ],
  template: `
    <h1>{{ 'reports.slaPerformance.title' | translate }}</h1>
    <app-report-date-range (dateRangeChange)="onDateChange($event)" />
    <app-report-export-bar [reportType]="'sla-performance'" [params]="currentParams" />

    <div class="charts-grid">
      <app-report-chart-card [title]="'reports.slaPerformance.firstResponse'" [loading]="loading">
        @if (report) {
          <ngx-charts-gauge
            [results]="[{ name: 'First Response', value: report.overallFirstResponseCompliance }]"
            [min]="0" [max]="100" [angleSpan]="240" [startAngle]="-120"
            [view]="[400, 250]">
          </ngx-charts-gauge>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.slaPerformance.resolution'" [loading]="loading">
        @if (report) {
          <ngx-charts-gauge
            [results]="[{ name: 'Resolution', value: report.overallResolutionCompliance }]"
            [min]="0" [max]="100" [angleSpan]="240" [startAngle]="-120"
            [view]="[400, 250]">
          </ngx-charts-gauge>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.slaPerformance.trend'" [loading]="loading">
        @if (trendData.length > 0) {
          <ngx-charts-line-chart
            [results]="trendData" [xAxis]="true" [yAxis]="true"
            [legend]="true" [view]="[700, 300]" [autoScale]="true">
          </ngx-charts-line-chart>
        }
      </app-report-chart-card>
    </div>

    @if (report && report.breachDetails.length > 0) {
      <h3>{{ 'reports.slaPerformance.breaches' | translate }}</h3>
      <table mat-table [dataSource]="report.breachDetails" class="full-width">
        <ng-container matColumnDef="ticketNumber">
          <th mat-header-cell *matHeaderCellDef>{{ 'tickets.ticketNumber' | translate }}</th>
          <td mat-cell *matCellDef="let b">{{ b.ticketNumber }}</td>
        </ng-container>
        <ng-container matColumnDef="breachType">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.slaPerformance.breachType' | translate }}</th>
          <td mat-cell *matCellDef="let b">{{ b.breachType }}</td>
        </ng-container>
        <ng-container matColumnDef="policyName">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.slaPerformance.policy' | translate }}</th>
          <td mat-cell *matCellDef="let b">{{ b.policyName }}</td>
        </ng-container>
        <ng-container matColumnDef="minutesLate">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.slaPerformance.minutesLate' | translate }}</th>
          <td mat-cell *matCellDef="let b">{{ b.minutesLate | number:'1.0-0' }}</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="breachColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: breachColumns;"></tr>
      </table>
    }
  `,
  styles: [`
    .charts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 16px; }
    .full-width { width: 100%; }
  `]
})
export class SlaPerformanceReportComponent {
  private reportsService = inject(ReportsService);

  loading = false;
  report: SlaPerformanceReportDto | null = null;
  currentParams: Record<string, any> = {};
  trendData: any[] = [];
  breachColumns = ['ticketNumber', 'breachType', 'policyName', 'minutesLate'];

  onDateChange(range: { startDate: string; endDate: string }): void {
    this.currentParams = { startDate: range.startDate, endDate: range.endDate };
    this.loadReport();
  }

  private loadReport(): void {
    this.loading = true;
    this.reportsService.getSlaPerformance(this.currentParams).subscribe({
      next: (data) => {
        this.report = data;
        this.trendData = [
          { name: 'FR On Time', series: data.timeSeries.map(t => ({ name: t.period, value: t.firstResponseOnTime })) },
          { name: 'FR Breached', series: data.timeSeries.map(t => ({ name: t.period, value: t.firstResponseBreached })) },
          { name: 'Res On Time', series: data.timeSeries.map(t => ({ name: t.period, value: t.resolutionOnTime })) },
          { name: 'Res Breached', series: data.timeSeries.map(t => ({ name: t.period, value: t.resolutionBreached })) }
        ];
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }
}
```

- [ ] **Step 3: Create AgentPerformanceReportComponent**

Create `src/client/src/app/features/reports/agent-performance-report/agent-performance-report.ts`:

```typescript
import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ReportsService, AgentPerformanceReportDto } from '../reports.service';
import { ReportDateRangeComponent } from '../shared/report-date-range/report-date-range';
import { ReportExportBarComponent } from '../shared/report-export-bar/report-export-bar';
import { ReportChartCardComponent } from '../shared/report-chart-card/report-chart-card';

@Component({
  selector: 'app-agent-performance-report',
  imports: [
    TranslateModule, MatTableModule, NgxChartsModule,
    ReportDateRangeComponent, ReportExportBarComponent, ReportChartCardComponent
  ],
  template: `
    <h1>{{ 'reports.agentPerformance.title' | translate }}</h1>
    <app-report-date-range (dateRangeChange)="onDateChange($event)" />
    <app-report-export-bar [reportType]="'agent-performance'" [params]="currentParams" />

    @if (report?.topPerformer) {
      <div class="top-performer">
        {{ 'reports.agentPerformance.topPerformer' | translate }}: <strong>{{ report.topPerformer.agentName }}</strong>
      </div>
    }

    <div class="charts-grid">
      <app-report-chart-card [title]="'reports.agentPerformance.ticketsPerAgent'" [loading]="loading">
        @if (barData.length > 0) {
          <ngx-charts-bar-horizontal
            [results]="barData" [xAxis]="true" [yAxis]="true"
            [view]="[600, 300]">
          </ngx-charts-bar-horizontal>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.agentPerformance.timeComparison'" [loading]="loading">
        @if (groupedBarData.length > 0) {
          <ngx-charts-bar-vertical-2d
            [results]="groupedBarData" [xAxis]="true" [yAxis]="true"
            [legend]="true" [view]="[600, 300]">
          </ngx-charts-bar-vertical-2d>
        }
      </app-report-chart-card>
    </div>

    @if (report && report.agents.length > 0) {
      <table mat-table [dataSource]="report.agents" class="full-width">
        <ng-container matColumnDef="agentName">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.agentPerformance.agent' | translate }}</th>
          <td mat-cell *matCellDef="let a">{{ a.agentName }}</td>
        </ng-container>
        <ng-container matColumnDef="ticketsHandled">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.agentPerformance.handled' | translate }}</th>
          <td mat-cell *matCellDef="let a">{{ a.ticketsHandled }}</td>
        </ng-container>
        <ng-container matColumnDef="ticketsResolved">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.agentPerformance.resolved' | translate }}</th>
          <td mat-cell *matCellDef="let a">{{ a.ticketsResolved }}</td>
        </ng-container>
        <ng-container matColumnDef="slaCompliancePercent">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.agentPerformance.slaCompliance' | translate }}</th>
          <td mat-cell *matCellDef="let a">{{ a.slaCompliancePercent }}%</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="agentColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: agentColumns;"></tr>
      </table>
    }
  `,
  styles: [`
    .charts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(450px, 1fr)); gap: 16px; }
    .full-width { width: 100%; margin-block-start: 16px; }
    .top-performer { margin-block: 12px; font-size: 16px; padding: 12px; background: rgba(76, 175, 80, 0.1); border-radius: 8px; }
  `]
})
export class AgentPerformanceReportComponent {
  private reportsService = inject(ReportsService);

  loading = false;
  report: AgentPerformanceReportDto | null = null;
  currentParams: Record<string, any> = {};
  barData: any[] = [];
  groupedBarData: any[] = [];
  agentColumns = ['agentName', 'ticketsHandled', 'ticketsResolved', 'slaCompliancePercent'];

  onDateChange(range: { startDate: string; endDate: string }): void {
    this.currentParams = { startDate: range.startDate, endDate: range.endDate };
    this.loadReport();
  }

  private loadReport(): void {
    this.loading = true;
    this.reportsService.getAgentPerformance(this.currentParams).subscribe({
      next: (data) => {
        this.report = data;
        this.barData = data.agents.map(a => ({ name: a.agentName, value: a.ticketsHandled }));
        this.groupedBarData = data.agents.map(a => ({
          name: a.agentName,
          series: [
            { name: 'Avg Resolution', value: a.avgResolutionMinutes },
            { name: 'Avg First Response', value: a.avgFirstResponseMinutes }
          ]
        }));
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }
}
```

- [ ] **Step 4: Create ChannelAnalyticsReportComponent**

Create `src/client/src/app/features/reports/channel-analytics-report/channel-analytics-report.ts`:

```typescript
import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ReportsService, ChannelAnalyticsReportDto } from '../reports.service';
import { ReportDateRangeComponent } from '../shared/report-date-range/report-date-range';
import { ReportExportBarComponent } from '../shared/report-export-bar/report-export-bar';
import { ReportChartCardComponent } from '../shared/report-chart-card/report-chart-card';

@Component({
  selector: 'app-channel-analytics-report',
  imports: [
    TranslateModule, MatTableModule, NgxChartsModule,
    ReportDateRangeComponent, ReportExportBarComponent, ReportChartCardComponent
  ],
  template: `
    <h1>{{ 'reports.channelAnalytics.title' | translate }}</h1>
    <app-report-date-range (dateRangeChange)="onDateChange($event)" />
    <app-report-export-bar [reportType]="'channel-analytics'" [params]="currentParams" />

    <div class="charts-grid">
      <app-report-chart-card [title]="'reports.channelAnalytics.distribution'" [loading]="loading">
        @if (pieData.length > 0) {
          <ngx-charts-pie-chart
            [results]="pieData" [legend]="true"
            [view]="[500, 300]">
          </ngx-charts-pie-chart>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.channelAnalytics.volumeOverTime'" [loading]="loading">
        @if (stackedData.length > 0) {
          <ngx-charts-bar-vertical-stacked
            [results]="stackedData" [xAxis]="true" [yAxis]="true"
            [legend]="true" [view]="[600, 300]">
          </ngx-charts-bar-vertical-stacked>
        }
      </app-report-chart-card>
    </div>

    @if (report && report.channelBreakdown.length > 0) {
      <table mat-table [dataSource]="report.channelBreakdown" class="full-width">
        <ng-container matColumnDef="channel">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.channelAnalytics.channel' | translate }}</th>
          <td mat-cell *matCellDef="let c">{{ c.channel }}</td>
        </ng-container>
        <ng-container matColumnDef="conversationCount">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.channelAnalytics.conversations' | translate }}</th>
          <td mat-cell *matCellDef="let c">{{ c.conversationCount }}</td>
        </ng-container>
        <ng-container matColumnDef="messageCount">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.channelAnalytics.messages' | translate }}</th>
          <td mat-cell *matCellDef="let c">{{ c.messageCount }}</td>
        </ng-container>
        <ng-container matColumnDef="avgResponseMinutes">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.channelAnalytics.avgResponse' | translate }}</th>
          <td mat-cell *matCellDef="let c">{{ c.avgResponseMinutes }}</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="channelColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: channelColumns;"></tr>
      </table>
    }
  `,
  styles: [`
    .charts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(450px, 1fr)); gap: 16px; }
    .full-width { width: 100%; margin-block-start: 16px; }
  `]
})
export class ChannelAnalyticsReportComponent {
  private reportsService = inject(ReportsService);

  loading = false;
  report: ChannelAnalyticsReportDto | null = null;
  currentParams: Record<string, any> = {};
  pieData: any[] = [];
  stackedData: any[] = [];
  channelColumns = ['channel', 'conversationCount', 'messageCount', 'avgResponseMinutes'];

  onDateChange(range: { startDate: string; endDate: string }): void {
    this.currentParams = { startDate: range.startDate, endDate: range.endDate };
    this.loadReport();
  }

  private loadReport(): void {
    this.loading = true;
    this.reportsService.getChannelAnalytics(this.currentParams).subscribe({
      next: (data) => {
        this.report = data;
        this.pieData = data.channelBreakdown.map(c => ({ name: c.channel, value: c.conversationCount }));
        const periods = [...new Set(data.timeSeries.map(t => t.period))].sort();
        const channels = [...new Set(data.timeSeries.map(t => t.channel))];
        this.stackedData = periods.map(period => ({
          name: period,
          series: channels.map(ch => ({
            name: ch,
            value: data.timeSeries.find(t => t.period === period && t.channel === ch)?.conversationCount ?? 0
          }))
        }));
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }
}
```

- [ ] **Step 5: Create AiUsageReportComponent**

Create `src/client/src/app/features/reports/ai-usage-report/ai-usage-report.ts`:

```typescript
import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ReportsService, AiUsageReportDto } from '../reports.service';
import { ReportDateRangeComponent } from '../shared/report-date-range/report-date-range';
import { ReportExportBarComponent } from '../shared/report-export-bar/report-export-bar';
import { ReportChartCardComponent } from '../shared/report-chart-card/report-chart-card';

@Component({
  selector: 'app-ai-usage-report',
  imports: [
    TranslateModule, MatTableModule, NgxChartsModule,
    ReportDateRangeComponent, ReportExportBarComponent, ReportChartCardComponent
  ],
  template: `
    <h1>{{ 'reports.aiUsage.title' | translate }}</h1>
    <app-report-date-range (dateRangeChange)="onDateChange($event)" />
    <app-report-export-bar [reportType]="'ai-usage'" [params]="currentParams" />

    @if (report) {
      <div class="summary-cards">
        <div class="summary-card">
          <span class="label">{{ 'reports.aiUsage.avgConfidence' | translate }}</span>
          <span class="value">{{ report.avgConfidence }}%</span>
        </div>
        <div class="summary-card">
          <span class="label">{{ 'reports.aiUsage.totalTokens' | translate }}</span>
          <span class="value">{{ report.totalTokensUsed | number }}</span>
        </div>
      </div>
    }

    <div class="charts-grid">
      <app-report-chart-card [title]="'reports.aiUsage.byType'" [loading]="loading">
        @if (barData.length > 0) {
          <ngx-charts-bar-vertical
            [results]="barData" [xAxis]="true" [yAxis]="true"
            [view]="[500, 300]">
          </ngx-charts-bar-vertical>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.aiUsage.acceptanceRate'" [loading]="loading">
        @if (gaugeData.length > 0) {
          <ngx-charts-gauge
            [results]="gaugeData" [min]="0" [max]="100"
            [angleSpan]="240" [startAngle]="-120" [view]="[400, 250]">
          </ngx-charts-gauge>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.aiUsage.tokenTrend'" [loading]="loading">
        @if (trendData.length > 0) {
          <ngx-charts-line-chart
            [results]="trendData" [xAxis]="true" [yAxis]="true"
            [view]="[700, 300]" [autoScale]="true">
          </ngx-charts-line-chart>
        }
      </app-report-chart-card>
    </div>

    @if (report && report.suggestionsByType.length > 0) {
      <table mat-table [dataSource]="report.suggestionsByType" class="full-width">
        <ng-container matColumnDef="type">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.aiUsage.type' | translate }}</th>
          <td mat-cell *matCellDef="let s">{{ s.type }}</td>
        </ng-container>
        <ng-container matColumnDef="totalCount">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.aiUsage.total' | translate }}</th>
          <td mat-cell *matCellDef="let s">{{ s.totalCount }}</td>
        </ng-container>
        <ng-container matColumnDef="acceptedCount">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.aiUsage.accepted' | translate }}</th>
          <td mat-cell *matCellDef="let s">{{ s.acceptedCount }}</td>
        </ng-container>
        <ng-container matColumnDef="acceptanceRate">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.aiUsage.acceptanceRate' | translate }}</th>
          <td mat-cell *matCellDef="let s">{{ s.acceptanceRate }}%</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="typeColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: typeColumns;"></tr>
      </table>
    }
  `,
  styles: [`
    .charts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 16px; }
    .full-width { width: 100%; margin-block-start: 16px; }
    .summary-cards { display: flex; gap: 16px; margin-block: 16px; }
    .summary-card { padding: 16px 24px; background: rgba(63, 81, 181, 0.08); border-radius: 8px; display: flex; flex-direction: column; }
    .summary-card .label { font-size: 12px; color: rgba(0,0,0,0.6); }
    .summary-card .value { font-size: 24px; font-weight: 600; }
  `]
})
export class AiUsageReportComponent {
  private reportsService = inject(ReportsService);

  loading = false;
  report: AiUsageReportDto | null = null;
  currentParams: Record<string, any> = {};
  barData: any[] = [];
  gaugeData: any[] = [];
  trendData: any[] = [];
  typeColumns = ['type', 'totalCount', 'acceptedCount', 'acceptanceRate'];

  onDateChange(range: { startDate: string; endDate: string }): void {
    this.currentParams = { startDate: range.startDate, endDate: range.endDate };
    this.loadReport();
  }

  private loadReport(): void {
    this.loading = true;
    this.reportsService.getAiUsage(this.currentParams).subscribe({
      next: (data) => {
        this.report = data;
        this.barData = data.suggestionsByType.map(s => ({ name: s.type, value: s.totalCount }));
        const overallAcceptance = data.suggestionsByType.length > 0
          ? data.suggestionsByType.reduce((sum, s) => sum + s.acceptedCount, 0) * 100 /
            Math.max(data.suggestionsByType.reduce((sum, s) => sum + s.totalCount, 0), 1)
          : 0;
        this.gaugeData = [{ name: 'Acceptance Rate', value: Math.round(overallAcceptance * 10) / 10 }];
        this.trendData = [
          { name: 'Suggestions', series: data.timeSeries.map(t => ({ name: t.period, value: t.suggestionCount })) }
        ];
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }
}
```

- [ ] **Step 6: Create CsatReportComponent**

Create `src/client/src/app/features/reports/csat-report/csat-report.ts`:

```typescript
import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { DatePipe } from '@angular/common';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ReportsService, CsatReportDto } from '../reports.service';
import { ReportDateRangeComponent } from '../shared/report-date-range/report-date-range';
import { ReportExportBarComponent } from '../shared/report-export-bar/report-export-bar';
import { ReportChartCardComponent } from '../shared/report-chart-card/report-chart-card';

@Component({
  selector: 'app-csat-report',
  imports: [
    TranslateModule, MatTableModule, DatePipe, NgxChartsModule,
    ReportDateRangeComponent, ReportExportBarComponent, ReportChartCardComponent
  ],
  template: `
    <h1>{{ 'reports.csat.title' | translate }}</h1>
    <app-report-date-range (dateRangeChange)="onDateChange($event)" />
    <app-report-export-bar [reportType]="'csat'" [params]="currentParams" />

    @if (report) {
      <div class="summary-cards">
        <div class="summary-card">
          <span class="label">{{ 'reports.csat.avgRating' | translate }}</span>
          <span class="value">{{ report.averageRating }} / 5</span>
        </div>
        <div class="summary-card">
          <span class="label">{{ 'reports.csat.totalResponses' | translate }}</span>
          <span class="value">{{ report.totalResponses }}</span>
        </div>
      </div>
    }

    <div class="charts-grid">
      <app-report-chart-card [title]="'reports.csat.avgRating'" [loading]="loading">
        @if (gaugeData.length > 0) {
          <ngx-charts-gauge
            [results]="gaugeData" [min]="0" [max]="5"
            [angleSpan]="240" [startAngle]="-120" [view]="[400, 250]">
          </ngx-charts-gauge>
        }
      </app-report-chart-card>

      <app-report-chart-card [title]="'reports.csat.ratingDistribution'" [loading]="loading">
        @if (distData.length > 0) {
          <ngx-charts-bar-vertical
            [results]="distData" [xAxis]="true" [yAxis]="true"
            [view]="[500, 300]">
          </ngx-charts-bar-vertical>
        }
      </app-report-chart-card>
    </div>

    @if (report && report.recentFeedback.length > 0) {
      <h3>{{ 'reports.csat.recentFeedback' | translate }}</h3>
      <table mat-table [dataSource]="report.recentFeedback" class="full-width">
        <ng-container matColumnDef="ticketNumber">
          <th mat-header-cell *matHeaderCellDef>{{ 'tickets.ticketNumber' | translate }}</th>
          <td mat-cell *matCellDef="let f">{{ f.ticketNumber }}</td>
        </ng-container>
        <ng-container matColumnDef="customerName">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.csat.customer' | translate }}</th>
          <td mat-cell *matCellDef="let f">{{ f.customerName }}</td>
        </ng-container>
        <ng-container matColumnDef="rating">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.csat.rating' | translate }}</th>
          <td mat-cell *matCellDef="let f">{{ '★'.repeat(f.rating) }}{{ '☆'.repeat(5 - f.rating) }}</td>
        </ng-container>
        <ng-container matColumnDef="comment">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.csat.comment' | translate }}</th>
          <td mat-cell *matCellDef="let f">{{ f.comment || '-' }}</td>
        </ng-container>
        <ng-container matColumnDef="submittedAt">
          <th mat-header-cell *matHeaderCellDef>{{ 'reports.csat.submittedAt' | translate }}</th>
          <td mat-cell *matCellDef="let f">{{ f.submittedAt | date: 'short' }}</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="feedbackColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: feedbackColumns;"></tr>
      </table>
    }
  `,
  styles: [`
    .charts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 16px; }
    .full-width { width: 100%; margin-block-start: 16px; }
    .summary-cards { display: flex; gap: 16px; margin-block: 16px; }
    .summary-card { padding: 16px 24px; background: rgba(76, 175, 80, 0.08); border-radius: 8px; display: flex; flex-direction: column; }
    .summary-card .label { font-size: 12px; color: rgba(0,0,0,0.6); }
    .summary-card .value { font-size: 24px; font-weight: 600; }
  `]
})
export class CsatReportComponent {
  private reportsService = inject(ReportsService);

  loading = false;
  report: CsatReportDto | null = null;
  currentParams: Record<string, any> = {};
  gaugeData: any[] = [];
  distData: any[] = [];
  feedbackColumns = ['ticketNumber', 'customerName', 'rating', 'comment', 'submittedAt'];

  onDateChange(range: { startDate: string; endDate: string }): void {
    this.currentParams = { startDate: range.startDate, endDate: range.endDate };
    this.loadReport();
  }

  private loadReport(): void {
    this.loading = true;
    this.reportsService.getCsat(this.currentParams).subscribe({
      next: (data) => {
        this.report = data;
        this.gaugeData = [{ name: 'Average Rating', value: data.averageRating }];
        this.distData = data.ratingDistribution.map(r => ({ name: `${r.rating} Star`, value: r.count }));
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }
}
```

- [ ] **Step 7: Add CSAT feedback widget to portal ticket detail**

Find the portal ticket detail component (likely at `src/client/src/app/features/portal/`). Add a feedback section that shows when the ticket has a final status and no feedback exists. The widget should have 5 clickable star icons (`mat-icon` with `star`/`star_border`), a textarea for comment, and a submit button. On submit, call `POST api/v1/portal/tickets/{id}/feedback` with `{ rating, comment }`. Show a success message on completion. The exact file path and implementation depend on the existing portal ticket detail component structure — the implementer should read the component and add the widget at the bottom of the template.

- [ ] **Step 8: Build frontend**

Run from `src/client/`:
```bash
npx ng build --configuration development
```

Expected: build succeeds.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat(reports-ui): add 6 report page components with charts and portal CSAT widget"
```

---

### Task 5: Dashboard Enhancement

**Files:**
- Create: `src/CustomerSupport.Application/Dashboard/DTOs/TicketTrendDto.cs`
- Create: `src/CustomerSupport.Application/Dashboard/DTOs/CategoryDistributionDto.cs`
- Create: `src/CustomerSupport.Application/Dashboard/DTOs/PriorityBreakdownDto.cs`
- Create: `src/CustomerSupport.Application/Dashboard/DTOs/ChannelVolumeDto.cs`
- Create: `src/CustomerSupport.Application/Dashboard/DTOs/SlaBreachDto.cs`
- Create: `src/CustomerSupport.Application/Dashboard/Queries/GetTicketTrendsQuery.cs`
- Create: `src/CustomerSupport.Application/Dashboard/Queries/GetCategoryDistributionQuery.cs`
- Create: `src/CustomerSupport.Application/Dashboard/Queries/GetPriorityBreakdownQuery.cs`
- Create: `src/CustomerSupport.Application/Dashboard/Queries/GetChannelVolumeQuery.cs`
- Create: `src/CustomerSupport.Application/Dashboard/Queries/GetRecentSlaBreachesQuery.cs`
- Modify: `src/CustomerSupport.API/Controllers/DashboardController.cs` — add 5 new endpoints
- Modify: `src/client/src/app/features/dashboard/dashboard.service.ts` — add 5 new methods
- Modify: `src/client/src/app/features/dashboard/dashboard/dashboard.ts` — add chart sections

**Interfaces:**
- Consumes: `AppDbContext`, existing Dashboard DTOs and patterns, `NgxChartsModule` (installed in Task 3)
- Produces: 5 new dashboard query handlers, 5 new API endpoints, enhanced dashboard UI with ngx-charts

- [ ] **Step 1: Create dashboard DTOs**

Create `src/CustomerSupport.Application/Dashboard/DTOs/TicketTrendDto.cs`:

```csharp
namespace CustomerSupport.Application.Dashboard.DTOs;

public record TicketTrendDto(string Date, int CreatedCount, int ResolvedCount);
```

Create `src/CustomerSupport.Application/Dashboard/DTOs/CategoryDistributionDto.cs`:

```csharp
namespace CustomerSupport.Application.Dashboard.DTOs;

public record CategoryDistributionDto(Guid CategoryId, string CategoryName, string CategoryNameAr, int TicketCount);
```

Create `src/CustomerSupport.Application/Dashboard/DTOs/PriorityBreakdownDto.cs`:

```csharp
namespace CustomerSupport.Application.Dashboard.DTOs;

public record PriorityBreakdownDto(Guid PriorityId, string PriorityName, string PriorityNameAr, int Level, int TicketCount);
```

Create `src/CustomerSupport.Application/Dashboard/DTOs/ChannelVolumeDto.cs`:

```csharp
namespace CustomerSupport.Application.Dashboard.DTOs;

public record ChannelVolumeDto(string Channel, int ConversationCount, string Date);
```

Create `src/CustomerSupport.Application/Dashboard/DTOs/SlaBreachDto.cs`:

```csharp
namespace CustomerSupport.Application.Dashboard.DTOs;

public record SlaBreachDto(Guid TicketId, string TicketNumber, string BreachType, string PolicyName, DateTime DueAt, DateTime BreachedAt, double MinutesLate);
```

- [ ] **Step 2: Create 5 dashboard query handlers**

Create `src/CustomerSupport.Application/Dashboard/Queries/GetTicketTrendsQuery.cs`:

```csharp
using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetTicketTrendsQuery(int Days = 30) : IRequest<List<TicketTrendDto>>;

public class GetTicketTrendsQueryHandler : IRequestHandler<GetTicketTrendsQuery, List<TicketTrendDto>>
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public GetTicketTrendsQueryHandler(AppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<List<TicketTrendDto>> Handle(GetTicketTrendsQuery request, CancellationToken cancellationToken)
    {
        var since = _dateTimeService.UtcNow.Date.AddDays(-request.Days);
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        var tickets = await _context.Tickets
            .Where(t => t.CreatedAt >= since)
            .Select(t => new { t.CreatedAt, t.StatusId, t.UpdatedAt })
            .ToListAsync(cancellationToken);

        return Enumerable.Range(0, request.Days + 1)
            .Select(i => since.AddDays(i))
            .Select(date => new TicketTrendDto(
                date.ToString("yyyy-MM-dd"),
                tickets.Count(t => t.CreatedAt.Date == date),
                tickets.Count(t => finalStatusIds.Contains(t.StatusId) && t.UpdatedAt.Date == date)))
            .ToList();
    }
}
```

Create `src/CustomerSupport.Application/Dashboard/Queries/GetCategoryDistributionQuery.cs`:

```csharp
using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetCategoryDistributionQuery : IRequest<List<CategoryDistributionDto>>;

public class GetCategoryDistributionQueryHandler : IRequestHandler<GetCategoryDistributionQuery, List<CategoryDistributionDto>>
{
    private readonly AppDbContext _context;

    public GetCategoryDistributionQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<CategoryDistributionDto>> Handle(GetCategoryDistributionQuery request, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        return await _context.Tickets
            .Where(t => !finalStatusIds.Contains(t.StatusId))
            .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Category!.NameAr })
            .Select(g => new CategoryDistributionDto(g.Key.CategoryId, g.Key.Name, g.Key.NameAr, g.Count()))
            .OrderByDescending(c => c.TicketCount)
            .ToListAsync(cancellationToken);
    }
}
```

Create `src/CustomerSupport.Application/Dashboard/Queries/GetPriorityBreakdownQuery.cs`:

```csharp
using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetPriorityBreakdownQuery : IRequest<List<PriorityBreakdownDto>>;

public class GetPriorityBreakdownQueryHandler : IRequestHandler<GetPriorityBreakdownQuery, List<PriorityBreakdownDto>>
{
    private readonly AppDbContext _context;

    public GetPriorityBreakdownQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<PriorityBreakdownDto>> Handle(GetPriorityBreakdownQuery request, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        return await _context.Tickets
            .Where(t => !finalStatusIds.Contains(t.StatusId))
            .GroupBy(t => new { t.PriorityId, t.Priority!.Name, t.Priority!.NameAr, t.Priority!.Level })
            .Select(g => new PriorityBreakdownDto(g.Key.PriorityId, g.Key.Name, g.Key.NameAr, g.Key.Level, g.Count()))
            .OrderByDescending(p => p.Level)
            .ToListAsync(cancellationToken);
    }
}
```

Create `src/CustomerSupport.Application/Dashboard/Queries/GetChannelVolumeQuery.cs`:

```csharp
using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetChannelVolumeQuery(int Days = 30) : IRequest<List<ChannelVolumeDto>>;

public class GetChannelVolumeQueryHandler : IRequestHandler<GetChannelVolumeQuery, List<ChannelVolumeDto>>
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public GetChannelVolumeQueryHandler(AppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<List<ChannelVolumeDto>> Handle(GetChannelVolumeQuery request, CancellationToken cancellationToken)
    {
        var since = _dateTimeService.UtcNow.Date.AddDays(-request.Days);
        return await _context.Conversations
            .Where(c => c.CreatedAt >= since)
            .GroupBy(c => new { Channel = c.Channel.ToString(), Date = c.CreatedAt.Date.ToString() })
            .Select(g => new ChannelVolumeDto(g.Key.Channel, g.Count(), g.Key.Date))
            .ToListAsync(cancellationToken);
    }
}
```

Create `src/CustomerSupport.Application/Dashboard/Queries/GetRecentSlaBreachesQuery.cs`:

```csharp
using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetRecentSlaBreachesQuery(int Count = 10) : IRequest<List<SlaBreachDto>>;

public class GetRecentSlaBreachesQueryHandler : IRequestHandler<GetRecentSlaBreachesQuery, List<SlaBreachDto>>
{
    private readonly AppDbContext _context;

    public GetRecentSlaBreachesQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<SlaBreachDto>> Handle(GetRecentSlaBreachesQuery request, CancellationToken cancellationToken)
    {
        return await _context.SlaBreachLogs
            .Include(b => b.Ticket)
            .Include(b => b.SlaPolicy)
            .OrderByDescending(b => b.BreachedAt)
            .Take(request.Count)
            .Select(b => new SlaBreachDto(
                b.TicketId, b.Ticket.TicketNumber, b.BreachType,
                b.SlaPolicy.Name, b.DueAt, b.BreachedAt,
                (b.BreachedAt - b.DueAt).TotalMinutes))
            .ToListAsync(cancellationToken);
    }
}
```

- [ ] **Step 3: Add 5 new endpoints to DashboardController**

In `src/CustomerSupport.API/Controllers/DashboardController.cs`, add these endpoints after the existing `GetTeamWorkload` method:

```csharp
    [HttpGet("ticket-trends")]
    [Authorize(Policy = "Permission:dashboard.view")]
    public async Task<ActionResult<List<TicketTrendDto>>> GetTicketTrends([FromQuery] int days = 30)
        => Ok(await _mediator.Send(new GetTicketTrendsQuery(days)));

    [HttpGet("category-distribution")]
    [Authorize(Policy = "Permission:dashboard.view")]
    public async Task<ActionResult<List<CategoryDistributionDto>>> GetCategoryDistribution()
        => Ok(await _mediator.Send(new GetCategoryDistributionQuery()));

    [HttpGet("priority-breakdown")]
    [Authorize(Policy = "Permission:dashboard.view")]
    public async Task<ActionResult<List<PriorityBreakdownDto>>> GetPriorityBreakdown()
        => Ok(await _mediator.Send(new GetPriorityBreakdownQuery()));

    [HttpGet("channel-volume")]
    [Authorize(Policy = "Permission:dashboard.view")]
    public async Task<ActionResult<List<ChannelVolumeDto>>> GetChannelVolume([FromQuery] int days = 30)
        => Ok(await _mediator.Send(new GetChannelVolumeQuery(days)));

    [HttpGet("recent-sla-breaches")]
    [Authorize(Policy = "Permission:dashboard.view")]
    public async Task<ActionResult<List<SlaBreachDto>>> GetRecentSlaBreaches([FromQuery] int count = 10)
        => Ok(await _mediator.Send(new GetRecentSlaBreachesQuery(count)));
```

Add the necessary usings at the top of the file:

```csharp
using CustomerSupport.Application.Dashboard.DTOs;
```

(Note: `DashboardStatsDto`, `SlaSummaryDto`, `AgentWorkloadDto` should already be imported; the new DTOs are in the same namespace.)

- [ ] **Step 4: Extend dashboard.service.ts**

In `src/client/src/app/features/dashboard/dashboard.service.ts`, add these interfaces and methods:

After the existing `AgentWorkloadDto` interface, add:

```typescript
export interface TicketTrendDto {
  date: string;
  createdCount: number;
  resolvedCount: number;
}

export interface CategoryDistributionDto {
  categoryId: string;
  categoryName: string;
  categoryNameAr: string;
  ticketCount: number;
}

export interface PriorityBreakdownDto {
  priorityId: string;
  priorityName: string;
  priorityNameAr: string;
  level: number;
  ticketCount: number;
}

export interface ChannelVolumeDto {
  channel: string;
  conversationCount: number;
  date: string;
}

export interface SlaBreachDto {
  ticketId: string;
  ticketNumber: string;
  breachType: string;
  policyName: string;
  dueAt: string;
  breachedAt: string;
  minutesLate: number;
}
```

Inside the `DashboardService` class, add after `getTeamWorkload()`:

```typescript
  getTicketTrends(days = 30): Observable<TicketTrendDto[]> {
    return this.get<TicketTrendDto[]>('/v1/Dashboard/ticket-trends', { days });
  }

  getCategoryDistribution(): Observable<CategoryDistributionDto[]> {
    return this.get<CategoryDistributionDto[]>('/v1/Dashboard/category-distribution');
  }

  getPriorityBreakdown(): Observable<PriorityBreakdownDto[]> {
    return this.get<PriorityBreakdownDto[]>('/v1/Dashboard/priority-breakdown');
  }

  getChannelVolume(days = 30): Observable<ChannelVolumeDto[]> {
    return this.get<ChannelVolumeDto[]>('/v1/Dashboard/channel-volume', { days });
  }

  getRecentSlaBreaches(count = 10): Observable<SlaBreachDto[]> {
    return this.get<SlaBreachDto[]>('/v1/Dashboard/recent-sla-breaches', { count });
  }
```

- [ ] **Step 5: Enhance DashboardComponent with chart sections**

In `src/client/src/app/features/dashboard/dashboard/dashboard.ts`, add `NgxChartsModule` to imports:

```typescript
import { NgxChartsModule } from '@swimlane/ngx-charts';
```

Add `NgxChartsModule` to the `imports` array in the `@Component` decorator.

Import the new DTO types:

```typescript
import {
  DashboardService,
  DashboardStatsDto,
  SlaSummaryDto,
  AgentWorkloadDto,
  TicketTrendDto,
  CategoryDistributionDto,
  PriorityBreakdownDto,
  ChannelVolumeDto,
  SlaBreachDto
} from '../dashboard.service';
```

Add new class properties:

```typescript
  ticketTrends: TicketTrendDto[] = [];
  categoryDistribution: CategoryDistributionDto[] = [];
  priorityBreakdown: PriorityBreakdownDto[] = [];
  channelVolume: ChannelVolumeDto[] = [];
  recentBreaches: SlaBreachDto[] = [];
  breachColumns = ['ticketNumber', 'breachType', 'policyName', 'minutesLate'];

  trendLineData: any[] = [];
  categoryPieData: any[] = [];
  priorityBarData: any[] = [];
  channelStackedData: any[] = [];
```

In the `loadAll()` method's `forkJoin`, add the 5 new calls:

```typescript
    forkJoin({
      stats: this.dashboardService.getStats(),
      slaSummary: this.dashboardService.getSlaSummary(),
      myTickets: this.dashboardService.getMyTickets(this.myTicketsPage, this.myTicketsPageSize),
      teamWorkload: this.dashboardService.getTeamWorkload(),
      ticketTrends: this.dashboardService.getTicketTrends(),
      categoryDistribution: this.dashboardService.getCategoryDistribution(),
      priorityBreakdown: this.dashboardService.getPriorityBreakdown(),
      channelVolume: this.dashboardService.getChannelVolume(),
      recentBreaches: this.dashboardService.getRecentSlaBreaches()
    }).subscribe({
      next: ({ stats, slaSummary, myTickets, teamWorkload, ticketTrends, categoryDistribution, priorityBreakdown, channelVolume, recentBreaches }) => {
        this.stats = stats;
        this.slaSummary = slaSummary;
        this.myTickets = myTickets.items;
        this.myTicketsTotalCount = myTickets.totalCount;
        this.myTicketsPage = myTickets.page;
        this.myTicketsPageSize = myTickets.pageSize;
        this.teamWorkload = [...teamWorkload].sort((a, b) => b.openTickets - a.openTickets);

        this.ticketTrends = ticketTrends;
        this.trendLineData = [
          { name: 'Created', series: ticketTrends.map(t => ({ name: t.date, value: t.createdCount })) },
          { name: 'Resolved', series: ticketTrends.map(t => ({ name: t.date, value: t.resolvedCount })) }
        ];
        this.categoryDistribution = categoryDistribution;
        this.categoryPieData = categoryDistribution.map(c => ({ name: c.categoryName, value: c.ticketCount }));
        this.priorityBreakdown = priorityBreakdown;
        this.priorityBarData = priorityBreakdown.map(p => ({ name: p.priorityName, value: p.ticketCount }));
        this.channelVolume = channelVolume;
        const channels = [...new Set(channelVolume.map(c => c.channel))];
        const dates = [...new Set(channelVolume.map(c => c.date))].sort();
        this.channelStackedData = dates.map(date => ({
          name: date,
          series: channels.map(ch => ({
            name: ch,
            value: channelVolume.find(c => c.date === date && c.channel === ch)?.conversationCount ?? 0
          }))
        }));
        this.recentBreaches = recentBreaches;
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
```

Add chart template sections at the bottom of the template (after the team workload card, before closing):

```html
    <div class="charts-grid">
      <mat-card>
        <mat-card-header><mat-card-title>{{ 'dashboard.ticketTrends' | translate }}</mat-card-title></mat-card-header>
        <mat-card-content>
          @if (trendLineData.length > 0) {
            <ngx-charts-line-chart
              [results]="trendLineData" [xAxis]="true" [yAxis]="true"
              [legend]="true" [view]="[550, 250]" [autoScale]="true">
            </ngx-charts-line-chart>
          }
        </mat-card-content>
      </mat-card>

      <mat-card>
        <mat-card-header><mat-card-title>{{ 'dashboard.categoryDistribution' | translate }}</mat-card-title></mat-card-header>
        <mat-card-content>
          @if (categoryPieData.length > 0) {
            <ngx-charts-pie-chart [results]="categoryPieData" [legend]="true" [view]="[450, 250]"></ngx-charts-pie-chart>
          }
        </mat-card-content>
      </mat-card>

      <mat-card>
        <mat-card-header><mat-card-title>{{ 'dashboard.priorityBreakdown' | translate }}</mat-card-title></mat-card-header>
        <mat-card-content>
          @if (priorityBarData.length > 0) {
            <ngx-charts-bar-horizontal [results]="priorityBarData" [xAxis]="true" [yAxis]="true" [view]="[450, 250]"></ngx-charts-bar-horizontal>
          }
        </mat-card-content>
      </mat-card>

      <mat-card>
        <mat-card-header><mat-card-title>{{ 'dashboard.channelVolume' | translate }}</mat-card-title></mat-card-header>
        <mat-card-content>
          @if (channelStackedData.length > 0) {
            <ngx-charts-bar-vertical-stacked
              [results]="channelStackedData" [xAxis]="true" [yAxis]="true"
              [legend]="true" [view]="[550, 250]">
            </ngx-charts-bar-vertical-stacked>
          }
        </mat-card-content>
      </mat-card>
    </div>

    @if (recentBreaches.length > 0) {
      <mat-card class="breaches-card">
        <mat-card-header><mat-card-title>{{ 'dashboard.recentSlaBreaches' | translate }}</mat-card-title></mat-card-header>
        <mat-card-content>
          <table mat-table [dataSource]="recentBreaches" class="full-width">
            <ng-container matColumnDef="ticketNumber">
              <th mat-header-cell *matHeaderCellDef>{{ 'tickets.ticketNumber' | translate }}</th>
              <td mat-cell *matCellDef="let b"><a [routerLink]="['/admin/tickets', b.ticketId]">{{ b.ticketNumber }}</a></td>
            </ng-container>
            <ng-container matColumnDef="breachType">
              <th mat-header-cell *matHeaderCellDef>{{ 'dashboard.breachType' | translate }}</th>
              <td mat-cell *matCellDef="let b">{{ b.breachType }}</td>
            </ng-container>
            <ng-container matColumnDef="policyName">
              <th mat-header-cell *matHeaderCellDef>{{ 'dashboard.policy' | translate }}</th>
              <td mat-cell *matCellDef="let b">{{ b.policyName }}</td>
            </ng-container>
            <ng-container matColumnDef="minutesLate">
              <th mat-header-cell *matHeaderCellDef>{{ 'dashboard.minutesLate' | translate }}</th>
              <td mat-cell *matCellDef="let b">{{ b.minutesLate | number:'1.0-0' }}</td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="breachColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: breachColumns;"></tr>
          </table>
        </mat-card-content>
      </mat-card>
    }
```

Add to the component's styles:

```css
    .charts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(450px, 1fr)); gap: 16px; margin-block-end: 16px; }
    .breaches-card { margin-block-end: 16px; }
```

- [ ] **Step 6: Add dashboard i18n keys**

In `src/client/src/assets/i18n/en.json`, add to the `"dashboard"` section:

```json
    "ticketTrends": "Ticket Trends (30 Days)",
    "categoryDistribution": "Open Tickets by Category",
    "priorityBreakdown": "Open Tickets by Priority",
    "channelVolume": "Conversations by Channel",
    "recentSlaBreaches": "Recent SLA Breaches",
    "breachType": "Breach Type",
    "policy": "Policy",
    "minutesLate": "Minutes Late"
```

In `src/client/src/assets/i18n/ar.json`, add to the `"dashboard"` section:

```json
    "ticketTrends": "اتجاهات التذاكر (30 يوم)",
    "categoryDistribution": "التذاكر المفتوحة حسب الفئة",
    "priorityBreakdown": "التذاكر المفتوحة حسب الأولوية",
    "channelVolume": "المحادثات حسب القناة",
    "recentSlaBreaches": "خروقات اتفاقية مستوى الخدمة الأخيرة",
    "breachType": "نوع الخرق",
    "policy": "السياسة",
    "minutesLate": "الدقائق المتأخرة"
```

- [ ] **Step 7: Build both frontend and backend**

Run:
```bash
dotnet build src/CustomerSupport.API
```

And from `src/client/`:
```bash
npx ng build --configuration development
```

Expected: both build successfully.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat(dashboard): add ticket trends, category/priority distribution, channel volume, and SLA breaches charts"
```

---

### Task 6: External Integrations Backend

**Files:**
- Create: `src/CustomerSupport.Domain/Entities/WebhookSubscription.cs`
- Create: `src/CustomerSupport.Domain/Entities/WebhookDeliveryLog.cs`
- Create: `src/CustomerSupport.Domain/Interfaces/IWebhookDispatcher.cs`
- Create: `src/CustomerSupport.Domain/Interfaces/IErpConnector.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/WebhookSubscriptionConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/WebhookDeliveryLogConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Integrations/WebhookDispatcher.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Integrations/MockErpConnector.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Integrations/ErpSettings.cs`
- Create: `src/CustomerSupport.Application/Common/Notifications/TicketStatusChangedNotification.cs`
- Create: `src/CustomerSupport.Application/Common/Notifications/SlaBreachedNotification.cs`
- Create: `src/CustomerSupport.Application/Common/Notifications/ConversationClosedNotification.cs`
- Create: `src/CustomerSupport.Application/Common/Notifications/TicketEscalatedNotification.cs`
- Create: `src/CustomerSupport.Application/Integrations/Handlers/WebhookTicketCreatedHandler.cs`
- Create: `src/CustomerSupport.Application/Integrations/Handlers/WebhookTicketStatusChangedHandler.cs`
- Create: `src/CustomerSupport.Application/Integrations/Handlers/WebhookSlaBreachedHandler.cs`
- Create: `src/CustomerSupport.Application/Integrations/Handlers/WebhookConversationHandler.cs`
- Create: `src/CustomerSupport.Application/Integrations/DTOs/WebhookSubscriptionDto.cs`
- Create: `src/CustomerSupport.Application/Integrations/DTOs/WebhookDeliveryLogDto.cs`
- Create: `src/CustomerSupport.Application/Integrations/Commands/CreateWebhookSubscriptionCommand.cs`
- Create: `src/CustomerSupport.Application/Integrations/Commands/UpdateWebhookSubscriptionCommand.cs`
- Create: `src/CustomerSupport.Application/Integrations/Commands/DeleteWebhookSubscriptionCommand.cs`
- Create: `src/CustomerSupport.Application/Integrations/Commands/TestWebhookCommand.cs`
- Create: `src/CustomerSupport.Application/Integrations/Queries/GetWebhookSubscriptionsQuery.cs`
- Create: `src/CustomerSupport.Application/Integrations/Queries/GetWebhookSubscriptionQuery.cs`
- Create: `src/CustomerSupport.Application/Integrations/Queries/GetWebhookDeliveryLogsQuery.cs`
- Create: `src/CustomerSupport.Application/Integrations/Validators/CreateWebhookSubscriptionValidator.cs`
- Create: `src/CustomerSupport.API/Controllers/WebhookSubscriptionController.cs`
- Create: `src/CustomerSupport.API/Controllers/ErpController.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs` — add DbSets
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs` — register services
- Modify: `src/CustomerSupport.API/appsettings.json` — add ErpSettings
- Modify: `src/CustomerSupport.API/appsettings.Development.json` — add ErpSettings
- Modify: `src/CustomerSupport.API/Program.cs` — configure ErpSettings
- Modify: `src/CustomerSupport.Application/Tickets/Commands/UpdateTicketStatusCommand.cs` — publish TicketStatusChangedNotification

**Interfaces:**
- Consumes: `BaseEntity`, `ITenantEntity`, `AppDbContext`, existing MediatR notifications (`TicketCreatedNotification`, `ConversationCreatedNotification`)
- Produces:
  - `IWebhookDispatcher.DispatchAsync(Guid tenantId, string eventName, object payload)` — used by webhook event handlers
  - `IErpConnector` with `SyncTicketAsync`, `SyncCustomerAsync`, `GetCustomerByExternalIdAsync`
  - `WebhookSubscription` and `WebhookDeliveryLog` entities
  - 4 new MediatR notification types
  - CRUD endpoints at `api/v1/webhooks/subscriptions`
  - ERP endpoints at `api/v1/integrations/erp`

- [ ] **Step 1: Create WebhookSubscription entity**

Create `src/CustomerSupport.Domain/Entities/WebhookSubscription.cs`:

```csharp
namespace CustomerSupport.Domain.Entities;

public class WebhookSubscription : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Events { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public string? Headers { get; set; }

    public ICollection<WebhookDeliveryLog> DeliveryLogs { get; set; } = new List<WebhookDeliveryLog>();
}
```

- [ ] **Step 2: Create WebhookDeliveryLog entity**

Create `src/CustomerSupport.Domain/Entities/WebhookDeliveryLog.cs`:

```csharp
namespace CustomerSupport.Domain.Entities;

public class WebhookDeliveryLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string Event { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int? StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public int Attempt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public WebhookSubscription Subscription { get; set; } = null!;
}
```

- [ ] **Step 3: Create EF configurations**

Create `src/CustomerSupport.Infrastructure/Persistence/Configurations/WebhookSubscriptionConfiguration.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Url).IsRequired().HasMaxLength(2000);
        builder.Property(w => w.Secret).IsRequired().HasMaxLength(500);
        builder.Property(w => w.Events).IsRequired();
        builder.Property(w => w.Headers).HasMaxLength(4000);

        builder.HasIndex(w => new { w.TenantId, w.IsActive });
        builder.HasIndex(w => new { w.TenantId, w.Name }).IsUnique();

        builder.HasMany(w => w.DeliveryLogs).WithOne(d => d.Subscription)
            .HasForeignKey(d => d.SubscriptionId).OnDelete(DeleteBehavior.Cascade);
    }
}
```

Create `src/CustomerSupport.Infrastructure/Persistence/Configurations/WebhookDeliveryLogConfiguration.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class WebhookDeliveryLogConfiguration : IEntityTypeConfiguration<WebhookDeliveryLog>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLog> builder)
    {
        builder.ToTable("WebhookDeliveryLogs");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Event).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Payload).IsRequired().HasMaxLength(65536);
        builder.Property(d => d.ResponseBody).HasMaxLength(4000);
        builder.Property(d => d.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(d => new { d.SubscriptionId, d.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(d => new { d.TenantId, d.Event, d.CreatedAt }).IsDescending(false, false, true);
    }
}
```

- [ ] **Step 4: Add DbSets to AppDbContext**

In `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs`, add after `TicketFeedbacks`:

```csharp
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();
```

- [ ] **Step 5: Create IWebhookDispatcher interface**

Create `src/CustomerSupport.Domain/Interfaces/IWebhookDispatcher.cs`:

```csharp
namespace CustomerSupport.Domain.Interfaces;

public interface IWebhookDispatcher
{
    Task DispatchAsync(Guid tenantId, string eventName, object payload);
}
```

- [ ] **Step 6: Create IErpConnector interface**

Create `src/CustomerSupport.Domain/Interfaces/IErpConnector.cs`:

```csharp
namespace CustomerSupport.Domain.Interfaces;

public interface IErpConnector
{
    Task<ErpSyncResult> SyncTicketAsync(ErpTicketData ticket);
    Task<ErpSyncResult> SyncCustomerAsync(ErpCustomerData customer);
    Task<ErpCustomerData?> GetCustomerByExternalIdAsync(string externalId);
}

public record ErpSyncResult(bool Success, string? ExternalId, string? ErrorMessage);
public record ErpTicketData(Guid TicketId, string TicketNumber, string Subject, string CustomerName, string Status, string Priority, DateTime CreatedAt, DateTime? ResolvedAt);
public record ErpCustomerData(string? ExternalId, string Name, string? Email, string? Phone, string? Company);
```

- [ ] **Step 7: Implement WebhookDispatcher**

Create `src/CustomerSupport.Infrastructure/Services/Integrations/WebhookDispatcher.cs`:

```csharp
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services.Integrations;

public class WebhookDispatcher : IWebhookDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDispatcher> _logger;

    public WebhookDispatcher(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task DispatchAsync(Guid tenantId, string eventName, object payload)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var subscriptions = await context.WebhookSubscriptions
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .ToListAsync();

        var matching = subscriptions
            .Where(s => JsonSerializer.Deserialize<string[]>(s.Events)?.Contains(eventName) == true)
            .ToList();

        var jsonPayload = JsonSerializer.Serialize(payload);

        foreach (var sub in matching)
        {
            _ = Task.Run(() => DeliverWithRetry(sub, tenantId, eventName, jsonPayload));
        }
    }

    private async Task DeliverWithRetry(WebhookSubscription subscription, Guid tenantId, string eventName, string jsonPayload)
    {
        var delays = new[] { 0, 2000, 8000 };

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            if (attempt > 1)
                await Task.Delay(delays[attempt - 1]);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                var signature = ComputeHmac(subscription.Secret, jsonPayload);

                var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                request.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
                request.Headers.Add("X-Webhook-Event", eventName);

                if (!string.IsNullOrEmpty(subscription.Headers))
                {
                    var extraHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(subscription.Headers);
                    if (extraHeaders is not null)
                    {
                        foreach (var (key, value) in extraHeaders)
                            request.Headers.TryAddWithoutValidation(key, value);
                    }
                }

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                var log = new WebhookDeliveryLog
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SubscriptionId = subscription.Id,
                    Event = eventName,
                    Payload = jsonPayload.Length > 65536 ? jsonPayload[..65536] : jsonPayload,
                    StatusCode = (int)response.StatusCode,
                    ResponseBody = responseBody.Length > 4000 ? responseBody[..4000] : responseBody,
                    Attempt = attempt,
                    Success = response.IsSuccessStatusCode
                };

                context.WebhookDeliveryLogs.Add(log);
                await context.SaveChangesAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Webhook delivered: {Event} to {Url} (attempt {Attempt})", eventName, subscription.Url, attempt);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Webhook delivery failed: {Event} to {Url} (attempt {Attempt})", eventName, subscription.Url, attempt);

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.WebhookDeliveryLogs.Add(new WebhookDeliveryLog
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SubscriptionId = subscription.Id,
                    Event = eventName,
                    Payload = jsonPayload.Length > 65536 ? jsonPayload[..65536] : jsonPayload,
                    Attempt = attempt,
                    Success = false,
                    ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message
                });
                await context.SaveChangesAsync();
            }
        }
    }

    private static string ComputeHmac(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(hash);
    }
}
```

- [ ] **Step 8: Implement MockErpConnector and ErpSettings**

Create `src/CustomerSupport.Infrastructure/Services/Integrations/ErpSettings.cs`:

```csharp
namespace CustomerSupport.Infrastructure.Services.Integrations;

public class ErpSettings
{
    public string Provider { get; set; } = "Mock";
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
```

Create `src/CustomerSupport.Infrastructure/Services/Integrations/MockErpConnector.cs`:

```csharp
using CustomerSupport.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services.Integrations;

public class MockErpConnector : IErpConnector
{
    private readonly ILogger<MockErpConnector> _logger;

    public MockErpConnector(ILogger<MockErpConnector> logger) => _logger = logger;

    public Task<ErpSyncResult> SyncTicketAsync(ErpTicketData ticket)
    {
        _logger.LogInformation("Mock ERP: SyncTicket {TicketNumber} ({Subject})", ticket.TicketNumber, ticket.Subject);
        return Task.FromResult(new ErpSyncResult(true, Guid.NewGuid().ToString(), null));
    }

    public Task<ErpSyncResult> SyncCustomerAsync(ErpCustomerData customer)
    {
        _logger.LogInformation("Mock ERP: SyncCustomer {Name} ({Email})", customer.Name, customer.Email);
        return Task.FromResult(new ErpSyncResult(true, Guid.NewGuid().ToString(), null));
    }

    public Task<ErpCustomerData?> GetCustomerByExternalIdAsync(string externalId)
    {
        _logger.LogInformation("Mock ERP: GetCustomer {ExternalId}", externalId);
        return Task.FromResult<ErpCustomerData?>(null);
    }
}
```

- [ ] **Step 9: Create 4 new MediatR notification types**

Create `src/CustomerSupport.Application/Common/Notifications/TicketStatusChangedNotification.cs`:

```csharp
using MediatR;

namespace CustomerSupport.Application.Common.Notifications;

public record TicketStatusChangedNotification(
    Guid TicketId, Guid TenantId, Guid OldStatusId, Guid NewStatusId) : INotification;
```

Create `src/CustomerSupport.Application/Common/Notifications/SlaBreachedNotification.cs`:

```csharp
using MediatR;

namespace CustomerSupport.Application.Common.Notifications;

public record SlaBreachedNotification(
    Guid TicketId, Guid TenantId, string BreachType, Guid SlaPolicyId) : INotification;
```

Create `src/CustomerSupport.Application/Common/Notifications/ConversationClosedNotification.cs`:

```csharp
using MediatR;

namespace CustomerSupport.Application.Common.Notifications;

public record ConversationClosedNotification(
    Guid ConversationId, Guid TenantId) : INotification;
```

Create `src/CustomerSupport.Application/Common/Notifications/TicketEscalatedNotification.cs`:

```csharp
using MediatR;

namespace CustomerSupport.Application.Common.Notifications;

public record TicketEscalatedNotification(
    Guid TicketId, Guid TenantId, string Reason) : INotification;
```

- [ ] **Step 10: Publish TicketStatusChangedNotification in UpdateTicketStatusCommand**

In `src/CustomerSupport.Application/Tickets/Commands/UpdateTicketStatusCommand.cs`, add `IPublisher` to the handler constructor and publish the notification after the status change:

Add using:
```csharp
using CustomerSupport.Application.Common.Notifications;
```

Add `IPublisher _publisher` field and constructor parameter. After `await _ticketRepository.UpdateAsync(ticket, cancellationToken);`, add:

```csharp
        try
        {
            await _publisher.Publish(new TicketStatusChangedNotification(
                ticket.Id, ticket.TenantId, request.StatusId == ticket.StatusId ? ticket.StatusId : oldStatus!.Id, request.StatusId), cancellationToken);
        }
        catch { }
```

The full updated handler class should be:

```csharp
public class UpdateTicketStatusCommandHandler : IRequestHandler<UpdateTicketStatusCommand, Result>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPublisher _publisher;

    public UpdateTicketStatusCommandHandler(
        ITicketRepository ticketRepository, AppDbContext context,
        ICurrentUserService currentUserService, IPublisher publisher)
    {
        _ticketRepository = ticketRepository;
        _context = context;
        _currentUserService = currentUserService;
        _publisher = publisher;
    }

    public async Task<Result> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null) return Result.Failure("Ticket not found.");

        var oldStatusId = ticket.StatusId;
        var oldStatus = await _context.TicketStatuses.FindAsync([ticket.StatusId], cancellationToken);
        var newStatus = await _context.TicketStatuses.FindAsync([request.StatusId], cancellationToken);

        _context.TicketHistories.Add(new TicketHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            UserId = _currentUserService.UserId == Guid.Empty ? null : _currentUserService.UserId,
            Field = "Status",
            OldValue = oldStatus?.Name,
            NewValue = newStatus?.Name,
            CreatedAt = DateTime.UtcNow
        });

        ticket.StatusId = request.StatusId;
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);

        try
        {
            await _publisher.Publish(new TicketStatusChangedNotification(
                ticket.Id, ticket.TenantId, oldStatusId, request.StatusId), cancellationToken);
        }
        catch { }

        return Result.Success();
    }
}
```

- [ ] **Step 11: Create webhook event handlers**

Create `src/CustomerSupport.Application/Integrations/Handlers/WebhookTicketCreatedHandler.cs`:

```csharp
using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Handlers;

public class WebhookTicketCreatedHandler : INotificationHandler<TicketCreatedNotification>
{
    private readonly IWebhookDispatcher _dispatcher;
    private readonly AppDbContext _context;

    public WebhookTicketCreatedHandler(IWebhookDispatcher dispatcher, AppDbContext context)
    {
        _dispatcher = dispatcher;
        _context = context;
    }

    public async Task Handle(TicketCreatedNotification notification, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Customer).Include(t => t.Category).Include(t => t.Priority)
            .FirstOrDefaultAsync(t => t.Id == notification.TicketId, cancellationToken);
        if (ticket is null) return;

        await _dispatcher.DispatchAsync(notification.TenantId, "ticket.created", new
        {
            ticketId = ticket.Id,
            ticketNumber = ticket.TicketNumber,
            subject = ticket.Subject,
            categoryName = ticket.Category?.Name,
            priorityName = ticket.Priority?.Name,
            customerName = ticket.Customer?.Name
        });
    }
}
```

Create `src/CustomerSupport.Application/Integrations/Handlers/WebhookTicketStatusChangedHandler.cs`:

```csharp
using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Handlers;

public class WebhookTicketStatusChangedHandler : INotificationHandler<TicketStatusChangedNotification>
{
    private readonly IWebhookDispatcher _dispatcher;
    private readonly AppDbContext _context;

    public WebhookTicketStatusChangedHandler(IWebhookDispatcher dispatcher, AppDbContext context)
    {
        _dispatcher = dispatcher;
        _context = context;
    }

    public async Task Handle(TicketStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == notification.TicketId, cancellationToken);
        if (ticket is null) return;

        var oldStatus = await _context.TicketStatuses.FindAsync([notification.OldStatusId], cancellationToken);
        var newStatus = await _context.TicketStatuses.FindAsync([notification.NewStatusId], cancellationToken);

        await _dispatcher.DispatchAsync(notification.TenantId, "ticket.status_changed", new
        {
            ticketId = ticket.Id,
            ticketNumber = ticket.TicketNumber,
            oldStatus = oldStatus?.Name,
            newStatus = newStatus?.Name
        });

        if (newStatus?.IsFinal == true)
        {
            await _dispatcher.DispatchAsync(notification.TenantId, "ticket.resolved", new
            {
                ticketId = ticket.Id,
                ticketNumber = ticket.TicketNumber,
                resolvedAt = DateTime.UtcNow,
                resolutionMinutes = (DateTime.UtcNow - ticket.CreatedAt).TotalMinutes
            });
        }
    }
}
```

Create `src/CustomerSupport.Application/Integrations/Handlers/WebhookSlaBreachedHandler.cs`:

```csharp
using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Handlers;

public class WebhookSlaBreachedHandler : INotificationHandler<SlaBreachedNotification>
{
    private readonly IWebhookDispatcher _dispatcher;
    private readonly AppDbContext _context;

    public WebhookSlaBreachedHandler(IWebhookDispatcher dispatcher, AppDbContext context)
    {
        _dispatcher = dispatcher;
        _context = context;
    }

    public async Task Handle(SlaBreachedNotification notification, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == notification.TicketId, cancellationToken);
        var policy = await _context.SlaPolicies.FindAsync([notification.SlaPolicyId], cancellationToken);
        if (ticket is null) return;

        await _dispatcher.DispatchAsync(notification.TenantId, "sla.breached", new
        {
            ticketId = ticket.Id,
            ticketNumber = ticket.TicketNumber,
            breachType = notification.BreachType,
            policyName = policy?.Name,
            dueAt = DateTime.UtcNow
        });
    }
}
```

Create `src/CustomerSupport.Application/Integrations/Handlers/WebhookConversationHandler.cs`:

```csharp
using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Handlers;

public class WebhookConversationCreatedHandler : INotificationHandler<ConversationCreatedNotification>
{
    private readonly IWebhookDispatcher _dispatcher;
    private readonly AppDbContext _context;

    public WebhookConversationCreatedHandler(IWebhookDispatcher dispatcher, AppDbContext context)
    {
        _dispatcher = dispatcher;
        _context = context;
    }

    public async Task Handle(ConversationCreatedNotification notification, CancellationToken cancellationToken)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == notification.ConversationId, cancellationToken);
        if (conversation is null) return;

        await _dispatcher.DispatchAsync(conversation.TenantId, "conversation.created", new
        {
            conversationId = conversation.Id,
            channel = conversation.Channel.ToString(),
            customerName = conversation.Customer?.Name
        });
    }
}

public class WebhookConversationClosedHandler : INotificationHandler<ConversationClosedNotification>
{
    private readonly IWebhookDispatcher _dispatcher;

    public WebhookConversationClosedHandler(IWebhookDispatcher dispatcher) => _dispatcher = dispatcher;

    public async Task Handle(ConversationClosedNotification notification, CancellationToken cancellationToken)
    {
        await _dispatcher.DispatchAsync(notification.TenantId, "conversation.closed", new
        {
            conversationId = notification.ConversationId,
            closedAt = DateTime.UtcNow
        });
    }
}
```

- [ ] **Step 12: Create webhook CRUD commands, queries, and DTOs**

Create `src/CustomerSupport.Application/Integrations/DTOs/WebhookSubscriptionDto.cs`:

```csharp
namespace CustomerSupport.Application.Integrations.DTOs;

public record WebhookSubscriptionDto(
    Guid Id, string Name, string Url, string[] Events,
    bool IsActive, DateTime CreatedAt);

public record WebhookDeliveryLogDto(
    Guid Id, string Event, int? StatusCode, bool Success,
    int Attempt, string? ErrorMessage, DateTime CreatedAt);
```

Create `src/CustomerSupport.Application/Integrations/Commands/CreateWebhookSubscriptionCommand.cs`:

```csharp
using System.Text.Json;
using CustomerSupport.Application.Integrations.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Integrations.Commands;

public record CreateWebhookSubscriptionCommand(
    string Name, string Url, string Secret, string[] Events,
    bool IsActive, Dictionary<string, string>? Headers = null) : IRequest<WebhookSubscriptionDto>;

public class CreateWebhookSubscriptionCommandHandler : IRequestHandler<CreateWebhookSubscriptionCommand, WebhookSubscriptionDto>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateWebhookSubscriptionCommandHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<WebhookSubscriptionDto> Handle(CreateWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            Name = request.Name,
            Url = request.Url,
            Secret = request.Secret,
            Events = JsonSerializer.Serialize(request.Events),
            IsActive = request.IsActive,
            Headers = request.Headers is not null ? JsonSerializer.Serialize(request.Headers) : null
        };

        _context.WebhookSubscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        return new WebhookSubscriptionDto(
            subscription.Id, subscription.Name, subscription.Url,
            request.Events, subscription.IsActive, subscription.CreatedAt);
    }
}
```

Create `src/CustomerSupport.Application/Integrations/Commands/UpdateWebhookSubscriptionCommand.cs`:

```csharp
using System.Text.Json;
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Commands;

public record UpdateWebhookSubscriptionCommand(
    Guid Id, string Name, string Url, string Secret,
    string[] Events, bool IsActive, Dictionary<string, string>? Headers = null) : IRequest<Result>;

public class UpdateWebhookSubscriptionCommandHandler : IRequestHandler<UpdateWebhookSubscriptionCommand, Result>
{
    private readonly AppDbContext _context;

    public UpdateWebhookSubscriptionCommandHandler(AppDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var sub = await _context.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (sub is null) return Result.Failure("Subscription not found.");

        sub.Name = request.Name;
        sub.Url = request.Url;
        sub.Secret = request.Secret;
        sub.Events = JsonSerializer.Serialize(request.Events);
        sub.IsActive = request.IsActive;
        sub.Headers = request.Headers is not null ? JsonSerializer.Serialize(request.Headers) : null;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

Create `src/CustomerSupport.Application/Integrations/Commands/DeleteWebhookSubscriptionCommand.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Commands;

public record DeleteWebhookSubscriptionCommand(Guid Id) : IRequest<Result>;

public class DeleteWebhookSubscriptionCommandHandler : IRequestHandler<DeleteWebhookSubscriptionCommand, Result>
{
    private readonly AppDbContext _context;

    public DeleteWebhookSubscriptionCommandHandler(AppDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var sub = await _context.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (sub is null) return Result.Failure("Subscription not found.");

        _context.WebhookSubscriptions.Remove(sub);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

Create `src/CustomerSupport.Application/Integrations/Commands/TestWebhookCommand.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Commands;

public record TestWebhookCommand(Guid SubscriptionId) : IRequest<Result>;

public class TestWebhookCommandHandler : IRequestHandler<TestWebhookCommand, Result>
{
    private readonly AppDbContext _context;
    private readonly IWebhookDispatcher _dispatcher;
    private readonly ICurrentUserService _currentUserService;

    public TestWebhookCommandHandler(AppDbContext context, IWebhookDispatcher dispatcher, ICurrentUserService currentUserService)
    {
        _context = context;
        _dispatcher = dispatcher;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(TestWebhookCommand request, CancellationToken cancellationToken)
    {
        var sub = await _context.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken);
        if (sub is null) return Result.Failure("Subscription not found.");

        await _dispatcher.DispatchAsync(_currentUserService.TenantId, "test.ping", new
        {
            message = "Test webhook delivery",
            timestamp = DateTime.UtcNow
        });

        return Result.Success();
    }
}
```

Create `src/CustomerSupport.Application/Integrations/Queries/GetWebhookSubscriptionsQuery.cs`:

```csharp
using System.Text.Json;
using CustomerSupport.Application.Integrations.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Queries;

public record GetWebhookSubscriptionsQuery : IRequest<List<WebhookSubscriptionDto>>;

public class GetWebhookSubscriptionsQueryHandler : IRequestHandler<GetWebhookSubscriptionsQuery, List<WebhookSubscriptionDto>>
{
    private readonly AppDbContext _context;

    public GetWebhookSubscriptionsQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<WebhookSubscriptionDto>> Handle(GetWebhookSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.WebhookSubscriptions
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new WebhookSubscriptionDto(
                s.Id, s.Name, s.Url,
                JsonSerializer.Deserialize<string[]>(s.Events) ?? Array.Empty<string>(),
                s.IsActive, s.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
```

Create `src/CustomerSupport.Application/Integrations/Queries/GetWebhookSubscriptionQuery.cs`:

```csharp
using System.Text.Json;
using CustomerSupport.Application.Integrations.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Queries;

public record GetWebhookSubscriptionQuery(Guid Id) : IRequest<WebhookSubscriptionDto?>;

public class GetWebhookSubscriptionQueryHandler : IRequestHandler<GetWebhookSubscriptionQuery, WebhookSubscriptionDto?>
{
    private readonly AppDbContext _context;

    public GetWebhookSubscriptionQueryHandler(AppDbContext context) => _context = context;

    public async Task<WebhookSubscriptionDto?> Handle(GetWebhookSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var s = await _context.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (s is null) return null;
        return new WebhookSubscriptionDto(s.Id, s.Name, s.Url,
            JsonSerializer.Deserialize<string[]>(s.Events) ?? Array.Empty<string>(),
            s.IsActive, s.CreatedAt);
    }
}
```

Create `src/CustomerSupport.Application/Integrations/Queries/GetWebhookDeliveryLogsQuery.cs`:

```csharp
using CustomerSupport.Application.Integrations.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Queries;

public record GetWebhookDeliveryLogsQuery(Guid SubscriptionId, int Page = 1, int PageSize = 20) : IRequest<List<WebhookDeliveryLogDto>>;

public class GetWebhookDeliveryLogsQueryHandler : IRequestHandler<GetWebhookDeliveryLogsQuery, List<WebhookDeliveryLogDto>>
{
    private readonly AppDbContext _context;

    public GetWebhookDeliveryLogsQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<WebhookDeliveryLogDto>> Handle(GetWebhookDeliveryLogsQuery request, CancellationToken cancellationToken)
    {
        return await _context.WebhookDeliveryLogs
            .Where(d => d.SubscriptionId == request.SubscriptionId)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new WebhookDeliveryLogDto(
                d.Id, d.Event, d.StatusCode, d.Success,
                d.Attempt, d.ErrorMessage, d.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
```

Create `src/CustomerSupport.Application/Integrations/Validators/CreateWebhookSubscriptionValidator.cs`:

```csharp
using CustomerSupport.Application.Integrations.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Integrations.Validators;

public class CreateWebhookSubscriptionValidator : AbstractValidator<CreateWebhookSubscriptionCommand>
{
    private static readonly string[] ValidEvents =
        ["ticket.created", "ticket.status_changed", "ticket.resolved", "ticket.escalated",
         "sla.breached", "conversation.created", "conversation.closed"];

    public CreateWebhookSubscriptionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2000)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme == "https")
            .WithMessage("URL must be a valid HTTPS URL.");
        RuleFor(x => x.Secret).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Events).NotEmpty()
            .Must(events => events.All(e => ValidEvents.Contains(e)))
            .WithMessage($"Events must be from: {string.Join(", ", ValidEvents)}");
    }
}
```

- [ ] **Step 13: Create WebhookSubscriptionController**

Create `src/CustomerSupport.API/Controllers/WebhookSubscriptionController.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Integrations.Commands;
using CustomerSupport.Application.Integrations.DTOs;
using CustomerSupport.Application.Integrations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/webhooks/subscriptions")]
public class WebhookSubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhookSubscriptionController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = "Permission:integrations.view")]
    public async Task<ActionResult<List<WebhookSubscriptionDto>>> GetAll()
        => Ok(await _mediator.Send(new GetWebhookSubscriptionsQuery()));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:integrations.view")]
    public async Task<ActionResult<WebhookSubscriptionDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetWebhookSubscriptionQuery(id));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:integrations.manage")]
    public async Task<ActionResult<WebhookSubscriptionDto>> Create(CreateWebhookSubscriptionCommand command)
        => CreatedAtAction(nameof(GetById), new { id = (await _mediator.Send(command)).Id }, await _mediator.Send(command));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:integrations.manage")]
    public async Task<ActionResult<Result>> Update(Guid id, UpdateWebhookSubscriptionCommand command)
    {
        if (id != command.Id) return BadRequest();
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:integrations.manage")]
    public async Task<ActionResult<Result>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteWebhookSubscriptionCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/test")]
    [Authorize(Policy = "Permission:integrations.manage")]
    public async Task<ActionResult<Result>> Test(Guid id)
        => Ok(await _mediator.Send(new TestWebhookCommand(id)));

    [HttpGet("{id:guid}/deliveries")]
    [Authorize(Policy = "Permission:integrations.view")]
    public async Task<ActionResult<List<WebhookDeliveryLogDto>>> GetDeliveries(
        Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new GetWebhookDeliveryLogsQuery(id, page, pageSize)));
}
```

- [ ] **Step 14: Create ErpController**

Create `src/CustomerSupport.API/Controllers/ErpController.cs`:

```csharp
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using CustomerSupport.Infrastructure.Services.Integrations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/integrations/erp")]
[Authorize(Policy = "Permission:integrations.manage")]
public class ErpController : ControllerBase
{
    private readonly IErpConnector _erpConnector;
    private readonly AppDbContext _context;
    private readonly ErpSettings _settings;

    public ErpController(IErpConnector erpConnector, AppDbContext context, IOptions<ErpSettings> settings)
    {
        _erpConnector = erpConnector;
        _context = context;
        _settings = settings.Value;
    }

    [HttpGet("status")]
    public ActionResult GetStatus()
        => Ok(new { provider = _settings.Provider, connected = _settings.Provider == "Mock" ? "Mock Mode" : "Connected" });

    [HttpPost("sync-ticket/{ticketId:guid}")]
    public async Task<ActionResult> SyncTicket(Guid ticketId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Customer).Include(t => t.Status).Include(t => t.Priority)
            .FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket is null) return NotFound();

        var result = await _erpConnector.SyncTicketAsync(new ErpTicketData(
            ticket.Id, ticket.TicketNumber, ticket.Subject,
            ticket.Customer?.Name ?? "", ticket.Status?.Name ?? "",
            ticket.Priority?.Name ?? "", ticket.CreatedAt, null));

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("sync-customer/{customerId:guid}")]
    public async Task<ActionResult> SyncCustomer(Guid customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer is null) return NotFound();

        var result = await _erpConnector.SyncCustomerAsync(new ErpCustomerData(
            null, customer.Name, customer.Email, customer.Phone, customer.Company));

        return result.Success ? Ok(result) : BadRequest(result);
    }
}
```

- [ ] **Step 15: Register services in DI**

In `src/CustomerSupport.Infrastructure/DependencyInjection.cs`, add after the report services registration:

```csharp
        // Integration Services
        services.AddHttpClient();
        services.AddSingleton<IWebhookDispatcher, WebhookDispatcher>();

        services.AddScoped<IErpConnector>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var provider = config.GetValue<string>("ErpSettings:Provider") ?? "Mock";
            if (provider != "Mock")
                throw new InvalidOperationException($"ERP provider '{provider}' is not supported. Only 'Mock' is available.");
            return ActivatorUtilities.CreateInstance<MockErpConnector>(sp);
        });
```

Add using:
```csharp
using CustomerSupport.Infrastructure.Services.Integrations;
```

- [ ] **Step 16: Add ErpSettings to config**

In `src/CustomerSupport.API/appsettings.json`, add after the AiSettings section:

```json
  "ErpSettings": {
    "Provider": "Mock",
    "BaseUrl": "",
    "ApiKey": ""
  }
```

In `src/CustomerSupport.API/appsettings.Development.json`, add:

```json
  "ErpSettings": {
    "Provider": "Mock"
  }
```

In `src/CustomerSupport.API/Program.cs`, add after the `AiSettings` configuration line:

```csharp
builder.Services.Configure<ErpSettings>(builder.Configuration.GetSection("ErpSettings"));
```

Add using at top:
```csharp
using CustomerSupport.Infrastructure.Services.Integrations;
```

- [ ] **Step 17: Create EF migration**

Run:
```bash
dotnet ef migrations add AddWebhookEntities --project src/CustomerSupport.Infrastructure --startup-project src/CustomerSupport.API --output-dir Persistence/Migrations
```

- [ ] **Step 18: Apply migration**

Run:
```bash
dotnet ef database update --project src/CustomerSupport.Infrastructure --startup-project src/CustomerSupport.API
```

- [ ] **Step 19: Build and verify**

Run:
```bash
dotnet build src/CustomerSupport.API
```

Expected: build succeeds.

- [ ] **Step 20: Commit**

```bash
git add -A
git commit -m "feat(integrations): add outbound webhooks with HMAC signing, ERP stub, and new domain notifications"
```

---

### Task 7: Integrations UI

**Files:**
- Create: `src/client/src/app/features/integrations/integrations.service.ts`
- Create: `src/client/src/app/features/integrations/integrations.routes.ts`
- Create: `src/client/src/app/features/integrations/integrations-page/integrations-page.ts`
- Create: `src/client/src/app/features/integrations/webhook-subscription-dialog/webhook-subscription-dialog.ts`
- Create: `src/client/src/app/features/integrations/webhook-delivery-log/webhook-delivery-log.ts`
- Modify: `src/client/src/app/app.routes.ts` — add integrations route
- Modify: `src/client/src/assets/i18n/en.json` — add integrations i18n keys
- Modify: `src/client/src/assets/i18n/ar.json` — add integrations i18n keys (Arabic)

**Interfaces:**
- Consumes: `ApiService`, webhook DTOs from Task 6 API
- Produces: `IntegrationsService`, integrations page with webhook CRUD, delivery log viewer, ERP status card

- [ ] **Step 1: Create IntegrationsService**

Create `src/client/src/app/features/integrations/integrations.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { Observable } from 'rxjs';

export interface WebhookSubscriptionDto {
  id: string;
  name: string;
  url: string;
  events: string[];
  isActive: boolean;
  createdAt: string;
}

export interface WebhookDeliveryLogDto {
  id: string;
  event: string;
  statusCode: number | null;
  success: boolean;
  attempt: number;
  errorMessage: string | null;
  createdAt: string;
}

export interface CreateWebhookSubscription {
  name: string;
  url: string;
  secret: string;
  events: string[];
  isActive: boolean;
  headers?: Record<string, string>;
}

export interface ErpStatusDto {
  provider: string;
  connected: string;
}

@Injectable({ providedIn: 'root' })
export class IntegrationsService extends ApiService {
  getSubscriptions(): Observable<WebhookSubscriptionDto[]> {
    return this.get<WebhookSubscriptionDto[]>('/v1/webhooks/subscriptions');
  }

  getSubscription(id: string): Observable<WebhookSubscriptionDto> {
    return this.get<WebhookSubscriptionDto>(`/v1/webhooks/subscriptions/${id}`);
  }

  createSubscription(data: CreateWebhookSubscription): Observable<WebhookSubscriptionDto> {
    return this.post<WebhookSubscriptionDto>('/v1/webhooks/subscriptions', data);
  }

  updateSubscription(id: string, data: any): Observable<any> {
    return this.put<any>(`/v1/webhooks/subscriptions/${id}`, { id, ...data });
  }

  deleteSubscription(id: string): Observable<any> {
    return this.delete<any>(`/v1/webhooks/subscriptions/${id}`);
  }

  testSubscription(id: string): Observable<any> {
    return this.post<any>(`/v1/webhooks/subscriptions/${id}/test`, {});
  }

  getDeliveryLogs(subscriptionId: string, page = 1, pageSize = 20): Observable<WebhookDeliveryLogDto[]> {
    return this.get<WebhookDeliveryLogDto[]>(`/v1/webhooks/subscriptions/${subscriptionId}/deliveries`, { page, pageSize });
  }

  getErpStatus(): Observable<ErpStatusDto> {
    return this.get<ErpStatusDto>('/v1/integrations/erp/status');
  }

  syncTicket(ticketId: string): Observable<any> {
    return this.post<any>(`/v1/integrations/erp/sync-ticket/${ticketId}`, {});
  }

  syncCustomer(customerId: string): Observable<any> {
    return this.post<any>(`/v1/integrations/erp/sync-customer/${customerId}`, {});
  }
}
```

- [ ] **Step 2: Create WebhookSubscriptionDialogComponent**

Create `src/client/src/app/features/integrations/webhook-subscription-dialog/webhook-subscription-dialog.ts`:

```typescript
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

const AVAILABLE_EVENTS = [
  'ticket.created', 'ticket.status_changed', 'ticket.resolved',
  'ticket.escalated', 'sla.breached', 'conversation.created', 'conversation.closed'
];

@Component({
  selector: 'app-webhook-subscription-dialog',
  imports: [
    FormsModule, TranslateModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatCheckboxModule, MatButtonModule, MatSlideToggleModule
  ],
  template: `
    <h2 mat-dialog-title>{{ (data ? 'integrations.editSubscription' : 'integrations.createSubscription') | translate }}</h2>
    <mat-dialog-content>
      <mat-form-field class="full-width">
        <mat-label>{{ 'integrations.name' | translate }}</mat-label>
        <input matInput [(ngModel)]="form.name" required>
      </mat-form-field>

      <mat-form-field class="full-width">
        <mat-label>{{ 'integrations.url' | translate }}</mat-label>
        <input matInput [(ngModel)]="form.url" required placeholder="https://">
      </mat-form-field>

      <mat-form-field class="full-width">
        <mat-label>{{ 'integrations.secret' | translate }}</mat-label>
        <input matInput [(ngModel)]="form.secret" required>
      </mat-form-field>
      <button mat-stroked-button type="button" (click)="generateSecret()" class="generate-btn">
        {{ 'integrations.generateSecret' | translate }}
      </button>

      <div class="events-section">
        <label>{{ 'integrations.events' | translate }}</label>
        @for (event of availableEvents; track event) {
          <mat-checkbox [(ngModel)]="selectedEvents[event]">{{ event }}</mat-checkbox>
        }
      </div>

      <mat-slide-toggle [(ngModel)]="form.isActive">
        {{ 'integrations.active' | translate }}
      </mat-slide-toggle>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ 'common.cancel' | translate }}</button>
      <button mat-flat-button color="primary" (click)="save()" [disabled]="!isValid()">
        {{ 'common.save' | translate }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width { width: 100%; }
    .events-section { margin: 16px 0; display: flex; flex-direction: column; gap: 4px; }
    .events-section label { font-weight: 500; margin-bottom: 8px; }
    .generate-btn { margin-bottom: 16px; }
    mat-dialog-content { min-width: 400px; }
  `]
})
export class WebhookSubscriptionDialogComponent {
  private dialogRef = inject(MatDialogRef);
  data = inject(MAT_DIALOG_DATA, { optional: true }) as any;

  availableEvents = AVAILABLE_EVENTS;
  selectedEvents: Record<string, boolean> = {};

  form = {
    name: '',
    url: '',
    secret: '',
    isActive: true
  };

  constructor() {
    if (this.data) {
      this.form.name = this.data.name;
      this.form.url = this.data.url;
      this.form.secret = '';
      this.form.isActive = this.data.isActive;
      for (const event of this.data.events || []) {
        this.selectedEvents[event] = true;
      }
    }
  }

  generateSecret(): void {
    const array = new Uint8Array(32);
    crypto.getRandomValues(array);
    this.form.secret = Array.from(array, b => b.toString(16).padStart(2, '0')).join('');
  }

  isValid(): boolean {
    return !!this.form.name && !!this.form.url && (!!this.form.secret || !!this.data) &&
      Object.values(this.selectedEvents).some(v => v);
  }

  save(): void {
    const events = Object.entries(this.selectedEvents)
      .filter(([, v]) => v).map(([k]) => k);
    this.dialogRef.close({ ...this.form, events });
  }
}
```

- [ ] **Step 3: Create WebhookDeliveryLogComponent**

Create `src/client/src/app/features/integrations/webhook-delivery-log/webhook-delivery-log.ts`:

```typescript
import { Component, input, inject, OnChanges } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { IntegrationsService, WebhookDeliveryLogDto } from '../integrations.service';

@Component({
  selector: 'app-webhook-delivery-log',
  imports: [TranslateModule, DatePipe, MatTableModule, MatIconModule, MatButtonModule],
  template: `
    @if (subscriptionId()) {
      <h3>{{ 'integrations.deliveryLog' | translate }}</h3>
      <table mat-table [dataSource]="logs" class="full-width">
        <ng-container matColumnDef="event">
          <th mat-header-cell *matHeaderCellDef>{{ 'integrations.event' | translate }}</th>
          <td mat-cell *matCellDef="let log">{{ log.event }}</td>
        </ng-container>
        <ng-container matColumnDef="statusCode">
          <th mat-header-cell *matHeaderCellDef>{{ 'integrations.statusCode' | translate }}</th>
          <td mat-cell *matCellDef="let log">{{ log.statusCode ?? '-' }}</td>
        </ng-container>
        <ng-container matColumnDef="success">
          <th mat-header-cell *matHeaderCellDef>{{ 'integrations.success' | translate }}</th>
          <td mat-cell *matCellDef="let log">
            <mat-icon [class]="log.success ? 'success-icon' : 'error-icon'">
              {{ log.success ? 'check_circle' : 'error' }}
            </mat-icon>
          </td>
        </ng-container>
        <ng-container matColumnDef="attempt">
          <th mat-header-cell *matHeaderCellDef>{{ 'integrations.attempt' | translate }}</th>
          <td mat-cell *matCellDef="let log">{{ log.attempt }}</td>
        </ng-container>
        <ng-container matColumnDef="createdAt">
          <th mat-header-cell *matHeaderCellDef>{{ 'integrations.timestamp' | translate }}</th>
          <td mat-cell *matCellDef="let log">{{ log.createdAt | date: 'short' }}</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="columns"></tr>
        <tr mat-row *matRowDef="let row; columns: columns;"></tr>
      </table>
      <button mat-stroked-button (click)="loadMore()">{{ 'integrations.loadMore' | translate }}</button>
    }
  `,
  styles: [`
    .full-width { width: 100%; }
    .success-icon { color: #4caf50; }
    .error-icon { color: #f44336; }
  `]
})
export class WebhookDeliveryLogComponent implements OnChanges {
  subscriptionId = input<string | null>(null);

  private integrationsService = inject(IntegrationsService);
  logs: WebhookDeliveryLogDto[] = [];
  columns = ['event', 'statusCode', 'success', 'attempt', 'createdAt'];
  page = 1;

  ngOnChanges(): void {
    if (this.subscriptionId()) {
      this.page = 1;
      this.logs = [];
      this.loadLogs();
    }
  }

  loadMore(): void {
    this.page++;
    this.loadLogs();
  }

  private loadLogs(): void {
    const id = this.subscriptionId();
    if (!id) return;
    this.integrationsService.getDeliveryLogs(id, this.page).subscribe(data => {
      this.logs = [...this.logs, ...data];
    });
  }
}
```

- [ ] **Step 4: Create IntegrationsPageComponent**

Create `src/client/src/app/features/integrations/integrations-page/integrations-page.ts`:

```typescript
import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { IntegrationsService, WebhookSubscriptionDto, ErpStatusDto } from '../integrations.service';
import { WebhookSubscriptionDialogComponent } from '../webhook-subscription-dialog/webhook-subscription-dialog';
import { WebhookDeliveryLogComponent } from '../webhook-delivery-log/webhook-delivery-log';

@Component({
  selector: 'app-integrations-page',
  imports: [
    TranslateModule, DatePipe,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule,
    MatSlideToggleModule, MatChipsModule,
    WebhookDeliveryLogComponent
  ],
  template: `
    <h1>{{ 'integrations.title' | translate }}</h1>

    <mat-card class="erp-card">
      <mat-card-header>
        <mat-card-title>{{ 'integrations.erpStatus' | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        @if (erpStatus) {
          <p><strong>{{ 'integrations.provider' | translate }}:</strong> {{ erpStatus.provider }}</p>
          <p><strong>{{ 'integrations.connectionStatus' | translate }}:</strong> {{ erpStatus.connected }}</p>
        }
      </mat-card-content>
    </mat-card>

    <mat-card>
      <mat-card-header>
        <mat-card-title>{{ 'integrations.webhookSubscriptions' | translate }}</mat-card-title>
        <button mat-flat-button color="primary" (click)="openDialog()">
          <mat-icon>add</mat-icon> {{ 'integrations.createSubscription' | translate }}
        </button>
      </mat-card-header>
      <mat-card-content>
        <table mat-table [dataSource]="subscriptions" class="full-width">
          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef>{{ 'integrations.name' | translate }}</th>
            <td mat-cell *matCellDef="let s">{{ s.name }}</td>
          </ng-container>
          <ng-container matColumnDef="url">
            <th mat-header-cell *matHeaderCellDef>{{ 'integrations.url' | translate }}</th>
            <td mat-cell *matCellDef="let s">{{ s.url }}</td>
          </ng-container>
          <ng-container matColumnDef="events">
            <th mat-header-cell *matHeaderCellDef>{{ 'integrations.events' | translate }}</th>
            <td mat-cell *matCellDef="let s">
              <mat-chip-set>
                @for (event of s.events; track event) {
                  <mat-chip>{{ event }}</mat-chip>
                }
              </mat-chip-set>
            </td>
          </ng-container>
          <ng-container matColumnDef="isActive">
            <th mat-header-cell *matHeaderCellDef>{{ 'integrations.active' | translate }}</th>
            <td mat-cell *matCellDef="let s">
              <mat-icon [class]="s.isActive ? 'active-icon' : 'inactive-icon'">
                {{ s.isActive ? 'check_circle' : 'cancel' }}
              </mat-icon>
            </td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef>{{ 'common.actions' | translate }}</th>
            <td mat-cell *matCellDef="let s">
              <button mat-icon-button (click)="openDialog(s)"><mat-icon>edit</mat-icon></button>
              <button mat-icon-button (click)="testWebhook(s.id)"><mat-icon>send</mat-icon></button>
              <button mat-icon-button (click)="viewLogs(s.id)"><mat-icon>list</mat-icon></button>
              <button mat-icon-button color="warn" (click)="deleteSubscription(s.id)"><mat-icon>delete</mat-icon></button>
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns;"></tr>
        </table>
      </mat-card-content>
    </mat-card>

    <app-webhook-delivery-log [subscriptionId]="selectedSubscriptionId" />
  `,
  styles: [`
    .full-width { width: 100%; }
    .erp-card { margin-block-end: 16px; }
    mat-card-header { display: flex; justify-content: space-between; align-items: center; }
    .active-icon { color: #4caf50; }
    .inactive-icon { color: #9e9e9e; }
  `]
})
export class IntegrationsPageComponent {
  private integrationsService = inject(IntegrationsService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  subscriptions: WebhookSubscriptionDto[] = [];
  erpStatus: ErpStatusDto | null = null;
  selectedSubscriptionId: string | null = null;
  columns = ['name', 'url', 'events', 'isActive', 'actions'];

  ngOnInit(): void {
    this.loadSubscriptions();
    this.integrationsService.getErpStatus().subscribe(s => this.erpStatus = s);
  }

  loadSubscriptions(): void {
    this.integrationsService.getSubscriptions().subscribe(s => this.subscriptions = s);
  }

  openDialog(existing?: WebhookSubscriptionDto): void {
    const ref = this.dialog.open(WebhookSubscriptionDialogComponent, { data: existing || null });
    ref.afterClosed().subscribe(result => {
      if (!result) return;
      if (existing) {
        this.integrationsService.updateSubscription(existing.id, result).subscribe(() => this.loadSubscriptions());
      } else {
        this.integrationsService.createSubscription(result).subscribe(() => this.loadSubscriptions());
      }
    });
  }

  testWebhook(id: string): void {
    this.integrationsService.testSubscription(id).subscribe(() => {
      this.snackBar.open('Test webhook sent', '', { duration: 3000 });
    });
  }

  viewLogs(id: string): void {
    this.selectedSubscriptionId = this.selectedSubscriptionId === id ? null : id;
  }

  deleteSubscription(id: string): void {
    this.integrationsService.deleteSubscription(id).subscribe(() => this.loadSubscriptions());
  }
}
```

- [ ] **Step 5: Create integrations routes**

Create `src/client/src/app/features/integrations/integrations.routes.ts`:

```typescript
import { Routes } from '@angular/router';

export const integrationsRoutes: Routes = [
  { path: '', loadComponent: () => import('./integrations-page/integrations-page').then(m => m.IntegrationsPageComponent) },
];
```

- [ ] **Step 6: Add integrations route to app.routes.ts**

In `src/client/src/app/app.routes.ts`, add inside the admin children array, after the reports route:

```typescript
      {
        path: 'integrations',
        loadChildren: () => import('./features/integrations/integrations.routes').then(m => m.integrationsRoutes)
      },
```

- [ ] **Step 7: Add integrations i18n keys to en.json**

In `src/client/src/assets/i18n/en.json`, add an `"integrations"` section:

```json
  "integrations": {
    "title": "Integrations",
    "webhookSubscriptions": "Webhook Subscriptions",
    "createSubscription": "New Subscription",
    "editSubscription": "Edit Subscription",
    "name": "Name",
    "url": "URL",
    "secret": "Secret",
    "generateSecret": "Generate Secret",
    "events": "Events",
    "active": "Active",
    "event": "Event",
    "statusCode": "Status Code",
    "success": "Success",
    "attempt": "Attempt",
    "timestamp": "Timestamp",
    "deliveryLog": "Delivery Log",
    "loadMore": "Load More",
    "erpStatus": "ERP Integration",
    "provider": "Provider",
    "connectionStatus": "Status",
    "testSent": "Test webhook sent"
  }
```

- [ ] **Step 8: Add integrations i18n keys to ar.json**

In `src/client/src/assets/i18n/ar.json`, add an `"integrations"` section:

```json
  "integrations": {
    "title": "التكاملات",
    "webhookSubscriptions": "اشتراكات الويب هوك",
    "createSubscription": "اشتراك جديد",
    "editSubscription": "تعديل الاشتراك",
    "name": "الاسم",
    "url": "الرابط",
    "secret": "المفتاح السري",
    "generateSecret": "توليد المفتاح",
    "events": "الأحداث",
    "active": "نشط",
    "event": "الحدث",
    "statusCode": "رمز الحالة",
    "success": "نجاح",
    "attempt": "المحاولة",
    "timestamp": "الوقت",
    "deliveryLog": "سجل التسليم",
    "loadMore": "تحميل المزيد",
    "erpStatus": "تكامل ERP",
    "provider": "المزود",
    "connectionStatus": "الحالة",
    "testSent": "تم إرسال اختبار الويب هوك"
  }
```

- [ ] **Step 9: Build frontend**

Run from `src/client/`:
```bash
npx ng build --configuration development
```

Expected: build succeeds.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "feat(integrations-ui): add webhook subscription management and ERP status page"
```
