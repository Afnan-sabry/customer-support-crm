using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Services;

public class ChatSessionService : IChatSessionService
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public ChatSessionService(AppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<Conversation> StartSessionAsync(Guid customerId, Guid tenantId, string? subject = null)
    {
        var existing = await _context.Conversations
            .FirstOrDefaultAsync(c =>
                c.CustomerId == customerId &&
                c.Channel == ChannelType.LiveChat &&
                c.Status == ConversationStatus.Active);

        if (existing is not null) return existing;

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customerId,
            Channel = ChannelType.LiveChat,
            Status = ConversationStatus.Active,
            Subject = subject ?? "Live Chat"
        };

        await _context.Conversations.AddAsync(conversation);
        await _context.SaveChangesAsync();

        return conversation;
    }

    public async Task EndSessionAsync(Guid conversationId)
    {
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation is null || conversation.Status == ConversationStatus.Closed) return;

        conversation.Status = ConversationStatus.Closed;
        conversation.ClosedAt = _dateTimeService.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetQueuePositionAsync(Guid conversationId)
    {
        var unassigned = await _context.Conversations
            .Where(c => c.Channel == ChannelType.LiveChat &&
                        c.Status == ConversationStatus.Active &&
                        c.AssignedAgentId == null)
            .OrderBy(c => c.CreatedAt)
            .Select(c => c.Id)
            .ToListAsync();

        var position = unassigned.IndexOf(conversationId);
        return position >= 0 ? position + 1 : 0;
    }

    public async Task<List<Conversation>> GetActiveSessionsAsync(Guid? agentId = null)
    {
        var query = _context.Conversations
            .Include(c => c.Customer)
            .Where(c => c.Channel == ChannelType.LiveChat && c.Status == ConversationStatus.Active);

        if (agentId.HasValue)
            query = query.Where(c => c.AssignedAgentId == agentId.Value || c.AssignedAgentId == null);

        return await query.OrderByDescending(c => c.UpdatedAt).ToListAsync();
    }
}
