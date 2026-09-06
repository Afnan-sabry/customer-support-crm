# Seeder Tenant Query Filter Bypass

**Date:** 2026-09-06
**Status:** Implemented
**Scope:** Bounded fix — seed data infrastructure

## Problem

`AppDbContext` applies a global query filter on every `ITenantEntity` type:

```csharp
modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == currentUserService.TenantId);
```

During application startup, seeders run before any user is authenticated. `ICurrentUserService.TenantId` resolves to `Guid.Empty`, which means every LINQ query against a tenant-filtered `DbSet` silently returns zero rows — even when matching data already exists for the real tenant ID.

Seeders that guard against duplicates with `AnyAsync` see an empty result set, conclude the data is missing, and attempt to re-insert. This hits the unique index and throws:

```
Microsoft.Data.SqlClient.SqlException: Cannot insert duplicate key row in object
'dbo.NotificationTemplates' with unique index 'IX_NotificationTemplates_TenantId_Key'.
The duplicate key value is (00000000-0000-0000-0000-000000000001, ticket.commented).
```

## Root Cause

The `AnyAsync` duplicate check in `NotificationTemplateSeeder` queried through the global tenant filter without calling `.IgnoreQueryFilters()`. The filter condition `TenantId == Guid.Empty` never matched rows seeded with the real tenant ID (`00000000-0000-0000-0000-000000000001`), so the guard always returned `false` on subsequent startups.

The same pattern existed in `TicketReferenceDataSeeder` for `TicketCategory`, `TicketPriority`, and `TicketStatus`.

## Rule

**Every seeder that queries an `ITenantEntity` table must call `.IgnoreQueryFilters()` before any LINQ operator (`AnyAsync`, `Where`, `FirstOrDefaultAsync`, etc.).**

```csharp
// WRONG — filtered by Guid.Empty at startup, misses existing rows
if (!await context.NotificationTemplates.AnyAsync(t => t.TenantId == tenantId && t.Key == key))

// CORRECT — bypasses the global filter, sees all rows
if (!await context.NotificationTemplates.IgnoreQueryFilters().AnyAsync(t => t.TenantId == tenantId && t.Key == key))
```

## Affected Seeders

| Seeder | Queried Entity | Implements `ITenantEntity` | Fix Required |
|---|---|---|---|
| `NotificationTemplateSeeder` | `NotificationTemplate` | Yes | `.IgnoreQueryFilters()` added |
| `TicketReferenceDataSeeder` | `TicketCategory`, `TicketPriority`, `TicketStatus` | Yes | `.IgnoreQueryFilters()` added |

## Unaffected Seeders

| Seeder | Queried Entity | Why Safe |
|---|---|---|
| `DefaultTenantSeeder` | `Tenant` | Does not implement `ITenantEntity` |
| `PermissionSeeder` | `Permission` | Does not implement `ITenantEntity` |
| `RoleAndUserSeeder` | `ApplicationRole`, `ApplicationUser` | Explicitly excluded from the global filter in `OnModelCreating` |

## Files Changed

- `src/CustomerSupport.Infrastructure/Persistence/Seeders/NotificationTemplateSeeder.cs`
- `src/CustomerSupport.Infrastructure/Persistence/Seeders/TicketReferenceDataSeeder.cs`

## Convention for Future Seeders

Any new seeder added for a tenant-scoped entity must include `IgnoreQueryFilters()` on every query. This is not compiler-enforceable — it is a convention that should be checked during code review whenever a new seeder is introduced.
