using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Sla.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Sla.Queries;

public record GetSlaPoliciesQuery(bool? IsActive, int Page = 1, int PageSize = 20)
    : IRequest<PaginatedList<SlaPolicyDto>>;

public class GetSlaPoliciesQueryHandler : IRequestHandler<GetSlaPoliciesQuery, PaginatedList<SlaPolicyDto>>
{
    private readonly ISlaRepository _repository;

    public GetSlaPoliciesQueryHandler(ISlaRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedList<SlaPolicyDto>> Handle(GetSlaPoliciesQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.GetQueryable()
            .Include(s => s.Priority)
            .Include(s => s.Category)
            .AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        query = query.OrderBy(s => s.Name);

        var projected = query.Select(s => new SlaPolicyDto(
            s.Id, s.Name, s.NameAr,
            s.PriorityId, s.Priority != null ? s.Priority.Name : null,
            s.CategoryId, s.Category != null ? s.Category.Name : null,
            s.FirstResponseMinutes, s.ResolutionMinutes, s.IsActive));

        return await PaginatedList<SlaPolicyDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
