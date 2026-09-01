using System.Security.Claims;
using CustomerSupport.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var id = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return id is not null ? Guid.Parse(id) : Guid.Empty;
        }
    }

    public Guid TenantId
    {
        get
        {
            var tenantId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("TenantId");
            return tenantId is not null ? Guid.Parse(tenantId) : Guid.Empty;
        }
    }

    public IReadOnlyList<string> Permissions
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindAll("Permission")
                .Select(c => c.Value)
                .ToList()
                .AsReadOnly() ?? new List<string>().AsReadOnly();
        }
    }
}
