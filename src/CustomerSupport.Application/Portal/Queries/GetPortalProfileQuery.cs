using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Queries;

public record GetPortalProfileQuery(Guid PortalUserId) : IRequest<PortalUserDto?>;

public class GetPortalProfileQueryHandler : IRequestHandler<GetPortalProfileQuery, PortalUserDto?>
{
    private readonly AppDbContext _context;

    public GetPortalProfileQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PortalUserDto?> Handle(GetPortalProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.PortalUsers
            .FirstOrDefaultAsync(u => u.Id == request.PortalUserId, cancellationToken);

        return user is null ? null : new PortalUserDto(
            user.Id, user.Email, user.FullName, user.FullNameAr, user.Phone, user.CustomerId);
    }
}
