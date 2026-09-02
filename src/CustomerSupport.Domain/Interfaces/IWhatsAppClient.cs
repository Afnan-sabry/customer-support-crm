namespace CustomerSupport.Domain.Interfaces;

public interface IWhatsAppClient
{
    Task<string> SendTextMessageAsync(string phoneNumber, string text);
    Task<string> SendMediaMessageAsync(string phoneNumber, string mediaUrl, string caption);
}
