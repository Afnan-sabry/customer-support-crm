using CustomerSupport.Application.Customers.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Customers.Commands;

public record CreateCustomerContactCommand(
    Guid CustomerId, string Name, string NameAr, string? Email,
    string? Phone, string? Title, bool IsPrimary) : IRequest<CustomerContactDto>;

public class CreateCustomerContactCommandHandler : IRequestHandler<CreateCustomerContactCommand, CustomerContactDto>
{
    private readonly AppDbContext _context;

    public CreateCustomerContactCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerContactDto> Handle(CreateCustomerContactCommand request, CancellationToken cancellationToken)
    {
        var contact = new CustomerContact
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Name = request.Name,
            NameAr = request.NameAr,
            Email = request.Email,
            Phone = request.Phone,
            Title = request.Title,
            IsPrimary = request.IsPrimary
        };

        _context.CustomerContacts.Add(contact);
        await _context.SaveChangesAsync(cancellationToken);

        return new CustomerContactDto(contact.Id, contact.Name, contact.NameAr, contact.Email, contact.Phone, contact.Title, contact.IsPrimary);
    }
}
