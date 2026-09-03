using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;

namespace CustomerSupport.Infrastructure.Services.Channels;

public class ChannelProviderFactory : IChannelProviderFactory
{
    private readonly IEnumerable<IChannelProvider> _providers;

    public ChannelProviderFactory(IEnumerable<IChannelProvider> providers)
    {
        _providers = providers;
    }

    public IChannelProvider GetProvider(ChannelType channel)
    {
        return _providers.FirstOrDefault(p => p.Channel == channel)
            ?? throw new InvalidOperationException($"No channel provider registered for {channel}");
    }
}
