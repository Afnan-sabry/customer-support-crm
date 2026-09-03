using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Commands;

public record PortalRegisterCommand(
    string Email, string Password,
    string FullName, string FullNameAr, string? Phone) : IRequest<PortalTokenResponse>;

public class PortalRegisterCommandHandler : IRequestHandler<PortalRegisterCommand, PortalTokenResponse>
{
    private readonly AppDbContext _context;
    private readonly IPortalTokenService _tokenService;
    private readonly IDateTimeService _dateTimeService;

    public PortalRegisterCommandHandler(
        AppDbContext context, IPortalTokenService tokenService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tokenService = tokenService;
        _dateTimeService = dateTimeService;
    }

    public async Task<PortalTokenResponse> Handle(PortalRegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.PortalUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existing is not null)
            throw new InvalidOperationException("A user with this email already exists");

        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == request.Email && c.TenantId == tenantId, cancellationToken);

        if (customer is null)
        {
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = request.FullName,
                NameAr = request.FullNameAr,
                Email = request.Email,
                Phone = request.Phone
            };
            await _context.Customers.AddAsync(customer, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var portalUser = new PortalUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customer.Id,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            FullNameAr = request.FullNameAr,
            Phone = request.Phone,
            IsActive = true,
            CreatedAt = _dateTimeService.UtcNow,
            UpdatedAt = _dateTimeService.UtcNow
        };

        await _context.PortalUsers.AddAsync(portalUser, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var (accessToken, refreshToken) = _tokenService.GenerateTokens(portalUser);

        portalUser.RefreshToken = refreshToken;
        portalUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(cancellationToken);

        return new PortalTokenResponse(accessToken, refreshToken,
            new PortalUserDto(portalUser.Id, portalUser.Email, portalUser.FullName, portalUser.FullNameAr, portalUser.Phone, portalUser.CustomerId));
    }
}
