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
