using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Commands;

public record PortalLoginCommand(string Email, string Password) : IRequest<PortalTokenResponse>;

public class PortalLoginCommandHandler : IRequestHandler<PortalLoginCommand, PortalTokenResponse>
{
    private readonly AppDbContext _context;
    private readonly IPortalTokenService _tokenService;

    public PortalLoginCommandHandler(AppDbContext context, IPortalTokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<PortalTokenResponse> Handle(PortalLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.PortalUsers
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid email or password");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        var (accessToken, refreshToken) = _tokenService.GenerateTokens(user);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(cancellationToken);

        return new PortalTokenResponse(accessToken, refreshToken,
            new PortalUserDto(user.Id, user.Email, user.FullName, user.FullNameAr, user.Phone, user.CustomerId));
    }
}
