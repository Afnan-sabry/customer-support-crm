using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CustomerSupport.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IEnumerable<INotificationDispatcher> _dispatchers;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext context,
        IEnumerable<INotificationDispatcher> dispatchers,
        IDateTimeService dateTimeService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _dispatchers = dispatchers;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    public async Task SendAsync(Guid tenantId, string templateKey, NotificationRecipientInfo recipient, Dictionary<string, string> placeholders, string? data = null)
    {
        var template = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Key == templateKey && t.IsActive);

        if (template is null)
        {
            _logger.LogWarning("Notification template {Key} not found for tenant {TenantId}", templateKey, tenantId);
            return;
        }

        var title = RenderTemplate(template.Subject, placeholders);
        var titleAr = RenderTemplate(template.SubjectAr, placeholders);
        var body = RenderTemplate(template.BodyTemplate, placeholders);
        var bodyAr = RenderTemplate(template.BodyTemplateAr, placeholders);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecipientType = recipient.Type,
            RecipientId = recipient.Id,
            TemplateKey = templateKey,
            Title = title,
            TitleAr = titleAr,
            Body = body,
            BodyAr = bodyAr,
            Data = data
        };

        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();

        var channels = JsonSerializer.Deserialize<string[]>(template.Channels) ?? [];

        foreach (var channel in channels)
        {
            var dispatcher = _dispatchers.FirstOrDefault(d => d.Channel == channel);
            if (dispatcher is null) continue;

            try
            {
                await dispatcher.DispatchAsync(notification, recipient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch notification via {Channel}", channel);
            }
        }
    }

    private static string RenderTemplate(string template, Dictionary<string, string> placeholders)
    {
        var result = template;
        foreach (var (key, value) in placeholders)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }
        return result;
    }
}
