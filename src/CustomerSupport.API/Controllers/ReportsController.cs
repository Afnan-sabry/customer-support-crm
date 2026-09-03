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
