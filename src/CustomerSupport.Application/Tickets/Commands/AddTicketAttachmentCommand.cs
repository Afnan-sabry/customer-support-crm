using CustomerSupport.Application.Tickets.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Tickets.Commands;

// Phase 1: metadata-only. Actual file upload/storage handling is out of scope.
public record AddTicketAttachmentCommand(Guid TicketId, string FileName, string FilePath, string ContentType, long FileSize) : IRequest<TicketAttachmentDto>;

public class AddTicketAttachmentCommandHandler : IRequestHandler<AddTicketAttachmentCommand, TicketAttachmentDto>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddTicketAttachmentCommandHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<TicketAttachmentDto> Handle(AddTicketAttachmentCommand request, CancellationToken cancellationToken)
    {
        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            FileName = request.FileName,
            FilePath = request.FilePath,
            ContentType = request.ContentType,
            FileSize = request.FileSize,
            UploadedById = _currentUserService.UserId
        };

        _context.TicketAttachments.Add(attachment);
        await _context.SaveChangesAsync(cancellationToken);

        return new TicketAttachmentDto(attachment.Id, attachment.FileName, attachment.ContentType, attachment.FileSize, attachment.CreatedAt);
    }
}
