# Phase 3 — Channels & Portal: Design Specification

**Date:** 2026-09-02
**Status:** Approved
**Author:** Afnan Sabry / Claude
**Parent Spec:** `docs/superpowers/specs/2026-08-31-customer-support-crm-design.md`

---

## 1. Overview

Phase 3 adds multi-channel communication (email, WhatsApp, live chat, SMS), a customer self-service portal, and a unified notification infrastructure to the Customer Support CRM. It builds on the ticket and SLA foundation from Phases 1–2.

**Key decisions:**
- Full channel abstractions with mock/stub providers (swap in real providers via DI, zero code changes)
- Separate `PortalUser` entity with its own JWT audience (isolated from internal Identity)
- Dedicated agent chat console for live chat (not embedded in ticket detail)

## 2. Scope

| Task | Description |
|------|-------------|
| P3.1 | Conversation & Message Domain — unified channel abstraction |
| P3.2 | Email Channel Integration — inbound/outbound with mock sender |
| P3.3 | WhatsApp Channel Integration — stub API client |
| P3.4 | Live Chat — SignalR hub, agent console, customer widget |
| P3.5 | SMS Channel Integration — stub client |
| P3.6 | Customer Portal Backend — PortalUser auth, ticket submission, knowledge access |
| P3.7 | Customer Portal UI — public portal pages |
| P3.8 | Notification Infrastructure — in-app (SignalR), email, SMS dispatch |

**Dependencies:**
- P3.1 first (foundation for all channels)
- P3.2, P3.3, P3.4, P3.5 each independent (sequential execution, no inter-channel dependencies)
- P3.6 → P3.7 (portal backend before UI)
- P3.8 last (consumes IEmailSender from P3.2, ISmsClient from P3.5, needs SignalR from P3.4)

## 3. Conversation & Message Domain (P3.1)

### 3.1 Entities

**Conversation:**

| Column | Type | Notes |
|--------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | FK→Tenant, global query filter |
| CustomerId | Guid | FK→Customer |
| TicketId | Guid? | FK→Ticket, nullable (conversation can precede ticket) |
| Channel | int (enum) | ChannelType: Email=0, WhatsApp=1, LiveChat=2, SMS=3, Portal=4 |
| Status | int (enum) | ConversationStatus: Active=0, Closed=1, Archived=2 |
| Subject | string? | Optional subject (mainly for email) |
| ExternalReference | string? | Channel-specific thread ID (email Message-ID, WhatsApp chat ID, phone number) |
| AssignedAgentId | Guid? | FK→ApplicationUser |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime | |
| ClosedAt | DateTime? | |

Navigation: `Customer`, `Ticket`, `AssignedAgent`, `Messages` (collection).

**Message:**

| Column | Type | Notes |
|--------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | FK→Tenant |
| ConversationId | Guid | FK→Conversation |
| Direction | int (enum) | MessageDirection: Inbound=0, Outbound=1 |
| SenderType | int (enum) | SenderType: Customer=0, Agent=1, System=2 |
| SenderId | Guid? | ApplicationUser.Id when agent-sent |
| Content | string | Message body |
| ContentType | int (enum) | ContentType: Text=0, Html=1, Markdown=2 |
| Channel | int (enum) | Denormalized from Conversation.Channel |
| ExternalMessageId | string? | Provider message ID |
| Metadata | string? | JSON — channel-specific data (email headers, WhatsApp media URLs) |
| SentAt | DateTime | |
| DeliveredAt | DateTime? | |
| ReadAt | DateTime? | |

Navigation: `Conversation`, `Attachments` (collection).

**MessageAttachment:**

| Column | Type | Notes |
|--------|------|-------|
| Id | Guid | PK |
| MessageId | Guid | FK→Message |
| FileName | string | |
| ContentType | string | MIME type |
| FileSizeBytes | long | |
| StoragePath | string | Relative path in local/blob storage |

### 3.2 Enums

```csharp
public enum ChannelType { Email = 0, WhatsApp = 1, LiveChat = 2, SMS = 3, Portal = 4 }
public enum ConversationStatus { Active = 0, Closed = 1, Archived = 2 }
public enum MessageDirection { Inbound = 0, Outbound = 1 }
public enum SenderType { Customer = 0, Agent = 1, System = 2 }
public enum ContentType { Text = 0, Html = 1, Markdown = 2 }
```

### 3.3 Channel Provider Abstraction

```csharp
public interface IChannelProvider
{
    ChannelType Channel { get; }
    Task<Message> SendMessageAsync(Conversation conversation, string content, ContentType contentType, Guid? agentId = null);
    Task<Message> ProcessInboundAsync(InboundMessageDto inbound);
}

public interface IChannelProviderFactory
{
    IChannelProvider GetProvider(ChannelType channel);
}
```

`ChannelProviderFactory` resolves providers from DI keyed by `ChannelType`. Each channel registers its provider in `DependencyInjection.cs`.

### 3.4 API

**ConversationsController** — `api/v1/conversations`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | List conversations (filter: channel, status, customerId, assignedAgentId, search; paginated) |
| GET | `/{id}` | Get conversation with messages (paginated messages) |
| POST | `/` | Create conversation (manual, e.g. agent starts outbound) |
| POST | `/{id}/messages` | Send message in conversation (delegates to channel provider) |
| PUT | `/{id}/close` | Close conversation |
| PUT | `/{id}/reopen` | Reopen conversation |
| PUT | `/{id}/assign` | Assign agent to conversation |

**Permissions:** `conversations.view`, `conversations.manage` — added to PermissionSeeder.

### 3.5 MediatR Notifications

```csharp
public record ConversationCreatedNotification(Guid ConversationId, Guid TenantId, ChannelType Channel) : INotification;
public record MessageReceivedNotification(Guid MessageId, Guid ConversationId, Guid TenantId, MessageDirection Direction) : INotification;
```

Handler on `ConversationCreatedNotification`: auto-creates a ticket if no TicketId set (links conversation to new ticket).

## 4. Email Channel (P3.2)

### 4.1 Interfaces & Implementations

```csharp
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, string? replyToMessageId = null);
}

public class MockEmailSender : IEmailSender
{
    // Logs to ILogger, stores in-memory list for dev inspection
}
```

`EmailChannelProvider : IChannelProvider` — uses `IEmailSender` for outbound, processes `InboundEmailDto` for inbound.

### 4.2 Inbound Webhook

`EmailInboundController` — `POST api/v1/webhooks/email/inbound`

- Accepts `InboundEmailDto`: `From`, `To`, `Subject`, `HtmlBody`, `TextBody`, `MessageId`, `InReplyTo?`, `Attachments[]`
- Validates API key header (`X-Webhook-Key`)
- Matches existing conversation by `InReplyTo` → `ExternalReference`, or creates new conversation
- Resolves customer by email address (creates new customer if unknown)
- Stores email headers in `Message.Metadata` JSON

### 4.3 Outbound

- Sets `ExternalReference` to generated Message-ID on first outbound
- Content type: Html
- Captures To/From/Subject in Metadata

## 5. WhatsApp Channel (P3.3)

### 5.1 Interfaces & Implementations

```csharp
public interface IWhatsAppClient
{
    Task<string> SendTextMessageAsync(string phoneNumber, string text);
    Task<string> SendMediaMessageAsync(string phoneNumber, string mediaUrl, string caption);
}

public class MockWhatsAppClient : IWhatsAppClient
{
    // Logs to ILogger, returns fake message IDs
}
```

`WhatsAppChannelProvider : IChannelProvider` — uses `IWhatsAppClient`.

### 5.2 Webhook

`WhatsAppWebhookController` — `POST api/v1/webhooks/whatsapp/inbound`

- Accepts payload mimicking WhatsApp Business API webhook structure
- Validates API key header
- Matches conversation by phone number (`ExternalReference`)
- Handles text and media messages (media URLs in Metadata JSON)

## 6. Live Chat (P3.4)

### 6.1 SignalR Hub

```csharp
public class ChatHub : Hub
{
    Task JoinChat(Guid conversationId);
    Task SendMessage(Guid conversationId, string content);
    Task SendTypingIndicator(Guid conversationId);
    Task EndChat(Guid conversationId);
}
```

- Two groups per conversation: `chat-{id}-customer`, `chat-{id}-agent`
- Authentication: both Bearer (agent) and Portal (customer) JWT schemes
- On `SendMessage`: creates Message entity via `LiveChatChannelProvider`, broadcasts to both groups
- On `EndChat`: closes conversation, notifies both groups

### 6.2 Chat Session Management

```csharp
public interface IChatSessionService
{
    Task<Conversation> StartSessionAsync(Guid customerId, Guid tenantId);
    Task EndSessionAsync(Guid conversationId);
    Task<int> GetQueuePositionAsync(Guid conversationId);
    Task SetAgentAvailabilityAsync(Guid agentId, bool isAvailable);
    Task<List<Conversation>> GetActiveSessionsAsync(Guid? agentId = null);
}
```

`ChatSessionService` — manages active chat sessions, agent availability (in-memory dictionary backed by DB), queue position calculation.

### 6.3 Agent Chat Console (Admin UI)

New feature module: `src/client/src/app/features/chat/`

- **Chat console page** (`/admin/chat`) — full-page layout:
  - Left sidebar: list of active conversations (assigned to me + unassigned queue), unread indicators, customer name, last message preview
  - Main panel: active conversation messages, real-time updates via SignalR, message input with send button
  - Typing indicators shown when customer is typing
  - Session controls: accept from queue, end chat, transfer to another agent
- **ChatService** — Angular service wrapping SignalR `HubConnection` + REST API calls
- **ChatHubService** — manages SignalR connection lifecycle, auto-reconnect

### 6.4 Customer Chat Widget (Portal UI)

Embedded in `PortalLayout` as a floating chat button (bottom-right):
- Click opens chat panel: connects to `ChatHub`, starts or resumes session
- Shows queue position while waiting for agent
- Real-time messaging with typing indicators
- Minimizable, persists across portal page navigation

## 7. SMS Channel (P3.5)

### 7.1 Interfaces & Implementations

```csharp
public interface ISmsClient
{
    Task<string> SendAsync(string phoneNumber, string message);
}

public class MockSmsClient : ISmsClient
{
    // Logs to ILogger, returns fake message ID
}
```

`SmsChannelProvider : IChannelProvider` — uses `ISmsClient`.

### 7.2 Webhook

`SmsWebhookController` — `POST api/v1/webhooks/sms/inbound`

- Validates API key header
- Matches conversation by phone number (`ExternalReference`)
- Text-only content type

## 8. Customer Portal Backend (P3.6)

### 8.1 PortalUser Entity

| Column | Type | Notes |
|--------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | FK→Tenant |
| CustomerId | Guid | FK→Customer |
| Email | string | Unique per tenant |
| PasswordHash | string | BCrypt hashed |
| FullName | string | |
| FullNameAr | string | |
| Phone | string? | |
| IsActive | bool | Default true |
| IsEmailVerified | bool | Default false (no email verification flow in V1) |
| RefreshToken | string? | |
| RefreshTokenExpiryTime | DateTime? | |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime | |

Navigation: `Customer`, `Tenant`.

### 8.2 Authentication

**Dual JWT scheme** in Program.cs:

```csharp
builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", options => { /* existing admin config */ })
    .AddJwtBearer("Portal", options => {
        // Same signing key, different ValidAudience = "portal"
    });
```

`PortalTokenService` — generates JWT with:
- Audience: `"portal"`
- Claims: `PortalUserId`, `CustomerId`, `TenantId`, `Email`, `FullName`

**PortalAuthController** — `api/v1/portal/auth`

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/register` | Create PortalUser (matches existing Customer by email, or creates new Customer) |
| POST | `/login` | Authenticate, return JWT + refresh token |
| POST | `/refresh` | Refresh token rotation |

Password hashing: `BCrypt.Net-Next` (not Identity's hasher — PortalUser is outside Identity).

### 8.3 Portal API

**PortalController** — `api/v1/portal` — `[Authorize(AuthenticationSchemes = "Portal")]`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/tickets` | My tickets (by CustomerId from token, paginated) |
| POST | `/tickets` | Submit ticket (creates Ticket + Conversation with Channel=Portal) |
| GET | `/tickets/{id}` | Ticket detail with public comments only |
| POST | `/tickets/{id}/comments` | Add comment (creates Message in conversation) |
| GET | `/knowledge` | Search published articles (query, categoryId, paginated) |
| GET | `/knowledge/{id}` | View article (increments view count) |
| GET | `/profile` | Get own profile |
| PUT | `/profile` | Update name, phone, password |

Ticket submission triggers `TicketCreatedNotification` (existing handler applies SLA, auto-assignment).

## 9. Customer Portal UI (P3.7)

Routes under `/portal/`, uses existing `PortalLayout`.

### 9.1 Pages

| Route | Component | Description |
|-------|-----------|-------------|
| `/portal/login` | PortalLoginComponent | Login form |
| `/portal/register` | PortalRegisterComponent | Registration form |
| `/portal/home` | PortalHomeComponent | KB search bar, Submit Ticket CTA, Track Tickets link |
| `/portal/tickets` | PortalTicketListComponent | My tickets table with status chips |
| `/portal/tickets/new` | PortalTicketFormComponent | Ticket submission form |
| `/portal/tickets/:id` | PortalTicketDetailComponent | Conversation thread, add comment |
| `/portal/knowledge` | PortalKnowledgeListComponent | Article list with search and categories |
| `/portal/knowledge/:id` | PortalKnowledgeViewerComponent | Article viewer |
| `/portal/profile` | PortalProfileComponent | Edit profile |

### 9.2 Services

- `PortalAuthService` — login, register, token storage, auto-refresh
- `PortalApiService` extends `ApiService` — uses same base URL but portal-prefixed endpoints (`/api/v1/portal/...`), attaches Portal JWT from `PortalAuthService` token storage (separate localStorage key from admin JWT)
- `PortalTicketService`, `PortalKnowledgeService` — typed API calls

### 9.3 Portal Guard

`portalAuthGuard` — redirects unauthenticated portal users to `/portal/login`. Separate from admin `authGuard`.

## 10. Notification Infrastructure (P3.8)

### 10.1 Entities

**NotificationTemplate:**

| Column | Type | Notes |
|--------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | FK→Tenant |
| Key | string | Unique per tenant: `ticket.created`, `sla.breached`, etc. |
| Subject | string | Template subject with `{{placeholders}}` |
| SubjectAr | string | |
| BodyTemplate | string | Template body with `{{placeholders}}` |
| BodyTemplateAr | string | |
| Channels | string | JSON array: `["InApp","Email","SMS"]` |
| IsActive | bool | |

**Notification:**

| Column | Type | Notes |
|--------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | FK→Tenant |
| RecipientType | int (enum) | RecipientType: Agent=0, PortalUser=1 |
| RecipientId | Guid | ApplicationUser.Id or PortalUser.Id |
| TemplateKey | string | Reference to template used |
| Title | string | Rendered title |
| TitleAr | string | |
| Body | string | Rendered body |
| BodyAr | string | |
| Data | string? | JSON — link target, entity IDs |
| IsRead | bool | Default false |
| ReadAt | DateTime? | |
| CreatedAt | DateTime | |

### 10.2 Services

```csharp
public interface INotificationService
{
    Task SendAsync(string templateKey, NotificationRecipient recipient, Dictionary<string, string> placeholders);
    Task SendBulkAsync(string templateKey, IEnumerable<NotificationRecipient> recipients, Dictionary<string, string> placeholders);
}

public record NotificationRecipient(Guid Id, RecipientType Type, string? Email = null, string? Phone = null);
```

`NotificationService`:
1. Loads `NotificationTemplate` by key
2. Renders placeholders into subject/body (simple `{{key}}` → value replacement)
3. Saves `Notification` entity (for in-app)
4. Fans out to dispatchers based on template's Channels array

**Dispatchers:**

```csharp
public interface INotificationDispatcher
{
    string Channel { get; } // "InApp", "Email", "SMS"
    Task DispatchAsync(Notification notification, NotificationRecipient recipient);
}
```

- `InAppNotificationDispatcher` — saves to DB, broadcasts via `NotificationHub`
- `EmailNotificationDispatcher` — calls `IEmailSender.SendAsync` (from P3.2)
- `SmsNotificationDispatcher` — calls `ISmsClient.SendAsync` (from P3.5)

### 10.3 SignalR NotificationHub

```csharp
public class NotificationHub : Hub
{
    // Agents join group "notifications-{userId}" on connect
    // Client methods: ReceiveNotification(notification), UpdateUnreadCount(count)
}
```

### 10.4 API

**NotificationsController** — `api/v1/notifications`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | List my notifications (paginated, filter: isRead) |
| GET | `/unread-count` | Unread count (for badge) |
| PUT | `/{id}/read` | Mark as read |
| PUT | `/read-all` | Mark all as read |

**Permissions:** `notifications.view` (all authenticated users get this by default).

### 10.5 Admin Layout Integration

Bell icon in the admin layout toolbar: unread count badge, dropdown showing recent notifications, "View All" link. Connected to `NotificationHub` for real-time updates.

### 10.6 MediatR Handler Wiring

New notification handlers on existing events:

| Event | Template Key | Recipients |
|-------|-------------|------------|
| `TicketCreatedNotification` | `ticket.created` | Assigned agent |
| `TicketCommentAddedNotification` | `ticket.commented` | Assigned agent (if comment by customer) |
| `MessageReceivedNotification` | `conversation.new_message` | Assigned agent |
| SLA breach (from SlaMonitoringService) | `sla.breached` | Assigned agent |

### 10.7 Seed Data

Default `NotificationTemplate` records for all template keys above, bilingual, all channels enabled.

## 11. New Permissions

| Key | Module | Seeded to Roles |
|-----|--------|----------------|
| conversations.view | Conversations | SuperAdmin, Admin, Agent |
| conversations.manage | Conversations | SuperAdmin, Admin |
| notifications.view | Notifications | SuperAdmin, Admin, Agent |
| chat.view | Chat | SuperAdmin, Admin, Agent |
| chat.manage | Chat | SuperAdmin, Admin |

## 12. File Map

### Backend — New Files

```
src/CustomerSupport.Domain/
  Enums/
    ChannelType.cs
    ConversationStatus.cs
    MessageDirection.cs
    SenderType.cs
    ContentType.cs
    RecipientType.cs
  Entities/
    Conversation.cs
    Message.cs
    MessageAttachment.cs
    PortalUser.cs
    NotificationTemplate.cs
    Notification.cs
  Interfaces/
    IChannelProvider.cs
    IChannelProviderFactory.cs
    IEmailSender.cs
    IWhatsAppClient.cs
    ISmsClient.cs
    IChatSessionService.cs
    INotificationService.cs
    INotificationDispatcher.cs
    IPortalTokenService.cs

src/CustomerSupport.Application/
  Conversations/
    DTOs/
      ConversationDto.cs
      MessageDto.cs
      InboundMessageDto.cs
      InboundEmailDto.cs
      SendMessageRequest.cs
      CreateConversationRequest.cs
    Queries/
      GetConversationsQuery.cs
      GetConversationByIdQuery.cs
    Commands/
      CreateConversationCommand.cs
      SendMessageCommand.cs
      CloseConversationCommand.cs
      AssignConversationCommand.cs
    Handlers/
      GetConversationsHandler.cs
      GetConversationByIdHandler.cs
      CreateConversationHandler.cs
      SendMessageHandler.cs
      CloseConversationHandler.cs
      AssignConversationHandler.cs
    Notifications/
      ConversationCreatedNotification.cs
      MessageReceivedNotification.cs
      AutoCreateTicketHandler.cs
  Portal/
    DTOs/
      PortalUserDto.cs
      PortalLoginRequest.cs
      PortalRegisterRequest.cs
      PortalTicketRequest.cs
      PortalTokenResponse.cs
    Commands/
      PortalLoginCommand.cs
      PortalRegisterCommand.cs
      PortalSubmitTicketCommand.cs
      PortalAddCommentCommand.cs
      PortalUpdateProfileCommand.cs
    Queries/
      GetPortalTicketsQuery.cs
      GetPortalTicketByIdQuery.cs
      GetPortalProfileQuery.cs
    Handlers/ (one per command/query)
    Validators/ (one per command)
  Notifications/
    DTOs/
      NotificationDto.cs
      NotificationRecipient.cs
    Commands/
      SendNotificationCommand.cs
      MarkNotificationReadCommand.cs
      MarkAllNotificationsReadCommand.cs
    Queries/
      GetNotificationsQuery.cs
      GetUnreadCountQuery.cs
    Handlers/
      SendNotificationHandler.cs
      MarkNotificationReadHandler.cs
      GetNotificationsHandler.cs
      GetUnreadCountHandler.cs
      TicketCreatedNotifyHandler.cs
      TicketCommentNotifyHandler.cs
      MessageReceivedNotifyHandler.cs

src/CustomerSupport.Infrastructure/
  Persistence/
    Configurations/
      ConversationConfiguration.cs
      MessageConfiguration.cs
      MessageAttachmentConfiguration.cs
      PortalUserConfiguration.cs
      NotificationTemplateConfiguration.cs
      NotificationConfiguration.cs
    Seeders/
      NotificationTemplateSeeder.cs
  Services/
    Channels/
      ChannelProviderFactory.cs
      EmailChannelProvider.cs
      WhatsAppChannelProvider.cs
      LiveChatChannelProvider.cs
      SmsChannelProvider.cs
    MockProviders/
      MockEmailSender.cs
      MockWhatsAppClient.cs
      MockSmsClient.cs
    ChatSessionService.cs
    PortalTokenService.cs
    PortalPasswordService.cs
    NotificationService.cs
    Dispatchers/
      InAppNotificationDispatcher.cs
      EmailNotificationDispatcher.cs
      SmsNotificationDispatcher.cs

src/CustomerSupport.API/
  Controllers/
    ConversationsController.cs
    PortalAuthController.cs
    PortalController.cs
    NotificationsController.cs
    EmailInboundController.cs (webhook)
    WhatsAppWebhookController.cs
    SmsWebhookController.cs
  Hubs/
    ChatHub.cs
    NotificationHub.cs
```

### Frontend — New Files

```
src/client/src/app/
  features/
    chat/
      chat.routes.ts
      chat.service.ts
      chat-hub.service.ts
      chat-console/chat-console.ts
    portal/
      portal.routes.ts
      portal-auth.service.ts
      portal-api.service.ts
      portal-ticket.service.ts
      portal-knowledge.service.ts
      portal-auth.guard.ts
      portal-login/portal-login.ts
      portal-register/portal-register.ts
      portal-home/portal-home.ts
      portal-ticket-list/portal-ticket-list.ts
      portal-ticket-form/portal-ticket-form.ts
      portal-ticket-detail/portal-ticket-detail.ts
      portal-knowledge-list/portal-knowledge-list.ts
      portal-knowledge-viewer/portal-knowledge-viewer.ts
      portal-profile/portal-profile.ts
    notifications/
      notifications.service.ts
      notification-hub.service.ts
      notification-bell/notification-bell.ts
      notification-list/notification-list.ts
  shared/
    components/
      chat-widget/chat-widget.ts (portal chat bubble)
```

## 13. Global Constraints

All constraints from the parent spec (Section 5–7) apply. Additionally:

- **Tenant isolation:** All new entities include `TenantId` with EF Core global query filters
- **Bilingual:** All user-facing strings have EN/AR pairs, notification templates are bilingual
- **No Phase 1/2 table modifications:** New entities link via FKs only
- **Mock providers:** All external integrations use mock implementations; real providers are a configuration swap
- **Webhook security:** All inbound webhooks validate `X-Webhook-Key` header against configured secret
- **SignalR authentication:** Both ChatHub and NotificationHub authenticate via JWT (Bearer for agents, Portal for customers on ChatHub)
- **BCrypt for PortalUser:** Use `BCrypt.Net-Next` NuGet package for portal password hashing (PortalUser is outside ASP.NET Identity)
- **No email verification flow in V1:** `IsEmailVerified` defaults to false, no verification email sent
