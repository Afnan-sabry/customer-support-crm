using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Customers.Commands;

public record DeleteCustomerCommand(Guid Id) : IRequest<Result>;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, Result>
{
    private readonly ICustomerRepository _repository;

    public DeleteCustomerCommandHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (customer is null) return Result.Failure("Customer not found.");

        customer.IsActive = false;
        await _repository.UpdateAsync(customer, cancellationToken);
        return Result.Success();
    }
}
