using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Notifications.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Notifications.Queries;

public record GetNotificationsQuery(
    bool? IsRead, int Page = 1, int PageSize = 20) : IRequest<PaginatedList<NotificationDto>>;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, PaginatedList<NotificationDto>>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationsQueryHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Notifications
            .Where(n => n.RecipientId == _currentUserService.UserId);

        if (request.IsRead.HasValue)
            query = query.Where(n => n.IsRead == request.IsRead.Value);

        var projected = query.OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.TitleAr, n.Body, n.BodyAr,
                n.Data, n.IsRead, n.CreatedAt, n.ReadAt));

        return await PaginatedList<NotificationDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
