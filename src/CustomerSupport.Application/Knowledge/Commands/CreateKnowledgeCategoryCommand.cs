using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Knowledge.Commands;

public record CreateKnowledgeCategoryCommand(
    string Name, string NameAr,
    Guid? ParentCategoryId, int Order) : IRequest<KnowledgeCategoryDto>;

public class CreateKnowledgeCategoryCommandHandler : IRequestHandler<CreateKnowledgeCategoryCommand, KnowledgeCategoryDto>
{
    private readonly IKnowledgeRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreateKnowledgeCategoryCommandHandler(IKnowledgeRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<KnowledgeCategoryDto> Handle(CreateKnowledgeCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new KnowledgeCategory
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            Name = request.Name,
            NameAr = request.NameAr,
            ParentCategoryId = request.ParentCategoryId,
            Order = request.Order
        };

        await _repository.AddCategoryAsync(category, cancellationToken);
        return new KnowledgeCategoryDto(category.Id, category.Name, category.NameAr, category.ParentCategoryId, category.Order, category.IsActive);
    }
}
