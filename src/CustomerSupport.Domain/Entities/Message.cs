using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Entities;

public class Message : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ConversationId { get; set; }
    public MessageDirection Direction { get; set; }
    public SenderType SenderType { get; set; }
    public Guid? SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public ContentType ContentType { get; set; }
    public ChannelType Channel { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? Metadata { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public Conversation? Conversation { get; set; }
    public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
}
