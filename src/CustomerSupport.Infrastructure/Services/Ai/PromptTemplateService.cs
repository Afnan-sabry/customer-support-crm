using CustomerSupport.Domain.Interfaces;

namespace CustomerSupport.Infrastructure.Services.Ai;

public class PromptTemplateService : IPromptTemplateService
{
    public string GetTemplate(string key)
    {
        return PromptTemplates.Templates.TryGetValue(key, out var template)
            ? template
            : throw new KeyNotFoundException($"Prompt template '{key}' not found.");
    }

    public string Render(string key, Dictionary<string, string> placeholders)
    {
        var template = GetTemplate(key);
        foreach (var (placeholder, value) in placeholders)
        {
            template = template.Replace($"{{{placeholder}}}", value);
        }
        return template;
    }
}
