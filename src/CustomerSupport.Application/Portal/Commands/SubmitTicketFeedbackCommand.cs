using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Commands;

public record SubmitTicketFeedbackCommand(
    Guid TicketId, Guid CustomerId, Guid TenantId,
    int Rating, string? Comment) : IRequest<Result>;

public class SubmitTicketFeedbackCommandHandler : IRequestHandler<SubmitTicketFeedbackCommand, Result>
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public SubmitTicketFeedbackCommandHandler(AppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result> Handle(SubmitTicketFeedbackCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.CustomerId == request.CustomerId, cancellationToken);

        if (ticket is null)
            return Result.Failure("Ticket not found.");

        if (ticket.Status is null || !ticket.Status.IsFinal)
            return Result.Failure("Feedback can only be submitted for resolved tickets.");

        var existingFeedback = await _context.TicketFeedbacks
            .AnyAsync(f => f.TicketId == request.TicketId, cancellationToken);

        if (existingFeedback)
            return Result.Failure("Feedback has already been submitted for this ticket.");

        _context.TicketFeedbacks.Add(new TicketFeedback
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            TicketId = request.TicketId,
            CustomerId = request.CustomerId,
            Rating = request.Rating,
            Comment = request.Comment,
            SubmittedAt = _dateTimeService.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
