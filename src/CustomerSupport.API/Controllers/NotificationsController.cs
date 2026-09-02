using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Notifications.Commands;
using CustomerSupport.Application.Notifications.DTOs;
using CustomerSupport.Application.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<NotificationDto>>> GetNotifications(
        [FromQuery] bool? isRead, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _mediator.Send(new GetNotificationsQuery(isRead, page, pageSize)));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        return Ok(await _mediator.Send(new GetUnreadCountQuery()));
    }

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<Result>> MarkAsRead(Guid id)
    {
        var result = await _mediator.Send(new MarkNotificationReadCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<Result>> MarkAllAsRead()
    {
        return Ok(await _mediator.Send(new MarkAllNotificationsReadCommand()));
    }
}
