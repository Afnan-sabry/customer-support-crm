using CustomerSupport.Application.Tickets.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Tickets.Queries;

public record GetTicketCategoriesQuery : IRequest<List<TicketCategoryDto>>;

public class GetTicketCategoriesQueryHandler : IRequestHandler<GetTicketCategoriesQuery, List<TicketCategoryDto>>
{
    private readonly AppDbContext _context;

    public GetTicketCategoriesQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<TicketCategoryDto>> Handle(GetTicketCategoriesQuery request, CancellationToken cancellationToken)
        => await _context.TicketCategories.Where(c => c.IsActive)
            .Select(c => new TicketCategoryDto(c.Id, c.Name, c.NameAr))
            .ToListAsync(cancellationToken);
}
