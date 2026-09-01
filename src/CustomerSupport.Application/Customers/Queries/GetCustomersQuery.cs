using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Customers.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Customers.Queries;

public record GetCustomersQuery(string? Search, bool? IsActive, int Page = 1, int PageSize = 20)
    : IRequest<PaginatedList<CustomerDto>>;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PaginatedList<CustomerDto>>
{
    private readonly ICustomerRepository _repository;

    public GetCustomersQueryHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedList<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.GetQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(search) ||
                c.NameAr.Contains(search) ||
                (c.Email != null && c.Email.ToLower().Contains(search)) ||
                (c.Company != null && c.Company.ToLower().Contains(search)));
        }

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        var projected = query.OrderBy(c => c.Name).Select(c =>
            new CustomerDto(c.Id, c.Name, c.NameAr, c.Email, c.Phone, c.Company, c.CompanyAr, c.IsActive));

        return await PaginatedList<CustomerDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
