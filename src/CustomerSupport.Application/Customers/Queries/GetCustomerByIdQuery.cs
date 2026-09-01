using CustomerSupport.Application.Customers.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Customers.Queries;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDetailDto?>;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDetailDto?>
{
    private readonly ICustomerRepository _repository;

    public GetCustomerByIdQueryHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<CustomerDetailDto?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdWithContactsAsync(request.Id, cancellationToken);
        if (customer is null) return null;

        return new CustomerDetailDto(
            customer.Id, customer.Name, customer.NameAr, customer.Email, customer.Phone,
            customer.Company, customer.CompanyAr, customer.Address, customer.IsActive,
            customer.Contacts.Select(c =>
                new CustomerContactDto(c.Id, c.Name, c.NameAr, c.Email, c.Phone, c.Title, c.IsPrimary)).ToList());
    }
}
