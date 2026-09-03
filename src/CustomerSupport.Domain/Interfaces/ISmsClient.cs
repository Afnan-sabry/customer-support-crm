namespace CustomerSupport.Domain.Interfaces;

public interface ISmsClient
{
    Task<string> SendAsync(string phoneNumber, string message);
}
