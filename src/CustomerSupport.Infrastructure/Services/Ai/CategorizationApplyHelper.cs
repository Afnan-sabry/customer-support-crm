using System.Text.Json;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Services.Ai;

public static class CategorizationApplyHelper
{
    public static async Task<bool> ApplyCategorizationAsync(
        string aiOutputJson,
        Ticket ticket,
        DbContext context,
        IDateTimeService dateTimeService,
        CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(aiOutputJson);
        var root = doc.RootElement;

        var applied = false;

        if (root.TryGetProperty("categoryId", out var catEl) && Guid.TryParse(catEl.GetString(), out var catId))
        {
            context.Set<TicketHistory>().Add(new TicketHistory
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                Field = "CategoryId",
                OldValue = ticket.CategoryId.ToString(),
                NewValue = catId.ToString(),
                CreatedAt = dateTimeService.UtcNow
            });
            ticket.CategoryId = catId;
            applied = true;
        }

        if (root.TryGetProperty("priorityId", out var priEl) && Guid.TryParse(priEl.GetString(), out var priId))
        {
            context.Set<TicketHistory>().Add(new TicketHistory
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                Field = "PriorityId",
                OldValue = ticket.PriorityId.ToString(),
                NewValue = priId.ToString(),
                CreatedAt = dateTimeService.UtcNow
            });
            ticket.PriorityId = priId;
            applied = true;
        }

        return applied;
    }
}
