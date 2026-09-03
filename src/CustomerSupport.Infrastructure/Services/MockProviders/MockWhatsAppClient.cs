using CustomerSupport.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services.MockProviders;

public class MockWhatsAppClient : IWhatsAppClient
{
    private readonly ILogger<MockWhatsAppClient> _logger;

    public MockWhatsAppClient(ILogger<MockWhatsAppClient> logger)
    {
        _logger = logger;
    }

    public Task<string> SendTextMessageAsync(string phoneNumber, string text)
    {
        var messageId = $"wamid.{Guid.NewGuid():N}";
        _logger.LogInformation("[MockWhatsApp] Text to {Phone}: {Text} (id: {Id})", phoneNumber, text, messageId);
        return Task.FromResult(messageId);
    }

    public Task<string> SendMediaMessageAsync(string phoneNumber, string mediaUrl, string caption)
    {
        var messageId = $"wamid.{Guid.NewGuid():N}";
        _logger.LogInformation("[MockWhatsApp] Media to {Phone}: {Url} — {Caption} (id: {Id})", phoneNumber, mediaUrl, caption, messageId);
        return Task.FromResult(messageId);
    }
}
