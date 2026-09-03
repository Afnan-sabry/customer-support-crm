using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Commands;

public record PortalUpdateProfileCommand(
    Guid PortalUserId, string FullName, string FullNameAr,
    string? Phone, string? NewPassword) : IRequest<Result>;

public class PortalUpdateProfileCommandHandler : IRequestHandler<PortalUpdateProfileCommand, Result>
{
    private readonly AppDbContext _context;

    public PortalUpdateProfileCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(PortalUpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.PortalUsers
            .FirstOrDefaultAsync(u => u.Id == request.PortalUserId, cancellationToken);

        if (user is null) return Result.Failure(["User not found"]);

        user.FullName = request.FullName;
        user.FullNameAr = request.FullNameAr;
        user.Phone = request.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.NewPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
