using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Conversations.DTOs;

public record MessageDto(
    Guid Id, Guid ConversationId,
    MessageDirection Direction, SenderType SenderType,
    Guid? SenderId, string? SenderName,
    string Content, ContentType ContentType,
    ChannelType Channel, string? Metadata,
    DateTime SentAt, DateTime? DeliveredAt, DateTime? ReadAt,
    List<MessageAttachmentDto> Attachments);

public record MessageAttachmentDto(
    Guid Id, string FileName, string ContentType, long FileSizeBytes);
