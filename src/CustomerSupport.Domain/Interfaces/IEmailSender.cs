namespace CustomerSupport.Domain.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, string? replyToMessageId = null);
}
