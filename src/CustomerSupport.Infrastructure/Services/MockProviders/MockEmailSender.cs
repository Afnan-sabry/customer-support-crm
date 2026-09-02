using CustomerSupport.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services.MockProviders;

public class MockEmailSender : IEmailSender
{
    private readonly ILogger<MockEmailSender> _logger;

    public MockEmailSender(ILogger<MockEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, string? replyToMessageId = null)
    {
        _logger.LogInformation(
            "[MockEmail] To: {To}, Subject: {Subject}, ReplyTo: {ReplyTo}, Body length: {Length}",
            to, subject, replyToMessageId, htmlBody.Length);
        return Task.CompletedTask;
    }
}
