namespace CustomerSupport.Domain.Interfaces;

public interface IPromptTemplateService
{
    string GetTemplate(string key);
    string Render(string key, Dictionary<string, string> placeholders);
}
