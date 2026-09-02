using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Sla.Commands;

public record DeleteSlaPolicyCommand(Guid Id) : IRequest<Result>;

public class DeleteSlaPolicyCommandHandler : IRequestHandler<DeleteSlaPolicyCommand, Result>
{
    private readonly ISlaRepository _repository;

    public DeleteSlaPolicyCommandHandler(ISlaRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(DeleteSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("SLA policy not found.");

        policy.IsActive = false;
        await _repository.UpdateAsync(policy, cancellationToken);
        return Result.Success();
    }
}
