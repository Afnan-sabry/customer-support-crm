using CustomerSupport.Application.Sla.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Sla.Commands;

public record CreateSlaPolicyCommand(
    string Name, string NameAr,
    Guid? PriorityId, Guid? CategoryId,
    int FirstResponseMinutes, int ResolutionMinutes) : IRequest<SlaPolicyDto>;

public class CreateSlaPolicyCommandHandler : IRequestHandler<CreateSlaPolicyCommand, SlaPolicyDto>
{
    private readonly ISlaRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly AppDbContext _context;

    public CreateSlaPolicyCommandHandler(ISlaRepository repository, ICurrentUserService currentUserService, AppDbContext context)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async Task<SlaPolicyDto> Handle(CreateSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            Name = request.Name,
            NameAr = request.NameAr,
            PriorityId = request.PriorityId,
            CategoryId = request.CategoryId,
            FirstResponseMinutes = request.FirstResponseMinutes,
            ResolutionMinutes = request.ResolutionMinutes
        };

        await _repository.AddAsync(policy, cancellationToken);

        var priorityName = request.PriorityId.HasValue
            ? await _context.TicketPriorities.Where(p => p.Id == request.PriorityId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var categoryName = request.CategoryId.HasValue
            ? await _context.TicketCategories.Where(c => c.Id == request.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        return new SlaPolicyDto(policy.Id, policy.Name, policy.NameAr,
            policy.PriorityId, priorityName, policy.CategoryId, categoryName,
            policy.FirstResponseMinutes, policy.ResolutionMinutes, policy.IsActive);
    }
}
