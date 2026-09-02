using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _context;

    public ConversationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Conversations
            .Include(c => c.Customer)
            .Include(c => c.AssignedAgent)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Conversation?> GetByIdWithMessagesAsync(Guid id, int messagePage = 1, int messagePageSize = 50, CancellationToken cancellationToken = default)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Customer)
            .Include(c => c.Ticket)
            .Include(c => c.AssignedAgent)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (conversation is null) return null;

        var messages = await _context.Messages
            .Where(m => m.ConversationId == id)
            .OrderByDescending(m => m.SentAt)
            .Skip((messagePage - 1) * messagePageSize)
            .Take(messagePageSize)
            .Include(m => m.Attachments)
            .ToListAsync(cancellationToken);

        conversation.Messages = messages;
        return conversation;
    }

    public async Task<Conversation?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default)
        => await _context.Conversations
            .FirstOrDefaultAsync(c => c.ExternalReference == externalReference, cancellationToken);

    public async Task<IReadOnlyList<Conversation>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Conversations.ToListAsync(cancellationToken);

    public async Task<Conversation> AddAsync(Conversation entity, CancellationToken cancellationToken = default)
    {
        await _context.Conversations.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Conversation entity, CancellationToken cancellationToken = default)
    {
        _context.Conversations.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public IQueryable<Conversation> GetQueryable() => _context.Conversations.AsQueryable();
}
