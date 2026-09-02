using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CustomerSupport.Infrastructure.Persistence.Seeders;

public static class NotificationTemplateSeeder
{
    private static readonly (string Key, string Subject, string SubjectAr, string Body, string BodyAr)[] Templates =
    [
        ("ticket.created",
         "New Ticket: {{ticketNumber}}",
         "تذكرة جديدة: {{ticketNumber}}",
         "A new ticket '{{subject}}' has been created by {{customerName}} with {{priority}} priority.",
         "تم إنشاء تذكرة جديدة '{{subject}}' بواسطة {{customerName}} بأولوية {{priority}}."),

        ("ticket.assigned",
         "Ticket Assigned: {{ticketNumber}}",
         "تم تعيين تذكرة: {{ticketNumber}}",
         "Ticket '{{subject}}' has been assigned to you.",
         "تم تعيين التذكرة '{{subject}}' لك."),

        ("ticket.commented",
         "New Comment on {{ticketNumber}}",
         "تعليق جديد على {{ticketNumber}}",
         "{{commenterName}} commented on ticket '{{subject}}'.",
         "{{commenterName}} علق على التذكرة '{{subject}}'."),

        ("sla.breached",
         "SLA Breach: {{ticketNumber}}",
         "انتهاك اتفاقية مستوى الخدمة: {{ticketNumber}}",
         "SLA breach detected on ticket '{{subject}}' — {{breachType}}.",
         "تم اكتشاف انتهاك اتفاقية مستوى الخدمة على التذكرة '{{subject}}' — {{breachType}}."),

        ("conversation.new_message",
         "New Message from {{customerName}}",
         "رسالة جديدة من {{customerName}}",
         "New inbound message from {{customerName}} via {{channel}}.",
         "رسالة واردة جديدة من {{customerName}} عبر {{channel}}.")
    ];

    public static async Task SeedAsync(AppDbContext context, Guid tenantId)
    {
        var allChannels = JsonSerializer.Serialize(new[] { "InApp", "Email" });

        foreach (var (key, subject, subjectAr, body, bodyAr) in Templates)
        {
            if (!await context.NotificationTemplates.AnyAsync(t => t.TenantId == tenantId && t.Key == key))
            {
                context.NotificationTemplates.Add(new NotificationTemplate
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Key = key,
                    Subject = subject,
                    SubjectAr = subjectAr,
                    BodyTemplate = body,
                    BodyTemplateAr = bodyAr,
                    Channels = allChannels,
                    IsActive = true
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
