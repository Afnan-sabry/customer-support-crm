using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Interfaces;

public interface IChannelProvider
{
    ChannelType Channel { get; }
    Task<Message> SendMessageAsync(Conversation conversation, string content, ContentType contentType, Guid? agentId = null);
}
