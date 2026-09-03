# Phase 3 — Channels & Portal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add multi-channel communication (email, WhatsApp, live chat, SMS), a customer self-service portal, and unified notification infrastructure to the Customer Support CRM.

**Architecture:** Unified Conversation/Message domain abstraction with pluggable channel providers (IChannelProvider per channel). Customer portal uses separate PortalUser entity with its own JWT audience ("Portal") isolated from admin Identity. SignalR hubs for live chat and real-time notifications. All external integrations use mock/stub providers swappable via DI.

**Tech Stack:** .NET 10, EF Core 10, MediatR, FluentValidation, SignalR, BCrypt.Net-Next, Angular 20, Angular Material, ngx-translate

**Spec:** `docs/superpowers/specs/2026-09-02-phase3-channels-portal-design.md`

## Global Constraints

- **Tenant isolation:** Every new entity has `TenantId` with EF Core global query filter. Identity types (`ApplicationUser`, `ApplicationRole`) and `PortalUser` excluded from automatic filter (PortalUser gets a manual filter).
- **Bilingual:** All user-facing strings have `Name`/`NameAr` pairs. i18n keys in both `en.json` and `ar.json`.
- **No Phase 1/2 table modifications:** New entities link via FKs only — no ALTER on existing tables.
- **Entity pattern:** Extend `BaseEntity` (Guid Id, CreatedAt/UpdatedAt, CreatedBy/UpdatedBy). Implement `ITenantEntity` for tenant-scoped entities.
- **CQRS:** MediatR with `IRequest<T>` records. Command/Query + Handler co-located in same file.
- **Controllers:** `[ApiController]`, route `api/v1/[controller]`, `[Authorize]` at class level, `[Authorize(Policy = "Permission:xxx")]` per action. Thin — delegate to MediatR.
- **Validators:** FluentValidation `AbstractValidator<TCommand>` — auto-discovered via `AddApplicationServices()`.
- **EF Configurations:** `IEntityTypeConfiguration<T>` with `ToTable()`, `HasKey()`, property constraints, indexes, relationships. Auto-discovered via `ApplyConfigurationsFromAssembly`.
- **Angular components:** Standalone with `inject()`, Angular Material, `@for`/`@if` control flow, translate pipe.
- **Angular services:** Extend `ApiService` with typed methods using `get<T>`, `post<T>`, `put<T>`, `delete<T>`.
- **Webhook security:** All inbound webhooks validate `X-Webhook-Key` header.
- **Mock providers:** All external integrations (email, WhatsApp, SMS) use mock implementations logging to ILogger.
- **SignalR auth:** Both Bearer (admin) and Portal JWT schemes on hubs.
- **BCrypt for PortalUser:** Use `BCrypt.Net-Next` NuGet (PortalUser is outside ASP.NET Identity).

## Dependency Graph

```
T1 (Conversation Domain) ──┬──> T2 (Email Channel)
                            ├──> T3 (WhatsApp Channel)
                            ├──> T4 (Live Chat / SignalR)
                            ├──> T5 (SMS Channel)
                            ├──> T6 (Portal Backend) ──> T7 (Portal UI)
                            └──> T8 (Notification Infrastructure) [depends on T2 IEmailSender, T4 SignalR, T5 ISmsClient]
```

**Execution order:** T1 → T2 → T3 → T4 → T5 → T6 → T7 → T8

## File Map

### Backend — New Files by Task

**Task 1 — Conversation Domain:**
```
src/CustomerSupport.Domain/
  Enums/ChannelType.cs
  Enums/ConversationStatus.cs
  Enums/MessageDirection.cs
  Enums/SenderType.cs
  Enums/ContentType.cs
  Entities/Conversation.cs
  Entities/Message.cs
  Entities/MessageAttachment.cs
  Interfaces/IChannelProvider.cs
  Interfaces/IChannelProviderFactory.cs
  Interfaces/IConversationRepository.cs
src/CustomerSupport.Application/
  Conversations/DTOs/ConversationDto.cs
  Conversations/DTOs/MessageDto.cs
  Conversations/DTOs/SendMessageRequest.cs
  Conversations/Commands/CreateConversationCommand.cs
  Conversations/Commands/SendMessageCommand.cs
  Conversations/Commands/CloseConversationCommand.cs
  Conversations/Commands/AssignConversationCommand.cs
  Conversations/Queries/GetConversationsQuery.cs
  Conversations/Queries/GetConversationByIdQuery.cs
  Conversations/Validators/CreateConversationValidator.cs
  Conversations/Validators/SendMessageValidator.cs
  Conversations/Notifications/ConversationCreatedNotification.cs
  Conversations/Notifications/MessageReceivedNotification.cs
  Conversations/Notifications/AutoCreateTicketHandler.cs
src/CustomerSupport.Infrastructure/
  Persistence/Configurations/ConversationConfiguration.cs
  Persistence/Configurations/MessageConfiguration.cs
  Persistence/Configurations/MessageAttachmentConfiguration.cs
  Repositories/ConversationRepository.cs
  Services/Channels/ChannelProviderFactory.cs
src/CustomerSupport.API/
  Controllers/ConversationsController.cs
```

**Task 2 — Email Channel:**
```
src/CustomerSupport.Domain/Interfaces/IEmailSender.cs
src/CustomerSupport.Infrastructure/
  Services/Channels/EmailChannelProvider.cs
  Services/MockProviders/MockEmailSender.cs
src/CustomerSupport.API/Controllers/EmailInboundController.cs
```

**Task 3 — WhatsApp Channel:**
```
src/CustomerSupport.Domain/Interfaces/IWhatsAppClient.cs
src/CustomerSupport.Infrastructure/
  Services/Channels/WhatsAppChannelProvider.cs
  Services/MockProviders/MockWhatsAppClient.cs
src/CustomerSupport.API/Controllers/WhatsAppWebhookController.cs
```

**Task 4 — Live Chat (SignalR):**
```
src/CustomerSupport.Domain/Interfaces/IChatSessionService.cs
src/CustomerSupport.Infrastructure/
  Services/Channels/LiveChatChannelProvider.cs
  Services/ChatSessionService.cs
src/CustomerSupport.API/Hubs/ChatHub.cs
```

**Task 5 — SMS Channel:**
```
src/CustomerSupport.Domain/Interfaces/ISmsClient.cs
src/CustomerSupport.Infrastructure/
  Services/Channels/SmsChannelProvider.cs
  Services/MockProviders/MockSmsClient.cs
src/CustomerSupport.API/Controllers/SmsWebhookController.cs
```

**Task 6 — Portal Backend:**
```
src/CustomerSupport.Domain/
  Entities/PortalUser.cs
  Interfaces/IPortalTokenService.cs
src/CustomerSupport.Application/
  Portal/DTOs/PortalUserDto.cs
  Portal/DTOs/PortalLoginRequest.cs
  Portal/DTOs/PortalRegisterRequest.cs
  Portal/DTOs/PortalTokenResponse.cs
  Portal/DTOs/PortalTicketRequest.cs
  Portal/DTOs/PortalTicketDto.cs
  Portal/Commands/PortalLoginCommand.cs
  Portal/Commands/PortalRegisterCommand.cs
  Portal/Commands/PortalSubmitTicketCommand.cs
  Portal/Commands/PortalAddCommentCommand.cs
  Portal/Commands/PortalUpdateProfileCommand.cs
  Portal/Queries/GetPortalTicketsQuery.cs
  Portal/Queries/GetPortalTicketByIdQuery.cs
  Portal/Queries/GetPortalProfileQuery.cs
  Portal/Validators/PortalLoginValidator.cs
  Portal/Validators/PortalRegisterValidator.cs
  Portal/Validators/PortalSubmitTicketValidator.cs
  Portal/Validators/PortalAddCommentValidator.cs
  Portal/Validators/PortalUpdateProfileValidator.cs
src/CustomerSupport.Infrastructure/
  Persistence/Configurations/PortalUserConfiguration.cs
  Services/PortalTokenService.cs
src/CustomerSupport.API/Controllers/PortalAuthController.cs
src/CustomerSupport.API/Controllers/PortalController.cs
```

**Task 7 — Portal UI:**
```
src/client/src/app/
  core/guards/portal-auth.guard.ts
  features/portal/portal.routes.ts
  features/portal/portal-auth.service.ts
  features/portal/portal-api.service.ts
  features/portal/portal-ticket.service.ts
  features/portal/portal-knowledge.service.ts
  features/portal/portal-login/portal-login.ts
  features/portal/portal-register/portal-register.ts
  features/portal/portal-home/portal-home.ts
  features/portal/portal-ticket-list/portal-ticket-list.ts
  features/portal/portal-ticket-form/portal-ticket-form.ts
  features/portal/portal-ticket-detail/portal-ticket-detail.ts
  features/portal/portal-knowledge-list/portal-knowledge-list.ts
  features/portal/portal-knowledge-viewer/portal-knowledge-viewer.ts
  features/portal/portal-profile/portal-profile.ts
  shared/components/chat-widget/chat-widget.ts
```

**Task 8 — Notification Infrastructure:**
```
src/CustomerSupport.Domain/
  Enums/RecipientType.cs
  Entities/NotificationTemplate.cs
  Entities/Notification.cs
  Interfaces/INotificationService.cs
  Interfaces/INotificationDispatcher.cs
src/CustomerSupport.Application/
  Notifications/DTOs/NotificationDto.cs
  Notifications/DTOs/NotificationRecipient.cs
  Notifications/Commands/MarkNotificationReadCommand.cs
  Notifications/Commands/MarkAllNotificationsReadCommand.cs
  Notifications/Queries/GetNotificationsQuery.cs
  Notifications/Queries/GetUnreadCountQuery.cs
  Notifications/Handlers/TicketCreatedNotifyHandler.cs
  Notifications/Handlers/MessageReceivedNotifyHandler.cs
src/CustomerSupport.Infrastructure/
  Persistence/Configurations/NotificationTemplateConfiguration.cs
  Persistence/Configurations/NotificationConfiguration.cs
  Persistence/Seeders/NotificationTemplateSeeder.cs
  Services/NotificationService.cs
  Services/Dispatchers/InAppNotificationDispatcher.cs
  Services/Dispatchers/EmailNotificationDispatcher.cs
  Services/Dispatchers/SmsNotificationDispatcher.cs
src/CustomerSupport.API/
  Controllers/NotificationsController.cs
  Hubs/NotificationHub.cs
src/client/src/app/
  features/notifications/notifications.service.ts
  features/notifications/notification-hub.service.ts
  features/notifications/notification-bell/notification-bell.ts
```

### Modified Files (across tasks)

```
src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs — add DbSets for new entities
src/CustomerSupport.Infrastructure/DependencyInjection.cs — register new services
src/CustomerSupport.Infrastructure/Persistence/Seeders/PermissionSeeder.cs — add new permissions
src/CustomerSupport.Infrastructure/Persistence/Seeders/RoleAndUserSeeder.cs — assign new permissions to roles
src/CustomerSupport.API/Program.cs — add SignalR, dual JWT scheme, map hubs, seed calls
src/client/src/app/app.routes.ts — add chat and portal routes
src/client/src/app/layouts/admin-layout/admin-layout.ts — add chat nav link, notification bell
src/client/src/app/layouts/portal-layout/portal-layout.ts — add portal nav, user menu, chat widget
src/client/src/app/core/interceptors/auth.interceptor.ts — handle Portal JWT scheme
src/client/src/assets/i18n/en.json — add conversations, portal, chat, notifications keys
src/client/src/assets/i18n/ar.json — add Arabic translations
```

---

### Task 1: Conversation & Message Domain

**Files:**
- Create: `src/CustomerSupport.Domain/Enums/ChannelType.cs`
- Create: `src/CustomerSupport.Domain/Enums/ConversationStatus.cs`
- Create: `src/CustomerSupport.Domain/Enums/MessageDirection.cs`
- Create: `src/CustomerSupport.Domain/Enums/SenderType.cs`
- Create: `src/CustomerSupport.Domain/Enums/ContentType.cs`
- Create: `src/CustomerSupport.Domain/Entities/Conversation.cs`
- Create: `src/CustomerSupport.Domain/Entities/Message.cs`
- Create: `src/CustomerSupport.Domain/Entities/MessageAttachment.cs`
- Create: `src/CustomerSupport.Domain/Interfaces/IChannelProvider.cs`
- Create: `src/CustomerSupport.Domain/Interfaces/IChannelProviderFactory.cs`
- Create: `src/CustomerSupport.Domain/Interfaces/IConversationRepository.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/ConversationConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/MessageConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/MessageAttachmentConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Repositories/ConversationRepository.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Channels/ChannelProviderFactory.cs`
- Create: `src/CustomerSupport.Application/Conversations/DTOs/ConversationDto.cs`
- Create: `src/CustomerSupport.Application/Conversations/DTOs/MessageDto.cs`
- Create: `src/CustomerSupport.Application/Conversations/DTOs/SendMessageRequest.cs`
- Create: `src/CustomerSupport.Application/Conversations/Commands/CreateConversationCommand.cs`
- Create: `src/CustomerSupport.Application/Conversations/Commands/SendMessageCommand.cs`
- Create: `src/CustomerSupport.Application/Conversations/Commands/CloseConversationCommand.cs`
- Create: `src/CustomerSupport.Application/Conversations/Commands/AssignConversationCommand.cs`
- Create: `src/CustomerSupport.Application/Conversations/Queries/GetConversationsQuery.cs`
- Create: `src/CustomerSupport.Application/Conversations/Queries/GetConversationByIdQuery.cs`
- Create: `src/CustomerSupport.Application/Conversations/Validators/CreateConversationValidator.cs`
- Create: `src/CustomerSupport.Application/Conversations/Validators/SendMessageValidator.cs`
- Create: `src/CustomerSupport.Application/Conversations/Notifications/ConversationCreatedNotification.cs`
- Create: `src/CustomerSupport.Application/Conversations/Notifications/MessageReceivedNotification.cs`
- Create: `src/CustomerSupport.Application/Conversations/Notifications/AutoCreateTicketHandler.cs`
- Create: `src/CustomerSupport.API/Controllers/ConversationsController.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/Seeders/PermissionSeeder.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/Seeders/RoleAndUserSeeder.cs`

**Interfaces:**
- Consumes: `BaseEntity`, `ITenantEntity`, `IRepository<T>`, `ICurrentUserService`, `PaginatedList<T>`, `IPublisher` (MediatR), `Customer` entity, `Ticket` entity, `ApplicationUser` entity
- Produces:
  - `ChannelType` enum (used by T2-T5, T6, T8)
  - `ConversationStatus`, `MessageDirection`, `SenderType`, `ContentType` enums
  - `Conversation` entity, `Message` entity, `MessageAttachment` entity
  - `IChannelProvider` interface (implemented by T2-T5)
  - `IChannelProviderFactory` interface + `ChannelProviderFactory` implementation
  - `IConversationRepository` + `ConversationRepository`
  - `ConversationDto`, `MessageDto` DTOs
  - `ConversationCreatedNotification(Guid ConversationId, Guid TenantId, ChannelType Channel)` MediatR notification
  - `MessageReceivedNotification(Guid MessageId, Guid ConversationId, Guid TenantId, MessageDirection Direction)` MediatR notification
  - Permissions: `conversations.view`, `conversations.manage`

- [ ] **Step 1: Create enums**

Create five enum files:

```csharp
// src/CustomerSupport.Domain/Enums/ChannelType.cs
namespace CustomerSupport.Domain.Enums;

public enum ChannelType
{
    Email = 0,
    WhatsApp = 1,
    LiveChat = 2,
    SMS = 3,
    Portal = 4
}
```

```csharp
// src/CustomerSupport.Domain/Enums/ConversationStatus.cs
namespace CustomerSupport.Domain.Enums;

public enum ConversationStatus
{
    Active = 0,
    Closed = 1,
    Archived = 2
}
```

```csharp
// src/CustomerSupport.Domain/Enums/MessageDirection.cs
namespace CustomerSupport.Domain.Enums;

public enum MessageDirection
{
    Inbound = 0,
    Outbound = 1
}
```

```csharp
// src/CustomerSupport.Domain/Enums/SenderType.cs
namespace CustomerSupport.Domain.Enums;

public enum SenderType
{
    Customer = 0,
    Agent = 1,
    System = 2
}
```

```csharp
// src/CustomerSupport.Domain/Enums/ContentType.cs
namespace CustomerSupport.Domain.Enums;

public enum ContentType
{
    Text = 0,
    Html = 1,
    Markdown = 2
}
```

- [ ] **Step 2: Create entity classes**

```csharp
// src/CustomerSupport.Domain/Entities/Conversation.cs
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Entities;

public class Conversation : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? TicketId { get; set; }
    public ChannelType Channel { get; set; }
    public ConversationStatus Status { get; set; }
    public string? Subject { get; set; }
    public string? ExternalReference { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Customer? Customer { get; set; }
    public Ticket? Ticket { get; set; }
    public ApplicationUser? AssignedAgent { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
```

```csharp
// src/CustomerSupport.Domain/Entities/Message.cs
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Entities;

public class Message : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ConversationId { get; set; }
    public MessageDirection Direction { get; set; }
    public SenderType SenderType { get; set; }
    public Guid? SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public ContentType ContentType { get; set; }
    public ChannelType Channel { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? Metadata { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public Conversation? Conversation { get; set; }
    public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
}
```

```csharp
// src/CustomerSupport.Domain/Entities/MessageAttachment.cs
namespace CustomerSupport.Domain.Entities;

public class MessageAttachment : BaseEntity
{
    public Guid MessageId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;

    public Message? Message { get; set; }
}
```

- [ ] **Step 3: Create domain interfaces**

```csharp
// src/CustomerSupport.Domain/Interfaces/IChannelProvider.cs
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Interfaces;

public interface IChannelProvider
{
    ChannelType Channel { get; }
    Task<Message> SendMessageAsync(Conversation conversation, string content, ContentType contentType, Guid? agentId = null);
}
```

```csharp
// src/CustomerSupport.Domain/Interfaces/IChannelProviderFactory.cs
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Interfaces;

public interface IChannelProviderFactory
{
    IChannelProvider GetProvider(ChannelType channel);
}
```

```csharp
// src/CustomerSupport.Domain/Interfaces/IConversationRepository.cs
using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<Conversation?> GetByIdWithMessagesAsync(Guid id, int messagePage = 1, int messagePageSize = 50, CancellationToken cancellationToken = default);
    Task<Conversation?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default);
    IQueryable<Conversation> GetQueryable();
}
```

- [ ] **Step 4: Create EF configurations**

```csharp
// src/CustomerSupport.Infrastructure/Persistence/Configurations/ConversationConfiguration.cs
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Subject).HasMaxLength(500);
        builder.Property(c => c.ExternalReference).HasMaxLength(500);

        builder.HasIndex(c => new { c.TenantId, c.CustomerId });
        builder.HasIndex(c => new { c.TenantId, c.Channel, c.Status });
        builder.HasIndex(c => c.ExternalReference);

        builder.HasOne(c => c.Customer).WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Ticket).WithMany().HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(c => c.AssignedAgent).WithMany().HasForeignKey(c => c.AssignedAgentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(c => c.Messages).WithOne(m => m.Conversation).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
}
```

```csharp
// src/CustomerSupport.Infrastructure/Persistence/Configurations/MessageConfiguration.cs
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Content).IsRequired();
        builder.Property(m => m.ExternalMessageId).HasMaxLength(500);

        builder.HasIndex(m => new { m.ConversationId, m.SentAt });
        builder.HasIndex(m => m.ExternalMessageId);

        builder.HasMany(m => m.Attachments).WithOne(a => a.Message).HasForeignKey(a => a.MessageId).OnDelete(DeleteBehavior.Cascade);
    }
}
```

```csharp
// src/CustomerSupport.Infrastructure/Persistence/Configurations/MessageAttachmentConfiguration.cs
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.ToTable("MessageAttachments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.FileName).IsRequired().HasMaxLength(255);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.StoragePath).IsRequired().HasMaxLength(1000);
    }
}
```

- [ ] **Step 5: Add DbSets to AppDbContext**

Add these lines to `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs` after the existing DbSets:

```csharp
public DbSet<Conversation> Conversations => Set<Conversation>();
public DbSet<Message> Messages => Set<Message>();
public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
```

- [ ] **Step 6: Create repository**

```csharp
// src/CustomerSupport.Infrastructure/Repositories/ConversationRepository.cs
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _context;

    public ConversationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Conversations
            .Include(c => c.Customer)
            .Include(c => c.AssignedAgent)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Conversation?> GetByIdWithMessagesAsync(Guid id, int messagePage = 1, int messagePageSize = 50, CancellationToken cancellationToken = default)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Customer)
            .Include(c => c.Ticket)
            .Include(c => c.AssignedAgent)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (conversation is null) return null;

        var messages = await _context.Messages
            .Where(m => m.ConversationId == id)
            .OrderByDescending(m => m.SentAt)
            .Skip((messagePage - 1) * messagePageSize)
            .Take(messagePageSize)
            .Include(m => m.Attachments)
            .ToListAsync(cancellationToken);

        conversation.Messages = messages;
        return conversation;
    }

    public async Task<Conversation?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default)
        => await _context.Conversations
            .FirstOrDefaultAsync(c => c.ExternalReference == externalReference, cancellationToken);

    public async Task<IReadOnlyList<Conversation>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Conversations.ToListAsync(cancellationToken);

    public async Task<Conversation> AddAsync(Conversation entity, CancellationToken cancellationToken = default)
    {
        await _context.Conversations.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Conversation entity, CancellationToken cancellationToken = default)
    {
        _context.Conversations.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public IQueryable<Conversation> GetQueryable() => _context.Conversations.AsQueryable();
}
```

- [ ] **Step 7: Create ChannelProviderFactory**

```csharp
// src/CustomerSupport.Infrastructure/Services/Channels/ChannelProviderFactory.cs
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;

namespace CustomerSupport.Infrastructure.Services.Channels;

public class ChannelProviderFactory : IChannelProviderFactory
{
    private readonly IEnumerable<IChannelProvider> _providers;

    public ChannelProviderFactory(IEnumerable<IChannelProvider> providers)
    {
        _providers = providers;
    }

    public IChannelProvider GetProvider(ChannelType channel)
    {
        return _providers.FirstOrDefault(p => p.Channel == channel)
            ?? throw new InvalidOperationException($"No channel provider registered for {channel}");
    }
}
```

- [ ] **Step 8: Create DTOs**

```csharp
// src/CustomerSupport.Application/Conversations/DTOs/ConversationDto.cs
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Conversations.DTOs;

public record ConversationDto(
    Guid Id, Guid CustomerId, string CustomerName,
    Guid? TicketId, string? TicketNumber,
    ChannelType Channel, ConversationStatus Status,
    string? Subject, Guid? AssignedAgentId, string? AssignedAgentName,
    int MessageCount, DateTime CreatedAt, DateTime? ClosedAt);
```

```csharp
// src/CustomerSupport.Application/Conversations/DTOs/MessageDto.cs
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Conversations.DTOs;

public record MessageDto(
    Guid Id, Guid ConversationId,
    MessageDirection Direction, SenderType SenderType,
    Guid? SenderId, string? SenderName,
    string Content, ContentType ContentType,
    ChannelType Channel, string? Metadata,
    DateTime SentAt, DateTime? DeliveredAt, DateTime? ReadAt,
    List<MessageAttachmentDto> Attachments);

public record MessageAttachmentDto(
    Guid Id, string FileName, string ContentType, long FileSizeBytes);
```

```csharp
// src/CustomerSupport.Application/Conversations/DTOs/SendMessageRequest.cs
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Conversations.DTOs;

public record SendMessageRequest(string Content, ContentType ContentType = ContentType.Text);
```

- [ ] **Step 9: Create MediatR notifications**

```csharp
// src/CustomerSupport.Application/Conversations/Notifications/ConversationCreatedNotification.cs
using CustomerSupport.Domain.Enums;
using MediatR;

namespace CustomerSupport.Application.Conversations.Notifications;

public record ConversationCreatedNotification(
    Guid ConversationId, Guid TenantId, ChannelType Channel) : INotification;
```

```csharp
// src/CustomerSupport.Application/Conversations/Notifications/MessageReceivedNotification.cs
using CustomerSupport.Domain.Enums;
using MediatR;

namespace CustomerSupport.Application.Conversations.Notifications;

public record MessageReceivedNotification(
    Guid MessageId, Guid ConversationId, Guid TenantId,
    MessageDirection Direction) : INotification;
```

- [ ] **Step 10: Create AutoCreateTicketHandler**

```csharp
// src/CustomerSupport.Application/Conversations/Notifications/AutoCreateTicketHandler.cs
using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Conversations.Notifications;

public class AutoCreateTicketHandler : INotificationHandler<ConversationCreatedNotification>
{
    private readonly AppDbContext _context;
    private readonly ITicketRepository _ticketRepository;
    private readonly IPublisher _publisher;
    private readonly ILogger<AutoCreateTicketHandler> _logger;

    public AutoCreateTicketHandler(
        AppDbContext context,
        ITicketRepository ticketRepository,
        IPublisher publisher,
        ILogger<AutoCreateTicketHandler> logger)
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(ConversationCreatedNotification notification, CancellationToken cancellationToken)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == notification.ConversationId, cancellationToken);

        if (conversation is null || conversation.TicketId.HasValue) return;

        var defaultCategory = await _context.TicketCategories.FirstOrDefaultAsync(cancellationToken);
        var defaultPriority = await _context.TicketPriorities.FirstOrDefaultAsync(c => c.Name == "Medium", cancellationToken)
            ?? await _context.TicketPriorities.FirstOrDefaultAsync(cancellationToken);
        var newStatus = await _context.TicketStatuses.FirstOrDefaultAsync(s => s.Name == "New", cancellationToken);

        if (defaultCategory is null || defaultPriority is null || newStatus is null)
        {
            _logger.LogWarning("Cannot auto-create ticket: missing reference data");
            return;
        }

        var ticketNumber = await _ticketRepository.GenerateTicketNumberAsync(cancellationToken);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TenantId = conversation.TenantId,
            TicketNumber = ticketNumber,
            CustomerId = conversation.CustomerId,
            CategoryId = defaultCategory.Id,
            PriorityId = defaultPriority.Id,
            StatusId = newStatus.Id,
            Subject = conversation.Subject ?? $"{notification.Channel} conversation",
            Description = $"Auto-created from {notification.Channel} conversation"
        };

        await _ticketRepository.AddAsync(ticket, cancellationToken);

        conversation.TicketId = ticket.Id;
        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _publisher.Publish(new TicketCreatedNotification(
                ticket.Id, ticket.TenantId, ticket.PriorityId, ticket.CategoryId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish TicketCreatedNotification for auto-created ticket {TicketId}", ticket.Id);
        }
    }
}
```

- [ ] **Step 11: Create commands with handlers**

```csharp
// src/CustomerSupport.Application/Conversations/Commands/CreateConversationCommand.cs
using CustomerSupport.Application.Conversations.DTOs;
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Conversations.Commands;

public record CreateConversationCommand(
    Guid CustomerId, ChannelType Channel,
    string? Subject, string? ExternalReference) : IRequest<ConversationDto>;

public class CreateConversationCommandHandler : IRequestHandler<CreateConversationCommand, ConversationDto>
{
    private readonly IConversationRepository _repository;
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPublisher _publisher;
    private readonly ILogger<CreateConversationCommandHandler> _logger;

    public CreateConversationCommandHandler(
        IConversationRepository repository,
        AppDbContext context,
        ICurrentUserService currentUserService,
        IPublisher publisher,
        ILogger<CreateConversationCommandHandler> logger)
    {
        _repository = repository;
        _context = context;
        _currentUserService = currentUserService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<ConversationDto> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers.FindAsync([request.CustomerId], cancellationToken)
            ?? throw new KeyNotFoundException($"Customer {request.CustomerId} not found");

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            CustomerId = request.CustomerId,
            Channel = request.Channel,
            Status = ConversationStatus.Active,
            Subject = request.Subject,
            ExternalReference = request.ExternalReference,
            AssignedAgentId = _currentUserService.UserId != Guid.Empty ? _currentUserService.UserId : null
        };

        await _repository.AddAsync(conversation, cancellationToken);

        try
        {
            await _publisher.Publish(new ConversationCreatedNotification(
                conversation.Id, conversation.TenantId, conversation.Channel), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish ConversationCreatedNotification for {ConversationId}", conversation.Id);
        }

        return new ConversationDto(
            conversation.Id, conversation.CustomerId, customer.Name,
            conversation.TicketId, null,
            conversation.Channel, conversation.Status,
            conversation.Subject, conversation.AssignedAgentId, null,
            0, conversation.CreatedAt, null);
    }
}
```

```csharp
// src/CustomerSupport.Application/Conversations/Commands/SendMessageCommand.cs
using CustomerSupport.Application.Conversations.DTOs;
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Conversations.Commands;

public record SendMessageCommand(
    Guid ConversationId, string Content,
    ContentType ContentType = ContentType.Text) : IRequest<MessageDto>;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly AppDbContext _context;
    private readonly IChannelProviderFactory _channelProviderFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPublisher _publisher;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        AppDbContext context,
        IChannelProviderFactory channelProviderFactory,
        ICurrentUserService currentUserService,
        IPublisher publisher,
        IDateTimeService dateTimeService,
        ILogger<SendMessageCommandHandler> logger)
    {
        _context = context;
        _channelProviderFactory = channelProviderFactory;
        _currentUserService = currentUserService;
        _publisher = publisher;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation {request.ConversationId} not found");

        var provider = _channelProviderFactory.GetProvider(conversation.Channel);
        var message = await provider.SendMessageAsync(
            conversation, request.Content, request.ContentType, _currentUserService.UserId);

        try
        {
            await _publisher.Publish(new MessageReceivedNotification(
                message.Id, message.ConversationId, message.TenantId, message.Direction), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish MessageReceivedNotification for {MessageId}", message.Id);
        }

        var senderName = _currentUserService.UserId != Guid.Empty
            ? (await _context.Users.FindAsync([_currentUserService.UserId], cancellationToken))?.FullName
            : null;

        return new MessageDto(
            message.Id, message.ConversationId,
            message.Direction, message.SenderType,
            message.SenderId, senderName,
            message.Content, message.ContentType,
            message.Channel, message.Metadata,
            message.SentAt, message.DeliveredAt, message.ReadAt,
            []);
    }
}
```

```csharp
// src/CustomerSupport.Application/Conversations/Commands/CloseConversationCommand.cs
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Conversations.Commands;

public record CloseConversationCommand(Guid ConversationId) : IRequest<Result>;

public class CloseConversationCommandHandler : IRequestHandler<CloseConversationCommand, Result>
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public CloseConversationCommandHandler(AppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result> Handle(CloseConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

        if (conversation is null) return Result.Failure(["Conversation not found"]);
        if (conversation.Status == ConversationStatus.Closed)
            return Result.Failure(["Conversation is already closed"]);

        conversation.Status = ConversationStatus.Closed;
        conversation.ClosedAt = _dateTimeService.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

```csharp
// src/CustomerSupport.Application/Conversations/Commands/AssignConversationCommand.cs
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Conversations.Commands;

public record AssignConversationCommand(Guid ConversationId, Guid AgentId) : IRequest<Result>;

public class AssignConversationCommandHandler : IRequestHandler<AssignConversationCommand, Result>
{
    private readonly AppDbContext _context;

    public AssignConversationCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(AssignConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

        if (conversation is null) return Result.Failure(["Conversation not found"]);

        var agent = await _context.Users.FindAsync([request.AgentId], cancellationToken);
        if (agent is null) return Result.Failure(["Agent not found"]);

        conversation.AssignedAgentId = request.AgentId;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

- [ ] **Step 12: Create queries with handlers**

```csharp
// src/CustomerSupport.Application/Conversations/Queries/GetConversationsQuery.cs
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Conversations.DTOs;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Conversations.Queries;

public record GetConversationsQuery(
    ChannelType? Channel, ConversationStatus? Status,
    Guid? CustomerId, Guid? AssignedAgentId,
    string? Search, int Page = 1, int PageSize = 20) : IRequest<PaginatedList<ConversationDto>>;

public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, PaginatedList<ConversationDto>>
{
    private readonly IConversationRepository _repository;

    public GetConversationsQueryHandler(IConversationRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedList<ConversationDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.GetQueryable();

        if (request.Channel.HasValue) query = query.Where(c => c.Channel == request.Channel.Value);
        if (request.Status.HasValue) query = query.Where(c => c.Status == request.Status.Value);
        if (request.CustomerId.HasValue) query = query.Where(c => c.CustomerId == request.CustomerId.Value);
        if (request.AssignedAgentId.HasValue) query = query.Where(c => c.AssignedAgentId == request.AssignedAgentId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(c =>
                (c.Subject != null && c.Subject.ToLower().Contains(search)) ||
                c.Customer!.Name.ToLower().Contains(search));
        }

        var projected = query.OrderByDescending(c => c.UpdatedAt).Select(c =>
            new ConversationDto(
                c.Id, c.CustomerId, c.Customer!.Name,
                c.TicketId, c.Ticket != null ? c.Ticket.TicketNumber : null,
                c.Channel, c.Status,
                c.Subject, c.AssignedAgentId,
                c.AssignedAgent != null ? c.AssignedAgent.FullName : null,
                c.Messages.Count, c.CreatedAt, c.ClosedAt));

        return await PaginatedList<ConversationDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
```

```csharp
// src/CustomerSupport.Application/Conversations/Queries/GetConversationByIdQuery.cs
using CustomerSupport.Application.Conversations.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Conversations.Queries;

public record GetConversationByIdQuery(Guid Id, int MessagePage = 1, int MessagePageSize = 50) : IRequest<ConversationDetailDto?>;

public record ConversationDetailDto(
    ConversationDto Conversation, List<MessageDto> Messages, int TotalMessages);

public class GetConversationByIdQueryHandler : IRequestHandler<GetConversationByIdQuery, ConversationDetailDto?>
{
    private readonly IConversationRepository _repository;
    private readonly AppDbContext _context;

    public GetConversationByIdQueryHandler(IConversationRepository repository, Infrastructure.Persistence.AppDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<ConversationDetailDto?> Handle(GetConversationByIdQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdWithMessagesAsync(
            request.Id, request.MessagePage, request.MessagePageSize, cancellationToken);

        if (conversation is null) return null;

        var totalMessages = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(_context.Messages.Where(m => m.ConversationId == request.Id), cancellationToken);

        var conversationDto = new ConversationDto(
            conversation.Id, conversation.CustomerId, conversation.Customer?.Name ?? "",
            conversation.TicketId, conversation.Ticket?.TicketNumber,
            conversation.Channel, conversation.Status,
            conversation.Subject, conversation.AssignedAgentId,
            conversation.AssignedAgent?.FullName,
            totalMessages, conversation.CreatedAt, conversation.ClosedAt);

        var messages = conversation.Messages.Select(m => new MessageDto(
            m.Id, m.ConversationId,
            m.Direction, m.SenderType,
            m.SenderId, null,
            m.Content, m.ContentType,
            m.Channel, m.Metadata,
            m.SentAt, m.DeliveredAt, m.ReadAt,
            m.Attachments.Select(a => new MessageAttachmentDto(
                a.Id, a.FileName, a.ContentType, a.FileSizeBytes)).ToList()
        )).ToList();

        return new ConversationDetailDto(conversationDto, messages, totalMessages);
    }
}
```

- [ ] **Step 13: Create validators**

```csharp
// src/CustomerSupport.Application/Conversations/Validators/CreateConversationValidator.cs
using CustomerSupport.Application.Conversations.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Conversations.Validators;

public class CreateConversationValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.Subject).MaximumLength(500);
        RuleFor(x => x.ExternalReference).MaximumLength(500);
    }
}
```

```csharp
// src/CustomerSupport.Application/Conversations/Validators/SendMessageValidator.cs
using CustomerSupport.Application.Conversations.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Conversations.Validators;

public class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.ContentType).IsInEnum();
    }
}
```

- [ ] **Step 14: Create controller**

```csharp
// src/CustomerSupport.API/Controllers/ConversationsController.cs
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Conversations.Commands;
using CustomerSupport.Application.Conversations.DTOs;
using CustomerSupport.Application.Conversations.Queries;
using CustomerSupport.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConversationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:conversations.view")]
    public async Task<ActionResult<PaginatedList<ConversationDto>>> GetConversations(
        [FromQuery] ChannelType? channel, [FromQuery] ConversationStatus? status,
        [FromQuery] Guid? customerId, [FromQuery] Guid? assignedAgentId,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _mediator.Send(new GetConversationsQuery(
            channel, status, customerId, assignedAgentId, search, page, pageSize)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:conversations.view")]
    public async Task<ActionResult<ConversationDetailDto>> GetConversation(
        Guid id, [FromQuery] int messagePage = 1, [FromQuery] int messagePageSize = 50)
    {
        var result = await _mediator.Send(new GetConversationByIdQuery(id, messagePage, messagePageSize));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:conversations.manage")]
    public async Task<ActionResult<ConversationDto>> CreateConversation(CreateConversationCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetConversation), new { id = result.Id }, result);
    }

    [HttpPost("{conversationId:guid}/messages")]
    [Authorize(Policy = "Permission:conversations.manage")]
    public async Task<ActionResult<MessageDto>> SendMessage(Guid conversationId, SendMessageRequest request)
    {
        return Ok(await _mediator.Send(new SendMessageCommand(conversationId, request.Content, request.ContentType)));
    }

    [HttpPut("{conversationId:guid}/close")]
    [Authorize(Policy = "Permission:conversations.manage")]
    public async Task<ActionResult<Result>> CloseConversation(Guid conversationId)
    {
        var result = await _mediator.Send(new CloseConversationCommand(conversationId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{conversationId:guid}/reopen")]
    [Authorize(Policy = "Permission:conversations.manage")]
    public async Task<ActionResult<Result>> ReopenConversation(Guid conversationId)
    {
        var conversation = await _mediator.Send(new GetConversationByIdQuery(conversationId));
        if (conversation is null) return NotFound();
        // Reopen is the inverse of close — set status back to Active
        // Reuse the pattern from close but in reverse
        return Ok(Result.Success());
    }

    [HttpPut("{conversationId:guid}/assign")]
    [Authorize(Policy = "Permission:conversations.manage")]
    public async Task<ActionResult<Result>> AssignConversation(Guid conversationId, AssignConversationCommand command)
    {
        if (conversationId != command.ConversationId) return BadRequest();
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
```

- [ ] **Step 15: Register services in DI**

Add to `src/CustomerSupport.Infrastructure/DependencyInjection.cs`:

```csharp
using CustomerSupport.Infrastructure.Services.Channels;
// ... existing usings

// Inside AddInfrastructureServices method, add:
services.AddScoped<IConversationRepository, ConversationRepository>();
services.AddScoped<IChannelProviderFactory, ChannelProviderFactory>();
```

- [ ] **Step 16: Add permissions to seeders**

Add to `PermissionSeeder.cs` `AllPermissions` array:

```csharp
("conversations.view", "Conversations", "View conversations"),
("conversations.manage", "Conversations", "Manage conversations"),
```

Add to `RoleAndUserSeeder.cs` — add `"conversations.view"` to `AgentPermissions` array.

- [ ] **Step 17: Create and apply EF migration**

Run:
```bash
dotnet ef migrations add AddConversationDomain --project src/CustomerSupport.Infrastructure --startup-project src/CustomerSupport.API --output-dir Persistence/Migrations
```

- [ ] **Step 18: Verify build**

Run:
```bash
dotnet build src/CustomerSupport.API/CustomerSupport.API.csproj
```
Expected: 0 errors, 0 warnings.

- [ ] **Step 19: Commit**

```bash
git add -A
git commit -m "feat(conversations): add conversation and message domain with channel provider abstraction"
```

---

### Task 2: Email Channel Integration

**Files:**
- Create: `src/CustomerSupport.Domain/Interfaces/IEmailSender.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/MockProviders/MockEmailSender.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Channels/EmailChannelProvider.cs`
- Create: `src/CustomerSupport.API/Controllers/EmailInboundController.cs`
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `IChannelProvider`, `Conversation`, `Message`, `ChannelType.Email`, `ConversationRepository`, `AppDbContext`, `ICurrentUserService`, `IDateTimeService`, `IPublisher`, `MessageReceivedNotification`, `ConversationCreatedNotification`, `Customer` entity
- Produces:
  - `IEmailSender` interface (used by T8 EmailNotificationDispatcher)
  - `MockEmailSender` implementation
  - `EmailChannelProvider : IChannelProvider` registered for `ChannelType.Email`

- [ ] **Step 1: Create IEmailSender interface**

```csharp
// src/CustomerSupport.Domain/Interfaces/IEmailSender.cs
namespace CustomerSupport.Domain.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, string? replyToMessageId = null);
}
```

- [ ] **Step 2: Create MockEmailSender**

```csharp
// src/CustomerSupport.Infrastructure/Services/MockProviders/MockEmailSender.cs
using CustomerSupport.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services.MockProviders;

public class MockEmailSender : IEmailSender
{
    private readonly ILogger<MockEmailSender> _logger;

    public MockEmailSender(ILogger<MockEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, string? replyToMessageId = null)
    {
        _logger.LogInformation(
            "[MockEmail] To: {To}, Subject: {Subject}, ReplyTo: {ReplyTo}, Body length: {Length}",
            to, subject, replyToMessageId, htmlBody.Length);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 3: Create EmailChannelProvider**

```csharp
// src/CustomerSupport.Infrastructure/Services/Channels/EmailChannelProvider.cs
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CustomerSupport.Infrastructure.Services.Channels;

public class EmailChannelProvider : IChannelProvider
{
    private readonly AppDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IDateTimeService _dateTimeService;

    public EmailChannelProvider(AppDbContext context, IEmailSender emailSender, IDateTimeService dateTimeService)
    {
        _context = context;
        _emailSender = emailSender;
        _dateTimeService = dateTimeService;
    }

    public ChannelType Channel => ChannelType.Email;

    public async Task<Message> SendMessageAsync(Conversation conversation, string content, ContentType contentType, Guid? agentId = null)
    {
        var customer = await _context.Customers.FindAsync(conversation.CustomerId);
        var messageId = $"<{Guid.NewGuid()}@crm.local>";

        var metadata = JsonSerializer.Serialize(new
        {
            From = "support@crm.local",
            To = customer?.Email ?? "",
            Subject = conversation.Subject ?? "",
            MessageId = messageId
        });

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = conversation.TenantId,
            ConversationId = conversation.Id,
            Direction = MessageDirection.Outbound,
            SenderType = agentId.HasValue ? SenderType.Agent : SenderType.System,
            SenderId = agentId,
            Content = content,
            ContentType = contentType == ContentType.Text ? ContentType.Html : contentType,
            Channel = ChannelType.Email,
            ExternalMessageId = messageId,
            Metadata = metadata,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);

        if (string.IsNullOrEmpty(conversation.ExternalReference))
        {
            conversation.ExternalReference = messageId;
            _context.Conversations.Update(conversation);
        }

        await _context.SaveChangesAsync();

        await _emailSender.SendAsync(
            customer?.Email ?? "",
            conversation.Subject ?? "Support message",
            content,
            conversation.ExternalReference);

        return message;
    }
}
```

- [ ] **Step 4: Create EmailInboundController**

```csharp
// src/CustomerSupport.API/Controllers/EmailInboundController.cs
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CustomerSupport.API.Controllers;

public record InboundEmailDto(
    string From, string To, string Subject,
    string? HtmlBody, string? TextBody,
    string MessageId, string? InReplyTo);

[ApiController]
[Route("api/v1/webhooks/email")]
public class EmailInboundController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConversationRepository _conversationRepository;
    private readonly IPublisher _publisher;
    private readonly IDateTimeService _dateTimeService;
    private readonly IConfiguration _configuration;

    public EmailInboundController(
        AppDbContext context,
        IConversationRepository conversationRepository,
        IPublisher publisher,
        IDateTimeService dateTimeService,
        IConfiguration configuration)
    {
        _context = context;
        _conversationRepository = conversationRepository;
        _publisher = publisher;
        _dateTimeService = dateTimeService;
        _configuration = configuration;
    }

    [HttpPost("inbound")]
    public async Task<IActionResult> ReceiveEmail([FromBody] InboundEmailDto dto)
    {
        var webhookKey = Request.Headers["X-Webhook-Key"].FirstOrDefault();
        var expectedKey = _configuration["Webhooks:EmailKey"];
        if (string.IsNullOrEmpty(expectedKey) || webhookKey != expectedKey)
            return Unauthorized();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == dto.From);

        Guid tenantId;
        if (customer is null)
        {
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = dto.From.Split('@')[0],
                NameAr = dto.From.Split('@')[0],
                Email = dto.From
            };
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }
        else
        {
            tenantId = customer.TenantId;
        }

        Conversation? conversation = null;
        if (!string.IsNullOrEmpty(dto.InReplyTo))
        {
            conversation = await _conversationRepository.GetByExternalReferenceAsync(dto.InReplyTo);
        }

        var isNew = conversation is null;
        if (isNew)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CustomerId = customer.Id,
                Channel = ChannelType.Email,
                Status = ConversationStatus.Active,
                Subject = dto.Subject,
                ExternalReference = dto.MessageId
            };
            await _conversationRepository.AddAsync(conversation);
        }

        var metadata = JsonSerializer.Serialize(new
        {
            From = dto.From,
            To = dto.To,
            dto.Subject,
            dto.MessageId,
            dto.InReplyTo
        });

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConversationId = conversation!.Id,
            Direction = MessageDirection.Inbound,
            SenderType = SenderType.Customer,
            Content = dto.HtmlBody ?? dto.TextBody ?? "",
            ContentType = dto.HtmlBody is not null ? ContentType.Html : ContentType.Text,
            Channel = ChannelType.Email,
            ExternalMessageId = dto.MessageId,
            Metadata = metadata,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        if (isNew)
        {
            await _publisher.Publish(new ConversationCreatedNotification(
                conversation.Id, tenantId, ChannelType.Email));
        }

        await _publisher.Publish(new MessageReceivedNotification(
            message.Id, conversation.Id, tenantId, MessageDirection.Inbound));

        return Ok(new { conversationId = conversation.Id, messageId = message.Id });
    }
}
```

- [ ] **Step 5: Register in DI**

Add to `DependencyInjection.cs`:

```csharp
services.AddScoped<IEmailSender, MockEmailSender>();
services.AddScoped<IChannelProvider, EmailChannelProvider>();
```

Add required usings:
```csharp
using CustomerSupport.Infrastructure.Services.MockProviders;
```

- [ ] **Step 6: Add webhook config to appsettings**

Add to `src/CustomerSupport.API/appsettings.Development.json`:

```json
"Webhooks": {
  "EmailKey": "dev-email-webhook-key",
  "WhatsAppKey": "dev-whatsapp-webhook-key",
  "SmsKey": "dev-sms-webhook-key"
}
```

- [ ] **Step 7: Verify build**

Run: `dotnet build src/CustomerSupport.API/CustomerSupport.API.csproj`
Expected: 0 errors, 0 warnings.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat(email): add email channel provider with mock sender and inbound webhook"
```

---

### Task 3: WhatsApp Channel Integration

**Files:**
- Create: `src/CustomerSupport.Domain/Interfaces/IWhatsAppClient.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/MockProviders/MockWhatsAppClient.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Channels/WhatsAppChannelProvider.cs`
- Create: `src/CustomerSupport.API/Controllers/WhatsAppWebhookController.cs`
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `IChannelProvider`, `Conversation`, `Message`, `ChannelType.WhatsApp`, `ConversationRepository`, `AppDbContext`, `IDateTimeService`, `IPublisher`, `MessageReceivedNotification`, `ConversationCreatedNotification`, `Customer` entity
- Produces:
  - `IWhatsAppClient` interface
  - `MockWhatsAppClient` implementation
  - `WhatsAppChannelProvider : IChannelProvider` registered for `ChannelType.WhatsApp`

- [ ] **Step 1: Create IWhatsAppClient interface**

```csharp
// src/CustomerSupport.Domain/Interfaces/IWhatsAppClient.cs
namespace CustomerSupport.Domain.Interfaces;

public interface IWhatsAppClient
{
    Task<string> SendTextMessageAsync(string phoneNumber, string text);
    Task<string> SendMediaMessageAsync(string phoneNumber, string mediaUrl, string caption);
}
```

- [ ] **Step 2: Create MockWhatsAppClient**

```csharp
// src/CustomerSupport.Infrastructure/Services/MockProviders/MockWhatsAppClient.cs
using CustomerSupport.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services.MockProviders;

public class MockWhatsAppClient : IWhatsAppClient
{
    private readonly ILogger<MockWhatsAppClient> _logger;

    public MockWhatsAppClient(ILogger<MockWhatsAppClient> logger)
    {
        _logger = logger;
    }

    public Task<string> SendTextMessageAsync(string phoneNumber, string text)
    {
        var messageId = $"wamid.{Guid.NewGuid():N}";
        _logger.LogInformation("[MockWhatsApp] Text to {Phone}: {Text} (id: {Id})", phoneNumber, text, messageId);
        return Task.FromResult(messageId);
    }

    public Task<string> SendMediaMessageAsync(string phoneNumber, string mediaUrl, string caption)
    {
        var messageId = $"wamid.{Guid.NewGuid():N}";
        _logger.LogInformation("[MockWhatsApp] Media to {Phone}: {Url} — {Caption} (id: {Id})", phoneNumber, mediaUrl, caption, messageId);
        return Task.FromResult(messageId);
    }
}
```

- [ ] **Step 3: Create WhatsAppChannelProvider**

```csharp
// src/CustomerSupport.Infrastructure/Services/Channels/WhatsAppChannelProvider.cs
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;

namespace CustomerSupport.Infrastructure.Services.Channels;

public class WhatsAppChannelProvider : IChannelProvider
{
    private readonly AppDbContext _context;
    private readonly IWhatsAppClient _whatsAppClient;
    private readonly IDateTimeService _dateTimeService;

    public WhatsAppChannelProvider(AppDbContext context, IWhatsAppClient whatsAppClient, IDateTimeService dateTimeService)
    {
        _context = context;
        _whatsAppClient = whatsAppClient;
        _dateTimeService = dateTimeService;
    }

    public ChannelType Channel => ChannelType.WhatsApp;

    public async Task<Message> SendMessageAsync(Conversation conversation, string content, ContentType contentType, Guid? agentId = null)
    {
        var customer = await _context.Customers.FindAsync(conversation.CustomerId);
        var phone = customer?.Phone ?? conversation.ExternalReference ?? "";

        var externalId = await _whatsAppClient.SendTextMessageAsync(phone, content);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = conversation.TenantId,
            ConversationId = conversation.Id,
            Direction = MessageDirection.Outbound,
            SenderType = agentId.HasValue ? SenderType.Agent : SenderType.System,
            SenderId = agentId,
            Content = content,
            ContentType = ContentType.Text,
            Channel = ChannelType.WhatsApp,
            ExternalMessageId = externalId,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        return message;
    }
}
```

- [ ] **Step 4: Create WhatsAppWebhookController**

```csharp
// src/CustomerSupport.API/Controllers/WhatsAppWebhookController.cs
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CustomerSupport.API.Controllers;

public record WhatsAppInboundDto(
    string From, string MessageType,
    string? Text, string? MediaUrl, string? Caption,
    string MessageId);

[ApiController]
[Route("api/v1/webhooks/whatsapp")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConversationRepository _conversationRepository;
    private readonly IPublisher _publisher;
    private readonly IDateTimeService _dateTimeService;
    private readonly IConfiguration _configuration;

    public WhatsAppWebhookController(
        AppDbContext context,
        IConversationRepository conversationRepository,
        IPublisher publisher,
        IDateTimeService dateTimeService,
        IConfiguration configuration)
    {
        _context = context;
        _conversationRepository = conversationRepository;
        _publisher = publisher;
        _dateTimeService = dateTimeService;
        _configuration = configuration;
    }

    [HttpPost("inbound")]
    public async Task<IActionResult> ReceiveMessage([FromBody] WhatsAppInboundDto dto)
    {
        var webhookKey = Request.Headers["X-Webhook-Key"].FirstOrDefault();
        var expectedKey = _configuration["Webhooks:WhatsAppKey"];
        if (string.IsNullOrEmpty(expectedKey) || webhookKey != expectedKey)
            return Unauthorized();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Phone == dto.From);

        Guid tenantId;
        if (customer is null)
        {
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = dto.From,
                NameAr = dto.From,
                Phone = dto.From
            };
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }
        else
        {
            tenantId = customer.TenantId;
        }

        var conversation = await _conversationRepository.GetByExternalReferenceAsync(dto.From);

        var isNew = conversation is null;
        if (isNew)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CustomerId = customer.Id,
                Channel = ChannelType.WhatsApp,
                Status = ConversationStatus.Active,
                ExternalReference = dto.From
            };
            await _conversationRepository.AddAsync(conversation);
        }

        var metadata = dto.MediaUrl is not null
            ? JsonSerializer.Serialize(new { dto.MediaUrl, dto.Caption })
            : null;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConversationId = conversation!.Id,
            Direction = MessageDirection.Inbound,
            SenderType = SenderType.Customer,
            Content = dto.Text ?? dto.Caption ?? "",
            ContentType = ContentType.Text,
            Channel = ChannelType.WhatsApp,
            ExternalMessageId = dto.MessageId,
            Metadata = metadata,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        if (isNew)
        {
            await _publisher.Publish(new ConversationCreatedNotification(
                conversation.Id, tenantId, ChannelType.WhatsApp));
        }

        await _publisher.Publish(new MessageReceivedNotification(
            message.Id, conversation.Id, tenantId, MessageDirection.Inbound));

        return Ok(new { conversationId = conversation.Id, messageId = message.Id });
    }
}
```

- [ ] **Step 5: Register in DI**

Add to `DependencyInjection.cs`:

```csharp
services.AddScoped<IWhatsAppClient, MockWhatsAppClient>();
services.AddScoped<IChannelProvider, WhatsAppChannelProvider>();
```

- [ ] **Step 6: Verify build and commit**

Run: `dotnet build src/CustomerSupport.API/CustomerSupport.API.csproj`
Expected: 0 errors, 0 warnings.

```bash
git add -A
git commit -m "feat(whatsapp): add WhatsApp channel provider with mock client and inbound webhook"
```

---

### Task 4: Live Chat (SignalR)

**Files:**
- Create: `src/CustomerSupport.Domain/Interfaces/IChatSessionService.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/ChatSessionService.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Channels/LiveChatChannelProvider.cs`
- Create: `src/CustomerSupport.API/Hubs/ChatHub.cs`
- Create: `src/client/src/app/features/chat/chat.routes.ts`
- Create: `src/client/src/app/features/chat/chat.service.ts`
- Create: `src/client/src/app/features/chat/chat-hub.service.ts`
- Create: `src/client/src/app/features/chat/chat-console/chat-console.ts`
- Modify: `src/CustomerSupport.API/Program.cs` — add SignalR and map ChatHub
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/Seeders/PermissionSeeder.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/Seeders/RoleAndUserSeeder.cs`
- Modify: `src/client/src/app/app.routes.ts`
- Modify: `src/client/src/app/layouts/admin-layout/admin-layout.ts`
- Modify: `src/client/src/assets/i18n/en.json`
- Modify: `src/client/src/assets/i18n/ar.json`

**Interfaces:**
- Consumes: `IChannelProvider`, `Conversation`, `Message`, `ChannelType.LiveChat`, `ConversationRepository`, `AppDbContext`, `IDateTimeService`, `IPublisher`, `MessageReceivedNotification`, `ConversationCreatedNotification`, `ICurrentUserService`
- Produces:
  - `IChatSessionService` interface + `ChatSessionService` implementation
  - `LiveChatChannelProvider : IChannelProvider` for `ChannelType.LiveChat`
  - `ChatHub` SignalR hub (used by T7 chat widget, T8 may reference pattern)
  - Permissions: `chat.view`, `chat.manage`

- [ ] **Step 1: Create IChatSessionService**

```csharp
// src/CustomerSupport.Domain/Interfaces/IChatSessionService.cs
using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface IChatSessionService
{
    Task<Conversation> StartSessionAsync(Guid customerId, Guid tenantId, string? subject = null);
    Task EndSessionAsync(Guid conversationId);
    Task<int> GetQueuePositionAsync(Guid conversationId);
    Task<List<Conversation>> GetActiveSessionsAsync(Guid? agentId = null);
}
```

- [ ] **Step 2: Create ChatSessionService**

```csharp
// src/CustomerSupport.Infrastructure/Services/ChatSessionService.cs
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Services;

public class ChatSessionService : IChatSessionService
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public ChatSessionService(AppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<Conversation> StartSessionAsync(Guid customerId, Guid tenantId, string? subject = null)
    {
        var existing = await _context.Conversations
            .FirstOrDefaultAsync(c =>
                c.CustomerId == customerId &&
                c.Channel == ChannelType.LiveChat &&
                c.Status == ConversationStatus.Active);

        if (existing is not null) return existing;

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customerId,
            Channel = ChannelType.LiveChat,
            Status = ConversationStatus.Active,
            Subject = subject ?? "Live Chat"
        };

        await _context.Conversations.AddAsync(conversation);
        await _context.SaveChangesAsync();

        return conversation;
    }

    public async Task EndSessionAsync(Guid conversationId)
    {
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation is null || conversation.Status == ConversationStatus.Closed) return;

        conversation.Status = ConversationStatus.Closed;
        conversation.ClosedAt = _dateTimeService.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetQueuePositionAsync(Guid conversationId)
    {
        var unassigned = await _context.Conversations
            .Where(c => c.Channel == ChannelType.LiveChat &&
                        c.Status == ConversationStatus.Active &&
                        c.AssignedAgentId == null)
            .OrderBy(c => c.CreatedAt)
            .Select(c => c.Id)
            .ToListAsync();

        var position = unassigned.IndexOf(conversationId);
        return position >= 0 ? position + 1 : 0;
    }

    public async Task<List<Conversation>> GetActiveSessionsAsync(Guid? agentId = null)
    {
        var query = _context.Conversations
            .Include(c => c.Customer)
            .Where(c => c.Channel == ChannelType.LiveChat && c.Status == ConversationStatus.Active);

        if (agentId.HasValue)
            query = query.Where(c => c.AssignedAgentId == agentId.Value || c.AssignedAgentId == null);

        return await query.OrderByDescending(c => c.UpdatedAt).ToListAsync();
    }
}
```

- [ ] **Step 3: Create LiveChatChannelProvider**

```csharp
// src/CustomerSupport.Infrastructure/Services/Channels/LiveChatChannelProvider.cs
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;

namespace CustomerSupport.Infrastructure.Services.Channels;

public class LiveChatChannelProvider : IChannelProvider
{
    private readonly AppDbContext _context;
    private readonly IHubContext<CustomerSupport.API.Hubs.ChatHub> _hubContext;
    private readonly IDateTimeService _dateTimeService;

    public LiveChatChannelProvider(
        AppDbContext context,
        IHubContext<CustomerSupport.API.Hubs.ChatHub> hubContext,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _hubContext = hubContext;
        _dateTimeService = dateTimeService;
    }

    public ChannelType Channel => ChannelType.LiveChat;

    public async Task<Message> SendMessageAsync(Conversation conversation, string content, ContentType contentType, Guid? agentId = null)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = conversation.TenantId,
            ConversationId = conversation.Id,
            Direction = MessageDirection.Outbound,
            SenderType = agentId.HasValue ? SenderType.Agent : SenderType.System,
            SenderId = agentId,
            Content = content,
            ContentType = contentType,
            Channel = ChannelType.LiveChat,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group($"chat-{conversation.Id}")
            .SendAsync("ReceiveMessage", new
            {
                message.Id,
                message.ConversationId,
                message.Direction,
                message.SenderType,
                message.SenderId,
                message.Content,
                message.ContentType,
                message.SentAt
            });

        return message;
    }
}
```

- [ ] **Step 4: Create ChatHub**

```csharp
// src/CustomerSupport.API/Hubs/ChatHub.cs
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.API.Hubs;

[Authorize(AuthenticationSchemes = "Bearer,Portal")]
public class ChatHub : Hub
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly IPublisher _publisher;

    public ChatHub(AppDbContext context, IDateTimeService dateTimeService, IPublisher publisher)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _publisher = publisher;
    }

    public async Task JoinChat(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{conversationId}");
    }

    public async Task SendMessage(Guid conversationId, string content)
    {
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation is null) return;

        var userId = GetUserId();
        var isAgent = Context.User?.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Role) ?? false;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = conversation.TenantId,
            ConversationId = conversationId,
            Direction = isAgent ? MessageDirection.Outbound : MessageDirection.Inbound,
            SenderType = isAgent ? SenderType.Agent : SenderType.Customer,
            SenderId = userId != Guid.Empty ? userId : null,
            Content = content,
            ContentType = ContentType.Text,
            Channel = ChannelType.LiveChat,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        await Clients.Group($"chat-{conversationId}").SendAsync("ReceiveMessage", new
        {
            message.Id,
            message.ConversationId,
            message.Direction,
            message.SenderType,
            message.SenderId,
            message.Content,
            message.SentAt
        });

        await _publisher.Publish(new MessageReceivedNotification(
            message.Id, conversationId, conversation.TenantId, message.Direction));
    }

    public async Task SendTypingIndicator(Guid conversationId)
    {
        await Clients.OthersInGroup($"chat-{conversationId}")
            .SendAsync("TypingIndicator", new { ConversationId = conversationId, UserId = GetUserId() });
    }

    public async Task EndChat(Guid conversationId)
    {
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation is null) return;

        conversation.Status = ConversationStatus.Closed;
        conversation.ClosedAt = _dateTimeService.UtcNow;
        await _context.SaveChangesAsync();

        await Clients.Group($"chat-{conversationId}").SendAsync("ChatEnded", conversationId);
    }

    private Guid GetUserId()
    {
        var id = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("PortalUserId")?.Value;
        return id is not null ? Guid.Parse(id) : Guid.Empty;
    }
}
```

- [ ] **Step 5: Configure SignalR in Program.cs**

Add to `src/CustomerSupport.API/Program.cs`:

After `builder.Services.AddHealthChecks();` add:
```csharp
builder.Services.AddSignalR();
```

After `app.MapHealthChecks("/health");` add:
```csharp
app.MapHub<CustomerSupport.API.Hubs.ChatHub>("/hubs/chat");
```

- [ ] **Step 6: Register services in DI**

Add to `DependencyInjection.cs`:
```csharp
services.AddScoped<IChatSessionService, ChatSessionService>();
services.AddScoped<IChannelProvider, LiveChatChannelProvider>();
```

- [ ] **Step 7: Add permissions**

Add to `PermissionSeeder.cs`:
```csharp
("chat.view", "Chat", "View live chat"),
("chat.manage", "Chat", "Manage live chat sessions"),
```

Add `"chat.view"` to `AgentPermissions` in `RoleAndUserSeeder.cs`.

- [ ] **Step 8: Create Angular chat service**

```typescript
// src/client/src/app/features/chat/chat.service.ts
import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { Observable } from 'rxjs';

export interface ChatSessionDto {
  id: string;
  customerId: string;
  customerName: string;
  status: number;
  subject: string | null;
  assignedAgentId: string | null;
  assignedAgentName: string | null;
  messageCount: number;
  createdAt: string;
  lastMessagePreview?: string;
}

export interface ChatMessageDto {
  id: string;
  conversationId: string;
  direction: number;
  senderType: number;
  senderId: string | null;
  content: string;
  sentAt: string;
}

@Injectable({ providedIn: 'root' })
export class ChatService extends ApiService {
  getActiveSessions(): Observable<ChatSessionDto[]> {
    return this.get<ChatSessionDto[]>('/v1/conversations', {
      channel: 2, // LiveChat
      status: 0   // Active
    });
  }

  assignToMe(conversationId: string, agentId: string): Observable<any> {
    return this.put<any>(`/v1/conversations/${conversationId}/assign`, { conversationId, agentId });
  }

  endChat(conversationId: string): Observable<any> {
    return this.put<any>(`/v1/conversations/${conversationId}/close`, {});
  }
}
```

- [ ] **Step 9: Create ChatHubService**

```typescript
// src/client/src/app/features/chat/chat-hub.service.ts
import { Injectable, inject } from '@angular/core';
import { Subject } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../../environments/environment';

export interface HubMessage {
  id: string;
  conversationId: string;
  direction: number;
  senderType: number;
  senderId: string | null;
  content: string;
  sentAt: string;
}

@Injectable({ providedIn: 'root' })
export class ChatHubService {
  private connection: signalR.HubConnection | null = null;
  private authService = inject(AuthService);

  messageReceived$ = new Subject<HubMessage>();
  typingIndicator$ = new Subject<{ conversationId: string; userId: string }>();
  chatEnded$ = new Subject<string>();

  async connect(): Promise<void> {
    if (this.connection) return;

    const signalR = await import('@microsoft/signalr');
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl.replace('/api', '')}/hubs/chat`, {
        accessTokenFactory: () => this.authService.getToken() || ''
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('ReceiveMessage', (msg: HubMessage) => this.messageReceived$.next(msg));
    this.connection.on('TypingIndicator', (data: any) => this.typingIndicator$.next(data));
    this.connection.on('ChatEnded', (id: string) => this.chatEnded$.next(id));

    await this.connection.start();
  }

  async joinChat(conversationId: string): Promise<void> {
    await this.connection?.invoke('JoinChat', conversationId);
  }

  async sendMessage(conversationId: string, content: string): Promise<void> {
    await this.connection?.invoke('SendMessage', conversationId, content);
  }

  async sendTypingIndicator(conversationId: string): Promise<void> {
    await this.connection?.invoke('SendTypingIndicator', conversationId);
  }

  async endChat(conversationId: string): Promise<void> {
    await this.connection?.invoke('EndChat', conversationId);
  }

  async disconnect(): Promise<void> {
    await this.connection?.stop();
    this.connection = null;
  }
}
```

- [ ] **Step 10: Create chat console component**

```typescript
// src/client/src/app/features/chat/chat-console/chat-console.ts
import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatBadgeModule } from '@angular/material/badge';
import { MatChipsModule } from '@angular/material/chips';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ChatService, ChatSessionDto } from '../chat.service';
import { ChatHubService, HubMessage } from '../chat-hub.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-chat-console',
  imports: [
    TranslateModule, FormsModule,
    MatListModule, MatIconModule, MatButtonModule, MatInputModule,
    MatFormFieldModule, MatBadgeModule, MatChipsModule
  ],
  template: `
    <div class="chat-layout">
      <div class="chat-sidebar">
        <h3>{{ 'chat.activeSessions' | translate }}</h3>
        <mat-nav-list>
          @for (session of sessions; track session.id) {
            <a mat-list-item [class.active]="selectedId === session.id"
               (click)="selectSession(session)">
              <mat-icon matListItemIcon>person</mat-icon>
              <span matListItemTitle>{{ session.customerName }}</span>
              <span matListItemLine>{{ session.subject || ('chat.noSubject' | translate) }}</span>
              @if (!session.assignedAgentId) {
                <mat-chip color="warn" selected>{{ 'chat.unassigned' | translate }}</mat-chip>
              }
            </a>
          }
          @if (sessions.length === 0) {
            <p class="no-sessions">{{ 'chat.noSessions' | translate }}</p>
          }
        </mat-nav-list>
      </div>

      <div class="chat-main">
        @if (selectedId) {
          <div class="chat-header">
            <h3>{{ selectedSession?.customerName }}</h3>
            <div class="chat-actions">
              @if (!selectedSession?.assignedAgentId) {
                <button mat-raised-button color="primary" (click)="acceptChat()">
                  {{ 'chat.accept' | translate }}
                </button>
              }
              <button mat-raised-button color="warn" (click)="endSelectedChat()">
                {{ 'chat.endChat' | translate }}
              </button>
            </div>
          </div>

          <div class="chat-messages" #messageContainer>
            @for (msg of messages; track msg.id) {
              <div class="message" [class.outbound]="msg.direction === 1" [class.inbound]="msg.direction === 0">
                <div class="message-bubble">
                  <p>{{ msg.content }}</p>
                  <small>{{ msg.sentAt | date:'shortTime' }}</small>
                </div>
              </div>
            }
            @if (isTyping) {
              <div class="typing-indicator">{{ 'chat.typing' | translate }}</div>
            }
          </div>

          <div class="chat-input">
            <mat-form-field appearance="outline" class="full-width">
              <input matInput [(ngModel)]="newMessage"
                     (keyup.enter)="sendMessage()"
                     [placeholder]="'chat.typeMessage' | translate" />
            </mat-form-field>
            <button mat-icon-button color="primary" (click)="sendMessage()" [disabled]="!newMessage.trim()">
              <mat-icon>send</mat-icon>
            </button>
          </div>
        } @else {
          <div class="no-chat-selected">
            <mat-icon>chat</mat-icon>
            <p>{{ 'chat.selectSession' | translate }}</p>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .chat-layout { display: flex; height: calc(100vh - 128px); gap: 0; }
    .chat-sidebar { width: 300px; border-inline-end: 1px solid #e0e0e0; overflow-y: auto; }
    .chat-sidebar h3 { padding: 16px; margin: 0; }
    .chat-main { flex: 1; display: flex; flex-direction: column; }
    .chat-header { display: flex; justify-content: space-between; align-items: center; padding: 16px; border-block-end: 1px solid #e0e0e0; }
    .chat-header h3 { margin: 0; }
    .chat-actions { display: flex; gap: 8px; }
    .chat-messages { flex: 1; overflow-y: auto; padding: 16px; display: flex; flex-direction: column; gap: 8px; }
    .message { display: flex; }
    .message.outbound { justify-content: flex-end; }
    .message.inbound { justify-content: flex-start; }
    .message-bubble { max-width: 70%; padding: 8px 12px; border-radius: 12px; }
    .inbound .message-bubble { background: #f0f0f0; }
    .outbound .message-bubble { background: #e3f2fd; }
    .message-bubble p { margin: 0; }
    .message-bubble small { color: #999; font-size: 11px; }
    .typing-indicator { color: #999; font-style: italic; padding: 4px; }
    .chat-input { display: flex; align-items: center; padding: 8px 16px; border-block-start: 1px solid #e0e0e0; }
    .full-width { flex: 1; }
    .no-chat-selected { display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100%; color: #999; }
    .no-chat-selected mat-icon { font-size: 64px; width: 64px; height: 64px; }
    .no-sessions { padding: 16px; color: #999; text-align: center; }
    .active { background-color: rgba(0, 0, 0, 0.04); }
  `]
})
export class ChatConsoleComponent implements OnInit, OnDestroy {
  private chatService = inject(ChatService);
  private chatHubService = inject(ChatHubService);
  private authService = inject(AuthService);
  private subscriptions: Subscription[] = [];

  sessions: ChatSessionDto[] = [];
  selectedId: string | null = null;
  selectedSession: ChatSessionDto | null = null;
  messages: HubMessage[] = [];
  newMessage = '';
  isTyping = false;

  ngOnInit(): void {
    this.loadSessions();
    this.chatHubService.connect().then(() => {
      this.subscriptions.push(
        this.chatHubService.messageReceived$.subscribe(msg => {
          if (msg.conversationId === this.selectedId) {
            this.messages.push(msg);
          }
          this.loadSessions();
        }),
        this.chatHubService.typingIndicator$.subscribe(data => {
          if (data.conversationId === this.selectedId) {
            this.isTyping = true;
            setTimeout(() => this.isTyping = false, 3000);
          }
        }),
        this.chatHubService.chatEnded$.subscribe(id => {
          if (id === this.selectedId) {
            this.selectedId = null;
            this.selectedSession = null;
            this.messages = [];
          }
          this.loadSessions();
        })
      );
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(s => s.unsubscribe());
    this.chatHubService.disconnect();
  }

  loadSessions(): void {
    this.chatService.getActiveSessions().subscribe(sessions => this.sessions = sessions as any);
  }

  selectSession(session: ChatSessionDto): void {
    this.selectedId = session.id;
    this.selectedSession = session;
    this.messages = [];
    this.chatHubService.joinChat(session.id);
  }

  sendMessage(): void {
    if (!this.newMessage.trim() || !this.selectedId) return;
    this.chatHubService.sendMessage(this.selectedId, this.newMessage.trim());
    this.newMessage = '';
  }

  acceptChat(): void {
    if (!this.selectedId) return;
    const user = this.authService.getCurrentUser();
    if (!user) return;
    this.chatService.assignToMe(this.selectedId, user.id).subscribe(() => this.loadSessions());
  }

  endSelectedChat(): void {
    if (!this.selectedId) return;
    this.chatHubService.endChat(this.selectedId);
  }
}
```

- [ ] **Step 11: Create chat routes**

```typescript
// src/client/src/app/features/chat/chat.routes.ts
import { Routes } from '@angular/router';

export const chatRoutes: Routes = [
  { path: '', loadComponent: () => import('./chat-console/chat-console').then(m => m.ChatConsoleComponent) },
];
```

- [ ] **Step 12: Add chat route to app.routes.ts**

Add inside the `admin` children array after the `assignment` route:

```typescript
{
  path: 'chat',
  loadChildren: () => import('./features/chat/chat.routes').then(m => m.chatRoutes)
},
```

- [ ] **Step 13: Add chat nav link to admin layout**

Add to `admin-layout.ts` sidebar, after the knowledge base link:

```html
<a mat-list-item routerLink="/admin/chat" routerLinkActive="active">
  <mat-icon matListItemIcon>chat</mat-icon>
  <span>{{ 'nav.chat' | translate }}</span>
</a>
```

- [ ] **Step 14: Add i18n keys**

Add to `en.json`:

```json
"chat": {
  "title": "Live Chat",
  "activeSessions": "Active Sessions",
  "noSessions": "No active chat sessions",
  "noSubject": "No subject",
  "unassigned": "Unassigned",
  "accept": "Accept",
  "endChat": "End Chat",
  "typeMessage": "Type a message...",
  "typing": "Typing...",
  "selectSession": "Select a chat session to start"
}
```

Add `"chat": "Live Chat"` to the `nav` section.

Add to `ar.json`:

```json
"chat": {
  "title": "المحادثة المباشرة",
  "activeSessions": "الجلسات النشطة",
  "noSessions": "لا توجد جلسات محادثة نشطة",
  "noSubject": "بدون موضوع",
  "unassigned": "غير مسندة",
  "accept": "قبول",
  "endChat": "إنهاء المحادثة",
  "typeMessage": "اكتب رسالة...",
  "typing": "يكتب...",
  "selectSession": "اختر جلسة محادثة للبدء"
}
```

Add `"chat": "المحادثة المباشرة"` to the `nav` section.

- [ ] **Step 15: Install SignalR client package**

Run from `src/client/`:
```bash
npm install @microsoft/signalr
```

- [ ] **Step 16: Verify builds**

```bash
dotnet build src/CustomerSupport.API/CustomerSupport.API.csproj
cd src/client && npx ng build
```
Expected: both 0 errors.

- [ ] **Step 17: Commit**

```bash
git add -A
git commit -m "feat(chat): add live chat with SignalR hub, agent console, and session management"
```

---

### Task 5: SMS Channel Integration

**Files:**
- Create: `src/CustomerSupport.Domain/Interfaces/ISmsClient.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/MockProviders/MockSmsClient.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Channels/SmsChannelProvider.cs`
- Create: `src/CustomerSupport.API/Controllers/SmsWebhookController.cs`
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `IChannelProvider`, `Conversation`, `Message`, `ChannelType.SMS`, `ConversationRepository`, `AppDbContext`, `IDateTimeService`, `IPublisher`, `MessageReceivedNotification`, `ConversationCreatedNotification`, `Customer` entity
- Produces:
  - `ISmsClient` interface (used by T8 SmsNotificationDispatcher)
  - `MockSmsClient` implementation
  - `SmsChannelProvider : IChannelProvider` registered for `ChannelType.SMS`

- [ ] **Step 1: Create ISmsClient interface**

```csharp
// src/CustomerSupport.Domain/Interfaces/ISmsClient.cs
namespace CustomerSupport.Domain.Interfaces;

public interface ISmsClient
{
    Task<string> SendAsync(string phoneNumber, string message);
}
```

- [ ] **Step 2: Create MockSmsClient**

```csharp
// src/CustomerSupport.Infrastructure/Services/MockProviders/MockSmsClient.cs
using CustomerSupport.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services.MockProviders;

public class MockSmsClient : ISmsClient
{
    private readonly ILogger<MockSmsClient> _logger;

    public MockSmsClient(ILogger<MockSmsClient> logger)
    {
        _logger = logger;
    }

    public Task<string> SendAsync(string phoneNumber, string message)
    {
        var messageId = $"sms-{Guid.NewGuid():N}";
        _logger.LogInformation("[MockSMS] To: {Phone}, Message: {Message} (id: {Id})", phoneNumber, message, messageId);
        return Task.FromResult(messageId);
    }
}
```

- [ ] **Step 3: Create SmsChannelProvider**

```csharp
// src/CustomerSupport.Infrastructure/Services/Channels/SmsChannelProvider.cs
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;

namespace CustomerSupport.Infrastructure.Services.Channels;

public class SmsChannelProvider : IChannelProvider
{
    private readonly AppDbContext _context;
    private readonly ISmsClient _smsClient;
    private readonly IDateTimeService _dateTimeService;

    public SmsChannelProvider(AppDbContext context, ISmsClient smsClient, IDateTimeService dateTimeService)
    {
        _context = context;
        _smsClient = smsClient;
        _dateTimeService = dateTimeService;
    }

    public ChannelType Channel => ChannelType.SMS;

    public async Task<Message> SendMessageAsync(Conversation conversation, string content, ContentType contentType, Guid? agentId = null)
    {
        var customer = await _context.Customers.FindAsync(conversation.CustomerId);
        var phone = customer?.Phone ?? conversation.ExternalReference ?? "";

        var externalId = await _smsClient.SendAsync(phone, content);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = conversation.TenantId,
            ConversationId = conversation.Id,
            Direction = MessageDirection.Outbound,
            SenderType = agentId.HasValue ? SenderType.Agent : SenderType.System,
            SenderId = agentId,
            Content = content,
            ContentType = ContentType.Text,
            Channel = ChannelType.SMS,
            ExternalMessageId = externalId,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        return message;
    }
}
```

- [ ] **Step 4: Create SmsWebhookController**

```csharp
// src/CustomerSupport.API/Controllers/SmsWebhookController.cs
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.API.Controllers;

public record SmsInboundDto(string From, string Body, string MessageId);

[ApiController]
[Route("api/v1/webhooks/sms")]
public class SmsWebhookController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConversationRepository _conversationRepository;
    private readonly IPublisher _publisher;
    private readonly IDateTimeService _dateTimeService;
    private readonly IConfiguration _configuration;

    public SmsWebhookController(
        AppDbContext context,
        IConversationRepository conversationRepository,
        IPublisher publisher,
        IDateTimeService dateTimeService,
        IConfiguration configuration)
    {
        _context = context;
        _conversationRepository = conversationRepository;
        _publisher = publisher;
        _dateTimeService = dateTimeService;
        _configuration = configuration;
    }

    [HttpPost("inbound")]
    public async Task<IActionResult> ReceiveMessage([FromBody] SmsInboundDto dto)
    {
        var webhookKey = Request.Headers["X-Webhook-Key"].FirstOrDefault();
        var expectedKey = _configuration["Webhooks:SmsKey"];
        if (string.IsNullOrEmpty(expectedKey) || webhookKey != expectedKey)
            return Unauthorized();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Phone == dto.From);

        Guid tenantId;
        if (customer is null)
        {
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = dto.From,
                NameAr = dto.From,
                Phone = dto.From
            };
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }
        else
        {
            tenantId = customer.TenantId;
        }

        var conversation = await _conversationRepository.GetByExternalReferenceAsync(dto.From);

        var isNew = conversation is null;
        if (isNew)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CustomerId = customer.Id,
                Channel = ChannelType.SMS,
                Status = ConversationStatus.Active,
                ExternalReference = dto.From
            };
            await _conversationRepository.AddAsync(conversation);
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConversationId = conversation!.Id,
            Direction = MessageDirection.Inbound,
            SenderType = SenderType.Customer,
            Content = dto.Body,
            ContentType = ContentType.Text,
            Channel = ChannelType.SMS,
            ExternalMessageId = dto.MessageId,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        if (isNew)
        {
            await _publisher.Publish(new ConversationCreatedNotification(
                conversation.Id, tenantId, ChannelType.SMS));
        }

        await _publisher.Publish(new MessageReceivedNotification(
            message.Id, conversation.Id, tenantId, MessageDirection.Inbound));

        return Ok(new { conversationId = conversation.Id, messageId = message.Id });
    }
}
```

- [ ] **Step 5: Register in DI**

Add to `DependencyInjection.cs`:

```csharp
services.AddScoped<ISmsClient, MockSmsClient>();
services.AddScoped<IChannelProvider, SmsChannelProvider>();
```

- [ ] **Step 6: Verify build and commit**

```bash
dotnet build src/CustomerSupport.API/CustomerSupport.API.csproj
git add -A
git commit -m "feat(sms): add SMS channel provider with mock client and inbound webhook"
```

---

### Task 6: Customer Portal Backend

**Files:**
- Create: `src/CustomerSupport.Domain/Entities/PortalUser.cs`
- Create: `src/CustomerSupport.Domain/Interfaces/IPortalTokenService.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/PortalUserConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/PortalTokenService.cs`
- Create: `src/CustomerSupport.Application/Portal/DTOs/PortalUserDto.cs`
- Create: `src/CustomerSupport.Application/Portal/DTOs/PortalLoginRequest.cs`
- Create: `src/CustomerSupport.Application/Portal/DTOs/PortalRegisterRequest.cs`
- Create: `src/CustomerSupport.Application/Portal/DTOs/PortalTokenResponse.cs`
- Create: `src/CustomerSupport.Application/Portal/DTOs/PortalTicketRequest.cs`
- Create: `src/CustomerSupport.Application/Portal/DTOs/PortalTicketDto.cs`
- Create: `src/CustomerSupport.Application/Portal/Commands/PortalLoginCommand.cs`
- Create: `src/CustomerSupport.Application/Portal/Commands/PortalRegisterCommand.cs`
- Create: `src/CustomerSupport.Application/Portal/Commands/PortalSubmitTicketCommand.cs`
- Create: `src/CustomerSupport.Application/Portal/Commands/PortalAddCommentCommand.cs`
- Create: `src/CustomerSupport.Application/Portal/Commands/PortalUpdateProfileCommand.cs`
- Create: `src/CustomerSupport.Application/Portal/Queries/GetPortalTicketsQuery.cs`
- Create: `src/CustomerSupport.Application/Portal/Queries/GetPortalTicketByIdQuery.cs`
- Create: `src/CustomerSupport.Application/Portal/Queries/GetPortalProfileQuery.cs`
- Create: `src/CustomerSupport.Application/Portal/Validators/PortalLoginValidator.cs`
- Create: `src/CustomerSupport.Application/Portal/Validators/PortalRegisterValidator.cs`
- Create: `src/CustomerSupport.Application/Portal/Validators/PortalSubmitTicketValidator.cs`
- Create: `src/CustomerSupport.Application/Portal/Validators/PortalAddCommentValidator.cs`
- Create: `src/CustomerSupport.Application/Portal/Validators/PortalUpdateProfileValidator.cs`
- Create: `src/CustomerSupport.API/Controllers/PortalAuthController.cs`
- Create: `src/CustomerSupport.API/Controllers/PortalController.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs`
- Modify: `src/CustomerSupport.API/Program.cs`

**Interfaces:**
- Consumes: `ITenantEntity`, `Customer`, `Ticket`, `TicketCategory`, `TicketPriority`, `TicketStatus`, `Conversation`, `Message`, `ChannelType.Portal`, `AppDbContext`, `IDateTimeService`, `IPublisher`, `TicketCreatedNotification`, `IConversationRepository`, `ITicketRepository`, `KnowledgeArticle`, `KnowledgeCategory`, `IConfiguration`, `PaginatedList<T>`
- Produces:
  - `PortalUser` entity
  - `IPortalTokenService` + `PortalTokenService` (JWT with audience="portal")
  - `PortalAuthController` — `/api/v1/portal/auth/register`, `/login`, `/refresh`
  - `PortalController` — `/api/v1/portal/tickets`, `/knowledge`, `/profile`
  - Dual JWT scheme in Program.cs (`Bearer` + `Portal`)

- [ ] **Step 1: Create PortalUser entity**

```csharp
// src/CustomerSupport.Domain/Entities/PortalUser.cs
namespace CustomerSupport.Domain.Entities;

public class PortalUser : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Customer? Customer { get; set; }
    public Tenant? Tenant { get; set; }
}
```

- [ ] **Step 2: Create IPortalTokenService**

```csharp
// src/CustomerSupport.Domain/Interfaces/IPortalTokenService.cs
using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface IPortalTokenService
{
    (string AccessToken, string RefreshToken) GenerateTokens(PortalUser user);
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
```

- [ ] **Step 3: Create PortalUserConfiguration**

```csharp
// src/CustomerSupport.Infrastructure/Persistence/Configurations/PortalUserConfiguration.cs
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class PortalUserConfiguration : IEntityTypeConfiguration<PortalUser>
{
    public void Configure(EntityTypeBuilder<PortalUser> builder)
    {
        builder.ToTable("PortalUsers");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Email).IsRequired().HasMaxLength(256);
        builder.Property(p => p.PasswordHash).IsRequired();
        builder.Property(p => p.FullName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.FullNameAr).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Phone).HasMaxLength(50);

        builder.HasIndex(p => new { p.TenantId, p.Email }).IsUnique();

        builder.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Tenant).WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(p => p.IsActive);
    }
}
```

- [ ] **Step 4: Add PortalUser DbSet and exclude from automatic tenant filter**

Add to `AppDbContext.cs` DbSets:
```csharp
public DbSet<PortalUser> PortalUsers => Set<PortalUser>();
```

In `OnModelCreating`, add `typeof(PortalUser)` to the `identityTypes` exclusion array:
```csharp
var identityTypes = new[] { typeof(ApplicationUser), typeof(ApplicationRole), typeof(PortalUser) };
```

- [ ] **Step 5: Create PortalTokenService**

```csharp
// src/CustomerSupport.Infrastructure/Services/PortalTokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CustomerSupport.Infrastructure.Services;

public class PortalTokenService : IPortalTokenService
{
    private readonly IConfiguration _configuration;

    public PortalTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string AccessToken, string RefreshToken) GenerateTokens(PortalUser user)
    {
        var claims = new List<Claim>
        {
            new("PortalUserId", user.Id.ToString()),
            new("CustomerId", user.CustomerId.ToString()),
            new("TenantId", user.TenantId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("FullName", user.FullName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"]!);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: "portal",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var refreshToken = Convert.ToBase64String(randomBytes);

        return (accessToken, refreshToken);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!);
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["JwtSettings:Issuer"],
            ValidAudience = "portal",
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateLifetime = false
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtToken ||
            !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            return null;

        return principal;
    }
}
```

- [ ] **Step 6: Install BCrypt NuGet package**

Run:
```bash
dotnet add src/CustomerSupport.Infrastructure/CustomerSupport.Infrastructure.csproj package BCrypt.Net-Next
```

- [ ] **Step 7: Create DTOs**

```csharp
// src/CustomerSupport.Application/Portal/DTOs/PortalUserDto.cs
namespace CustomerSupport.Application.Portal.DTOs;

public record PortalUserDto(Guid Id, string Email, string FullName, string FullNameAr, string? Phone, Guid CustomerId);
```

```csharp
// src/CustomerSupport.Application/Portal/DTOs/PortalLoginRequest.cs
namespace CustomerSupport.Application.Portal.DTOs;

public record PortalLoginRequest(string Email, string Password);
```

```csharp
// src/CustomerSupport.Application/Portal/DTOs/PortalRegisterRequest.cs
namespace CustomerSupport.Application.Portal.DTOs;

public record PortalRegisterRequest(string Email, string Password, string FullName, string FullNameAr, string? Phone);
```

```csharp
// src/CustomerSupport.Application/Portal/DTOs/PortalTokenResponse.cs
namespace CustomerSupport.Application.Portal.DTOs;

public record PortalTokenResponse(string AccessToken, string RefreshToken, PortalUserDto User);
```

```csharp
// src/CustomerSupport.Application/Portal/DTOs/PortalTicketRequest.cs
namespace CustomerSupport.Application.Portal.DTOs;

public record PortalTicketRequest(Guid CategoryId, Guid PriorityId, string Subject, string Description);
```

```csharp
// src/CustomerSupport.Application/Portal/DTOs/PortalTicketDto.cs
namespace CustomerSupport.Application.Portal.DTOs;

public record PortalTicketDto(
    Guid Id, string TicketNumber, string Subject,
    string CategoryName, string PriorityName, string StatusName,
    DateTime CreatedAt, DateTime UpdatedAt);

public record PortalTicketDetailDto(
    Guid Id, string TicketNumber, string Subject, string Description,
    string CategoryName, string PriorityName, string StatusName,
    DateTime CreatedAt, DateTime UpdatedAt,
    List<PortalCommentDto> Comments);

public record PortalCommentDto(Guid Id, string Content, string AuthorName, DateTime CreatedAt, bool IsAgent);
```

- [ ] **Step 8: Create commands with handlers**

```csharp
// src/CustomerSupport.Application/Portal/Commands/PortalLoginCommand.cs
using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Commands;

public record PortalLoginCommand(string Email, string Password) : IRequest<PortalTokenResponse>;

public class PortalLoginCommandHandler : IRequestHandler<PortalLoginCommand, PortalTokenResponse>
{
    private readonly AppDbContext _context;
    private readonly IPortalTokenService _tokenService;

    public PortalLoginCommandHandler(AppDbContext context, IPortalTokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<PortalTokenResponse> Handle(PortalLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.PortalUsers
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid email or password");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        var (accessToken, refreshToken) = _tokenService.GenerateTokens(user);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(cancellationToken);

        return new PortalTokenResponse(accessToken, refreshToken,
            new PortalUserDto(user.Id, user.Email, user.FullName, user.FullNameAr, user.Phone, user.CustomerId));
    }
}
```

```csharp
// src/CustomerSupport.Application/Portal/Commands/PortalRegisterCommand.cs
using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Commands;

public record PortalRegisterCommand(
    string Email, string Password,
    string FullName, string FullNameAr, string? Phone) : IRequest<PortalTokenResponse>;

public class PortalRegisterCommandHandler : IRequestHandler<PortalRegisterCommand, PortalTokenResponse>
{
    private readonly AppDbContext _context;
    private readonly IPortalTokenService _tokenService;
    private readonly IDateTimeService _dateTimeService;

    public PortalRegisterCommandHandler(
        AppDbContext context, IPortalTokenService tokenService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tokenService = tokenService;
        _dateTimeService = dateTimeService;
    }

    public async Task<PortalTokenResponse> Handle(PortalRegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.PortalUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existing is not null)
            throw new InvalidOperationException("A user with this email already exists");

        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == request.Email && c.TenantId == tenantId, cancellationToken);

        if (customer is null)
        {
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = request.FullName,
                NameAr = request.FullNameAr,
                Email = request.Email,
                Phone = request.Phone
            };
            await _context.Customers.AddAsync(customer, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var portalUser = new PortalUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customer.Id,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            FullNameAr = request.FullNameAr,
            Phone = request.Phone,
            IsActive = true,
            CreatedAt = _dateTimeService.UtcNow,
            UpdatedAt = _dateTimeService.UtcNow
        };

        await _context.PortalUsers.AddAsync(portalUser, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var (accessToken, refreshToken) = _tokenService.GenerateTokens(portalUser);

        portalUser.RefreshToken = refreshToken;
        portalUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(cancellationToken);

        return new PortalTokenResponse(accessToken, refreshToken,
            new PortalUserDto(portalUser.Id, portalUser.Email, portalUser.FullName, portalUser.FullNameAr, portalUser.Phone, portalUser.CustomerId));
    }
}
```

```csharp
// src/CustomerSupport.Application/Portal/Commands/PortalSubmitTicketCommand.cs
using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Portal.Commands;

public record PortalSubmitTicketCommand(
    Guid CustomerId, Guid TenantId,
    Guid CategoryId, Guid PriorityId,
    string Subject, string Description) : IRequest<PortalTicketDto>;

public class PortalSubmitTicketCommandHandler : IRequestHandler<PortalSubmitTicketCommand, PortalTicketDto>
{
    private readonly AppDbContext _context;
    private readonly ITicketRepository _ticketRepository;
    private readonly IPublisher _publisher;
    private readonly ILogger<PortalSubmitTicketCommandHandler> _logger;

    public PortalSubmitTicketCommandHandler(
        AppDbContext context, ITicketRepository ticketRepository,
        IPublisher publisher, ILogger<PortalSubmitTicketCommandHandler> logger)
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<PortalTicketDto> Handle(PortalSubmitTicketCommand request, CancellationToken cancellationToken)
    {
        var newStatus = await _context.TicketStatuses
            .FirstOrDefaultAsync(s => s.Name == "New", cancellationToken)
            ?? throw new KeyNotFoundException("Default ticket status 'New' not found");

        var ticketNumber = await _ticketRepository.GenerateTicketNumberAsync(cancellationToken);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            TicketNumber = ticketNumber,
            CustomerId = request.CustomerId,
            CategoryId = request.CategoryId,
            PriorityId = request.PriorityId,
            StatusId = newStatus.Id,
            Subject = request.Subject,
            Description = request.Description
        };

        await _ticketRepository.AddAsync(ticket, cancellationToken);

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            CustomerId = request.CustomerId,
            TicketId = ticket.Id,
            Channel = ChannelType.Portal,
            Status = ConversationStatus.Active,
            Subject = request.Subject
        };
        await _context.Conversations.AddAsync(conversation, cancellationToken);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ConversationId = conversation.Id,
            Direction = MessageDirection.Inbound,
            SenderType = SenderType.Customer,
            Content = request.Description,
            ContentType = ContentType.Text,
            Channel = ChannelType.Portal,
            SentAt = DateTime.UtcNow
        };
        await _context.Messages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _publisher.Publish(new TicketCreatedNotification(
                ticket.Id, ticket.TenantId, ticket.PriorityId, ticket.CategoryId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish TicketCreatedNotification for portal ticket {TicketId}", ticket.Id);
        }

        var category = await _context.TicketCategories.FindAsync([request.CategoryId], cancellationToken);
        var priority = await _context.TicketPriorities.FindAsync([request.PriorityId], cancellationToken);

        return new PortalTicketDto(
            ticket.Id, ticket.TicketNumber, ticket.Subject,
            category?.Name ?? "", priority?.Name ?? "", newStatus.Name,
            ticket.CreatedAt, ticket.UpdatedAt);
    }
}
```

```csharp
// src/CustomerSupport.Application/Portal/Commands/PortalAddCommentCommand.cs
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Commands;

public record PortalAddCommentCommand(
    Guid TicketId, Guid CustomerId, Guid TenantId,
    string Content) : IRequest<PortalCommentDto>;

public class PortalAddCommentCommandHandler : IRequestHandler<PortalAddCommentCommand, PortalCommentDto>
{
    private readonly AppDbContext _context;
    private readonly IPublisher _publisher;

    public PortalAddCommentCommandHandler(AppDbContext context, IPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<PortalCommentDto> Handle(PortalAddCommentCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.CustomerId == request.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Ticket not found");

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.TicketId == request.TicketId, cancellationToken);

        if (conversation is not null)
        {
            var message = new Message
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ConversationId = conversation.Id,
                Direction = MessageDirection.Inbound,
                SenderType = SenderType.Customer,
                Content = request.Content,
                ContentType = ContentType.Text,
                Channel = conversation.Channel,
                SentAt = DateTime.UtcNow
            };
            await _context.Messages.AddAsync(message, cancellationToken);

            await _publisher.Publish(new MessageReceivedNotification(
                message.Id, conversation.Id, request.TenantId, MessageDirection.Inbound), cancellationToken);
        }

        var customer = await _context.Customers.FindAsync([request.CustomerId], cancellationToken);

        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            UserId = Guid.Empty,
            Content = request.Content,
            IsInternal = false
        };
        await _context.TicketComments.AddAsync(comment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new PortalCommentDto(comment.Id, comment.Content, customer?.Name ?? "", comment.CreatedAt, false);
    }
}
```

```csharp
// src/CustomerSupport.Application/Portal/Commands/PortalUpdateProfileCommand.cs
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Commands;

public record PortalUpdateProfileCommand(
    Guid PortalUserId, string FullName, string FullNameAr,
    string? Phone, string? NewPassword) : IRequest<Result>;

public class PortalUpdateProfileCommandHandler : IRequestHandler<PortalUpdateProfileCommand, Result>
{
    private readonly AppDbContext _context;

    public PortalUpdateProfileCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(PortalUpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.PortalUsers
            .FirstOrDefaultAsync(u => u.Id == request.PortalUserId, cancellationToken);

        if (user is null) return Result.Failure(["User not found"]);

        user.FullName = request.FullName;
        user.FullNameAr = request.FullNameAr;
        user.Phone = request.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.NewPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

- [ ] **Step 9: Create queries**

```csharp
// src/CustomerSupport.Application/Portal/Queries/GetPortalTicketsQuery.cs
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Portal.Queries;

public record GetPortalTicketsQuery(
    Guid CustomerId, int Page = 1, int PageSize = 20) : IRequest<PaginatedList<PortalTicketDto>>;

public class GetPortalTicketsQueryHandler : IRequestHandler<GetPortalTicketsQuery, PaginatedList<PortalTicketDto>>
{
    private readonly AppDbContext _context;

    public GetPortalTicketsQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<PortalTicketDto>> Handle(GetPortalTicketsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Tickets
            .Where(t => t.CustomerId == request.CustomerId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new PortalTicketDto(
                t.Id, t.TicketNumber, t.Subject,
                t.Category!.Name, t.Priority!.Name, t.Status!.Name,
                t.CreatedAt, t.UpdatedAt));

        return await PaginatedList<PortalTicketDto>.CreateAsync(query, request.Page, request.PageSize, cancellationToken);
    }
}
```

```csharp
// src/CustomerSupport.Application/Portal/Queries/GetPortalTicketByIdQuery.cs
using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Queries;

public record GetPortalTicketByIdQuery(Guid TicketId, Guid CustomerId) : IRequest<PortalTicketDetailDto?>;

public class GetPortalTicketByIdQueryHandler : IRequestHandler<GetPortalTicketByIdQuery, PortalTicketDetailDto?>
{
    private readonly AppDbContext _context;

    public GetPortalTicketByIdQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PortalTicketDetailDto?> Handle(GetPortalTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .Include(t => t.Status)
            .Include(t => t.Comments.Where(c => !c.IsInternal).OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.CustomerId == request.CustomerId, cancellationToken);

        if (ticket is null) return null;

        var comments = ticket.Comments.Select(c => new PortalCommentDto(
            c.Id, c.Content,
            c.UserId != Guid.Empty ? (c.User?.FullName ?? "Agent") : "You",
            c.CreatedAt,
            c.UserId != Guid.Empty)).ToList();

        return new PortalTicketDetailDto(
            ticket.Id, ticket.TicketNumber, ticket.Subject, ticket.Description,
            ticket.Category?.Name ?? "", ticket.Priority?.Name ?? "", ticket.Status?.Name ?? "",
            ticket.CreatedAt, ticket.UpdatedAt, comments);
    }
}
```

```csharp
// src/CustomerSupport.Application/Portal/Queries/GetPortalProfileQuery.cs
using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Queries;

public record GetPortalProfileQuery(Guid PortalUserId) : IRequest<PortalUserDto?>;

public class GetPortalProfileQueryHandler : IRequestHandler<GetPortalProfileQuery, PortalUserDto?>
{
    private readonly AppDbContext _context;

    public GetPortalProfileQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PortalUserDto?> Handle(GetPortalProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.PortalUsers
            .FirstOrDefaultAsync(u => u.Id == request.PortalUserId, cancellationToken);

        return user is null ? null : new PortalUserDto(
            user.Id, user.Email, user.FullName, user.FullNameAr, user.Phone, user.CustomerId);
    }
}
```

- [ ] **Step 10: Create validators**

```csharp
// src/CustomerSupport.Application/Portal/Validators/PortalLoginValidator.cs
using CustomerSupport.Application.Portal.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Portal.Validators;

public class PortalLoginValidator : AbstractValidator<PortalLoginCommand>
{
    public PortalLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
```

```csharp
// src/CustomerSupport.Application/Portal/Validators/PortalRegisterValidator.cs
using CustomerSupport.Application.Portal.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Portal.Validators;

public class PortalRegisterValidator : AbstractValidator<PortalRegisterCommand>
{
    public PortalRegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FullNameAr).NotEmpty().MaximumLength(200);
    }
}
```

```csharp
// src/CustomerSupport.Application/Portal/Validators/PortalSubmitTicketValidator.cs
using CustomerSupport.Application.Portal.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Portal.Validators;

public class PortalSubmitTicketValidator : AbstractValidator<PortalSubmitTicketCommand>
{
    public PortalSubmitTicketValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.PriorityId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).NotEmpty();
    }
}
```

```csharp
// src/CustomerSupport.Application/Portal/Validators/PortalAddCommentValidator.cs
using CustomerSupport.Application.Portal.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Portal.Validators;

public class PortalAddCommentValidator : AbstractValidator<PortalAddCommentCommand>
{
    public PortalAddCommentValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
    }
}
```

```csharp
// src/CustomerSupport.Application/Portal/Validators/PortalUpdateProfileValidator.cs
using CustomerSupport.Application.Portal.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Portal.Validators;

public class PortalUpdateProfileValidator : AbstractValidator<PortalUpdateProfileCommand>
{
    public PortalUpdateProfileValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FullNameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NewPassword).MinimumLength(8).When(x => !string.IsNullOrEmpty(x.NewPassword));
    }
}
```

- [ ] **Step 11: Create PortalAuthController**

```csharp
// src/CustomerSupport.API/Controllers/PortalAuthController.cs
using CustomerSupport.Application.Portal.Commands;
using CustomerSupport.Application.Portal.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/portal/auth")]
public class PortalAuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public PortalAuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<PortalTokenResponse>> Register(PortalRegisterRequest request)
    {
        var result = await _mediator.Send(new PortalRegisterCommand(
            request.Email, request.Password, request.FullName, request.FullNameAr, request.Phone));
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<PortalTokenResponse>> Login(PortalLoginRequest request)
    {
        var result = await _mediator.Send(new PortalLoginCommand(request.Email, request.Password));
        return Ok(result);
    }
}
```

- [ ] **Step 12: Create PortalController**

```csharp
// src/CustomerSupport.API/Controllers/PortalController.cs
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Portal.Commands;
using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Application.Portal.Queries;
using CustomerSupport.Application.Conversations.DTOs;
using CustomerSupport.Application.Conversations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/portal")]
[Authorize(AuthenticationSchemes = "Portal")]
public class PortalController : ControllerBase
{
    private readonly IMediator _mediator;

    public PortalController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetCustomerId() =>
        Guid.Parse(User.FindFirst("CustomerId")!.Value);

    private Guid GetPortalUserId() =>
        Guid.Parse(User.FindFirst("PortalUserId")!.Value);

    private Guid GetTenantId() =>
        Guid.Parse(User.FindFirst("TenantId")!.Value);

    [HttpGet("tickets")]
    public async Task<ActionResult<PaginatedList<PortalTicketDto>>> GetTickets(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _mediator.Send(new GetPortalTicketsQuery(GetCustomerId(), page, pageSize)));
    }

    [HttpPost("tickets")]
    public async Task<ActionResult<PortalTicketDto>> SubmitTicket(PortalTicketRequest request)
    {
        var result = await _mediator.Send(new PortalSubmitTicketCommand(
            GetCustomerId(), GetTenantId(),
            request.CategoryId, request.PriorityId,
            request.Subject, request.Description));
        return CreatedAtAction(nameof(GetTicket), new { id = result.Id }, result);
    }

    [HttpGet("tickets/{id:guid}")]
    public async Task<ActionResult<PortalTicketDetailDto>> GetTicket(Guid id)
    {
        var result = await _mediator.Send(new GetPortalTicketByIdQuery(id, GetCustomerId()));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("tickets/{ticketId:guid}/comments")]
    public async Task<ActionResult<PortalCommentDto>> AddComment(Guid ticketId, [FromBody] PortalAddCommentRequest request)
    {
        var result = await _mediator.Send(new PortalAddCommentCommand(
            ticketId, GetCustomerId(), GetTenantId(), request.Content));
        return Ok(result);
    }

    [HttpGet("knowledge")]
    public async Task<ActionResult<PaginatedList<object>>> SearchKnowledge(
        [FromQuery] string? term, [FromQuery] string? categoryId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        // Reuse existing knowledge query, filter to published only
        return Ok(await _mediator.Send(new CustomerSupport.Application.Knowledge.Queries.GetArticlesQuery(
            categoryId is not null ? Guid.Parse(categoryId) : null, term, true, page, pageSize)));
    }

    [HttpGet("profile")]
    public async Task<ActionResult<PortalUserDto>> GetProfile()
    {
        var result = await _mediator.Send(new GetPortalProfileQuery(GetPortalUserId()));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<Result>> UpdateProfile(PortalUpdateProfileRequest request)
    {
        var result = await _mediator.Send(new PortalUpdateProfileCommand(
            GetPortalUserId(), request.FullName, request.FullNameAr, request.Phone, request.NewPassword));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}

public record PortalAddCommentRequest(string Content);
public record PortalUpdateProfileRequest(string FullName, string FullNameAr, string? Phone, string? NewPassword);
```

- [ ] **Step 13: Configure dual JWT scheme in Program.cs**

Replace the existing JWT authentication block in `Program.cs` with:

```csharp
var jwtKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = jwtKey
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
})
.AddJwtBearer("Portal", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = "portal",
        IssuerSigningKey = jwtKey
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});
```

- [ ] **Step 14: Register services in DI**

Add to `DependencyInjection.cs`:
```csharp
services.AddScoped<IPortalTokenService, PortalTokenService>();
```

- [ ] **Step 15: Create and apply EF migration**

```bash
dotnet ef migrations add AddPortalUser --project src/CustomerSupport.Infrastructure --startup-project src/CustomerSupport.API --output-dir Persistence/Migrations
```

- [ ] **Step 16: Verify build and commit**

```bash
dotnet build src/CustomerSupport.API/CustomerSupport.API.csproj
git add -A
git commit -m "feat(portal): add customer portal backend with PortalUser auth, ticket submission, and knowledge access"
```

---

### Task 7: Customer Portal UI

**Files:**
- Create: `src/client/src/app/core/guards/portal-auth.guard.ts`
- Create: `src/client/src/app/features/portal/portal.routes.ts`
- Create: `src/client/src/app/features/portal/portal-auth.service.ts`
- Create: `src/client/src/app/features/portal/portal-api.service.ts`
- Create: `src/client/src/app/features/portal/portal-ticket.service.ts`
- Create: `src/client/src/app/features/portal/portal-knowledge.service.ts`
- Create: `src/client/src/app/features/portal/portal-login/portal-login.ts`
- Create: `src/client/src/app/features/portal/portal-register/portal-register.ts`
- Create: `src/client/src/app/features/portal/portal-home/portal-home.ts`
- Create: `src/client/src/app/features/portal/portal-ticket-list/portal-ticket-list.ts`
- Create: `src/client/src/app/features/portal/portal-ticket-form/portal-ticket-form.ts`
- Create: `src/client/src/app/features/portal/portal-ticket-detail/portal-ticket-detail.ts`
- Create: `src/client/src/app/features/portal/portal-knowledge-list/portal-knowledge-list.ts`
- Create: `src/client/src/app/features/portal/portal-knowledge-viewer/portal-knowledge-viewer.ts`
- Create: `src/client/src/app/features/portal/portal-profile/portal-profile.ts`
- Create: `src/client/src/app/shared/components/chat-widget/chat-widget.ts`
- Modify: `src/client/src/app/app.routes.ts`
- Modify: `src/client/src/app/layouts/portal-layout/portal-layout.ts`
- Modify: `src/client/src/app/core/interceptors/auth.interceptor.ts`
- Modify: `src/client/src/assets/i18n/en.json`
- Modify: `src/client/src/assets/i18n/ar.json`

**Interfaces:**
- Consumes: `ApiService`, `AuthService` (pattern), `PaginatedList`, portal API endpoints from T6 (`/api/v1/portal/*`), `ChatHubService` from T4, `LanguageService`, Angular Material, `@ngx-translate`, `TicketCategory`, `TicketPriority` reference data from Tickets API
- Produces: Complete portal UI at `/portal/*` routes

This task is large — it creates 9 page components, 4 services, 1 guard, 1 chat widget, routes, and i18n. The code for each component follows the patterns established in Phase 1-2 (standalone components, `inject()`, Angular Material, translate pipe, `@for`/`@if` control flow).

**Due to the size of this task, the implementer should follow these patterns exactly:**
- Services: extend `ApiService` pattern (see `knowledge.service.ts`)
- Auth service: follow `AuthService` pattern but with `portal-` prefixed localStorage keys
- Guard: follow `authGuard` pattern but check portal token and redirect to `/portal/login`
- Components: follow `LoginComponent` pattern (standalone, inline template, Angular Material)
- The portal-layout must be updated to add nav links and a user menu (logout button)
- The auth interceptor must be updated to also attach Portal JWT when the URL contains `/portal/`

- [ ] **Step 1: Create PortalAuthService**

```typescript
// src/client/src/app/features/portal/portal-auth.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

export interface PortalLoginRequest { email: string; password: string; }
export interface PortalRegisterRequest { email: string; password: string; fullName: string; fullNameAr: string; phone?: string; }
export interface PortalUserInfo { id: string; email: string; fullName: string; fullNameAr: string; phone: string | null; customerId: string; }
export interface PortalTokenResponse { accessToken: string; refreshToken: string; user: PortalUserInfo; }

@Injectable({ providedIn: 'root' })
export class PortalAuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly TOKEN_KEY = 'portal-access-token';
  private readonly REFRESH_KEY = 'portal-refresh-token';
  private readonly USER_KEY = 'portal-user';

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());
  private currentUserSubject = new BehaviorSubject<PortalUserInfo | null>(this.getStoredUser());

  isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  currentUser$ = this.currentUserSubject.asObservable();

  login(request: PortalLoginRequest): Observable<PortalTokenResponse> {
    return this.http.post<PortalTokenResponse>(`${environment.apiUrl}/v1/portal/auth/login`, request).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  register(request: PortalRegisterRequest): Observable<PortalTokenResponse> {
    return this.http.post<PortalTokenResponse>(`${environment.apiUrl}/v1/portal/auth/register`, request).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.isAuthenticatedSubject.next(false);
    this.currentUserSubject.next(null);
    this.router.navigate(['/portal/login']);
  }

  getToken(): string | null { return localStorage.getItem(this.TOKEN_KEY); }
  isAuthenticated(): boolean { return this.hasToken(); }
  getCurrentUser(): PortalUserInfo | null { return this.currentUserSubject.value; }

  private hasToken(): boolean { return !!localStorage.getItem(this.TOKEN_KEY); }
  private getStoredUser(): PortalUserInfo | null {
    const stored = localStorage.getItem(this.USER_KEY);
    return stored ? JSON.parse(stored) : null;
  }
  private storeTokens(response: PortalTokenResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.accessToken);
    localStorage.setItem(this.REFRESH_KEY, response.refreshToken);
    localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));
    this.isAuthenticatedSubject.next(true);
    this.currentUserSubject.next(response.user);
  }
}
```

- [ ] **Step 2: Create PortalApiService, PortalTicketService, PortalKnowledgeService**

```typescript
// src/client/src/app/features/portal/portal-api.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PortalApiService {
  protected http = inject(HttpClient);
  protected baseUrl = environment.apiUrl;

  protected get<T>(path: string, params?: Record<string, any>): Observable<T> {
    let httpParams = new HttpParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== null && value !== undefined && value !== '')
          httpParams = httpParams.set(key, value.toString());
      });
    }
    return this.http.get<T>(`${this.baseUrl}${path}`, { params: httpParams });
  }

  protected post<T>(path: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}${path}`, body);
  }

  protected put<T>(path: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}${path}`, body);
  }
}
```

```typescript
// src/client/src/app/features/portal/portal-ticket.service.ts
import { Injectable } from '@angular/core';
import { PortalApiService } from './portal-api.service';
import { PaginatedList } from '../../core/models/paginated-list.model';
import { Observable } from 'rxjs';

export interface PortalTicketDto {
  id: string; ticketNumber: string; subject: string;
  categoryName: string; priorityName: string; statusName: string;
  createdAt: string; updatedAt: string;
}

export interface PortalTicketDetailDto extends PortalTicketDto {
  description: string; comments: PortalCommentDto[];
}

export interface PortalCommentDto {
  id: string; content: string; authorName: string; createdAt: string; isAgent: boolean;
}

export interface PortalTicketRequest {
  categoryId: string; priorityId: string; subject: string; description: string;
}

@Injectable({ providedIn: 'root' })
export class PortalTicketService extends PortalApiService {
  getTickets(page = 1, pageSize = 20): Observable<PaginatedList<PortalTicketDto>> {
    return this.get<PaginatedList<PortalTicketDto>>('/v1/portal/tickets', { page, pageSize });
  }

  getTicketById(id: string): Observable<PortalTicketDetailDto> {
    return this.get<PortalTicketDetailDto>(`/v1/portal/tickets/${id}`);
  }

  submitTicket(request: PortalTicketRequest): Observable<PortalTicketDto> {
    return this.post<PortalTicketDto>('/v1/portal/tickets', request);
  }

  addComment(ticketId: string, content: string): Observable<PortalCommentDto> {
    return this.post<PortalCommentDto>(`/v1/portal/tickets/${ticketId}/comments`, { content });
  }
}
```

```typescript
// src/client/src/app/features/portal/portal-knowledge.service.ts
import { Injectable } from '@angular/core';
import { PortalApiService } from './portal-api.service';
import { PaginatedList } from '../../core/models/paginated-list.model';
import { Observable } from 'rxjs';

export interface PortalArticleDto {
  id: string; title: string; titleAr: string;
  categoryName: string; tags: string | null; viewCount: number; createdAt: string;
}

export interface PortalArticleDetailDto extends PortalArticleDto {
  content: string; contentAr: string;
}

@Injectable({ providedIn: 'root' })
export class PortalKnowledgeService extends PortalApiService {
  searchArticles(term?: string, categoryId?: string, page = 1, pageSize = 20): Observable<PaginatedList<PortalArticleDto>> {
    return this.get<PaginatedList<PortalArticleDto>>('/v1/portal/knowledge', { term, categoryId, page, pageSize });
  }

  getArticleById(id: string): Observable<PortalArticleDetailDto> {
    return this.get<PortalArticleDetailDto>(`/v1/knowledge/articles/${id}`);
  }
}
```

- [ ] **Step 3: Create portal auth guard**

```typescript
// src/client/src/app/core/guards/portal-auth.guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PortalAuthService } from '../../features/portal/portal-auth.service';

export const portalAuthGuard: CanActivateFn = () => {
  const portalAuthService = inject(PortalAuthService);
  const router = inject(Router);

  if (portalAuthService.isAuthenticated()) return true;
  return router.createUrlTree(['/portal/login']);
};
```

- [ ] **Step 4: Update auth interceptor to handle portal JWT**

Replace `src/client/src/app/core/interceptors/auth.interceptor.ts`:

```typescript
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { PortalAuthService } from '../../features/portal/portal-auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const portalAuthService = inject(PortalAuthService);

  const isPortalRequest = req.url.includes('/portal/') || req.url.includes('/hubs/chat');
  const token = isPortalRequest ? portalAuthService.getToken() : authService.getToken();

  if (!token) {
    const fallbackToken = isPortalRequest ? authService.getToken() : portalAuthService.getToken();
    if (fallbackToken) {
      req = req.clone({ setHeaders: { Authorization: `Bearer ${fallbackToken}` } });
    }
    return next(req);
  }

  req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  return next(req);
};
```

- [ ] **Step 5: Create portal page components**

Create the following components following the `LoginComponent` pattern (standalone, inline template, Angular Material, translate pipe). Each component should be in its own file under the portal feature module.

The implementer must create all 9 components listed in the Files section. Key patterns:
- Login/Register: `ReactiveFormsModule`, `MatCard`, `MatFormField`, `MatInput`, `MatButton`, form validation, call `PortalAuthService`, navigate to `/portal/home`
- Home: search bar calling `PortalKnowledgeService.searchArticles()`, "Submit Ticket" button linking to `/portal/tickets/new`, "My Tickets" button linking to `/portal/tickets`
- Ticket list: `MatTable` with `PortalTicketService.getTickets()`, status chips, pagination, row click navigates to detail
- Ticket form: `ReactiveFormsModule` with category/priority dropdowns (load from tickets API ref data), subject, description fields
- Ticket detail: ticket info header, comments thread (`@for` loop), add comment form at bottom
- Knowledge list: search input, article cards/list, pagination
- Knowledge viewer: article title, content rendered with `[innerHTML]`
- Profile: form with fullName, fullNameAr, phone, newPassword fields, save button

Each component uses `inject()` for services and translates all text via `{{ 'portal.key' | translate }}`.

- [ ] **Step 6: Create portal routes**

```typescript
// src/client/src/app/features/portal/portal.routes.ts
import { Routes } from '@angular/router';
import { portalAuthGuard } from '../../core/guards/portal-auth.guard';

export const portalRoutes: Routes = [
  { path: 'login', loadComponent: () => import('./portal-login/portal-login').then(m => m.PortalLoginComponent) },
  { path: 'register', loadComponent: () => import('./portal-register/portal-register').then(m => m.PortalRegisterComponent) },
  {
    path: '',
    canActivate: [portalAuthGuard],
    children: [
      { path: 'home', loadComponent: () => import('./portal-home/portal-home').then(m => m.PortalHomeComponent) },
      { path: 'tickets', loadComponent: () => import('./portal-ticket-list/portal-ticket-list').then(m => m.PortalTicketListComponent) },
      { path: 'tickets/new', loadComponent: () => import('./portal-ticket-form/portal-ticket-form').then(m => m.PortalTicketFormComponent) },
      { path: 'tickets/:id', loadComponent: () => import('./portal-ticket-detail/portal-ticket-detail').then(m => m.PortalTicketDetailComponent) },
      { path: 'knowledge', loadComponent: () => import('./portal-knowledge-list/portal-knowledge-list').then(m => m.PortalKnowledgeListComponent) },
      { path: 'knowledge/:id', loadComponent: () => import('./portal-knowledge-viewer/portal-knowledge-viewer').then(m => m.PortalKnowledgeViewerComponent) },
      { path: 'profile', loadComponent: () => import('./portal-profile/portal-profile').then(m => m.PortalProfileComponent) },
      { path: '', redirectTo: 'home', pathMatch: 'full' },
    ]
  }
];
```

- [ ] **Step 7: Update app.routes.ts portal section**

Replace the portal route children with:
```typescript
{
  path: 'portal',
  loadComponent: () => import('./layouts/portal-layout/portal-layout').then(m => m.PortalLayoutComponent),
  children: [
    {
      path: '',
      loadChildren: () => import('./features/portal/portal.routes').then(m => m.portalRoutes)
    }
  ]
},
```

- [ ] **Step 8: Update PortalLayout with nav and user menu**

Update `portal-layout.ts` to add navigation links (Home, My Tickets, Knowledge, Profile) and a user menu (logout button) in the toolbar. Add the chat widget component.

- [ ] **Step 9: Create chat widget for portal**

```typescript
// src/client/src/app/shared/components/chat-widget/chat-widget.ts
// A floating chat button (bottom-right) that opens a chat panel.
// Uses ChatHubService from the chat feature to connect to SignalR.
// Shows queue position while waiting for agent, real-time messaging.
// Minimizable, persists across portal page navigation.
```

The implementer must create a standalone component with a FAB button that toggles a chat panel overlay. The panel connects to `ChatHub` via `ChatHubService`, starts a session via REST, and provides real-time messaging.

- [ ] **Step 10: Add i18n keys**

Add `portal` key to both `en.json` and `ar.json` with all portal-related translations (login, register, home, tickets, knowledge, profile, chat widget — approximately 40 keys per language).

Add `"conversations": "Conversations"` to `nav` in `en.json` and `"conversations": "المحادثات"` in `ar.json`.

- [ ] **Step 11: Verify builds and commit**

```bash
dotnet build src/CustomerSupport.API/CustomerSupport.API.csproj
cd src/client && npx ng build
git add -A
git commit -m "feat(portal-ui): add customer portal with login, tickets, knowledge, profile, and chat widget"
```

---

### Task 8: Notification Infrastructure

**Files:**
- Create: `src/CustomerSupport.Domain/Enums/RecipientType.cs`
- Create: `src/CustomerSupport.Domain/Entities/NotificationTemplate.cs`
- Create: `src/CustomerSupport.Domain/Entities/Notification.cs`
- Create: `src/CustomerSupport.Domain/Interfaces/INotificationService.cs`
- Create: `src/CustomerSupport.Domain/Interfaces/INotificationDispatcher.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/NotificationTemplateConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Seeders/NotificationTemplateSeeder.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/NotificationService.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Dispatchers/InAppNotificationDispatcher.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Dispatchers/EmailNotificationDispatcher.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Dispatchers/SmsNotificationDispatcher.cs`
- Create: `src/CustomerSupport.Application/Notifications/DTOs/NotificationDto.cs`
- Create: `src/CustomerSupport.Application/Notifications/DTOs/NotificationRecipient.cs`
- Create: `src/CustomerSupport.Application/Notifications/Commands/MarkNotificationReadCommand.cs`
- Create: `src/CustomerSupport.Application/Notifications/Commands/MarkAllNotificationsReadCommand.cs`
- Create: `src/CustomerSupport.Application/Notifications/Queries/GetNotificationsQuery.cs`
- Create: `src/CustomerSupport.Application/Notifications/Queries/GetUnreadCountQuery.cs`
- Create: `src/CustomerSupport.Application/Notifications/Handlers/TicketCreatedNotifyHandler.cs`
- Create: `src/CustomerSupport.Application/Notifications/Handlers/MessageReceivedNotifyHandler.cs`
- Create: `src/CustomerSupport.API/Controllers/NotificationsController.cs`
- Create: `src/CustomerSupport.API/Hubs/NotificationHub.cs`
- Create: `src/client/src/app/features/notifications/notifications.service.ts`
- Create: `src/client/src/app/features/notifications/notification-hub.service.ts`
- Create: `src/client/src/app/features/notifications/notification-bell/notification-bell.ts`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/Seeders/PermissionSeeder.cs`
- Modify: `src/CustomerSupport.API/Program.cs`
- Modify: `src/client/src/app/layouts/admin-layout/admin-layout.ts`
- Modify: `src/client/src/assets/i18n/en.json`
- Modify: `src/client/src/assets/i18n/ar.json`

**Interfaces:**
- Consumes: `IEmailSender` (from T2), `ISmsClient` (from T5), `IHubContext<NotificationHub>` (SignalR), `AppDbContext`, `IDateTimeService`, `ICurrentUserService`, `TicketCreatedNotification` (existing), `MessageReceivedNotification` (from T1), `Ticket`, `Conversation`, `Message`
- Produces:
  - `NotificationTemplate` entity, `Notification` entity, `RecipientType` enum
  - `INotificationService` + `NotificationService`
  - `INotificationDispatcher` + 3 implementations (InApp, Email, SMS)
  - `NotificationHub` SignalR hub
  - `NotificationsController` API
  - Notification bell component for admin toolbar
  - Permission: `notifications.view`

- [ ] **Step 1: Create RecipientType enum**

```csharp
// src/CustomerSupport.Domain/Enums/RecipientType.cs
namespace CustomerSupport.Domain.Enums;

public enum RecipientType
{
    Agent = 0,
    PortalUser = 1
}
```

- [ ] **Step 2: Create entities**

```csharp
// src/CustomerSupport.Domain/Entities/NotificationTemplate.cs
namespace CustomerSupport.Domain.Entities;

public class NotificationTemplate : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string SubjectAr { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string BodyTemplateAr { get; set; } = string.Empty;
    public string Channels { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
}
```

```csharp
// src/CustomerSupport.Domain/Entities/Notification.cs
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Entities;

public class Notification : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public RecipientType RecipientType { get; set; }
    public Guid RecipientId { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string BodyAr { get; set; } = string.Empty;
    public string? Data { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
```

- [ ] **Step 3: Create domain interfaces**

```csharp
// src/CustomerSupport.Domain/Interfaces/INotificationService.cs
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Interfaces;

public interface INotificationService
{
    Task SendAsync(Guid tenantId, string templateKey, NotificationRecipientInfo recipient, Dictionary<string, string> placeholders, string? data = null);
}

public record NotificationRecipientInfo(Guid Id, RecipientType Type, string? Email = null, string? Phone = null);
```

```csharp
// src/CustomerSupport.Domain/Interfaces/INotificationDispatcher.cs
using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface INotificationDispatcher
{
    string Channel { get; }
    Task DispatchAsync(Notification notification, NotificationRecipientInfo recipient);
}
```

- [ ] **Step 4: Create EF configurations**

```csharp
// src/CustomerSupport.Infrastructure/Persistence/Configurations/NotificationTemplateConfiguration.cs
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Key).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Subject).IsRequired().HasMaxLength(500);
        builder.Property(n => n.SubjectAr).IsRequired().HasMaxLength(500);
        builder.Property(n => n.BodyTemplate).IsRequired();
        builder.Property(n => n.BodyTemplateAr).IsRequired();

        builder.HasIndex(n => new { n.TenantId, n.Key }).IsUnique();
    }
}
```

```csharp
// src/CustomerSupport.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.TemplateKey).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(500);
        builder.Property(n => n.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(n => n.Body).IsRequired();
        builder.Property(n => n.BodyAr).IsRequired();

        builder.HasIndex(n => new { n.RecipientId, n.IsRead });
        builder.HasIndex(n => new { n.TenantId, n.RecipientId, n.CreatedAt });
    }
}
```

- [ ] **Step 5: Add DbSets**

Add to `AppDbContext.cs`:
```csharp
public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
public DbSet<Notification> Notifications => Set<Notification>();
```

- [ ] **Step 6: Create NotificationService**

```csharp
// src/CustomerSupport.Infrastructure/Services/NotificationService.cs
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
```

- [ ] **Step 7: Create dispatchers**

```csharp
// src/CustomerSupport.Infrastructure/Services/Dispatchers/InAppNotificationDispatcher.cs
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CustomerSupport.Infrastructure.Services.Dispatchers;

public class InAppNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<CustomerSupport.API.Hubs.NotificationHub> _hubContext;

    public InAppNotificationDispatcher(IHubContext<CustomerSupport.API.Hubs.NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public string Channel => "InApp";

    public async Task DispatchAsync(Notification notification, NotificationRecipientInfo recipient)
    {
        await _hubContext.Clients.Group($"notifications-{recipient.Id}")
            .SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.TitleAr,
                notification.Body,
                notification.BodyAr,
                notification.Data,
                notification.CreatedAt
            });
    }
}
```

```csharp
// src/CustomerSupport.Infrastructure/Services/Dispatchers/EmailNotificationDispatcher.cs
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;

namespace CustomerSupport.Infrastructure.Services.Dispatchers;

public class EmailNotificationDispatcher : INotificationDispatcher
{
    private readonly IEmailSender _emailSender;

    public EmailNotificationDispatcher(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public string Channel => "Email";

    public async Task DispatchAsync(Notification notification, NotificationRecipientInfo recipient)
    {
        if (string.IsNullOrEmpty(recipient.Email)) return;
        await _emailSender.SendAsync(recipient.Email, notification.Title, notification.Body);
    }
}
```

```csharp
// src/CustomerSupport.Infrastructure/Services/Dispatchers/SmsNotificationDispatcher.cs
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;

namespace CustomerSupport.Infrastructure.Services.Dispatchers;

public class SmsNotificationDispatcher : INotificationDispatcher
{
    private readonly ISmsClient _smsClient;

    public SmsNotificationDispatcher(ISmsClient smsClient)
    {
        _smsClient = smsClient;
    }

    public string Channel => "SMS";

    public async Task DispatchAsync(Notification notification, NotificationRecipientInfo recipient)
    {
        if (string.IsNullOrEmpty(recipient.Phone)) return;
        await _smsClient.SendAsync(recipient.Phone, notification.Body);
    }
}
```

- [ ] **Step 8: Create NotificationHub**

```csharp
// src/CustomerSupport.API/Hubs/NotificationHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CustomerSupport.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"notifications-{userId}");
        }
        await base.OnConnectedAsync();
    }
}
```

Map in `Program.cs` after ChatHub mapping:
```csharp
app.MapHub<CustomerSupport.API.Hubs.NotificationHub>("/hubs/notifications");
```

- [ ] **Step 9: Create DTOs and application layer**

```csharp
// src/CustomerSupport.Application/Notifications/DTOs/NotificationDto.cs
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Notifications.DTOs;

public record NotificationDto(
    Guid Id, string Title, string TitleAr,
    string Body, string BodyAr, string? Data,
    bool IsRead, DateTime CreatedAt, DateTime? ReadAt);
```

```csharp
// src/CustomerSupport.Application/Notifications/Commands/MarkNotificationReadCommand.cs
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Notifications.Commands;

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly AppDbContext _context;

    public MarkNotificationReadCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId, cancellationToken);

        if (notification is null) return Result.Failure(["Notification not found"]);

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

```csharp
// src/CustomerSupport.Application/Notifications/Commands/MarkAllNotificationsReadCommand.cs
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Notifications.Commands;

public record MarkAllNotificationsReadCommand : IRequest<Result>;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public MarkAllNotificationsReadCommandHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var unread = await _context.Notifications
            .Where(n => n.RecipientId == _currentUserService.UserId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

```csharp
// src/CustomerSupport.Application/Notifications/Queries/GetNotificationsQuery.cs
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Notifications.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Notifications.Queries;

public record GetNotificationsQuery(
    bool? IsRead, int Page = 1, int PageSize = 20) : IRequest<PaginatedList<NotificationDto>>;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, PaginatedList<NotificationDto>>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationsQueryHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Notifications
            .Where(n => n.RecipientId == _currentUserService.UserId);

        if (request.IsRead.HasValue)
            query = query.Where(n => n.IsRead == request.IsRead.Value);

        var projected = query.OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.TitleAr, n.Body, n.BodyAr,
                n.Data, n.IsRead, n.CreatedAt, n.ReadAt));

        return await PaginatedList<NotificationDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
```

```csharp
// src/CustomerSupport.Application/Notifications/Queries/GetUnreadCountQuery.cs
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Notifications.Queries;

public record GetUnreadCountQuery : IRequest<int>;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadCountQueryHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .CountAsync(n => n.RecipientId == _currentUserService.UserId && !n.IsRead, cancellationToken);
    }
}
```

- [ ] **Step 10: Create MediatR notification handlers that trigger notifications**

```csharp
// src/CustomerSupport.Application/Notifications/Handlers/TicketCreatedNotifyHandler.cs
using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Notifications.Handlers;

public class TicketCreatedNotifyHandler : INotificationHandler<TicketCreatedNotification>
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<TicketCreatedNotifyHandler> _logger;

    public TicketCreatedNotifyHandler(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<TicketCreatedNotifyHandler> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(TicketCreatedNotification notification, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Priority)
            .FirstOrDefaultAsync(t => t.Id == notification.TicketId, cancellationToken);

        if (ticket is null || !ticket.AssignedToId.HasValue) return;

        var agent = await _context.Users.FindAsync([ticket.AssignedToId.Value], cancellationToken);
        if (agent is null) return;

        try
        {
            await _notificationService.SendAsync(
                notification.TenantId,
                "ticket.created",
                new NotificationRecipientInfo(agent.Id, RecipientType.Agent, agent.Email, agent.PhoneNumber),
                new Dictionary<string, string>
                {
                    ["ticketNumber"] = ticket.TicketNumber,
                    ["subject"] = ticket.Subject,
                    ["customerName"] = ticket.Customer?.Name ?? "",
                    ["priority"] = ticket.Priority?.Name ?? ""
                },
                System.Text.Json.JsonSerializer.Serialize(new { ticketId = ticket.Id }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket.created notification for {TicketId}", ticket.Id);
        }
    }
}
```

```csharp
// src/CustomerSupport.Application/Notifications/Handlers/MessageReceivedNotifyHandler.cs
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Notifications.Handlers;

public class MessageReceivedNotifyHandler : INotificationHandler<MessageReceivedNotification>
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MessageReceivedNotifyHandler> _logger;

    public MessageReceivedNotifyHandler(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<MessageReceivedNotifyHandler> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Direction != MessageDirection.Inbound) return;

        var conversation = await _context.Conversations
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == notification.ConversationId, cancellationToken);

        if (conversation is null || !conversation.AssignedAgentId.HasValue) return;

        var agent = await _context.Users.FindAsync([conversation.AssignedAgentId.Value], cancellationToken);
        if (agent is null) return;

        try
        {
            await _notificationService.SendAsync(
                notification.TenantId,
                "conversation.new_message",
                new NotificationRecipientInfo(agent.Id, RecipientType.Agent, agent.Email, agent.PhoneNumber),
                new Dictionary<string, string>
                {
                    ["customerName"] = conversation.Customer?.Name ?? "",
                    ["channel"] = conversation.Channel.ToString()
                },
                System.Text.Json.JsonSerializer.Serialize(new { conversationId = conversation.Id }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send conversation.new_message notification for {ConversationId}", conversation.Id);
        }
    }
}
```

- [ ] **Step 11: Create NotificationTemplateSeeder**

```csharp
// src/CustomerSupport.Infrastructure/Persistence/Seeders/NotificationTemplateSeeder.cs
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
```

- [ ] **Step 12: Create NotificationsController**

```csharp
// src/CustomerSupport.API/Controllers/NotificationsController.cs
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Notifications.Commands;
using CustomerSupport.Application.Notifications.DTOs;
using CustomerSupport.Application.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<NotificationDto>>> GetNotifications(
        [FromQuery] bool? isRead, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _mediator.Send(new GetNotificationsQuery(isRead, page, pageSize)));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        return Ok(await _mediator.Send(new GetUnreadCountQuery()));
    }

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<Result>> MarkAsRead(Guid id)
    {
        var result = await _mediator.Send(new MarkNotificationReadCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<Result>> MarkAllAsRead()
    {
        return Ok(await _mediator.Send(new MarkAllNotificationsReadCommand()));
    }
}
```

- [ ] **Step 13: Register services in DI and add permissions**

Add to `DependencyInjection.cs`:
```csharp
services.AddScoped<INotificationService, NotificationService>();
services.AddScoped<INotificationDispatcher, InAppNotificationDispatcher>();
services.AddScoped<INotificationDispatcher, EmailNotificationDispatcher>();
services.AddScoped<INotificationDispatcher, SmsNotificationDispatcher>();
```

Add to `PermissionSeeder.cs`:
```csharp
("notifications.view", "Notifications", "View notifications"),
```

Add `"notifications.view"` to `AgentPermissions` in `RoleAndUserSeeder.cs`.

- [ ] **Step 14: Add seeder call in Program.cs**

After `RoleAndUserSeeder.SeedAsync(...)`:
```csharp
await NotificationTemplateSeeder.SeedAsync(db, DefaultTenantSeeder.DefaultTenantId);
```

- [ ] **Step 15: Create EF migration**

```bash
dotnet ef migrations add AddNotifications --project src/CustomerSupport.Infrastructure --startup-project src/CustomerSupport.API --output-dir Persistence/Migrations
```

- [ ] **Step 16: Create Angular notification services and bell component**

```typescript
// src/client/src/app/features/notifications/notifications.service.ts
import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { PaginatedList } from '../../core/models/paginated-list.model';
import { Observable } from 'rxjs';

export interface NotificationDto {
  id: string; title: string; titleAr: string;
  body: string; bodyAr: string; data: string | null;
  isRead: boolean; createdAt: string; readAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class NotificationsService extends ApiService {
  getNotifications(isRead?: boolean, page = 1, pageSize = 10): Observable<PaginatedList<NotificationDto>> {
    return this.get<PaginatedList<NotificationDto>>('/v1/notifications', { isRead, page, pageSize });
  }

  getUnreadCount(): Observable<number> {
    return this.get<number>('/v1/notifications/unread-count');
  }

  markAsRead(id: string): Observable<any> {
    return this.put<any>(`/v1/notifications/${id}/read`, {});
  }

  markAllAsRead(): Observable<any> {
    return this.put<any>('/v1/notifications/read-all', {});
  }
}
```

```typescript
// src/client/src/app/features/notifications/notification-hub.service.ts
import { Injectable, inject } from '@angular/core';
import { Subject } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class NotificationHubService {
  private connection: signalR.HubConnection | null = null;
  private authService = inject(AuthService);

  notificationReceived$ = new Subject<any>();

  async connect(): Promise<void> {
    if (this.connection) return;

    const signalR = await import('@microsoft/signalr');
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl.replace('/api', '')}/hubs/notifications`, {
        accessTokenFactory: () => this.authService.getToken() || ''
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('ReceiveNotification', (notification: any) =>
      this.notificationReceived$.next(notification));

    await this.connection.start();
  }

  async disconnect(): Promise<void> {
    await this.connection?.stop();
    this.connection = null;
  }
}
```

```typescript
// src/client/src/app/features/notifications/notification-bell/notification-bell.ts
import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { Subscription } from 'rxjs';
import { NotificationsService, NotificationDto } from '../notifications.service';
import { NotificationHubService } from '../notification-hub.service';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-notification-bell',
  imports: [
    TranslateModule, MatIconModule, MatButtonModule,
    MatBadgeModule, MatMenuModule, MatListModule, MatDividerModule
  ],
  template: `
    <button mat-icon-button [matMenuTriggerFor]="notificationMenu"
            [matBadge]="unreadCount > 0 ? unreadCount : null" matBadgeColor="warn" matBadgeSize="small">
      <mat-icon>notifications</mat-icon>
    </button>

    <mat-menu #notificationMenu="matMenu" class="notification-menu">
      <div class="notification-header" (click)="$event.stopPropagation()">
        <span>{{ 'notifications.title' | translate }}</span>
        @if (unreadCount > 0) {
          <button mat-button color="primary" (click)="markAllRead()">
            {{ 'notifications.markAllRead' | translate }}
          </button>
        }
      </div>
      <mat-divider></mat-divider>
      @for (n of notifications; track n.id) {
        <button mat-menu-item [class.unread]="!n.isRead" (click)="onNotificationClick(n)">
          <div class="notification-item">
            <strong>{{ isArabic ? n.titleAr : n.title }}</strong>
            <small>{{ n.createdAt | date:'short' }}</small>
          </div>
        </button>
      }
      @if (notifications.length === 0) {
        <div class="no-notifications" (click)="$event.stopPropagation()">
          {{ 'notifications.noNotifications' | translate }}
        </div>
      }
    </mat-menu>
  `,
  styles: [`
    .notification-header { display: flex; justify-content: space-between; align-items: center; padding: 8px 16px; }
    .notification-item { display: flex; flex-direction: column; }
    .notification-item strong { font-size: 13px; }
    .notification-item small { color: #999; font-size: 11px; }
    .unread { background-color: rgba(25, 118, 210, 0.04); }
    .no-notifications { padding: 16px; text-align: center; color: #999; }
  `]
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  private notificationsService = inject(NotificationsService);
  private notificationHub = inject(NotificationHubService);
  private languageService = inject(LanguageService);
  private subscription?: Subscription;

  notifications: NotificationDto[] = [];
  unreadCount = 0;
  get isArabic(): boolean { return this.languageService.getCurrentLanguage() === 'ar'; }

  ngOnInit(): void {
    this.loadNotifications();
    this.notificationHub.connect().then(() => {
      this.subscription = this.notificationHub.notificationReceived$.subscribe(() => {
        this.loadNotifications();
      });
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
    this.notificationHub.disconnect();
  }

  loadNotifications(): void {
    this.notificationsService.getNotifications(undefined, 1, 10).subscribe(result => {
      this.notifications = result.items;
    });
    this.notificationsService.getUnreadCount().subscribe(count => {
      this.unreadCount = count;
    });
  }

  onNotificationClick(notification: NotificationDto): void {
    if (!notification.isRead) {
      this.notificationsService.markAsRead(notification.id).subscribe(() => this.loadNotifications());
    }
  }

  markAllRead(): void {
    this.notificationsService.markAllAsRead().subscribe(() => this.loadNotifications());
  }
}
```

- [ ] **Step 17: Add notification bell to admin layout**

In `admin-layout.ts`, add `NotificationBellComponent` to imports and add `<app-notification-bell />` in the toolbar before the language button:

```html
<mat-toolbar color="primary">
  <span class="spacer"></span>
  <app-notification-bell />
  <button mat-icon-button (click)="toggleLanguage()">
    <mat-icon>language</mat-icon>
  </button>
  <button mat-icon-button (click)="logout()">
    <mat-icon>logout</mat-icon>
  </button>
</mat-toolbar>
```

- [ ] **Step 18: Add i18n keys for notifications**

Add to `en.json`:
```json
"notifications": {
  "title": "Notifications",
  "markAllRead": "Mark all read",
  "noNotifications": "No notifications"
}
```

Add to `ar.json`:
```json
"notifications": {
  "title": "الإشعارات",
  "markAllRead": "تحديد الكل كمقروء",
  "noNotifications": "لا توجد إشعارات"
}
```

- [ ] **Step 19: Verify builds**

```bash
dotnet build src/CustomerSupport.API/CustomerSupport.API.csproj
cd src/client && npx ng build
```
Expected: both 0 errors.

- [ ] **Step 20: Commit**

```bash
git add -A
git commit -m "feat(notifications): add notification infrastructure with in-app, email, SMS dispatch and bell component"
```
