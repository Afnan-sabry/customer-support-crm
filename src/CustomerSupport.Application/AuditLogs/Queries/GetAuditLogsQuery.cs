using CustomerSupport.Application.AuditLogs.DTOs;
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.AuditLogs.Queries;

public record GetAuditLogsQuery(
    string? EntityType,
    string? EntityId,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedList<AuditLogDto>>;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PaginatedList<AuditLogDto>>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAuditLogsQueryHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();
        query = query.Where(a => a.TenantId == _currentUserService.TenantId);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);

        if (!string.IsNullOrWhiteSpace(request.EntityId))
            query = query.Where(a => a.EntityId == request.EntityId);

        query = query.OrderByDescending(a => a.Timestamp);

        var projected = query.Select(a => new AuditLogDto(
            a.Id, a.UserId, a.EntityType, a.EntityId, a.Action,
            a.OldValues, a.NewValues, a.Timestamp, a.IpAddress));

        return await PaginatedList<AuditLogDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
