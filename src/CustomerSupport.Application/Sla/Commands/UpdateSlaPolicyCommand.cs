using CustomerSupport.Application.Sla.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Sla.Commands;

public record UpdateSlaPolicyCommand(
    Guid Id, string Name, string NameAr,
    Guid? PriorityId, Guid? CategoryId,
    int FirstResponseMinutes, int ResolutionMinutes) : IRequest<SlaPolicyDto>;

public class UpdateSlaPolicyCommandHandler : IRequestHandler<UpdateSlaPolicyCommand, SlaPolicyDto>
{
    private readonly ISlaRepository _repository;
    private readonly AppDbContext _context;

    public UpdateSlaPolicyCommandHandler(ISlaRepository repository, AppDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<SlaPolicyDto> Handle(UpdateSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("SLA policy not found.");

        policy.Name = request.Name;
        policy.NameAr = request.NameAr;
        policy.PriorityId = request.PriorityId;
        policy.CategoryId = request.CategoryId;
        policy.FirstResponseMinutes = request.FirstResponseMinutes;
        policy.ResolutionMinutes = request.ResolutionMinutes;

        await _repository.UpdateAsync(policy, cancellationToken);

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
