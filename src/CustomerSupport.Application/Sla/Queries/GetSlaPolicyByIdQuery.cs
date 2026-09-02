using CustomerSupport.Application.Sla.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Sla.Queries;

public record GetSlaPolicyByIdQuery(Guid Id) : IRequest<SlaPolicyDto>;

public class GetSlaPolicyByIdQueryHandler : IRequestHandler<GetSlaPolicyByIdQuery, SlaPolicyDto>
{
    private readonly AppDbContext _context;

    public GetSlaPolicyByIdQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SlaPolicyDto> Handle(GetSlaPolicyByIdQuery request, CancellationToken cancellationToken)
    {
        var policy = await _context.SlaPolicies
            .Include(s => s.Priority)
            .Include(s => s.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("SLA policy not found.");

        return new SlaPolicyDto(policy.Id, policy.Name, policy.NameAr,
            policy.PriorityId, policy.Priority?.Name,
            policy.CategoryId, policy.Category?.Name,
            policy.FirstResponseMinutes, policy.ResolutionMinutes, policy.IsActive);
    }
}
