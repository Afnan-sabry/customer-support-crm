using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Knowledge.Commands;

public record UpdateKnowledgeCategoryCommand(Guid Id, string Name, string NameAr, Guid? ParentCategoryId, int Order) : IRequest<KnowledgeCategoryDto>;

public class UpdateKnowledgeCategoryCommandHandler : IRequestHandler<UpdateKnowledgeCategoryCommand, KnowledgeCategoryDto>
{
    private readonly AppDbContext _context;

    public UpdateKnowledgeCategoryCommandHandler(AppDbContext context) => _context = context;

    public async Task<KnowledgeCategoryDto> Handle(UpdateKnowledgeCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.KnowledgeCategories.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException("Knowledge category not found.");

        category.Name = request.Name;
        category.NameAr = request.NameAr;
        category.ParentCategoryId = request.ParentCategoryId;
        category.Order = request.Order;

        await _context.SaveChangesAsync(cancellationToken);
        return new KnowledgeCategoryDto(category.Id, category.Name, category.NameAr, category.ParentCategoryId, category.Order, category.IsActive);
    }
}
