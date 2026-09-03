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
