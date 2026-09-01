using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Persistence.Seeders;

public static class TicketReferenceDataSeeder
{
    public static async Task SeedAsync(AppDbContext context, Guid tenantId)
    {
        if (!await context.Set<TicketCategory>().AnyAsync())
        {
            context.Set<TicketCategory>().AddRange(
                new TicketCategory { Id = Guid.NewGuid(), TenantId = tenantId, Name = "General Inquiry", NameAr = "استفسار عام", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TicketCategory { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Technical Support", NameAr = "دعم تقني", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TicketCategory { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Billing", NameAr = "الفواتير", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TicketCategory { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Complaint", NameAr = "شكوى", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        }

        if (!await context.Set<TicketPriority>().AnyAsync())
        {
            context.Set<TicketPriority>().AddRange(
                new TicketPriority { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Low", NameAr = "منخفض", Level = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TicketPriority { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Medium", NameAr = "متوسط", Level = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TicketPriority { Id = Guid.NewGuid(), TenantId = tenantId, Name = "High", NameAr = "مرتفع", Level = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TicketPriority { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Critical", NameAr = "حرج", Level = 4, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        }

        if (!await context.Set<TicketStatus>().AnyAsync())
        {
            context.Set<TicketStatus>().AddRange(
                new TicketStatus { Id = Guid.NewGuid(), TenantId = tenantId, Name = "New", NameAr = "جديد", Order = 1, IsFinal = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TicketStatus { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Open", NameAr = "مفتوح", Order = 2, IsFinal = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TicketStatus { Id = Guid.NewGuid(), TenantId = tenantId, Name = "In Progress", NameAr = "قيد التنفيذ", Order = 3, IsFinal = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TicketStatus { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Pending", NameAr = "معلق", Order = 4, IsFinal = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TicketStatus { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Resolved", NameAr = "تم الحل", Order = 5, IsFinal = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TicketStatus { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Closed", NameAr = "مغلق", Order = 6, IsFinal = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        }

        await context.SaveChangesAsync();
    }
}
