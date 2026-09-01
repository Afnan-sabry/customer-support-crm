namespace CustomerSupport.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    Guid TenantId { get; }
    IReadOnlyList<string> Permissions { get; }
}
