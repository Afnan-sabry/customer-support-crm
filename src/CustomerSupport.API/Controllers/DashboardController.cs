using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Application.Dashboard.Queries;
using CustomerSupport.Application.Tickets.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("stats")]
    [Authorize(Policy = "Permission:dashboard.view")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
        => Ok(await _mediator.Send(new GetDashboardStatsQuery()));

    [HttpGet("sla-summary")]
    [Authorize(Policy = "Permission:dashboard.view")]
    public async Task<ActionResult<SlaSummaryDto>> GetSlaSummary()
        => Ok(await _mediator.Send(new GetSlaSummaryQuery()));

    [HttpGet("my-tickets")]
    [Authorize(Policy = "Permission:dashboard.view")]
    public async Task<ActionResult<PaginatedList<TicketDto>>> GetMyTickets(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new GetMyTicketsQuery(page, pageSize)));

    [HttpGet("team-workload")]
    [Authorize(Policy = "Permission:dashboard.view")]
    public async Task<ActionResult<List<AgentWorkloadDto>>> GetTeamWorkload()
        => Ok(await _mediator.Send(new GetTeamWorkloadQuery()));

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
}
