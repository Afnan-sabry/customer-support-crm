using CustomerSupport.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Persistence.Seeders;

public static class RoleAndUserSeeder
{
    private static readonly Guid SuperAdminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid AdminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid AgentRoleId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    private static readonly string[] AdminExcludedPermissions =
        ["tenants.view", "tenants.manage", "settings.manage"];

    private static readonly string[] AgentPermissions =
    [
        "tickets.view", "tickets.create", "tickets.edit", "tickets.assign",
        "customers.view", "knowledgebase.view", "dashboard.view", "assignment.view",
        "conversations.view", "chat.view"
    ];

    private static readonly (Guid Id, string Name, string NameAr, string Email, string Password, Guid RoleId)[] DefaultUsers =
    [
        (Guid.Parse("20000000-0000-0000-0000-000000000001"), "Super Admin", "المدير العام", "superadmin@system.local", "SuperAdmin123!", SuperAdminRoleId),
        (Guid.Parse("20000000-0000-0000-0000-000000000002"), "Admin", "المدير", "admin@system.local", "Admin123!", AdminRoleId),
        (Guid.Parse("20000000-0000-0000-0000-000000000003"), "Agent", "الوكيل", "agent@system.local", "Agent123!", AgentRoleId),
    ];

    public static async Task SeedAsync(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        Guid tenantId)
    {
        await SeedRolesAsync(context, roleManager, tenantId);
        await SeedRolePermissionsAsync(context);
        await SeedUsersAsync(userManager, roleManager, tenantId);
    }

    private static async Task SeedRolesAsync(
        AppDbContext context,
        RoleManager<ApplicationRole> roleManager,
        Guid tenantId)
    {
        var roles = new (Guid Id, string Name, string NameAr)[]
        {
            (SuperAdminRoleId, "SuperAdmin", "مدير عام"),
            (AdminRoleId, "Admin", "مدير"),
            (AgentRoleId, "Agent", "وكيل"),
        };

        foreach (var (id, name, nameAr) in roles)
        {
            if (!await context.Roles.AnyAsync(r => r.Id == id))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Id = id,
                    Name = name,
                    NameAr = nameAr,
                    TenantId = tenantId,
                    IsSystem = true
                });
            }
        }
    }

    private static async Task SeedRolePermissionsAsync(AppDbContext context)
    {
        var allPermissions = await context.Permissions.ToListAsync();
        var existingMappings = await context.Set<RolePermission>().ToListAsync();

        var mappings = new List<(Guid RoleId, Permission Permission)>();

        foreach (var permission in allPermissions)
        {
            mappings.Add((SuperAdminRoleId, permission));

            if (!AdminExcludedPermissions.Contains(permission.Key))
                mappings.Add((AdminRoleId, permission));

            if (AgentPermissions.Contains(permission.Key))
                mappings.Add((AgentRoleId, permission));
        }

        foreach (var (roleId, permission) in mappings)
        {
            if (!existingMappings.Any(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id))
            {
                context.Set<RolePermission>().Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permission.Id
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        Guid tenantId)
    {
        foreach (var (id, name, nameAr, email, password, roleId) in DefaultUsers)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                continue;

            var user = new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                FullName = name,
                FullNameAr = nameAr,
                TenantId = tenantId,
                PreferredLanguage = "en",
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                var role = await roleManager.FindByIdAsync(roleId.ToString());
                if (role?.Name is not null)
                    await userManager.AddToRoleAsync(user, role.Name);
            }
        }
    }
}
