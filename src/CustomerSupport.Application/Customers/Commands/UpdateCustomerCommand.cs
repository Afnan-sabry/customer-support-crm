using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Customers.Commands;

public record UpdateCustomerCommand(
    Guid Id, string Name, string NameAr, string? Email, string? Phone,
    string? Company, string? CompanyAr, string? Address) : IRequest<Result>;

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, Result>
{
    private readonly ICustomerRepository _repository;

    public UpdateCustomerCommandHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (customer is null) return Result.Failure("Customer not found.");

        customer.Name = request.Name;
        customer.NameAr = request.NameAr;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.Company = request.Company;
        customer.CompanyAr = request.CompanyAr;
        customer.Address = request.Address;

        await _repository.UpdateAsync(customer, cancellationToken);
        return Result.Success();
    }
}
