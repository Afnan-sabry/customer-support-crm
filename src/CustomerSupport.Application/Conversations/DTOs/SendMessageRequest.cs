using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Conversations.DTOs;

public record SendMessageRequest(string Content, ContentType ContentType = ContentType.Text);
