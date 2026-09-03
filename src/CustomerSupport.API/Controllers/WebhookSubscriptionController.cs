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
