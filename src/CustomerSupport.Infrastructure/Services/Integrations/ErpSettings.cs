namespace CustomerSupport.Infrastructure.Services.Integrations;

public class ErpSettings
{
    public string Provider { get; set; } = "Mock";
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
