using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Notifications.Queries;

public record GetUnreadCountQuery : IRequest<int>;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadCountQueryHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .CountAsync(n => n.RecipientId == _currentUserService.UserId && !n.IsRead, cancellationToken);
    }
}
