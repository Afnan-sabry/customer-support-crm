using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Interfaces;

public interface IChannelProviderFactory
{
    IChannelProvider GetProvider(ChannelType channel);
}
