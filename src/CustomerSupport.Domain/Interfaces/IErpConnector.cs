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
