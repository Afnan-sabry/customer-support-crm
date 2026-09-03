using CustomerSupport.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services.MockProviders;

public class MockSmsClient : ISmsClient
{
    private readonly ILogger<MockSmsClient> _logger;

    public MockSmsClient(ILogger<MockSmsClient> logger)
    {
        _logger = logger;
    }

    public Task<string> SendAsync(string phoneNumber, string message)
    {
        var messageId = $"sms-{Guid.NewGuid():N}";
        _logger.LogInformation("[MockSMS] To: {Phone}, Message: {Message} (id: {Id})", phoneNumber, message, messageId);
        return Task.FromResult(messageId);
    }
}
