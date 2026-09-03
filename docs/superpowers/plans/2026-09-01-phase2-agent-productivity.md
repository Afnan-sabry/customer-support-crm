# Phase 2 — Agent Productivity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add SLA management, auto-assignment, escalation rules, knowledge base, and agent dashboard to the CRM.

**Architecture:** Extends Phase 1's Clean Architecture. New domain entities for SLA, escalation, assignment rules, and knowledge base link to existing Ticket/Customer entities via FKs. A BackgroundService monitors SLA compliance. MediatR notifications decouple ticket lifecycle events from SLA/assignment logic. Angular dashboard aggregates ticket and SLA data.

**Tech Stack:** .NET 10, EF Core 10, ASP.NET Core Identity, MediatR, FluentValidation, Angular 20, Angular Material, ngx-translate, Serilog

**Spec:** `docs/superpowers/specs/2026-08-31-customer-support-crm-design.md`

## Global Constraints

- .NET 10 (`net10.0` TFM), Angular 20 (CLI 20.3.10)
- SQL Server via EF Core Code-First migrations
- Every business entity has `TenantId` (Guid) — enforced by EF Core global query filter
- Bilingual fields: paired `Name` + `NameAr` columns (or `Title` + `TitleAr`)
- API routes: `api/v1/{resource}`
- Pagination: `?page=1&pageSize=20` → `{ items: T[], totalCount: int, page: int, pageSize: int }`
- ProblemDetails for all error responses
- JWT Bearer auth on all endpoints
- MediatR for all Application layer commands/queries
- FluentValidation for all command validation
- Soft deletes via `IsActive` flag on reference data
- Angular standalone components (no NgModules)
- i18n keys in `en.json`/`ar.json` — all user-facing text translated
- Phase 2 entities extend Phase 1 via foreign keys without modifying existing tables

## Dependency Graph

```
P2.1 SLA Policy Domain
  ├── P2.2 SLA Tracking & Breach Detection
  │     └── P2.3 Escalation Rules
  ├── P2.4 Auto-Assignment Rules
  │
  ├─── P2.5 Agent Dashboard API ──► P2.6 Agent Dashboard UI
  │     (depends on P2.1-P2.4 + P2.7)
  │
P2.7 Knowledge Base Backend ──► P2.8 Knowledge Base UI
  (parallel with P2.1-P2.4)
```

---

## File Map

### Domain Layer (`src/CustomerSupport.Domain/`)

```
Entities/
  SlaPolicy.cs                     — Id, TenantId, Name, NameAr, PriorityId?, CategoryId?, FirstResponseMinutes, ResolutionMinutes, IsActive
  TicketSla.cs                     — Id, TenantId, TicketId, SlaPolicyId, FirstResponseDue, ResolutionDue, FirstRespondedAt?, ResolvedAt?, FirstResponseBreached, ResolutionBreached
  SlaBreachLog.cs                  — Id, TenantId, TicketId, SlaPolicyId, BreachType, DueAt, BreachedAt
  EscalationRule.cs                — Id, TenantId, Name, NameAr, PriorityId?, CategoryId?, TriggerType, TriggerAfterMinutes, ActionType, ActionTarget?, Order, IsActive
  AssignmentRule.cs                — Id, TenantId, Name, NameAr, CategoryId?, PriorityId?, Strategy, AgentPool?, LastAssignedIndex, Order, IsActive
  KnowledgeCategory.cs             — Id, TenantId, Name, NameAr, ParentCategoryId?, Order, IsActive
  KnowledgeArticle.cs              — Id, TenantId, Title, TitleAr, Content, ContentAr, CategoryId, Tags?, IsPublished, ViewCount, IsActive

Interfaces/
  ISlaRepository.cs                — GetQueryable, FindBestPolicyAsync
  IKnowledgeRepository.cs          — GetQueryable for articles and categories
```

### Application Layer (`src/CustomerSupport.Application/`)

```
Common/Notifications/
  TicketCreatedNotification.cs     — record(TicketId, TenantId, PriorityId, CategoryId)
  TicketCommentAddedNotification.cs — record(TicketId, CommentUserId, TenantId)

Sla/
  Commands/
    CreateSlaPolicyCommand.cs      — CRUD create
    UpdateSlaPolicyCommand.cs      — CRUD update
    DeleteSlaPolicyCommand.cs      — soft-delete (IsActive = false)
  DTOs/
    SlaPolicyDto.cs
    TicketSlaDto.cs
    SlaBreachLogDto.cs
  Queries/
    GetSlaPoliciesQuery.cs         — paginated list
    GetSlaPolicyByIdQuery.cs
  Validators/
    CreateSlaPolicyValidator.cs
    UpdateSlaPolicyValidator.cs
  Handlers/
    ApplySlaPolicyHandler.cs       — INotificationHandler<TicketCreatedNotification>
    MarkFirstResponseHandler.cs    — INotificationHandler<TicketCommentAddedNotification>

Escalation/
  Commands/
    CreateEscalationRuleCommand.cs
    UpdateEscalationRuleCommand.cs
    DeleteEscalationRuleCommand.cs
  DTOs/
    EscalationRuleDto.cs
  Queries/
    GetEscalationRulesQuery.cs
  Validators/
    CreateEscalationRuleValidator.cs
    UpdateEscalationRuleValidator.cs

Assignment/
  Commands/
    CreateAssignmentRuleCommand.cs
    UpdateAssignmentRuleCommand.cs
    DeleteAssignmentRuleCommand.cs
  DTOs/
    AssignmentRuleDto.cs
  Queries/
    GetAssignmentRulesQuery.cs
  Validators/
    CreateAssignmentRuleValidator.cs
    UpdateAssignmentRuleValidator.cs
  Handlers/
    AutoAssignTicketHandler.cs     — INotificationHandler<TicketCreatedNotification>

Knowledge/
  Commands/
    CreateKnowledgeCategoryCommand.cs
    UpdateKnowledgeCategoryCommand.cs
    DeleteKnowledgeCategoryCommand.cs
    CreateKnowledgeArticleCommand.cs
    UpdateKnowledgeArticleCommand.cs
    DeleteKnowledgeArticleCommand.cs
  DTOs/
    KnowledgeCategoryDto.cs
    KnowledgeArticleDto.cs
    KnowledgeArticleDetailDto.cs
  Queries/
    GetKnowledgeCategoriesQuery.cs
    GetKnowledgeArticlesQuery.cs
    GetKnowledgeArticleByIdQuery.cs
    SearchKnowledgeArticlesQuery.cs
  Validators/
    CreateKnowledgeCategoryValidator.cs
    CreateKnowledgeArticleValidator.cs
    UpdateKnowledgeArticleValidator.cs

Dashboard/
  DTOs/
    DashboardStatsDto.cs
    SlaSummaryDto.cs
    AgentWorkloadDto.cs
  Queries/
    GetDashboardStatsQuery.cs
    GetSlaSummaryQuery.cs
    GetMyTicketsQuery.cs
    GetTeamWorkloadQuery.cs
```

### Infrastructure Layer (`src/CustomerSupport.Infrastructure/`)

```
Persistence/Configurations/
  SlaPolicyConfiguration.cs
  TicketSlaConfiguration.cs
  SlaBreachLogConfiguration.cs
  EscalationRuleConfiguration.cs
  AssignmentRuleConfiguration.cs
  KnowledgeCategoryConfiguration.cs
  KnowledgeArticleConfiguration.cs

Repositories/
  SlaRepository.cs
  KnowledgeRepository.cs

Services/
  SlaMonitoringService.cs          — BackgroundService, runs every 5 min
  EscalationService.cs             — executes escalation rules on breach
  AssignmentService.cs             — round-robin / least-load assignment
```

### API Layer (`src/CustomerSupport.API/`)

```
Controllers/
  SlaController.cs
  EscalationRulesController.cs
  AssignmentRulesController.cs
  KnowledgeController.cs
  DashboardController.cs
```

### Angular (`src/client/src/app/`)

```
features/
  knowledge/
    knowledge.service.ts
    knowledge-list/knowledge-list.ts
    knowledge-article/knowledge-article.ts
    knowledge-editor/knowledge-editor.ts
    knowledge-categories/knowledge-categories.ts
    knowledge.routes.ts
  sla/
    sla.service.ts
    sla-policy-list/sla-policy-list.ts
    sla-policy-form/sla-policy-form.ts
    sla.routes.ts
  escalation/
    escalation.service.ts
    escalation-rule-list/escalation-rule-list.ts
    escalation-rule-form/escalation-rule-form.ts
    escalation.routes.ts
  assignment/
    assignment.service.ts
    assignment-rule-list/assignment-rule-list.ts
    assignment-rule-form/assignment-rule-form.ts
    assignment.routes.ts
  dashboard/
    dashboard.service.ts
    dashboard/dashboard.ts
    dashboard.routes.ts
```

### Modified Files

```
src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs — add 7 DbSets
src/CustomerSupport.Infrastructure/DependencyInjection.cs — register repos, hosted service
src/CustomerSupport.Infrastructure/Persistence/Seeders/PermissionSeeder.cs — add Phase 2 permissions
src/CustomerSupport.Application/Tickets/Commands/CreateTicketCommand.cs — publish TicketCreatedNotification
src/CustomerSupport.Application/Tickets/Commands/AddTicketCommentCommand.cs — publish TicketCommentAddedNotification
src/client/src/app/app.routes.ts — add feature routes
src/client/src/assets/i18n/en.json — add Phase 2 keys
src/client/src/assets/i18n/ar.json — add Phase 2 keys
```

---

### Task 1: SLA Policy Domain & CRUD API (P2.1)

**Files:**
- Create: `src/CustomerSupport.Domain/Entities/SlaPolicy.cs`
- Create: `src/CustomerSupport.Domain/Entities/TicketSla.cs`
- Create: `src/CustomerSupport.Domain/Entities/SlaBreachLog.cs`
- Create: `src/CustomerSupport.Domain/Interfaces/ISlaRepository.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/SlaPolicyConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/TicketSlaConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/SlaBreachLogConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Repositories/SlaRepository.cs`
- Create: `src/CustomerSupport.Application/Sla/DTOs/SlaPolicyDto.cs`
- Create: `src/CustomerSupport.Application/Sla/DTOs/TicketSlaDto.cs`
- Create: `src/CustomerSupport.Application/Sla/DTOs/SlaBreachLogDto.cs`
- Create: `src/CustomerSupport.Application/Sla/Commands/CreateSlaPolicyCommand.cs`
- Create: `src/CustomerSupport.Application/Sla/Commands/UpdateSlaPolicyCommand.cs`
- Create: `src/CustomerSupport.Application/Sla/Commands/DeleteSlaPolicyCommand.cs`
- Create: `src/CustomerSupport.Application/Sla/Queries/GetSlaPoliciesQuery.cs`
- Create: `src/CustomerSupport.Application/Sla/Queries/GetSlaPolicyByIdQuery.cs`
- Create: `src/CustomerSupport.Application/Sla/Validators/CreateSlaPolicyValidator.cs`
- Create: `src/CustomerSupport.Application/Sla/Validators/UpdateSlaPolicyValidator.cs`
- Create: `src/CustomerSupport.Application/Common/Notifications/TicketCreatedNotification.cs`
- Create: `src/CustomerSupport.Application/Common/Notifications/TicketCommentAddedNotification.cs`
- Create: `src/CustomerSupport.Application/Sla/Handlers/ApplySlaPolicyHandler.cs`
- Create: `src/CustomerSupport.API/Controllers/SlaController.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs` — add SlaPolicies, TicketSlas, SlaBreachLogs DbSets
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs` — register ISlaRepository
- Modify: `src/CustomerSupport.Infrastructure/Persistence/Seeders/PermissionSeeder.cs` — add sla.view, sla.manage, dashboard.view, escalation.view, escalation.manage, assignment.view, assignment.manage
- Modify: `src/CustomerSupport.Application/Tickets/Commands/CreateTicketCommand.cs` — publish TicketCreatedNotification

**Interfaces:**
- Consumes: `BaseEntity`, `ITenantEntity`, `IRepository<T>`, `ICurrentUserService`, `AppDbContext`, `Ticket`, `TicketPriority`, `TicketCategory` from Phase 1
- Produces: `SlaPolicy`, `TicketSla`, `SlaBreachLog` entities; `ISlaRepository`; `SlaPolicyDto`, `TicketSlaDto`, `SlaBreachLogDto`; `TicketCreatedNotification`, `TicketCommentAddedNotification`; SLA CRUD API at `api/v1/sla`; `ApplySlaPolicyHandler` that creates TicketSla on ticket creation

- [ ] **Step 1: Create SLA domain entities**

Create `src/CustomerSupport.Domain/Entities/SlaPolicy.cs`:

```csharp
namespace CustomerSupport.Domain.Entities;

public class SlaPolicy : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public Guid? PriorityId { get; set; }
    public Guid? CategoryId { get; set; }
    public int FirstResponseMinutes { get; set; }
    public int ResolutionMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    public TicketPriority? Priority { get; set; }
    public TicketCategory? Category { get; set; }
}
```

Create `src/CustomerSupport.Domain/Entities/TicketSla.cs`:

```csharp
namespace CustomerSupport.Domain.Entities;

public class TicketSla : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TicketId { get; set; }
    public Guid SlaPolicyId { get; set; }
    public DateTime FirstResponseDue { get; set; }
    public DateTime ResolutionDue { get; set; }
    public DateTime? FirstRespondedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool FirstResponseBreached { get; set; }
    public bool ResolutionBreached { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public SlaPolicy SlaPolicy { get; set; } = null!;
}
```

Create `src/CustomerSupport.Domain/Entities/SlaBreachLog.cs`:

```csharp
namespace CustomerSupport.Domain.Entities;

public class SlaBreachLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TicketId { get; set; }
    public Guid SlaPolicyId { get; set; }
    public string BreachType { get; set; } = string.Empty;
    public DateTime DueAt { get; set; }
    public DateTime BreachedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public SlaPolicy SlaPolicy { get; set; } = null!;
}
```

- [ ] **Step 2: Create ISlaRepository interface**

Create `src/CustomerSupport.Domain/Interfaces/ISlaRepository.cs`:

```csharp
using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface ISlaRepository : IRepository<SlaPolicy>
{
    IQueryable<SlaPolicy> GetQueryable();
    Task<SlaPolicy?> FindBestPolicyAsync(Guid tenantId, Guid? priorityId, Guid? categoryId, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Create EF configurations**

Create `src/CustomerSupport.Infrastructure/Persistence/Configurations/SlaPolicyConfiguration.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.ToTable("SlaPolicies");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(s => s.FirstResponseMinutes).IsRequired();
        builder.Property(s => s.ResolutionMinutes).IsRequired();

        builder.HasOne(s => s.Priority).WithMany().HasForeignKey(s => s.PriorityId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(s => s.Category).WithMany().HasForeignKey(s => s.CategoryId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => new { s.TenantId, s.PriorityId, s.CategoryId });
    }
}
```

Create `src/CustomerSupport.Infrastructure/Persistence/Configurations/TicketSlaConfiguration.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class TicketSlaConfiguration : IEntityTypeConfiguration<TicketSla>
{
    public void Configure(EntityTypeBuilder<TicketSla> builder)
    {
        builder.ToTable("TicketSlas");
        builder.HasKey(t => t.Id);

        builder.HasOne(t => t.Ticket).WithMany().HasForeignKey(t => t.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.SlaPolicy).WithMany().HasForeignKey(t => t.SlaPolicyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TicketId).IsUnique();
        builder.HasIndex(t => new { t.TenantId, t.FirstResponseBreached });
        builder.HasIndex(t => new { t.TenantId, t.ResolutionBreached });
    }
}
```

Create `src/CustomerSupport.Infrastructure/Persistence/Configurations/SlaBreachLogConfiguration.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class SlaBreachLogConfiguration : IEntityTypeConfiguration<SlaBreachLog>
{
    public void Configure(EntityTypeBuilder<SlaBreachLog> builder)
    {
        builder.ToTable("SlaBreachLogs");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BreachType).IsRequired().HasMaxLength(50);

        builder.HasOne(b => b.Ticket).WithMany().HasForeignKey(b => b.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(b => b.SlaPolicy).WithMany().HasForeignKey(b => b.SlaPolicyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.TenantId, b.TicketId });
    }
}
```

- [ ] **Step 4: Create SlaRepository**

Create `src/CustomerSupport.Infrastructure/Repositories/SlaRepository.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Repositories;

public class SlaRepository : ISlaRepository
{
    private readonly AppDbContext _context;

    public SlaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SlaPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.SlaPolicies.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<SlaPolicy>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.SlaPolicies.ToListAsync(cancellationToken);

    public async Task<SlaPolicy> AddAsync(SlaPolicy entity, CancellationToken cancellationToken = default)
    {
        await _context.SlaPolicies.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(SlaPolicy entity, CancellationToken cancellationToken = default)
    {
        _context.SlaPolicies.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public IQueryable<SlaPolicy> GetQueryable() => _context.SlaPolicies.AsQueryable();

    public async Task<SlaPolicy?> FindBestPolicyAsync(Guid tenantId, Guid? priorityId, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        var policies = await _context.SlaPolicies
            .Where(p => p.IsActive)
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return policies.FirstOrDefault(p => p.PriorityId == priorityId && p.CategoryId == categoryId)
            ?? policies.FirstOrDefault(p => p.PriorityId == priorityId && p.CategoryId == null)
            ?? policies.FirstOrDefault(p => p.PriorityId == null && p.CategoryId == categoryId)
            ?? policies.FirstOrDefault(p => p.PriorityId == null && p.CategoryId == null);
    }
}
```

- [ ] **Step 5: Create SLA DTOs**

Create `src/CustomerSupport.Application/Sla/DTOs/SlaPolicyDto.cs`:

```csharp
namespace CustomerSupport.Application.Sla.DTOs;

public record SlaPolicyDto(
    Guid Id, string Name, string NameAr,
    Guid? PriorityId, string? PriorityName,
    Guid? CategoryId, string? CategoryName,
    int FirstResponseMinutes, int ResolutionMinutes, bool IsActive);
```

Create `src/CustomerSupport.Application/Sla/DTOs/TicketSlaDto.cs`:

```csharp
namespace CustomerSupport.Application.Sla.DTOs;

public record TicketSlaDto(
    Guid Id, Guid TicketId, string TicketNumber,
    Guid SlaPolicyId, string SlaPolicyName,
    DateTime FirstResponseDue, DateTime ResolutionDue,
    DateTime? FirstRespondedAt, DateTime? ResolvedAt,
    bool FirstResponseBreached, bool ResolutionBreached);
```

Create `src/CustomerSupport.Application/Sla/DTOs/SlaBreachLogDto.cs`:

```csharp
namespace CustomerSupport.Application.Sla.DTOs;

public record SlaBreachLogDto(
    Guid Id, Guid TicketId, string TicketNumber,
    string BreachType, DateTime DueAt, DateTime BreachedAt);
```

- [ ] **Step 6: Create SLA commands**

Create `src/CustomerSupport.Application/Sla/Commands/CreateSlaPolicyCommand.cs`:

```csharp
using CustomerSupport.Application.Sla.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Sla.Commands;

public record CreateSlaPolicyCommand(
    string Name, string NameAr,
    Guid? PriorityId, Guid? CategoryId,
    int FirstResponseMinutes, int ResolutionMinutes) : IRequest<SlaPolicyDto>;

public class CreateSlaPolicyCommandHandler : IRequestHandler<CreateSlaPolicyCommand, SlaPolicyDto>
{
    private readonly ISlaRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly AppDbContext _context;

    public CreateSlaPolicyCommandHandler(ISlaRepository repository, ICurrentUserService currentUserService, AppDbContext context)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async Task<SlaPolicyDto> Handle(CreateSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            Name = request.Name,
            NameAr = request.NameAr,
            PriorityId = request.PriorityId,
            CategoryId = request.CategoryId,
            FirstResponseMinutes = request.FirstResponseMinutes,
            ResolutionMinutes = request.ResolutionMinutes
        };

        await _repository.AddAsync(policy, cancellationToken);

        var priorityName = request.PriorityId.HasValue
            ? await _context.TicketPriorities.Where(p => p.Id == request.PriorityId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var categoryName = request.CategoryId.HasValue
            ? await _context.TicketCategories.Where(c => c.Id == request.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        return new SlaPolicyDto(policy.Id, policy.Name, policy.NameAr,
            policy.PriorityId, priorityName, policy.CategoryId, categoryName,
            policy.FirstResponseMinutes, policy.ResolutionMinutes, policy.IsActive);
    }
}
```

Create `src/CustomerSupport.Application/Sla/Commands/UpdateSlaPolicyCommand.cs`:

```csharp
using CustomerSupport.Application.Sla.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Sla.Commands;

public record UpdateSlaPolicyCommand(
    Guid Id, string Name, string NameAr,
    Guid? PriorityId, Guid? CategoryId,
    int FirstResponseMinutes, int ResolutionMinutes) : IRequest<SlaPolicyDto>;

public class UpdateSlaPolicyCommandHandler : IRequestHandler<UpdateSlaPolicyCommand, SlaPolicyDto>
{
    private readonly ISlaRepository _repository;
    private readonly AppDbContext _context;

    public UpdateSlaPolicyCommandHandler(ISlaRepository repository, AppDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<SlaPolicyDto> Handle(UpdateSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("SLA policy not found.");

        policy.Name = request.Name;
        policy.NameAr = request.NameAr;
        policy.PriorityId = request.PriorityId;
        policy.CategoryId = request.CategoryId;
        policy.FirstResponseMinutes = request.FirstResponseMinutes;
        policy.ResolutionMinutes = request.ResolutionMinutes;

        await _repository.UpdateAsync(policy, cancellationToken);

        var priorityName = request.PriorityId.HasValue
            ? await _context.TicketPriorities.Where(p => p.Id == request.PriorityId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var categoryName = request.CategoryId.HasValue
            ? await _context.TicketCategories.Where(c => c.Id == request.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        return new SlaPolicyDto(policy.Id, policy.Name, policy.NameAr,
            policy.PriorityId, priorityName, policy.CategoryId, categoryName,
            policy.FirstResponseMinutes, policy.ResolutionMinutes, policy.IsActive);
    }
}
```

Create `src/CustomerSupport.Application/Sla/Commands/DeleteSlaPolicyCommand.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Sla.Commands;

public record DeleteSlaPolicyCommand(Guid Id) : IRequest<Result>;

public class DeleteSlaPolicyCommandHandler : IRequestHandler<DeleteSlaPolicyCommand, Result>
{
    private readonly ISlaRepository _repository;

    public DeleteSlaPolicyCommandHandler(ISlaRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(DeleteSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("SLA policy not found.");

        policy.IsActive = false;
        await _repository.UpdateAsync(policy, cancellationToken);
        return Result.Success();
    }
}
```

- [ ] **Step 7: Create SLA queries**

Create `src/CustomerSupport.Application/Sla/Queries/GetSlaPoliciesQuery.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Sla.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Sla.Queries;

public record GetSlaPoliciesQuery(bool? IsActive, int Page = 1, int PageSize = 20)
    : IRequest<PaginatedList<SlaPolicyDto>>;

public class GetSlaPoliciesQueryHandler : IRequestHandler<GetSlaPoliciesQuery, PaginatedList<SlaPolicyDto>>
{
    private readonly ISlaRepository _repository;

    public GetSlaPoliciesQueryHandler(ISlaRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedList<SlaPolicyDto>> Handle(GetSlaPoliciesQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.GetQueryable()
            .Include(s => s.Priority)
            .Include(s => s.Category)
            .AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        query = query.OrderBy(s => s.Name);

        var projected = query.Select(s => new SlaPolicyDto(
            s.Id, s.Name, s.NameAr,
            s.PriorityId, s.Priority != null ? s.Priority.Name : null,
            s.CategoryId, s.Category != null ? s.Category.Name : null,
            s.FirstResponseMinutes, s.ResolutionMinutes, s.IsActive));

        return await PaginatedList<SlaPolicyDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
```

Create `src/CustomerSupport.Application/Sla/Queries/GetSlaPolicyByIdQuery.cs`:

```csharp
using CustomerSupport.Application.Sla.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Sla.Queries;

public record GetSlaPolicyByIdQuery(Guid Id) : IRequest<SlaPolicyDto>;

public class GetSlaPolicyByIdQueryHandler : IRequestHandler<GetSlaPolicyByIdQuery, SlaPolicyDto>
{
    private readonly AppDbContext _context;

    public GetSlaPolicyByIdQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SlaPolicyDto> Handle(GetSlaPolicyByIdQuery request, CancellationToken cancellationToken)
    {
        var policy = await _context.SlaPolicies
            .Include(s => s.Priority)
            .Include(s => s.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("SLA policy not found.");

        return new SlaPolicyDto(policy.Id, policy.Name, policy.NameAr,
            policy.PriorityId, policy.Priority?.Name,
            policy.CategoryId, policy.Category?.Name,
            policy.FirstResponseMinutes, policy.ResolutionMinutes, policy.IsActive);
    }
}
```

- [ ] **Step 8: Create SLA validators**

Create `src/CustomerSupport.Application/Sla/Validators/CreateSlaPolicyValidator.cs`:

```csharp
using CustomerSupport.Application.Sla.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Sla.Validators;

public class CreateSlaPolicyValidator : AbstractValidator<CreateSlaPolicyCommand>
{
    public CreateSlaPolicyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FirstResponseMinutes).GreaterThan(0);
        RuleFor(x => x.ResolutionMinutes).GreaterThan(0);
        RuleFor(x => x.ResolutionMinutes).GreaterThanOrEqualTo(x => x.FirstResponseMinutes)
            .WithMessage("Resolution time must be >= first response time.");
    }
}
```

Create `src/CustomerSupport.Application/Sla/Validators/UpdateSlaPolicyValidator.cs`:

```csharp
using CustomerSupport.Application.Sla.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Sla.Validators;

public class UpdateSlaPolicyValidator : AbstractValidator<UpdateSlaPolicyCommand>
{
    public UpdateSlaPolicyValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FirstResponseMinutes).GreaterThan(0);
        RuleFor(x => x.ResolutionMinutes).GreaterThan(0);
        RuleFor(x => x.ResolutionMinutes).GreaterThanOrEqualTo(x => x.FirstResponseMinutes)
            .WithMessage("Resolution time must be >= first response time.");
    }
}
```

- [ ] **Step 9: Create MediatR notifications**

Create `src/CustomerSupport.Application/Common/Notifications/TicketCreatedNotification.cs`:

```csharp
using MediatR;

namespace CustomerSupport.Application.Common.Notifications;

public record TicketCreatedNotification(
    Guid TicketId, Guid TenantId,
    Guid PriorityId, Guid CategoryId) : INotification;
```

Create `src/CustomerSupport.Application/Common/Notifications/TicketCommentAddedNotification.cs`:

```csharp
using MediatR;

namespace CustomerSupport.Application.Common.Notifications;

public record TicketCommentAddedNotification(
    Guid TicketId, Guid CommentUserId,
    Guid TenantId) : INotification;
```

- [ ] **Step 10: Create ApplySlaPolicyHandler**

Create `src/CustomerSupport.Application/Sla/Handlers/ApplySlaPolicyHandler.cs`:

```csharp
using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Sla.Handlers;

public class ApplySlaPolicyHandler : INotificationHandler<TicketCreatedNotification>
{
    private readonly ISlaRepository _slaRepository;
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public ApplySlaPolicyHandler(ISlaRepository slaRepository, AppDbContext context, IDateTimeService dateTimeService)
    {
        _slaRepository = slaRepository;
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(TicketCreatedNotification notification, CancellationToken cancellationToken)
    {
        var policy = await _slaRepository.FindBestPolicyAsync(
            notification.TenantId, notification.PriorityId, notification.CategoryId, cancellationToken);

        if (policy == null) return;

        var now = _dateTimeService.UtcNow;
        var ticketSla = new TicketSla
        {
            Id = Guid.NewGuid(),
            TenantId = notification.TenantId,
            TicketId = notification.TicketId,
            SlaPolicyId = policy.Id,
            FirstResponseDue = now.AddMinutes(policy.FirstResponseMinutes),
            ResolutionDue = now.AddMinutes(policy.ResolutionMinutes)
        };

        await _context.TicketSlas.AddAsync(ticketSla, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 11: Create SlaController**

Create `src/CustomerSupport.API/Controllers/SlaController.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Sla.Commands;
using CustomerSupport.Application.Sla.DTOs;
using CustomerSupport.Application.Sla.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SlaController : ControllerBase
{
    private readonly IMediator _mediator;

    public SlaController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = "Permission:sla.view")]
    public async Task<ActionResult<PaginatedList<SlaPolicyDto>>> GetAll(
        [FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new GetSlaPoliciesQuery(isActive, page, pageSize)));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:sla.view")]
    public async Task<ActionResult<SlaPolicyDto>> GetById(Guid id)
        => Ok(await _mediator.Send(new GetSlaPolicyByIdQuery(id)));

    [HttpPost]
    [Authorize(Policy = "Permission:sla.manage")]
    public async Task<ActionResult<SlaPolicyDto>> Create(CreateSlaPolicyCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:sla.manage")]
    public async Task<ActionResult<SlaPolicyDto>> Update(Guid id, UpdateSlaPolicyCommand command)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:sla.manage")]
    public async Task<ActionResult<Result>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteSlaPolicyCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
```

- [ ] **Step 12: Update AppDbContext with SLA DbSets**

Add to `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs` after existing DbSets:

```csharp
public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
public DbSet<TicketSla> TicketSlas => Set<TicketSla>();
public DbSet<SlaBreachLog> SlaBreachLogs => Set<SlaBreachLog>();
```

- [ ] **Step 13: Register ISlaRepository in DI**

Add to `src/CustomerSupport.Infrastructure/DependencyInjection.cs` after existing repository registrations:

```csharp
services.AddScoped<ISlaRepository, SlaRepository>();
```

Add the using:

```csharp
using CustomerSupport.Infrastructure.Repositories;
```

- [ ] **Step 14: Add Phase 2 permissions to seeder**

Add to `src/CustomerSupport.Infrastructure/Persistence/Seeders/PermissionSeeder.cs` permission array:

```csharp
("sla.view", "SLA", "View SLA policies"),
("sla.manage", "SLA", "Manage SLA policies"),
("escalation.view", "Escalation", "View escalation rules"),
("escalation.manage", "Escalation", "Manage escalation rules"),
("assignment.view", "Assignment", "View assignment rules"),
("assignment.manage", "Assignment", "Manage assignment rules"),
("dashboard.view", "Dashboard", "View agent dashboard"),
```

- [ ] **Step 15: Modify CreateTicketCommand to publish notification**

In `src/CustomerSupport.Application/Tickets/Commands/CreateTicketCommand.cs`, inject `IPublisher` and publish after save:

Add to constructor parameters: `IPublisher publisher`

Add after the ticket is saved and before the return statement:

```csharp
await _publisher.Publish(new Common.Notifications.TicketCreatedNotification(
    ticket.Id, ticket.TenantId, ticket.PriorityId, ticket.CategoryId), cancellationToken);
```

Add using: `using CustomerSupport.Application.Common.Notifications;`

- [ ] **Step 16: Generate migration and verify build**

```powershell
cd src/CustomerSupport.Infrastructure
dotnet ef migrations add AddSlaPolicyEntities --startup-project ../CustomerSupport.API
cd ../..
dotnet build src/CustomerSupport.sln
```

- [ ] **Step 17: Commit**

```bash
git add src/
git commit -m "feat(sla): add SLA policy domain, CRUD API, and ticket SLA tracking foundation"
```

---

### Task 2: SLA Tracking & Breach Detection (P2.2)

**Files:**
- Create: `src/CustomerSupport.Infrastructure/Services/SlaMonitoringService.cs`
- Create: `src/CustomerSupport.Application/Sla/Handlers/MarkFirstResponseHandler.cs`
- Modify: `src/CustomerSupport.Application/Tickets/Commands/AddTicketCommentCommand.cs` — publish TicketCommentAddedNotification
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs` — register hosted service

**Interfaces:**
- Consumes: `TicketSla`, `SlaBreachLog`, `TicketCommentAddedNotification`, `AppDbContext`, `IDateTimeService` from Task 1 / Phase 1
- Produces: `SlaMonitoringService` (background breach detection), `MarkFirstResponseHandler` (marks first response on agent comment)

- [ ] **Step 1: Create SLA monitoring background service**

Create `src/CustomerSupport.Infrastructure/Services/SlaMonitoringService.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services;

public class SlaMonitoringService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaMonitoringService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public SlaMonitoringService(IServiceScopeFactory scopeFactory, ILogger<SlaMonitoringService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckForBreachesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking SLA breaches");
            }
        }
    }

    private async Task CheckForBreachesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dateTimeService = scope.ServiceProvider.GetRequiredService<IDateTimeService>();
        var now = dateTimeService.UtcNow;

        var finalStatusIds = await context.TicketStatuses
            .Where(s => s.IsFinal)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var firstResponseBreaches = await context.TicketSlas
            .IgnoreQueryFilters()
            .Where(ts => !ts.FirstResponseBreached && ts.FirstRespondedAt == null && ts.FirstResponseDue < now)
            .Where(ts => !finalStatusIds.Contains(ts.Ticket.StatusId))
            .ToListAsync(cancellationToken);

        foreach (var sla in firstResponseBreaches)
        {
            sla.FirstResponseBreached = true;
            context.SlaBreachLogs.Add(new SlaBreachLog
            {
                Id = Guid.NewGuid(),
                TenantId = sla.TenantId,
                TicketId = sla.TicketId,
                SlaPolicyId = sla.SlaPolicyId,
                BreachType = "FirstResponse",
                DueAt = sla.FirstResponseDue,
                BreachedAt = now
            });
        }

        var resolutionBreaches = await context.TicketSlas
            .IgnoreQueryFilters()
            .Where(ts => !ts.ResolutionBreached && ts.ResolvedAt == null && ts.ResolutionDue < now)
            .Where(ts => !finalStatusIds.Contains(ts.Ticket.StatusId))
            .ToListAsync(cancellationToken);

        foreach (var sla in resolutionBreaches)
        {
            sla.ResolutionBreached = true;
            context.SlaBreachLogs.Add(new SlaBreachLog
            {
                Id = Guid.NewGuid(),
                TenantId = sla.TenantId,
                TicketId = sla.TicketId,
                SlaPolicyId = sla.SlaPolicyId,
                BreachType = "Resolution",
                DueAt = sla.ResolutionDue,
                BreachedAt = now
            });
        }

        if (firstResponseBreaches.Count > 0 || resolutionBreaches.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("SLA breaches detected: {FirstResponse} first-response, {Resolution} resolution",
                firstResponseBreaches.Count, resolutionBreaches.Count);
        }
    }
}
```

- [ ] **Step 2: Create MarkFirstResponseHandler**

Create `src/CustomerSupport.Application/Sla/Handlers/MarkFirstResponseHandler.cs`:

```csharp
using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Sla.Handlers;

public class MarkFirstResponseHandler : INotificationHandler<TicketCommentAddedNotification>
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public MarkFirstResponseHandler(AppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(TicketCommentAddedNotification notification, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == notification.TicketId, cancellationToken);

        if (ticket == null) return;

        var isAgent = notification.CommentUserId != Guid.Empty;
        if (!isAgent) return;

        var ticketSla = await _context.TicketSlas
            .FirstOrDefaultAsync(ts => ts.TicketId == notification.TicketId && ts.FirstRespondedAt == null, cancellationToken);

        if (ticketSla == null) return;

        ticketSla.FirstRespondedAt = _dateTimeService.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 3: Modify AddTicketCommentCommand to publish notification**

In `src/CustomerSupport.Application/Tickets/Commands/AddTicketCommentCommand.cs`, inject `IPublisher` and publish after save:

Add `IPublisher publisher` to the constructor.

Add after the comment is saved and before the return statement:

```csharp
await _publisher.Publish(new Common.Notifications.TicketCommentAddedNotification(
    request.TicketId, _currentUserService.UserId ?? Guid.Empty, ticket.TenantId), cancellationToken);
```

Add using: `using CustomerSupport.Application.Common.Notifications;`

- [ ] **Step 4: Register hosted service**

Add to `src/CustomerSupport.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddHostedService<SlaMonitoringService>();
```

Add using: `using CustomerSupport.Infrastructure.Services;`

- [ ] **Step 5: Verify build**

```powershell
dotnet build src/CustomerSupport.sln
```

- [ ] **Step 6: Commit**

```bash
git add src/
git commit -m "feat(sla): add SLA monitoring background service and first-response tracking"
```

---

### Task 3: Escalation Rules Domain & API (P2.3)

**Files:**
- Create: `src/CustomerSupport.Domain/Entities/EscalationRule.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/EscalationRuleConfiguration.cs`
- Create: `src/CustomerSupport.Application/Escalation/DTOs/EscalationRuleDto.cs`
- Create: `src/CustomerSupport.Application/Escalation/Commands/CreateEscalationRuleCommand.cs`
- Create: `src/CustomerSupport.Application/Escalation/Commands/UpdateEscalationRuleCommand.cs`
- Create: `src/CustomerSupport.Application/Escalation/Commands/DeleteEscalationRuleCommand.cs`
- Create: `src/CustomerSupport.Application/Escalation/Queries/GetEscalationRulesQuery.cs`
- Create: `src/CustomerSupport.Application/Escalation/Validators/CreateEscalationRuleValidator.cs`
- Create: `src/CustomerSupport.Application/Escalation/Validators/UpdateEscalationRuleValidator.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/EscalationService.cs`
- Create: `src/CustomerSupport.API/Controllers/EscalationRulesController.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs` — add EscalationRules DbSet
- Modify: `src/CustomerSupport.Infrastructure/Services/SlaMonitoringService.cs` — invoke EscalationService on breach

**Interfaces:**
- Consumes: `BaseEntity`, `ITenantEntity`, `TicketPriority`, `TicketCategory`, `SlaBreachLog`, `SlaMonitoringService` from Tasks 1–2
- Produces: `EscalationRule` entity, `EscalationRuleDto`, escalation CRUD API at `api/v1/escalation-rules`, `EscalationService` that executes rules on breach

- [ ] **Step 1: Create EscalationRule entity**

Create `src/CustomerSupport.Domain/Entities/EscalationRule.cs`:

```csharp
namespace CustomerSupport.Domain.Entities;

public class EscalationRule : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public Guid? PriorityId { get; set; }
    public Guid? CategoryId { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public int TriggerAfterMinutes { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? ActionTarget { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public TicketPriority? Priority { get; set; }
    public TicketCategory? Category { get; set; }
}
```

- [ ] **Step 2: Create EscalationRule configuration**

Create `src/CustomerSupport.Infrastructure/Persistence/Configurations/EscalationRuleConfiguration.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class EscalationRuleConfiguration : IEntityTypeConfiguration<EscalationRule>
{
    public void Configure(EntityTypeBuilder<EscalationRule> builder)
    {
        builder.ToTable("EscalationRules");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(e => e.TriggerType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ActionType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ActionTarget).HasMaxLength(500);

        builder.HasOne(e => e.Priority).WithMany().HasForeignKey(e => e.PriorityId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.TenantId, e.Order });
    }
}
```

- [ ] **Step 3: Create DTO**

Create `src/CustomerSupport.Application/Escalation/DTOs/EscalationRuleDto.cs`:

```csharp
namespace CustomerSupport.Application.Escalation.DTOs;

public record EscalationRuleDto(
    Guid Id, string Name, string NameAr,
    Guid? PriorityId, string? PriorityName,
    Guid? CategoryId, string? CategoryName,
    string TriggerType, int TriggerAfterMinutes,
    string ActionType, string? ActionTarget,
    int Order, bool IsActive);
```

- [ ] **Step 4: Create CRUD commands**

Create `src/CustomerSupport.Application/Escalation/Commands/CreateEscalationRuleCommand.cs`:

```csharp
using CustomerSupport.Application.Escalation.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Escalation.Commands;

public record CreateEscalationRuleCommand(
    string Name, string NameAr,
    Guid? PriorityId, Guid? CategoryId,
    string TriggerType, int TriggerAfterMinutes,
    string ActionType, string? ActionTarget,
    int Order) : IRequest<EscalationRuleDto>;

public class CreateEscalationRuleCommandHandler : IRequestHandler<CreateEscalationRuleCommand, EscalationRuleDto>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateEscalationRuleCommandHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<EscalationRuleDto> Handle(CreateEscalationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = new EscalationRule
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            Name = request.Name,
            NameAr = request.NameAr,
            PriorityId = request.PriorityId,
            CategoryId = request.CategoryId,
            TriggerType = request.TriggerType,
            TriggerAfterMinutes = request.TriggerAfterMinutes,
            ActionType = request.ActionType,
            ActionTarget = request.ActionTarget,
            Order = request.Order
        };

        await _context.EscalationRules.AddAsync(rule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var priorityName = request.PriorityId.HasValue
            ? await _context.TicketPriorities.Where(p => p.Id == request.PriorityId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var categoryName = request.CategoryId.HasValue
            ? await _context.TicketCategories.Where(c => c.Id == request.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        return new EscalationRuleDto(rule.Id, rule.Name, rule.NameAr,
            rule.PriorityId, priorityName, rule.CategoryId, categoryName,
            rule.TriggerType, rule.TriggerAfterMinutes, rule.ActionType, rule.ActionTarget,
            rule.Order, rule.IsActive);
    }
}
```

Create `src/CustomerSupport.Application/Escalation/Commands/UpdateEscalationRuleCommand.cs`:

```csharp
using CustomerSupport.Application.Escalation.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Escalation.Commands;

public record UpdateEscalationRuleCommand(
    Guid Id, string Name, string NameAr,
    Guid? PriorityId, Guid? CategoryId,
    string TriggerType, int TriggerAfterMinutes,
    string ActionType, string? ActionTarget,
    int Order) : IRequest<EscalationRuleDto>;

public class UpdateEscalationRuleCommandHandler : IRequestHandler<UpdateEscalationRuleCommand, EscalationRuleDto>
{
    private readonly AppDbContext _context;

    public UpdateEscalationRuleCommandHandler(AppDbContext context) => _context = context;

    public async Task<EscalationRuleDto> Handle(UpdateEscalationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.EscalationRules.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException("Escalation rule not found.");

        rule.Name = request.Name;
        rule.NameAr = request.NameAr;
        rule.PriorityId = request.PriorityId;
        rule.CategoryId = request.CategoryId;
        rule.TriggerType = request.TriggerType;
        rule.TriggerAfterMinutes = request.TriggerAfterMinutes;
        rule.ActionType = request.ActionType;
        rule.ActionTarget = request.ActionTarget;
        rule.Order = request.Order;

        await _context.SaveChangesAsync(cancellationToken);

        var priorityName = request.PriorityId.HasValue
            ? await _context.TicketPriorities.Where(p => p.Id == request.PriorityId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var categoryName = request.CategoryId.HasValue
            ? await _context.TicketCategories.Where(c => c.Id == request.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        return new EscalationRuleDto(rule.Id, rule.Name, rule.NameAr,
            rule.PriorityId, priorityName, rule.CategoryId, categoryName,
            rule.TriggerType, rule.TriggerAfterMinutes, rule.ActionType, rule.ActionTarget,
            rule.Order, rule.IsActive);
    }
}
```

Create `src/CustomerSupport.Application/Escalation/Commands/DeleteEscalationRuleCommand.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Escalation.Commands;

public record DeleteEscalationRuleCommand(Guid Id) : IRequest<Result>;

public class DeleteEscalationRuleCommandHandler : IRequestHandler<DeleteEscalationRuleCommand, Result>
{
    private readonly AppDbContext _context;

    public DeleteEscalationRuleCommandHandler(AppDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteEscalationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.EscalationRules.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException("Escalation rule not found.");

        rule.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

- [ ] **Step 5: Create query**

Create `src/CustomerSupport.Application/Escalation/Queries/GetEscalationRulesQuery.cs`:

```csharp
using CustomerSupport.Application.Escalation.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Escalation.Queries;

public record GetEscalationRulesQuery(bool? IsActive) : IRequest<List<EscalationRuleDto>>;

public class GetEscalationRulesQueryHandler : IRequestHandler<GetEscalationRulesQuery, List<EscalationRuleDto>>
{
    private readonly AppDbContext _context;

    public GetEscalationRulesQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<EscalationRuleDto>> Handle(GetEscalationRulesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.EscalationRules
            .Include(e => e.Priority)
            .Include(e => e.Category)
            .AsNoTracking()
            .AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(e => e.IsActive == request.IsActive.Value);

        return await query.OrderBy(e => e.Order)
            .Select(e => new EscalationRuleDto(
                e.Id, e.Name, e.NameAr,
                e.PriorityId, e.Priority != null ? e.Priority.Name : null,
                e.CategoryId, e.Category != null ? e.Category.Name : null,
                e.TriggerType, e.TriggerAfterMinutes,
                e.ActionType, e.ActionTarget,
                e.Order, e.IsActive))
            .ToListAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Create validators**

Create `src/CustomerSupport.Application/Escalation/Validators/CreateEscalationRuleValidator.cs`:

```csharp
using CustomerSupport.Application.Escalation.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Escalation.Validators;

public class CreateEscalationRuleValidator : AbstractValidator<CreateEscalationRuleCommand>
{
    public CreateEscalationRuleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TriggerType).NotEmpty().Must(t => t is "FirstResponseBreached" or "ResolutionBreached")
            .WithMessage("TriggerType must be 'FirstResponseBreached' or 'ResolutionBreached'.");
        RuleFor(x => x.TriggerAfterMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ActionType).NotEmpty().Must(a => a is "Reassign" or "ChangePriority")
            .WithMessage("ActionType must be 'Reassign' or 'ChangePriority'.");
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
```

Create `src/CustomerSupport.Application/Escalation/Validators/UpdateEscalationRuleValidator.cs`:

```csharp
using CustomerSupport.Application.Escalation.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Escalation.Validators;

public class UpdateEscalationRuleValidator : AbstractValidator<UpdateEscalationRuleCommand>
{
    public UpdateEscalationRuleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TriggerType).NotEmpty().Must(t => t is "FirstResponseBreached" or "ResolutionBreached")
            .WithMessage("TriggerType must be 'FirstResponseBreached' or 'ResolutionBreached'.");
        RuleFor(x => x.TriggerAfterMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ActionType).NotEmpty().Must(a => a is "Reassign" or "ChangePriority")
            .WithMessage("ActionType must be 'Reassign' or 'ChangePriority'.");
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
```

- [ ] **Step 7: Create EscalationService**

Create `src/CustomerSupport.Infrastructure/Services/EscalationService.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services;

public class EscalationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EscalationService> _logger;

    public EscalationService(AppDbContext context, ILogger<EscalationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ProcessBreachAsync(SlaBreachLog breach, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == breach.TicketId, cancellationToken);

        if (ticket == null) return;

        var rules = await _context.EscalationRules
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == breach.TenantId && r.IsActive)
            .Where(r => r.TriggerType == breach.BreachType)
            .Where(r => (r.PriorityId == null || r.PriorityId == ticket.PriorityId)
                     && (r.CategoryId == null || r.CategoryId == ticket.CategoryId))
            .OrderBy(r => r.Order)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            switch (rule.ActionType)
            {
                case "Reassign" when Guid.TryParse(rule.ActionTarget, out var targetUserId):
                    var oldAssignee = ticket.AssignedToId?.ToString();
                    ticket.AssignedToId = targetUserId;
                    _context.Set<TicketHistory>().Add(new TicketHistory
                    {
                        Id = Guid.NewGuid(),
                        TenantId = breach.TenantId,
                        TicketId = ticket.Id,
                        Field = "AssignedToId",
                        OldValue = oldAssignee,
                        NewValue = targetUserId.ToString(),
                        CreatedAt = DateTime.UtcNow
                    });
                    _logger.LogInformation("Escalation: Ticket {TicketId} reassigned to {UserId} by rule {RuleId}",
                        ticket.Id, targetUserId, rule.Id);
                    break;

                case "ChangePriority" when Guid.TryParse(rule.ActionTarget, out var targetPriorityId):
                    var oldPriority = ticket.PriorityId.ToString();
                    ticket.PriorityId = targetPriorityId;
                    _context.Set<TicketHistory>().Add(new TicketHistory
                    {
                        Id = Guid.NewGuid(),
                        TenantId = breach.TenantId,
                        TicketId = ticket.Id,
                        Field = "PriorityId",
                        OldValue = oldPriority,
                        NewValue = targetPriorityId.ToString(),
                        CreatedAt = DateTime.UtcNow
                    });
                    _logger.LogInformation("Escalation: Ticket {TicketId} priority changed to {PriorityId} by rule {RuleId}",
                        ticket.Id, targetPriorityId, rule.Id);
                    break;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 8: Integrate EscalationService into SlaMonitoringService**

Modify `src/CustomerSupport.Infrastructure/Services/SlaMonitoringService.cs` — in `CheckForBreachesAsync`, after saving breach logs, invoke escalation:

Add after the `SaveChangesAsync` call inside the breach count check:

```csharp
var escalationService = scope.ServiceProvider.GetRequiredService<EscalationService>();
var allBreaches = firstResponseBreaches.Select(s => context.SlaBreachLogs.Local
    .First(b => b.TicketId == s.TicketId && b.BreachType == "FirstResponse"))
    .Concat(resolutionBreaches.Select(s => context.SlaBreachLogs.Local
    .First(b => b.TicketId == s.TicketId && b.BreachType == "Resolution")));

foreach (var breach in allBreaches)
{
    await escalationService.ProcessBreachAsync(breach, cancellationToken);
}
```

Actually, simpler approach — track the breach logs we created:

Replace the breach detection section to collect the new breach logs, then iterate them:

Before the foreach loops, create a list: `var newBreachLogs = new List<SlaBreachLog>();`

In each foreach, add the new SlaBreachLog to both `context.SlaBreachLogs` and `newBreachLogs`.

After `SaveChangesAsync`, add:

```csharp
var escalationService = scope.ServiceProvider.GetRequiredService<EscalationService>();
foreach (var breach in newBreachLogs)
{
    try
    {
        await escalationService.ProcessBreachAsync(breach, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing escalation for breach {BreachId}", breach.Id);
    }
}
```

- [ ] **Step 9: Create controller**

Create `src/CustomerSupport.API/Controllers/EscalationRulesController.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Escalation.Commands;
using CustomerSupport.Application.Escalation.DTOs;
using CustomerSupport.Application.Escalation.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/escalation-rules")]
public class EscalationRulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EscalationRulesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = "Permission:escalation.view")]
    public async Task<ActionResult<List<EscalationRuleDto>>> GetAll([FromQuery] bool? isActive)
        => Ok(await _mediator.Send(new GetEscalationRulesQuery(isActive)));

    [HttpPost]
    [Authorize(Policy = "Permission:escalation.manage")]
    public async Task<ActionResult<EscalationRuleDto>> Create(CreateEscalationRuleCommand command)
        => CreatedAtAction(null, await _mediator.Send(command));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:escalation.manage")]
    public async Task<ActionResult<EscalationRuleDto>> Update(Guid id, UpdateEscalationRuleCommand command)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:escalation.manage")]
    public async Task<ActionResult<Result>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteEscalationRuleCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
```

- [ ] **Step 10: Update AppDbContext and DI**

Add to `AppDbContext.cs`:

```csharp
public DbSet<EscalationRule> EscalationRules => Set<EscalationRule>();
```

Add to `DependencyInjection.cs`:

```csharp
services.AddScoped<EscalationService>();
```

- [ ] **Step 11: Generate migration and verify build**

```powershell
cd src/CustomerSupport.Infrastructure
dotnet ef migrations add AddEscalationRules --startup-project ../CustomerSupport.API
cd ../..
dotnet build src/CustomerSupport.sln
```

- [ ] **Step 12: Commit**

```bash
git add src/
git commit -m "feat(escalation): add escalation rules domain, API, and breach-triggered execution"
```

---

### Task 4: Auto-Assignment Rules Domain & API (P2.4)

**Files:**
- Create: `src/CustomerSupport.Domain/Entities/AssignmentRule.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/AssignmentRuleConfiguration.cs`
- Create: `src/CustomerSupport.Application/Assignment/DTOs/AssignmentRuleDto.cs`
- Create: `src/CustomerSupport.Application/Assignment/Commands/CreateAssignmentRuleCommand.cs`
- Create: `src/CustomerSupport.Application/Assignment/Commands/UpdateAssignmentRuleCommand.cs`
- Create: `src/CustomerSupport.Application/Assignment/Commands/DeleteAssignmentRuleCommand.cs`
- Create: `src/CustomerSupport.Application/Assignment/Queries/GetAssignmentRulesQuery.cs`
- Create: `src/CustomerSupport.Application/Assignment/Validators/CreateAssignmentRuleValidator.cs`
- Create: `src/CustomerSupport.Application/Assignment/Validators/UpdateAssignmentRuleValidator.cs`
- Create: `src/CustomerSupport.Application/Assignment/Handlers/AutoAssignTicketHandler.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/AssignmentService.cs`
- Create: `src/CustomerSupport.API/Controllers/AssignmentRulesController.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs` — add AssignmentRules DbSet
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs` — register AssignmentService

**Interfaces:**
- Consumes: `BaseEntity`, `ITenantEntity`, `TicketCreatedNotification`, `Ticket`, `TicketHistory`, `ApplicationUser`, `AppDbContext` from Phase 1 and Tasks 1–2
- Produces: `AssignmentRule` entity, `AssignmentRuleDto`, assignment CRUD API at `api/v1/assignment-rules`, `AutoAssignTicketHandler` + `AssignmentService` for auto-assignment on ticket creation

- [ ] **Step 1: Create AssignmentRule entity**

Create `src/CustomerSupport.Domain/Entities/AssignmentRule.cs`:

```csharp
namespace CustomerSupport.Domain.Entities;

public class AssignmentRule : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public Guid? PriorityId { get; set; }
    public string Strategy { get; set; } = string.Empty;
    public string? AgentPool { get; set; }
    public int LastAssignedIndex { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public TicketCategory? Category { get; set; }
    public TicketPriority? Priority { get; set; }
}
```

- [ ] **Step 2: Create configuration**

Create `src/CustomerSupport.Infrastructure/Persistence/Configurations/AssignmentRuleConfiguration.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class AssignmentRuleConfiguration : IEntityTypeConfiguration<AssignmentRule>
{
    public void Configure(EntityTypeBuilder<AssignmentRule> builder)
    {
        builder.ToTable("AssignmentRules");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Strategy).IsRequired().HasMaxLength(50);
        builder.Property(a => a.AgentPool).HasMaxLength(4000);

        builder.HasOne(a => a.Category).WithMany().HasForeignKey(a => a.CategoryId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(a => a.Priority).WithMany().HasForeignKey(a => a.PriorityId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => new { a.TenantId, a.Order });
    }
}
```

- [ ] **Step 3: Create DTO**

Create `src/CustomerSupport.Application/Assignment/DTOs/AssignmentRuleDto.cs`:

```csharp
namespace CustomerSupport.Application.Assignment.DTOs;

public record AssignmentRuleDto(
    Guid Id, string Name, string NameAr,
    Guid? CategoryId, string? CategoryName,
    Guid? PriorityId, string? PriorityName,
    string Strategy, string? AgentPool,
    int Order, bool IsActive);
```

- [ ] **Step 4: Create CRUD commands**

Create `src/CustomerSupport.Application/Assignment/Commands/CreateAssignmentRuleCommand.cs`:

```csharp
using CustomerSupport.Application.Assignment.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Assignment.Commands;

public record CreateAssignmentRuleCommand(
    string Name, string NameAr,
    Guid? CategoryId, Guid? PriorityId,
    string Strategy, string? AgentPool,
    int Order) : IRequest<AssignmentRuleDto>;

public class CreateAssignmentRuleCommandHandler : IRequestHandler<CreateAssignmentRuleCommand, AssignmentRuleDto>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateAssignmentRuleCommandHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AssignmentRuleDto> Handle(CreateAssignmentRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = new AssignmentRule
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            Name = request.Name,
            NameAr = request.NameAr,
            CategoryId = request.CategoryId,
            PriorityId = request.PriorityId,
            Strategy = request.Strategy,
            AgentPool = request.AgentPool,
            Order = request.Order
        };

        await _context.AssignmentRules.AddAsync(rule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var categoryName = request.CategoryId.HasValue
            ? await _context.TicketCategories.Where(c => c.Id == request.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var priorityName = request.PriorityId.HasValue
            ? await _context.TicketPriorities.Where(p => p.Id == request.PriorityId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        return new AssignmentRuleDto(rule.Id, rule.Name, rule.NameAr,
            rule.CategoryId, categoryName, rule.PriorityId, priorityName,
            rule.Strategy, rule.AgentPool, rule.Order, rule.IsActive);
    }
}
```

Create `src/CustomerSupport.Application/Assignment/Commands/UpdateAssignmentRuleCommand.cs`:

```csharp
using CustomerSupport.Application.Assignment.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Assignment.Commands;

public record UpdateAssignmentRuleCommand(
    Guid Id, string Name, string NameAr,
    Guid? CategoryId, Guid? PriorityId,
    string Strategy, string? AgentPool,
    int Order) : IRequest<AssignmentRuleDto>;

public class UpdateAssignmentRuleCommandHandler : IRequestHandler<UpdateAssignmentRuleCommand, AssignmentRuleDto>
{
    private readonly AppDbContext _context;

    public UpdateAssignmentRuleCommandHandler(AppDbContext context) => _context = context;

    public async Task<AssignmentRuleDto> Handle(UpdateAssignmentRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.AssignmentRules.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException("Assignment rule not found.");

        rule.Name = request.Name;
        rule.NameAr = request.NameAr;
        rule.CategoryId = request.CategoryId;
        rule.PriorityId = request.PriorityId;
        rule.Strategy = request.Strategy;
        rule.AgentPool = request.AgentPool;
        rule.Order = request.Order;

        await _context.SaveChangesAsync(cancellationToken);

        var categoryName = request.CategoryId.HasValue
            ? await _context.TicketCategories.Where(c => c.Id == request.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var priorityName = request.PriorityId.HasValue
            ? await _context.TicketPriorities.Where(p => p.Id == request.PriorityId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        return new AssignmentRuleDto(rule.Id, rule.Name, rule.NameAr,
            rule.CategoryId, categoryName, rule.PriorityId, priorityName,
            rule.Strategy, rule.AgentPool, rule.Order, rule.IsActive);
    }
}
```

Create `src/CustomerSupport.Application/Assignment/Commands/DeleteAssignmentRuleCommand.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Assignment.Commands;

public record DeleteAssignmentRuleCommand(Guid Id) : IRequest<Result>;

public class DeleteAssignmentRuleCommandHandler : IRequestHandler<DeleteAssignmentRuleCommand, Result>
{
    private readonly AppDbContext _context;

    public DeleteAssignmentRuleCommandHandler(AppDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteAssignmentRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.AssignmentRules.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException("Assignment rule not found.");

        rule.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

- [ ] **Step 5: Create query**

Create `src/CustomerSupport.Application/Assignment/Queries/GetAssignmentRulesQuery.cs`:

```csharp
using CustomerSupport.Application.Assignment.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Assignment.Queries;

public record GetAssignmentRulesQuery(bool? IsActive) : IRequest<List<AssignmentRuleDto>>;

public class GetAssignmentRulesQueryHandler : IRequestHandler<GetAssignmentRulesQuery, List<AssignmentRuleDto>>
{
    private readonly AppDbContext _context;

    public GetAssignmentRulesQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<AssignmentRuleDto>> Handle(GetAssignmentRulesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AssignmentRules
            .Include(a => a.Category)
            .Include(a => a.Priority)
            .AsNoTracking()
            .AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(a => a.IsActive == request.IsActive.Value);

        return await query.OrderBy(a => a.Order)
            .Select(a => new AssignmentRuleDto(
                a.Id, a.Name, a.NameAr,
                a.CategoryId, a.Category != null ? a.Category.Name : null,
                a.PriorityId, a.Priority != null ? a.Priority.Name : null,
                a.Strategy, a.AgentPool,
                a.Order, a.IsActive))
            .ToListAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Create validators**

Create `src/CustomerSupport.Application/Assignment/Validators/CreateAssignmentRuleValidator.cs`:

```csharp
using CustomerSupport.Application.Assignment.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Assignment.Validators;

public class CreateAssignmentRuleValidator : AbstractValidator<CreateAssignmentRuleCommand>
{
    public CreateAssignmentRuleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Strategy).NotEmpty().Must(s => s is "RoundRobin" or "LeastLoad")
            .WithMessage("Strategy must be 'RoundRobin' or 'LeastLoad'.");
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
```

Create `src/CustomerSupport.Application/Assignment/Validators/UpdateAssignmentRuleValidator.cs`:

```csharp
using CustomerSupport.Application.Assignment.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Assignment.Validators;

public class UpdateAssignmentRuleValidator : AbstractValidator<UpdateAssignmentRuleCommand>
{
    public UpdateAssignmentRuleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Strategy).NotEmpty().Must(s => s is "RoundRobin" or "LeastLoad")
            .WithMessage("Strategy must be 'RoundRobin' or 'LeastLoad'.");
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
```

- [ ] **Step 7: Create AssignmentService**

Create `src/CustomerSupport.Infrastructure/Services/AssignmentService.cs`:

```csharp
using System.Text.Json;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services;

public class AssignmentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AssignmentService> _logger;

    public AssignmentService(AppDbContext context, ILogger<AssignmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid?> FindAssigneeAsync(Guid tenantId, Guid? categoryId, Guid? priorityId, CancellationToken cancellationToken)
    {
        var rules = await _context.AssignmentRules
            .Where(r => r.TenantId == tenantId && r.IsActive)
            .Where(r => (r.CategoryId == null || r.CategoryId == categoryId)
                     && (r.PriorityId == null || r.PriorityId == priorityId))
            .OrderBy(r => r.Order)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            var agentIds = GetAgentPool(rule);
            if (agentIds.Count == 0)
            {
                agentIds = await _context.Users
                    .Where(u => u.TenantId == tenantId && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);
            }

            if (agentIds.Count == 0) continue;

            Guid? assigneeId = rule.Strategy switch
            {
                "RoundRobin" => RoundRobin(rule, agentIds),
                "LeastLoad" => await LeastLoadAsync(agentIds, cancellationToken),
                _ => null
            };

            if (assigneeId.HasValue)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Auto-assigned ticket to {UserId} via rule {RuleId} ({Strategy})",
                    assigneeId, rule.Id, rule.Strategy);
                return assigneeId;
            }
        }

        return null;
    }

    private static List<Guid> GetAgentPool(AssignmentRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.AgentPool)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(rule.AgentPool) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static Guid RoundRobin(AssignmentRule rule, List<Guid> agents)
    {
        var index = rule.LastAssignedIndex % agents.Count;
        rule.LastAssignedIndex = index + 1;
        return agents[index];
    }

    private async Task<Guid?> LeastLoadAsync(List<Guid> agents, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var workloads = await _context.Tickets
            .Where(t => t.AssignedToId.HasValue && agents.Contains(t.AssignedToId.Value))
            .Where(t => !finalStatusIds.Contains(t.StatusId))
            .GroupBy(t => t.AssignedToId!.Value)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var allAgentLoads = agents.Select(a => new
        {
            AgentId = a,
            Count = workloads.FirstOrDefault(w => w.AgentId == a)?.Count ?? 0
        });

        return allAgentLoads.OrderBy(x => x.Count).First().AgentId;
    }
}
```

- [ ] **Step 8: Create AutoAssignTicketHandler**

Create `src/CustomerSupport.Application/Assignment/Handlers/AutoAssignTicketHandler.cs`:

```csharp
using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Infrastructure.Persistence;
using CustomerSupport.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Assignment.Handlers;

public class AutoAssignTicketHandler : INotificationHandler<TicketCreatedNotification>
{
    private readonly AppDbContext _context;
    private readonly AssignmentService _assignmentService;

    public AutoAssignTicketHandler(AppDbContext context, AssignmentService assignmentService)
    {
        _context = context;
        _assignmentService = assignmentService;
    }

    public async Task Handle(TicketCreatedNotification notification, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets.FindAsync([notification.TicketId], cancellationToken);
        if (ticket == null || ticket.AssignedToId.HasValue) return;

        var assigneeId = await _assignmentService.FindAssigneeAsync(
            notification.TenantId, notification.CategoryId, notification.PriorityId, cancellationToken);

        if (!assigneeId.HasValue) return;

        ticket.AssignedToId = assigneeId.Value;
        _context.Set<TicketHistory>().Add(new TicketHistory
        {
            Id = Guid.NewGuid(),
            TenantId = notification.TenantId,
            TicketId = ticket.Id,
            Field = "AssignedToId",
            OldValue = null,
            NewValue = assigneeId.Value.ToString(),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 9: Create controller**

Create `src/CustomerSupport.API/Controllers/AssignmentRulesController.cs`:

```csharp
using CustomerSupport.Application.Assignment.Commands;
using CustomerSupport.Application.Assignment.DTOs;
using CustomerSupport.Application.Assignment.Queries;
using CustomerSupport.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/assignment-rules")]
public class AssignmentRulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AssignmentRulesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = "Permission:assignment.view")]
    public async Task<ActionResult<List<AssignmentRuleDto>>> GetAll([FromQuery] bool? isActive)
        => Ok(await _mediator.Send(new GetAssignmentRulesQuery(isActive)));

    [HttpPost]
    [Authorize(Policy = "Permission:assignment.manage")]
    public async Task<ActionResult<AssignmentRuleDto>> Create(CreateAssignmentRuleCommand command)
        => CreatedAtAction(null, await _mediator.Send(command));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:assignment.manage")]
    public async Task<ActionResult<AssignmentRuleDto>> Update(Guid id, UpdateAssignmentRuleCommand command)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:assignment.manage")]
    public async Task<ActionResult<Result>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteAssignmentRuleCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
```

- [ ] **Step 10: Update AppDbContext and DI**

Add to `AppDbContext.cs`:

```csharp
public DbSet<AssignmentRule> AssignmentRules => Set<AssignmentRule>();
```

Add to `DependencyInjection.cs`:

```csharp
services.AddScoped<AssignmentService>();
```

- [ ] **Step 11: Generate migration and verify build**

```powershell
cd src/CustomerSupport.Infrastructure
dotnet ef migrations add AddAssignmentRules --startup-project ../CustomerSupport.API
cd ../..
dotnet build src/CustomerSupport.sln
```

- [ ] **Step 12: Commit**

```bash
git add src/
git commit -m "feat(assignment): add auto-assignment rules with round-robin and least-load strategies"
```

---

### Task 5: Knowledge Base Backend (P2.7)

**Files:**
- Create: `src/CustomerSupport.Domain/Entities/KnowledgeCategory.cs`
- Create: `src/CustomerSupport.Domain/Entities/KnowledgeArticle.cs`
- Create: `src/CustomerSupport.Domain/Interfaces/IKnowledgeRepository.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/KnowledgeCategoryConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/KnowledgeArticleConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Repositories/KnowledgeRepository.cs`
- Create: `src/CustomerSupport.Application/Knowledge/DTOs/KnowledgeCategoryDto.cs`
- Create: `src/CustomerSupport.Application/Knowledge/DTOs/KnowledgeArticleDto.cs`
- Create: `src/CustomerSupport.Application/Knowledge/DTOs/KnowledgeArticleDetailDto.cs`
- Create: `src/CustomerSupport.Application/Knowledge/Commands/CreateKnowledgeCategoryCommand.cs`
- Create: `src/CustomerSupport.Application/Knowledge/Commands/UpdateKnowledgeCategoryCommand.cs`
- Create: `src/CustomerSupport.Application/Knowledge/Commands/DeleteKnowledgeCategoryCommand.cs`
- Create: `src/CustomerSupport.Application/Knowledge/Commands/CreateKnowledgeArticleCommand.cs`
- Create: `src/CustomerSupport.Application/Knowledge/Commands/UpdateKnowledgeArticleCommand.cs`
- Create: `src/CustomerSupport.Application/Knowledge/Commands/DeleteKnowledgeArticleCommand.cs`
- Create: `src/CustomerSupport.Application/Knowledge/Queries/GetKnowledgeCategoriesQuery.cs`
- Create: `src/CustomerSupport.Application/Knowledge/Queries/GetKnowledgeArticlesQuery.cs`
- Create: `src/CustomerSupport.Application/Knowledge/Queries/GetKnowledgeArticleByIdQuery.cs`
- Create: `src/CustomerSupport.Application/Knowledge/Queries/SearchKnowledgeArticlesQuery.cs`
- Create: `src/CustomerSupport.Application/Knowledge/Validators/CreateKnowledgeCategoryValidator.cs`
- Create: `src/CustomerSupport.Application/Knowledge/Validators/CreateKnowledgeArticleValidator.cs`
- Create: `src/CustomerSupport.Application/Knowledge/Validators/UpdateKnowledgeArticleValidator.cs`
- Create: `src/CustomerSupport.API/Controllers/KnowledgeController.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs` — add KnowledgeCategories, KnowledgeArticles DbSets
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs` — register IKnowledgeRepository

**Interfaces:**
- Consumes: `BaseEntity`, `ITenantEntity`, `ICurrentUserService`, `AppDbContext`, `PaginatedList<T>` from Phase 1
- Produces: `KnowledgeCategory`, `KnowledgeArticle` entities; `IKnowledgeRepository`; `KnowledgeCategoryDto`, `KnowledgeArticleDto`, `KnowledgeArticleDetailDto`; Knowledge CRUD API at `api/v1/knowledge`

- [ ] **Step 1: Create Knowledge domain entities**

Create `src/CustomerSupport.Domain/Entities/KnowledgeCategory.cs`:

```csharp
namespace CustomerSupport.Domain.Entities;

public class KnowledgeCategory : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public KnowledgeCategory? ParentCategory { get; set; }
    public ICollection<KnowledgeCategory> SubCategories { get; set; } = [];
    public ICollection<KnowledgeArticle> Articles { get; set; } = [];
}
```

Create `src/CustomerSupport.Domain/Entities/KnowledgeArticle.cs`:

```csharp
namespace CustomerSupport.Domain.Entities;

public class KnowledgeArticle : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ContentAr { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string? Tags { get; set; }
    public bool IsPublished { get; set; }
    public int ViewCount { get; set; }
    public bool IsActive { get; set; } = true;

    public KnowledgeCategory Category { get; set; } = null!;
}
```

- [ ] **Step 2: Create IKnowledgeRepository**

Create `src/CustomerSupport.Domain/Interfaces/IKnowledgeRepository.cs`:

```csharp
using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface IKnowledgeRepository
{
    IQueryable<KnowledgeArticle> GetArticlesQueryable();
    IQueryable<KnowledgeCategory> GetCategoriesQueryable();
    Task<KnowledgeArticle?> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KnowledgeArticle> AddArticleAsync(KnowledgeArticle article, CancellationToken cancellationToken = default);
    Task UpdateArticleAsync(KnowledgeArticle article, CancellationToken cancellationToken = default);
    Task<KnowledgeCategory> AddCategoryAsync(KnowledgeCategory category, CancellationToken cancellationToken = default);
    Task UpdateCategoryAsync(KnowledgeCategory category, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Create EF configurations**

Create `src/CustomerSupport.Infrastructure/Persistence/Configurations/KnowledgeCategoryConfiguration.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class KnowledgeCategoryConfiguration : IEntityTypeConfiguration<KnowledgeCategory>
{
    public void Configure(EntityTypeBuilder<KnowledgeCategory> builder)
    {
        builder.ToTable("KnowledgeCategories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.NameAr).IsRequired().HasMaxLength(200);

        builder.HasOne(c => c.ParentCategory).WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TenantId, c.ParentCategoryId });
    }
}
```

Create `src/CustomerSupport.Infrastructure/Persistence/Configurations/KnowledgeArticleConfiguration.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class KnowledgeArticleConfiguration : IEntityTypeConfiguration<KnowledgeArticle>
{
    public void Configure(EntityTypeBuilder<KnowledgeArticle> builder)
    {
        builder.ToTable("KnowledgeArticles");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).IsRequired().HasMaxLength(500);
        builder.Property(a => a.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Content).IsRequired();
        builder.Property(a => a.ContentAr).IsRequired();
        builder.Property(a => a.Tags).HasMaxLength(2000);

        builder.HasOne(a => a.Category).WithMany(c => c.Articles)
            .HasForeignKey(a => a.CategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TenantId, a.CategoryId });
        builder.HasIndex(a => new { a.TenantId, a.IsPublished });
    }
}
```

- [ ] **Step 4: Create KnowledgeRepository**

Create `src/CustomerSupport.Infrastructure/Repositories/KnowledgeRepository.cs`:

```csharp
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Repositories;

public class KnowledgeRepository : IKnowledgeRepository
{
    private readonly AppDbContext _context;

    public KnowledgeRepository(AppDbContext context) => _context = context;

    public IQueryable<KnowledgeArticle> GetArticlesQueryable() => _context.KnowledgeArticles.AsQueryable();
    public IQueryable<KnowledgeCategory> GetCategoriesQueryable() => _context.KnowledgeCategories.AsQueryable();

    public async Task<KnowledgeArticle?> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.KnowledgeArticles.Include(a => a.Category).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<KnowledgeArticle> AddArticleAsync(KnowledgeArticle article, CancellationToken cancellationToken = default)
    {
        await _context.KnowledgeArticles.AddAsync(article, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return article;
    }

    public async Task UpdateArticleAsync(KnowledgeArticle article, CancellationToken cancellationToken = default)
    {
        _context.KnowledgeArticles.Update(article);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<KnowledgeCategory> AddCategoryAsync(KnowledgeCategory category, CancellationToken cancellationToken = default)
    {
        await _context.KnowledgeCategories.AddAsync(category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task UpdateCategoryAsync(KnowledgeCategory category, CancellationToken cancellationToken = default)
    {
        _context.KnowledgeCategories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 5: Create Knowledge DTOs**

Create `src/CustomerSupport.Application/Knowledge/DTOs/KnowledgeCategoryDto.cs`:

```csharp
namespace CustomerSupport.Application.Knowledge.DTOs;

public record KnowledgeCategoryDto(Guid Id, string Name, string NameAr, Guid? ParentCategoryId, int Order, bool IsActive);
```

Create `src/CustomerSupport.Application/Knowledge/DTOs/KnowledgeArticleDto.cs`:

```csharp
namespace CustomerSupport.Application.Knowledge.DTOs;

public record KnowledgeArticleDto(
    Guid Id, string Title, string TitleAr,
    Guid CategoryId, string CategoryName,
    string? Tags, bool IsPublished, int ViewCount, DateTime CreatedAt);
```

Create `src/CustomerSupport.Application/Knowledge/DTOs/KnowledgeArticleDetailDto.cs`:

```csharp
namespace CustomerSupport.Application.Knowledge.DTOs;

public record KnowledgeArticleDetailDto(
    Guid Id, string Title, string TitleAr,
    string Content, string ContentAr,
    Guid CategoryId, string CategoryName,
    string? Tags, bool IsPublished, int ViewCount,
    DateTime CreatedAt, DateTime UpdatedAt);
```

- [ ] **Step 6: Create Knowledge category commands**

Create `src/CustomerSupport.Application/Knowledge/Commands/CreateKnowledgeCategoryCommand.cs`:

```csharp
using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Knowledge.Commands;

public record CreateKnowledgeCategoryCommand(
    string Name, string NameAr,
    Guid? ParentCategoryId, int Order) : IRequest<KnowledgeCategoryDto>;

public class CreateKnowledgeCategoryCommandHandler : IRequestHandler<CreateKnowledgeCategoryCommand, KnowledgeCategoryDto>
{
    private readonly IKnowledgeRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreateKnowledgeCategoryCommandHandler(IKnowledgeRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<KnowledgeCategoryDto> Handle(CreateKnowledgeCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new KnowledgeCategory
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            Name = request.Name,
            NameAr = request.NameAr,
            ParentCategoryId = request.ParentCategoryId,
            Order = request.Order
        };

        await _repository.AddCategoryAsync(category, cancellationToken);
        return new KnowledgeCategoryDto(category.Id, category.Name, category.NameAr, category.ParentCategoryId, category.Order, category.IsActive);
    }
}
```

Create `src/CustomerSupport.Application/Knowledge/Commands/UpdateKnowledgeCategoryCommand.cs`:

```csharp
using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Knowledge.Commands;

public record UpdateKnowledgeCategoryCommand(Guid Id, string Name, string NameAr, Guid? ParentCategoryId, int Order) : IRequest<KnowledgeCategoryDto>;

public class UpdateKnowledgeCategoryCommandHandler : IRequestHandler<UpdateKnowledgeCategoryCommand, KnowledgeCategoryDto>
{
    private readonly AppDbContext _context;

    public UpdateKnowledgeCategoryCommandHandler(AppDbContext context) => _context = context;

    public async Task<KnowledgeCategoryDto> Handle(UpdateKnowledgeCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.KnowledgeCategories.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException("Knowledge category not found.");

        category.Name = request.Name;
        category.NameAr = request.NameAr;
        category.ParentCategoryId = request.ParentCategoryId;
        category.Order = request.Order;

        await _context.SaveChangesAsync(cancellationToken);
        return new KnowledgeCategoryDto(category.Id, category.Name, category.NameAr, category.ParentCategoryId, category.Order, category.IsActive);
    }
}
```

Create `src/CustomerSupport.Application/Knowledge/Commands/DeleteKnowledgeCategoryCommand.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Knowledge.Commands;

public record DeleteKnowledgeCategoryCommand(Guid Id) : IRequest<Result>;

public class DeleteKnowledgeCategoryCommandHandler : IRequestHandler<DeleteKnowledgeCategoryCommand, Result>
{
    private readonly AppDbContext _context;

    public DeleteKnowledgeCategoryCommandHandler(AppDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteKnowledgeCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.KnowledgeCategories.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException("Knowledge category not found.");

        category.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

- [ ] **Step 7: Create Knowledge article commands**

Create `src/CustomerSupport.Application/Knowledge/Commands/CreateKnowledgeArticleCommand.cs`:

```csharp
using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Knowledge.Commands;

public record CreateKnowledgeArticleCommand(
    string Title, string TitleAr,
    string Content, string ContentAr,
    Guid CategoryId, string? Tags,
    bool IsPublished) : IRequest<KnowledgeArticleDto>;

public class CreateKnowledgeArticleCommandHandler : IRequestHandler<CreateKnowledgeArticleCommand, KnowledgeArticleDto>
{
    private readonly IKnowledgeRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly AppDbContext _context;

    public CreateKnowledgeArticleCommandHandler(IKnowledgeRepository repository, ICurrentUserService currentUserService, AppDbContext context)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async Task<KnowledgeArticleDto> Handle(CreateKnowledgeArticleCommand request, CancellationToken cancellationToken)
    {
        var article = new KnowledgeArticle
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            Title = request.Title,
            TitleAr = request.TitleAr,
            Content = request.Content,
            ContentAr = request.ContentAr,
            CategoryId = request.CategoryId,
            Tags = request.Tags,
            IsPublished = request.IsPublished
        };

        await _repository.AddArticleAsync(article, cancellationToken);

        var categoryName = await _context.KnowledgeCategories
            .Where(c => c.Id == request.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken) ?? "";

        return new KnowledgeArticleDto(article.Id, article.Title, article.TitleAr,
            article.CategoryId, categoryName, article.Tags, article.IsPublished, article.ViewCount, article.CreatedAt);
    }
}
```

Create `src/CustomerSupport.Application/Knowledge/Commands/UpdateKnowledgeArticleCommand.cs`:

```csharp
using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Knowledge.Commands;

public record UpdateKnowledgeArticleCommand(
    Guid Id, string Title, string TitleAr,
    string Content, string ContentAr,
    Guid CategoryId, string? Tags,
    bool IsPublished) : IRequest<KnowledgeArticleDetailDto>;

public class UpdateKnowledgeArticleCommandHandler : IRequestHandler<UpdateKnowledgeArticleCommand, KnowledgeArticleDetailDto>
{
    private readonly IKnowledgeRepository _repository;

    public UpdateKnowledgeArticleCommandHandler(IKnowledgeRepository repository) => _repository = repository;

    public async Task<KnowledgeArticleDetailDto> Handle(UpdateKnowledgeArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _repository.GetArticleByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Knowledge article not found.");

        article.Title = request.Title;
        article.TitleAr = request.TitleAr;
        article.Content = request.Content;
        article.ContentAr = request.ContentAr;
        article.CategoryId = request.CategoryId;
        article.Tags = request.Tags;
        article.IsPublished = request.IsPublished;

        await _repository.UpdateArticleAsync(article, cancellationToken);

        return new KnowledgeArticleDetailDto(article.Id, article.Title, article.TitleAr,
            article.Content, article.ContentAr, article.CategoryId, article.Category?.Name ?? "",
            article.Tags, article.IsPublished, article.ViewCount, article.CreatedAt, article.UpdatedAt);
    }
}
```

Create `src/CustomerSupport.Application/Knowledge/Commands/DeleteKnowledgeArticleCommand.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Knowledge.Commands;

public record DeleteKnowledgeArticleCommand(Guid Id) : IRequest<Result>;

public class DeleteKnowledgeArticleCommandHandler : IRequestHandler<DeleteKnowledgeArticleCommand, Result>
{
    private readonly IKnowledgeRepository _repository;

    public DeleteKnowledgeArticleCommandHandler(IKnowledgeRepository repository) => _repository = repository;

    public async Task<Result> Handle(DeleteKnowledgeArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _repository.GetArticleByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Knowledge article not found.");

        article.IsActive = false;
        await _repository.UpdateArticleAsync(article, cancellationToken);
        return Result.Success();
    }
}
```

- [ ] **Step 8: Create Knowledge queries**

Create `src/CustomerSupport.Application/Knowledge/Queries/GetKnowledgeCategoriesQuery.cs`:

```csharp
using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Knowledge.Queries;

public record GetKnowledgeCategoriesQuery(bool? IsActive) : IRequest<List<KnowledgeCategoryDto>>;

public class GetKnowledgeCategoriesQueryHandler : IRequestHandler<GetKnowledgeCategoriesQuery, List<KnowledgeCategoryDto>>
{
    private readonly IKnowledgeRepository _repository;

    public GetKnowledgeCategoriesQueryHandler(IKnowledgeRepository repository) => _repository = repository;

    public async Task<List<KnowledgeCategoryDto>> Handle(GetKnowledgeCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.GetCategoriesQueryable().AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        return await query.OrderBy(c => c.Order)
            .Select(c => new KnowledgeCategoryDto(c.Id, c.Name, c.NameAr, c.ParentCategoryId, c.Order, c.IsActive))
            .ToListAsync(cancellationToken);
    }
}
```

Create `src/CustomerSupport.Application/Knowledge/Queries/GetKnowledgeArticlesQuery.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Knowledge.Queries;

public record GetKnowledgeArticlesQuery(
    Guid? CategoryId, bool? IsPublished,
    int Page = 1, int PageSize = 20) : IRequest<PaginatedList<KnowledgeArticleDto>>;

public class GetKnowledgeArticlesQueryHandler : IRequestHandler<GetKnowledgeArticlesQuery, PaginatedList<KnowledgeArticleDto>>
{
    private readonly IKnowledgeRepository _repository;

    public GetKnowledgeArticlesQueryHandler(IKnowledgeRepository repository) => _repository = repository;

    public async Task<PaginatedList<KnowledgeArticleDto>> Handle(GetKnowledgeArticlesQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.GetArticlesQueryable()
            .Include(a => a.Category)
            .Where(a => a.IsActive)
            .AsNoTracking();

        if (request.CategoryId.HasValue)
            query = query.Where(a => a.CategoryId == request.CategoryId.Value);

        if (request.IsPublished.HasValue)
            query = query.Where(a => a.IsPublished == request.IsPublished.Value);

        query = query.OrderByDescending(a => a.CreatedAt);

        var projected = query.Select(a => new KnowledgeArticleDto(
            a.Id, a.Title, a.TitleAr,
            a.CategoryId, a.Category.Name,
            a.Tags, a.IsPublished, a.ViewCount, a.CreatedAt));

        return await PaginatedList<KnowledgeArticleDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
```

Create `src/CustomerSupport.Application/Knowledge/Queries/GetKnowledgeArticleByIdQuery.cs`:

```csharp
using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Knowledge.Queries;

public record GetKnowledgeArticleByIdQuery(Guid Id) : IRequest<KnowledgeArticleDetailDto>;

public class GetKnowledgeArticleByIdQueryHandler : IRequestHandler<GetKnowledgeArticleByIdQuery, KnowledgeArticleDetailDto>
{
    private readonly IKnowledgeRepository _repository;
    private readonly AppDbContext _context;

    public GetKnowledgeArticleByIdQueryHandler(IKnowledgeRepository repository, AppDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<KnowledgeArticleDetailDto> Handle(GetKnowledgeArticleByIdQuery request, CancellationToken cancellationToken)
    {
        var article = await _repository.GetArticleByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Knowledge article not found.");

        article.ViewCount++;
        await _context.SaveChangesAsync(cancellationToken);

        return new KnowledgeArticleDetailDto(article.Id, article.Title, article.TitleAr,
            article.Content, article.ContentAr, article.CategoryId, article.Category?.Name ?? "",
            article.Tags, article.IsPublished, article.ViewCount, article.CreatedAt, article.UpdatedAt);
    }
}
```

Create `src/CustomerSupport.Application/Knowledge/Queries/SearchKnowledgeArticlesQuery.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Knowledge.Queries;

public record SearchKnowledgeArticlesQuery(string Query, int Page = 1, int PageSize = 20)
    : IRequest<PaginatedList<KnowledgeArticleDto>>;

public class SearchKnowledgeArticlesQueryHandler : IRequestHandler<SearchKnowledgeArticlesQuery, PaginatedList<KnowledgeArticleDto>>
{
    private readonly IKnowledgeRepository _repository;

    public SearchKnowledgeArticlesQueryHandler(IKnowledgeRepository repository) => _repository = repository;

    public async Task<PaginatedList<KnowledgeArticleDto>> Handle(SearchKnowledgeArticlesQuery request, CancellationToken cancellationToken)
    {
        var search = request.Query.ToLower();

        var query = _repository.GetArticlesQueryable()
            .Include(a => a.Category)
            .Where(a => a.IsActive && a.IsPublished)
            .Where(a => a.Title.ToLower().Contains(search)
                     || a.TitleAr.Contains(search)
                     || a.Content.ToLower().Contains(search)
                     || a.ContentAr.Contains(search)
                     || (a.Tags != null && a.Tags.ToLower().Contains(search)))
            .AsNoTracking()
            .OrderByDescending(a => a.ViewCount);

        var projected = query.Select(a => new KnowledgeArticleDto(
            a.Id, a.Title, a.TitleAr,
            a.CategoryId, a.Category.Name,
            a.Tags, a.IsPublished, a.ViewCount, a.CreatedAt));

        return await PaginatedList<KnowledgeArticleDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
```

- [ ] **Step 9: Create validators**

Create `src/CustomerSupport.Application/Knowledge/Validators/CreateKnowledgeCategoryValidator.cs`:

```csharp
using CustomerSupport.Application.Knowledge.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Knowledge.Validators;

public class CreateKnowledgeCategoryValidator : AbstractValidator<CreateKnowledgeCategoryCommand>
{
    public CreateKnowledgeCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
```

Create `src/CustomerSupport.Application/Knowledge/Validators/CreateKnowledgeArticleValidator.cs`:

```csharp
using CustomerSupport.Application.Knowledge.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Knowledge.Validators;

public class CreateKnowledgeArticleValidator : AbstractValidator<CreateKnowledgeArticleCommand>
{
    public CreateKnowledgeArticleValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.ContentAr).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Tags).MaximumLength(2000);
    }
}
```

Create `src/CustomerSupport.Application/Knowledge/Validators/UpdateKnowledgeArticleValidator.cs`:

```csharp
using CustomerSupport.Application.Knowledge.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Knowledge.Validators;

public class UpdateKnowledgeArticleValidator : AbstractValidator<UpdateKnowledgeArticleCommand>
{
    public UpdateKnowledgeArticleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.ContentAr).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Tags).MaximumLength(2000);
    }
}
```

- [ ] **Step 10: Create KnowledgeController**

Create `src/CustomerSupport.API/Controllers/KnowledgeController.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Knowledge.Commands;
using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Application.Knowledge.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class KnowledgeController : ControllerBase
{
    private readonly IMediator _mediator;

    public KnowledgeController(IMediator mediator) => _mediator = mediator;

    [HttpGet("categories")]
    [Authorize(Policy = "Permission:knowledgebase.view")]
    public async Task<ActionResult<List<KnowledgeCategoryDto>>> GetCategories([FromQuery] bool? isActive)
        => Ok(await _mediator.Send(new GetKnowledgeCategoriesQuery(isActive)));

    [HttpPost("categories")]
    [Authorize(Policy = "Permission:knowledgebase.manage")]
    public async Task<ActionResult<KnowledgeCategoryDto>> CreateCategory(CreateKnowledgeCategoryCommand command)
        => CreatedAtAction(null, await _mediator.Send(command));

    [HttpPut("categories/{id:guid}")]
    [Authorize(Policy = "Permission:knowledgebase.manage")]
    public async Task<ActionResult<KnowledgeCategoryDto>> UpdateCategory(Guid id, UpdateKnowledgeCategoryCommand command)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("categories/{id:guid}")]
    [Authorize(Policy = "Permission:knowledgebase.manage")]
    public async Task<ActionResult<Result>> DeleteCategory(Guid id)
    {
        var result = await _mediator.Send(new DeleteKnowledgeCategoryCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("articles")]
    [Authorize(Policy = "Permission:knowledgebase.view")]
    public async Task<ActionResult<PaginatedList<KnowledgeArticleDto>>> GetArticles(
        [FromQuery] Guid? categoryId, [FromQuery] bool? isPublished,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new GetKnowledgeArticlesQuery(categoryId, isPublished, page, pageSize)));

    [HttpGet("articles/{id:guid}")]
    [Authorize(Policy = "Permission:knowledgebase.view")]
    public async Task<ActionResult<KnowledgeArticleDetailDto>> GetArticleById(Guid id)
        => Ok(await _mediator.Send(new GetKnowledgeArticleByIdQuery(id)));

    [HttpGet("articles/search")]
    [Authorize(Policy = "Permission:knowledgebase.view")]
    public async Task<ActionResult<PaginatedList<KnowledgeArticleDto>>> SearchArticles(
        [FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new SearchKnowledgeArticlesQuery(query, page, pageSize)));

    [HttpPost("articles")]
    [Authorize(Policy = "Permission:knowledgebase.manage")]
    public async Task<ActionResult<KnowledgeArticleDto>> CreateArticle(CreateKnowledgeArticleCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetArticleById), new { id = result.Id }, result);
    }

    [HttpPut("articles/{id:guid}")]
    [Authorize(Policy = "Permission:knowledgebase.manage")]
    public async Task<ActionResult<KnowledgeArticleDetailDto>> UpdateArticle(Guid id, UpdateKnowledgeArticleCommand command)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("articles/{id:guid}")]
    [Authorize(Policy = "Permission:knowledgebase.manage")]
    public async Task<ActionResult<Result>> DeleteArticle(Guid id)
    {
        var result = await _mediator.Send(new DeleteKnowledgeArticleCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
```

- [ ] **Step 11: Update AppDbContext and DI**

Add to `AppDbContext.cs`:

```csharp
public DbSet<KnowledgeCategory> KnowledgeCategories => Set<KnowledgeCategory>();
public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
```

Add to `DependencyInjection.cs`:

```csharp
services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
```

- [ ] **Step 12: Generate migration and verify build**

```powershell
cd src/CustomerSupport.Infrastructure
dotnet ef migrations add AddKnowledgeBase --startup-project ../CustomerSupport.API
cd ../..
dotnet build src/CustomerSupport.sln
```

- [ ] **Step 13: Commit**

```bash
git add src/
git commit -m "feat(knowledge): add knowledge base categories and articles with CRUD and search"
```

---

### Task 6: Knowledge Base UI (P2.8)

**Files:**
- Create: `src/client/src/app/features/knowledge/knowledge.service.ts`
- Create: `src/client/src/app/features/knowledge/knowledge-list/knowledge-list.ts`
- Create: `src/client/src/app/features/knowledge/knowledge-article/knowledge-article.ts`
- Create: `src/client/src/app/features/knowledge/knowledge-editor/knowledge-editor.ts`
- Create: `src/client/src/app/features/knowledge/knowledge-categories/knowledge-categories.ts`
- Create: `src/client/src/app/features/knowledge/knowledge.routes.ts`
- Modify: `src/client/src/app/app.routes.ts` — add knowledge route
- Modify: `src/client/src/assets/i18n/en.json` — add knowledge keys
- Modify: `src/client/src/assets/i18n/ar.json` — add knowledge keys

**Interfaces:**
- Consumes: `ApiService`, `PaginatedList<T>`, `AuthService`, `ConfirmDialogComponent` from Phase 1
- Produces: Knowledge article list with search/filter, article viewer, article editor (admin), category management, all wired to `api/v1/knowledge`

- [ ] **Steps 1–8: (Full implementation for KnowledgeService, KnowledgeListComponent, KnowledgeArticleComponent, KnowledgeEditorComponent, KnowledgeCategoriesComponent, routes, i18n)**

Each component follows the established Phase 1 pattern:
- Standalone component with Material imports
- Inject service, call API
- Reactive forms for create/edit
- Material table with pagination for lists
- Confirm dialog for destructive actions
- All text via translate pipe

**KnowledgeService** extends ApiService with methods:
- `getCategories(isActive?)` → `GET /v1/knowledge/categories`
- `createCategory(request)` → `POST /v1/knowledge/categories`
- `updateCategory(id, request)` → `PUT /v1/knowledge/categories/{id}`
- `deleteCategory(id)` → `DELETE /v1/knowledge/categories/{id}`
- `getArticles(params)` → `GET /v1/knowledge/articles`
- `getArticleById(id)` → `GET /v1/knowledge/articles/{id}`
- `searchArticles(query, page, pageSize)` → `GET /v1/knowledge/articles/search`
- `createArticle(request)` → `POST /v1/knowledge/articles`
- `updateArticle(id, request)` → `PUT /v1/knowledge/articles/{id}`
- `deleteArticle(id)` → `DELETE /v1/knowledge/articles/{id}`

**Interfaces:**
```typescript
export interface KnowledgeCategoryDto {
  id: string; name: string; nameAr: string;
  parentCategoryId: string | null; order: number; isActive: boolean;
}

export interface KnowledgeArticleDto {
  id: string; title: string; titleAr: string;
  categoryId: string; categoryName: string;
  tags: string | null; isPublished: boolean;
  viewCount: number; createdAt: string;
}

export interface KnowledgeArticleDetailDto extends KnowledgeArticleDto {
  content: string; contentAr: string; updatedAt: string;
}
```

**KnowledgeListComponent**: Material table with columns (title, category, published, viewCount, createdAt, actions). Search bar with debounce. Category filter dropdown. Pagination. Create button routes to `/admin/knowledge/new`.

**KnowledgeArticleComponent**: Displays article detail — title, content, category, tags as chips, view count, dates. Edit button for admins.

**KnowledgeEditorComponent**: Reactive form with fields: title, titleAr, content (textarea), contentAr (textarea), categoryId (select), tags (input), isPublished (checkbox). Edit mode loads existing article.

**KnowledgeCategoriesComponent**: Simple list with inline add/edit. Name, NameAr, Order fields. Parent category select. Delete with confirm.

**Routes:**
```typescript
export const knowledgeRoutes: Routes = [
  { path: '', loadComponent: () => import('./knowledge-list/knowledge-list').then(m => m.KnowledgeListComponent) },
  { path: 'categories', loadComponent: () => import('./knowledge-categories/knowledge-categories').then(m => m.KnowledgeCategoriesComponent) },
  { path: 'new', loadComponent: () => import('./knowledge-editor/knowledge-editor').then(m => m.KnowledgeEditorComponent) },
  { path: ':id', loadComponent: () => import('./knowledge-article/knowledge-article').then(m => m.KnowledgeArticleComponent) },
  { path: ':id/edit', loadComponent: () => import('./knowledge-editor/knowledge-editor').then(m => m.KnowledgeEditorComponent) },
];
```

**i18n keys to add (en.json):**
```json
"knowledge": {
  "title": "Knowledge Base",
  "articles": "Articles",
  "categories": "Categories",
  "createArticle": "New Article",
  "editArticle": "Edit Article",
  "articleDetail": "Article Detail",
  "articleTitle": "Title",
  "articleTitleAr": "Title (Arabic)",
  "content": "Content",
  "contentAr": "Content (Arabic)",
  "category": "Category",
  "tags": "Tags",
  "published": "Published",
  "draft": "Draft",
  "viewCount": "Views",
  "search": "Search articles...",
  "noArticles": "No articles found",
  "deleteConfirm": "Are you sure you want to delete this article?",
  "categoryName": "Category Name",
  "categoryNameAr": "Category Name (Arabic)",
  "parentCategory": "Parent Category",
  "order": "Order",
  "manageCategories": "Manage Categories",
  "addCategory": "Add Category",
  "deleteCategoryConfirm": "Are you sure you want to delete this category?"
}
```

**i18n keys to add (ar.json):**
```json
"knowledge": {
  "title": "قاعدة المعرفة",
  "articles": "المقالات",
  "categories": "التصنيفات",
  "createArticle": "مقال جديد",
  "editArticle": "تعديل المقال",
  "articleDetail": "تفاصيل المقال",
  "articleTitle": "العنوان",
  "articleTitleAr": "العنوان (عربي)",
  "content": "المحتوى",
  "contentAr": "المحتوى (عربي)",
  "category": "التصنيف",
  "tags": "الوسوم",
  "published": "منشور",
  "draft": "مسودة",
  "viewCount": "المشاهدات",
  "search": "البحث في المقالات...",
  "noArticles": "لا توجد مقالات",
  "deleteConfirm": "هل أنت متأكد من حذف هذا المقال؟",
  "categoryName": "اسم التصنيف",
  "categoryNameAr": "اسم التصنيف (عربي)",
  "parentCategory": "التصنيف الأب",
  "order": "الترتيب",
  "manageCategories": "إدارة التصنيفات",
  "addCategory": "إضافة تصنيف",
  "deleteCategoryConfirm": "هل أنت متأكد من حذف هذا التصنيف؟"
}
```

- [ ] **Step 9: Verify Angular compiles, commit**

```powershell
cd src/client
ng build
```

```bash
git add src/client/
git commit -m "feat(knowledge-ui): add knowledge base article list, viewer, editor, and category management"
```

---

### Task 7: SLA & Automation Admin UI (P2.1/P2.3/P2.4 UI)

**Files:**
- Create: `src/client/src/app/features/sla/sla.service.ts`
- Create: `src/client/src/app/features/sla/sla-policy-list/sla-policy-list.ts`
- Create: `src/client/src/app/features/sla/sla-policy-form/sla-policy-form.ts`
- Create: `src/client/src/app/features/sla/sla.routes.ts`
- Create: `src/client/src/app/features/escalation/escalation.service.ts`
- Create: `src/client/src/app/features/escalation/escalation-rule-list/escalation-rule-list.ts`
- Create: `src/client/src/app/features/escalation/escalation-rule-form/escalation-rule-form.ts`
- Create: `src/client/src/app/features/escalation/escalation.routes.ts`
- Create: `src/client/src/app/features/assignment/assignment.service.ts`
- Create: `src/client/src/app/features/assignment/assignment-rule-list/assignment-rule-list.ts`
- Create: `src/client/src/app/features/assignment/assignment-rule-form/assignment-rule-form.ts`
- Create: `src/client/src/app/features/assignment/assignment.routes.ts`
- Modify: `src/client/src/app/app.routes.ts` — add sla, escalation, assignment routes
- Modify: `src/client/src/assets/i18n/en.json` — add sla, escalation, assignment keys
- Modify: `src/client/src/assets/i18n/ar.json` — add sla, escalation, assignment keys

**Interfaces:**
- Consumes: `ApiService`, `TicketsService` (for priority/category dropdowns), `UsersService` (for agent dropdowns), `ConfirmDialogComponent` from Phase 1 + Task 6
- Produces: SLA policy list/form, escalation rule list/form, assignment rule list/form, all wired to respective APIs

- [ ] **Steps 1–10: (Full implementation for SlaService, EscalationService, AssignmentService, all list and form components, routes, i18n)**

Each service extends ApiService. Each component follows Phase 1 standalone patterns with Material, reactive forms, translate pipe.

**SlaService**: `getSlaPolicies(params)`, `getSlaPolicyById(id)`, `createSlaPolicy(req)`, `updateSlaPolicy(id, req)`, `deleteSlaPolicy(id)`

**EscalationService**: `getEscalationRules(isActive?)`, `createEscalationRule(req)`, `updateEscalationRule(id, req)`, `deleteEscalationRule(id)`

**AssignmentService**: `getAssignmentRules(isActive?)`, `createAssignmentRule(req)`, `updateAssignmentRule(id, req)`, `deleteAssignmentRule(id)`

**SLA Policy Form fields**: name, nameAr, priorityId (select from TicketsService.getPriorities), categoryId (select from TicketsService.getCategories), firstResponseMinutes, resolutionMinutes

**Escalation Rule Form fields**: name, nameAr, priorityId, categoryId, triggerType (select: FirstResponseBreached/ResolutionBreached), triggerAfterMinutes, actionType (select: Reassign/ChangePriority), actionTarget (user select or priority select depending on actionType), order

**Assignment Rule Form fields**: name, nameAr, categoryId, priorityId, strategy (select: RoundRobin/LeastLoad), agentPool (multi-select of users), order

**i18n keys (en.json):**
```json
"sla": {
  "title": "SLA Policies",
  "createPolicy": "New SLA Policy",
  "editPolicy": "Edit SLA Policy",
  "name": "Policy Name",
  "nameAr": "Policy Name (Arabic)",
  "priority": "Priority",
  "category": "Category",
  "allPriorities": "All Priorities",
  "allCategories": "All Categories",
  "firstResponseMinutes": "First Response (minutes)",
  "resolutionMinutes": "Resolution (minutes)",
  "active": "Active",
  "inactive": "Inactive",
  "deleteConfirm": "Are you sure you want to deactivate this SLA policy?"
},
"escalation": {
  "title": "Escalation Rules",
  "createRule": "New Escalation Rule",
  "editRule": "Edit Escalation Rule",
  "name": "Rule Name",
  "nameAr": "Rule Name (Arabic)",
  "triggerType": "Trigger",
  "firstResponseBreached": "First Response Breached",
  "resolutionBreached": "Resolution Breached",
  "triggerAfterMinutes": "Trigger After (minutes)",
  "actionType": "Action",
  "reassign": "Reassign Ticket",
  "changePriority": "Change Priority",
  "actionTarget": "Target",
  "order": "Order",
  "deleteConfirm": "Are you sure you want to deactivate this escalation rule?"
},
"assignment": {
  "title": "Assignment Rules",
  "createRule": "New Assignment Rule",
  "editRule": "Edit Assignment Rule",
  "name": "Rule Name",
  "nameAr": "Rule Name (Arabic)",
  "strategy": "Strategy",
  "roundRobin": "Round Robin",
  "leastLoad": "Least Load",
  "agentPool": "Agent Pool",
  "allAgents": "All Agents",
  "order": "Order",
  "deleteConfirm": "Are you sure you want to deactivate this assignment rule?"
}
```

**i18n keys (ar.json):**
```json
"sla": {
  "title": "سياسات اتفاقية مستوى الخدمة",
  "createPolicy": "سياسة جديدة",
  "editPolicy": "تعديل السياسة",
  "name": "اسم السياسة",
  "nameAr": "اسم السياسة (عربي)",
  "priority": "الأولوية",
  "category": "التصنيف",
  "allPriorities": "جميع الأولويات",
  "allCategories": "جميع التصنيفات",
  "firstResponseMinutes": "الاستجابة الأولى (دقائق)",
  "resolutionMinutes": "الحل (دقائق)",
  "active": "نشط",
  "inactive": "غير نشط",
  "deleteConfirm": "هل أنت متأكد من إلغاء تنشيط هذه السياسة؟"
},
"escalation": {
  "title": "قواعد التصعيد",
  "createRule": "قاعدة تصعيد جديدة",
  "editRule": "تعديل قاعدة التصعيد",
  "name": "اسم القاعدة",
  "nameAr": "اسم القاعدة (عربي)",
  "triggerType": "المحفز",
  "firstResponseBreached": "تجاوز الاستجابة الأولى",
  "resolutionBreached": "تجاوز الحل",
  "triggerAfterMinutes": "التفعيل بعد (دقائق)",
  "actionType": "الإجراء",
  "reassign": "إعادة تعيين",
  "changePriority": "تغيير الأولوية",
  "actionTarget": "الهدف",
  "order": "الترتيب",
  "deleteConfirm": "هل أنت متأكد من إلغاء تنشيط قاعدة التصعيد؟"
},
"assignment": {
  "title": "قواعد التعيين",
  "createRule": "قاعدة تعيين جديدة",
  "editRule": "تعديل قاعدة التعيين",
  "name": "اسم القاعدة",
  "nameAr": "اسم القاعدة (عربي)",
  "strategy": "الاستراتيجية",
  "roundRobin": "التوزيع الدوري",
  "leastLoad": "الأقل حملاً",
  "agentPool": "مجموعة الوكلاء",
  "allAgents": "جميع الوكلاء",
  "order": "الترتيب",
  "deleteConfirm": "هل أنت متأكد من إلغاء تنشيط قاعدة التعيين؟"
}
```

- [ ] **Step 11: Verify Angular compiles, commit**

```powershell
cd src/client
ng build
```

```bash
git add src/client/
git commit -m "feat(admin-ui): add SLA policy, escalation rule, and assignment rule management pages"
```

---

### Task 8: Agent Dashboard API (P2.5)

**Files:**
- Create: `src/CustomerSupport.Application/Dashboard/DTOs/DashboardStatsDto.cs`
- Create: `src/CustomerSupport.Application/Dashboard/DTOs/SlaSummaryDto.cs`
- Create: `src/CustomerSupport.Application/Dashboard/DTOs/AgentWorkloadDto.cs`
- Create: `src/CustomerSupport.Application/Dashboard/Queries/GetDashboardStatsQuery.cs`
- Create: `src/CustomerSupport.Application/Dashboard/Queries/GetSlaSummaryQuery.cs`
- Create: `src/CustomerSupport.Application/Dashboard/Queries/GetMyTicketsQuery.cs`
- Create: `src/CustomerSupport.Application/Dashboard/Queries/GetTeamWorkloadQuery.cs`
- Create: `src/CustomerSupport.API/Controllers/DashboardController.cs`

**Interfaces:**
- Consumes: `AppDbContext`, `ICurrentUserService`, `Ticket`, `TicketSla`, `TicketStatus`, `ApplicationUser`, `PaginatedList<T>` from Phase 1 and Tasks 1–4
- Produces: Dashboard API endpoints at `api/v1/dashboard`: stats, SLA summary, my-tickets, team-workload

- [ ] **Step 1: Create Dashboard DTOs**

Create `src/CustomerSupport.Application/Dashboard/DTOs/DashboardStatsDto.cs`:

```csharp
namespace CustomerSupport.Application.Dashboard.DTOs;

public record DashboardStatsDto(
    int OpenTickets, int OverdueTickets, int ResolvedToday,
    int UnassignedTickets, int MyOpenTickets, int MyOverdueTickets);
```

Create `src/CustomerSupport.Application/Dashboard/DTOs/SlaSummaryDto.cs`:

```csharp
namespace CustomerSupport.Application.Dashboard.DTOs;

public record SlaSummaryDto(
    int TotalTracked, int FirstResponseOnTime, int FirstResponseBreached,
    int ResolutionOnTime, int ResolutionBreached,
    double FirstResponseCompliancePercent, double ResolutionCompliancePercent);
```

Create `src/CustomerSupport.Application/Dashboard/DTOs/AgentWorkloadDto.cs`:

```csharp
namespace CustomerSupport.Application.Dashboard.DTOs;

public record AgentWorkloadDto(Guid AgentId, string AgentName, int OpenTickets, int OverdueTickets);
```

- [ ] **Step 2: Create GetDashboardStatsQuery**

Create `src/CustomerSupport.Application/Dashboard/Queries/GetDashboardStatsQuery.cs`:

```csharp
using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public GetDashboardStatsQueryHandler(AppDbContext context, ICurrentUserService currentUserService, IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        var today = _dateTimeService.UtcNow.Date;
        var userId = _currentUserService.UserId;

        var openTickets = await _context.Tickets
            .CountAsync(t => !finalStatusIds.Contains(t.StatusId), cancellationToken);

        var overdueTickets = await _context.TicketSlas
            .CountAsync(ts => (ts.FirstResponseBreached || ts.ResolutionBreached)
                && !finalStatusIds.Contains(ts.Ticket.StatusId), cancellationToken);

        var resolvedToday = await _context.Tickets
            .CountAsync(t => finalStatusIds.Contains(t.StatusId)
                && t.UpdatedAt >= today, cancellationToken);

        var unassignedTickets = await _context.Tickets
            .CountAsync(t => !t.AssignedToId.HasValue
                && !finalStatusIds.Contains(t.StatusId), cancellationToken);

        var myOpenTickets = await _context.Tickets
            .CountAsync(t => t.AssignedToId == userId
                && !finalStatusIds.Contains(t.StatusId), cancellationToken);

        var myOverdueTickets = await _context.TicketSlas
            .CountAsync(ts => ts.Ticket.AssignedToId == userId
                && (ts.FirstResponseBreached || ts.ResolutionBreached)
                && !finalStatusIds.Contains(ts.Ticket.StatusId), cancellationToken);

        return new DashboardStatsDto(openTickets, overdueTickets, resolvedToday,
            unassignedTickets, myOpenTickets, myOverdueTickets);
    }
}
```

- [ ] **Step 3: Create GetSlaSummaryQuery**

Create `src/CustomerSupport.Application/Dashboard/Queries/GetSlaSummaryQuery.cs`:

```csharp
using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetSlaSummaryQuery : IRequest<SlaSummaryDto>;

public class GetSlaSummaryQueryHandler : IRequestHandler<GetSlaSummaryQuery, SlaSummaryDto>
{
    private readonly AppDbContext _context;

    public GetSlaSummaryQueryHandler(AppDbContext context) => _context = context;

    public async Task<SlaSummaryDto> Handle(GetSlaSummaryQuery request, CancellationToken cancellationToken)
    {
        var totalTracked = await _context.TicketSlas.CountAsync(cancellationToken);
        if (totalTracked == 0)
            return new SlaSummaryDto(0, 0, 0, 0, 0, 100, 100);

        var frBreached = await _context.TicketSlas.CountAsync(ts => ts.FirstResponseBreached, cancellationToken);
        var frOnTime = await _context.TicketSlas.CountAsync(ts => !ts.FirstResponseBreached && ts.FirstRespondedAt.HasValue, cancellationToken);

        var resBreached = await _context.TicketSlas.CountAsync(ts => ts.ResolutionBreached, cancellationToken);
        var resOnTime = await _context.TicketSlas.CountAsync(ts => !ts.ResolutionBreached && ts.ResolvedAt.HasValue, cancellationToken);

        var frTotal = frOnTime + frBreached;
        var resTotal = resOnTime + resBreached;

        return new SlaSummaryDto(totalTracked,
            frOnTime, frBreached, resOnTime, resBreached,
            frTotal > 0 ? Math.Round(frOnTime * 100.0 / frTotal, 1) : 100,
            resTotal > 0 ? Math.Round(resOnTime * 100.0 / resTotal, 1) : 100);
    }
}
```

- [ ] **Step 4: Create GetMyTicketsQuery**

Create `src/CustomerSupport.Application/Dashboard/Queries/GetMyTicketsQuery.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Tickets.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetMyTicketsQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedList<TicketDto>>;

public class GetMyTicketsQueryHandler : IRequestHandler<GetMyTicketsQuery, PaginatedList<TicketDto>>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyTicketsQueryHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<TicketDto>> Handle(GetMyTicketsQuery request, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        var query = _context.Tickets
            .Where(t => t.AssignedToId == _currentUserService.UserId)
            .Where(t => !finalStatusIds.Contains(t.StatusId))
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketDto(
                t.Id, t.TicketNumber, t.Subject,
                t.Customer.Name, t.Category.Name, t.Priority.Name,
                t.Status.Name, t.AssignedTo != null ? t.AssignedTo.FullName : null,
                t.CreatedAt));

        return await PaginatedList<TicketDto>.CreateAsync(query, request.Page, request.PageSize, cancellationToken);
    }
}
```

- [ ] **Step 5: Create GetTeamWorkloadQuery**

Create `src/CustomerSupport.Application/Dashboard/Queries/GetTeamWorkloadQuery.cs`:

```csharp
using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetTeamWorkloadQuery : IRequest<List<AgentWorkloadDto>>;

public class GetTeamWorkloadQueryHandler : IRequestHandler<GetTeamWorkloadQuery, List<AgentWorkloadDto>>
{
    private readonly AppDbContext _context;

    public GetTeamWorkloadQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<AgentWorkloadDto>> Handle(GetTeamWorkloadQuery request, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        var breachedTicketIds = await _context.TicketSlas
            .Where(ts => ts.FirstResponseBreached || ts.ResolutionBreached)
            .Select(ts => ts.TicketId)
            .ToListAsync(cancellationToken);

        return await _context.Tickets
            .Where(t => t.AssignedToId.HasValue && !finalStatusIds.Contains(t.StatusId))
            .GroupBy(t => new { t.AssignedToId, t.AssignedTo!.FullName })
            .Select(g => new AgentWorkloadDto(
                g.Key.AssignedToId!.Value,
                g.Key.FullName,
                g.Count(),
                g.Count(t => breachedTicketIds.Contains(t.Id))))
            .OrderByDescending(a => a.OpenTickets)
            .ToListAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Create DashboardController**

Create `src/CustomerSupport.API/Controllers/DashboardController.cs`:

```csharp
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Application.Dashboard.Queries;
using CustomerSupport.Application.Tickets.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("stats")]
    [Authorize(Policy = "Permission:dashboard.view")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
        => Ok(await _mediator.Send(new GetDashboardStatsQuery()));

    [HttpGet("sla-summary")]
    [Authorize(Policy = "Permission:dashboard.view")]
    public async Task<ActionResult<SlaSummaryDto>> GetSlaSummary()
        => Ok(await _mediator.Send(new GetSlaSummaryQuery()));

    [HttpGet("my-tickets")]
    [Authorize(Policy = "Permission:dashboard.view")]
    public async Task<ActionResult<PaginatedList<TicketDto>>> GetMyTickets(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new GetMyTicketsQuery(page, pageSize)));

    [HttpGet("team-workload")]
    [Authorize(Policy = "Permission:dashboard.view")]
    public async Task<ActionResult<List<AgentWorkloadDto>>> GetTeamWorkload()
        => Ok(await _mediator.Send(new GetTeamWorkloadQuery()));
}
```

- [ ] **Step 7: Verify build**

```powershell
dotnet build src/CustomerSupport.sln
```

- [ ] **Step 8: Commit**

```bash
git add src/
git commit -m "feat(dashboard): add agent dashboard API with stats, SLA summary, my-tickets, and team workload"
```

---

### Task 9: Agent Dashboard UI (P2.6)

**Files:**
- Create: `src/client/src/app/features/dashboard/dashboard.service.ts`
- Create: `src/client/src/app/features/dashboard/dashboard/dashboard.ts`
- Create: `src/client/src/app/features/dashboard/dashboard.routes.ts`
- Modify: `src/client/src/app/app.routes.ts` — add dashboard route (replace empty dashboard redirect)
- Modify: `src/client/src/assets/i18n/en.json` — add dashboard keys
- Modify: `src/client/src/assets/i18n/ar.json` — add dashboard keys

**Interfaces:**
- Consumes: `ApiService`, `PaginatedList<T>`, `TicketDto` from Phase 1, dashboard DTOs from Task 8
- Produces: Dashboard page with stat cards, SLA compliance indicators, my-tickets table, team workload chart, quick actions

- [ ] **Steps 1–5: (Full implementation for DashboardService, DashboardComponent, routes, i18n)**

**DashboardService** extends ApiService:
- `getStats()` → `GET /v1/dashboard/stats`
- `getSlaSummary()` → `GET /v1/dashboard/sla-summary`
- `getMyTickets(page, pageSize)` → `GET /v1/dashboard/my-tickets`
- `getTeamWorkload()` → `GET /v1/dashboard/team-workload`

**Interfaces:**
```typescript
export interface DashboardStatsDto {
  openTickets: number; overdueTickets: number; resolvedToday: number;
  unassignedTickets: number; myOpenTickets: number; myOverdueTickets: number;
}
export interface SlaSummaryDto {
  totalTracked: number; firstResponseOnTime: number; firstResponseBreached: number;
  resolutionOnTime: number; resolutionBreached: number;
  firstResponseCompliancePercent: number; resolutionCompliancePercent: number;
}
export interface AgentWorkloadDto {
  agentId: string; agentName: string; openTickets: number; overdueTickets: number;
}
```

**DashboardComponent**: Four sections:
1. **Stat Cards Row**: 6 Material cards showing OpenTickets, OverdueTickets, ResolvedToday, UnassignedTickets, MyOpenTickets, MyOverdueTickets — each with an icon, count, and label. Overdue cards in warn color.
2. **SLA Compliance**: Two progress indicators showing FirstResponse and Resolution compliance percentages. Green >= 90%, Yellow >= 70%, Red < 70%.
3. **My Tickets Table**: Material table of assigned tickets (ticketNumber, subject, priority, status, createdAt) with link to ticket detail. Paginated.
4. **Team Workload**: Material table showing agent name, open tickets, overdue tickets. Sorted by load.

All data loaded via `forkJoin` on init. Refresh button to reload.

**i18n keys (en.json):**
```json
"dashboard": {
  "title": "Dashboard",
  "openTickets": "Open Tickets",
  "overdueTickets": "Overdue Tickets",
  "resolvedToday": "Resolved Today",
  "unassignedTickets": "Unassigned",
  "myOpenTickets": "My Open Tickets",
  "myOverdueTickets": "My Overdue",
  "slaCompliance": "SLA Compliance",
  "firstResponse": "First Response",
  "resolution": "Resolution",
  "myTickets": "My Tickets",
  "teamWorkload": "Team Workload",
  "agent": "Agent",
  "open": "Open",
  "overdue": "Overdue",
  "refresh": "Refresh",
  "noTickets": "No tickets assigned",
  "compliance": "Compliance"
}
```

**i18n keys (ar.json):**
```json
"dashboard": {
  "title": "لوحة المعلومات",
  "openTickets": "التذاكر المفتوحة",
  "overdueTickets": "التذاكر المتأخرة",
  "resolvedToday": "تم حلها اليوم",
  "unassignedTickets": "غير معينة",
  "myOpenTickets": "تذاكري المفتوحة",
  "myOverdueTickets": "تذاكري المتأخرة",
  "slaCompliance": "التزام اتفاقية الخدمة",
  "firstResponse": "الاستجابة الأولى",
  "resolution": "الحل",
  "myTickets": "تذاكري",
  "teamWorkload": "حمل عمل الفريق",
  "agent": "الوكيل",
  "open": "مفتوحة",
  "overdue": "متأخرة",
  "refresh": "تحديث",
  "noTickets": "لا توجد تذاكر معينة",
  "compliance": "الالتزام"
}
```

- [ ] **Step 6: Update app.routes.ts**

Add to the admin children in `src/client/src/app/app.routes.ts`:

```typescript
{
  path: 'dashboard',
  loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.dashboardRoutes)
},
{
  path: 'knowledge',
  loadChildren: () => import('./features/knowledge/knowledge.routes').then(m => m.knowledgeRoutes)
},
{
  path: 'sla',
  loadChildren: () => import('./features/sla/sla.routes').then(m => m.slaRoutes)
},
{
  path: 'escalation',
  loadChildren: () => import('./features/escalation/escalation.routes').then(m => m.escalationRoutes)
},
{
  path: 'assignment',
  loadChildren: () => import('./features/assignment/assignment.routes').then(m => m.assignmentRoutes)
},
```

- [ ] **Step 7: Verify Angular compiles, commit**

```powershell
cd src/client
ng build
```

```bash
git add src/client/
git commit -m "feat(dashboard-ui): add agent dashboard with stats, SLA compliance, my-tickets, and team workload"
```

---

## Verification Checklist

After all 9 tasks complete:

- [ ] `dotnet build src/CustomerSupport.sln` succeeds
- [ ] API starts and responds at `/health`
- [ ] Swagger shows all new endpoints: sla, escalation-rules, assignment-rules, knowledge, dashboard
- [ ] `ng build` in `src/client/` succeeds
- [ ] SLA policy CRUD works (create, list, update, deactivate)
- [ ] Creating a ticket auto-applies matching SLA policy (TicketSla created)
- [ ] Background service detects SLA breaches after configured minutes
- [ ] Escalation rules execute on SLA breach (reassign or change priority)
- [ ] Auto-assignment assigns new tickets via round-robin or least-load
- [ ] Knowledge base categories CRUD works
- [ ] Knowledge base articles CRUD + search works
- [ ] Agent dashboard shows stats, SLA compliance, my tickets, team workload
- [ ] SLA/escalation/assignment admin pages render and function
- [ ] Language toggle switches English ↔ Arabic with RTL/LTR on all new pages
- [ ] All new endpoints require proper permissions
- [ ] Tenant isolation maintained on all new queries
