using CustomerSupport.Application.Customers.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Customers.Commands;

public record CreateCustomerCommand(
    string Name, string NameAr, string? Email, string? Phone,
    string? Company, string? CompanyAr, string? Address) : IRequest<CustomerDto>;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreateCustomerCommandHandler(ICustomerRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            Name = request.Name,
            NameAr = request.NameAr,
            Email = request.Email,
            Phone = request.Phone,
            Company = request.Company,
            CompanyAr = request.CompanyAr,
            Address = request.Address
        };

        await _repository.AddAsync(customer, cancellationToken);

        return new CustomerDto(customer.Id, customer.Name, customer.NameAr, customer.Email, customer.Phone, customer.Company, customer.CompanyAr, customer.IsActive);
    }
}
