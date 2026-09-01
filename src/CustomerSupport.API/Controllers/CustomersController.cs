using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Customers.Commands;
using CustomerSupport.Application.Customers.DTOs;
using CustomerSupport.Application.Customers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<PaginatedList<CustomerDto>>> GetCustomers(
        [FromQuery] string? search, [FromQuery] bool? isActive,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _mediator.Send(new GetCustomersQuery(search, isActive, page, pageSize)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<CustomerDetailDto>> GetCustomer(Guid id)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:customers.create")]
    public async Task<ActionResult<CustomerDto>> CreateCustomer(CreateCustomerCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCustomer), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:customers.edit")]
    public async Task<ActionResult<Result>> UpdateCustomer(Guid id, UpdateCustomerCommand command)
    {
        if (id != command.Id) return BadRequest();
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:customers.delete")]
    public async Task<ActionResult<Result>> DeleteCustomer(Guid id)
    {
        var result = await _mediator.Send(new DeleteCustomerCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{customerId:guid}/contacts")]
    [Authorize(Policy = "Permission:customers.edit")]
    public async Task<ActionResult<CustomerContactDto>> CreateContact(Guid customerId, CreateCustomerContactCommand command)
    {
        if (customerId != command.CustomerId) return BadRequest();
        return CreatedAtAction(nameof(GetCustomer), new { id = customerId }, await _mediator.Send(command));
    }
}
