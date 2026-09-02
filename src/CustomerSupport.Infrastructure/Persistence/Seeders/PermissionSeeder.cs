using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Persistence.Seeders;

public static class PermissionSeeder
{
    private static readonly (string Key, string Module, string Description)[] AllPermissions =
    [
        ("tenants.view", "Tenants", "View tenants"),
        ("tenants.manage", "Tenants", "Create and edit tenants"),
        ("users.view", "Users", "View users"),
        ("users.create", "Users", "Create users"),
        ("users.edit", "Users", "Edit users"),
        ("users.deactivate", "Users", "Deactivate users"),
        ("roles.view", "Roles", "View roles"),
        ("roles.manage", "Roles", "Create, edit roles and assign permissions"),
        ("customers.view", "Customers", "View customers"),
        ("customers.create", "Customers", "Create customers"),
        ("customers.edit", "Customers", "Edit customers"),
        ("customers.delete", "Customers", "Delete customers"),
        ("tickets.view", "Tickets", "View tickets"),
        ("tickets.create", "Tickets", "Create tickets"),
        ("tickets.edit", "Tickets", "Edit tickets"),
        ("tickets.assign", "Tickets", "Assign tickets to agents"),
        ("tickets.delete", "Tickets", "Delete tickets"),
        ("audit.view", "Audit", "View audit logs"),
        ("knowledgebase.view", "KnowledgeBase", "View knowledge base"),
        ("knowledgebase.manage", "KnowledgeBase", "Manage knowledge base articles"),
        ("reports.view", "Reports", "View reports"),
        ("settings.manage", "Settings", "Manage system settings"),
        ("sla.view", "SLA", "View SLA policies"),
        ("sla.manage", "SLA", "Manage SLA policies"),
        ("escalation.view", "Escalation", "View escalation rules"),
        ("escalation.manage", "Escalation", "Manage escalation rules"),
        ("assignment.view", "Assignment", "View assignment rules"),
        ("assignment.manage", "Assignment", "Manage assignment rules"),
        ("dashboard.view", "Dashboard", "View agent dashboard"),
        ("conversations.view", "Conversations", "View conversations"),
        ("conversations.manage", "Conversations", "Manage conversations"),
        ("chat.view", "Chat", "View live chat"),
        ("chat.manage", "Chat", "Manage live chat sessions"),
    ];

    public static async Task SeedAsync(AppDbContext context)
    {
        foreach (var (key, module, description) in AllPermissions)
        {
            if (!await context.Permissions.AnyAsync(p => p.Key == key))
            {
                context.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    Module = module,
                    Description = description
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
