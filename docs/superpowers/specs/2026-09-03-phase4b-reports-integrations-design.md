# Phase 4B — Reports & Integrations Design Spec

## Overview

Add reporting, enhanced dashboards, and external integration capabilities to the Customer Support CRM. Builds on the data foundation from Phases 1-4A (Tickets, SLA, Conversations, AI Suggestions) to deliver actionable analytics and outbound event dispatch.

**Scope:** P4.4 Reports Domain, P4.5 Reports UI, P4.6 Management Dashboards, P4.7 External Integrations

**Dependencies:** P4.4 first → P4.5 → P4.6 (sequential). P4.7 is independent.

---

## Project-Wide Constraints

- **.NET 10**, EF Core 10, Angular 20, Material 20
- **MediatR** CQRS: IRequest<T> + Handler co-located in same file
- **FluentValidation** for all command/query validators
- **Multi-tenant**: all new entities implement `ITenantEntity`; global query filters in AppDbContext ensure isolation
- **Bilingual**: all user-facing lookup/label entities have `Name`/`NameAr` pairs; Angular uses `@ngx-translate/core` with en.json/ar.json
- **Angular patterns**: standalone components, `inject()`, `@for`/`@if` control flow, extends `ApiService` base class
- **Auth**: JWT Bearer with permission-based `[Authorize(Policy = "Permission:xxx")]`
- **No new background-job framework** — use in-process `Task.Delay` for retries, not Hangfire/Quartz
- **Charting**: ngx-charts (swimlane) for all frontend chart visualizations
- **Export**: ClosedXML for Excel, QuestPDF for PDF, built-in `StreamWriter` for CSV
- **Real-time queries**: all report data computed live from the main database (no pre-aggregated materialized views)

---

## P4.4 — Reports Domain

### 4.4.1 New Entity: TicketFeedback

Captures customer satisfaction ratings after ticket closure.

```
TicketFeedback : BaseEntity, ITenantEntity
  TenantId        Guid
  TicketId        Guid            (FK → Ticket, unique)
  CustomerId      Guid            (FK → Customer)
  Rating          int             (1-5)
  Comment         string?         (max 1000 chars)
  SubmittedAt     DateTime
```

- One feedback per ticket (unique constraint on TicketId)
- Submitted via customer portal after ticket reaches a final status (`TicketStatus.IsFinal == true`)
- EF configuration: index on `(TenantId, TicketId)`, index on `(TenantId, SubmittedAt)`

### 4.4.2 Report Query Handlers

All queries accept `DateRangeFilter` (StartDate, EndDate) and are tenant-scoped automatically via EF global filters.

**Location:** `src/CustomerSupport.Application/Reports/`

#### TicketVolumeReportQuery

- **Input:** DateRangeFilter, GroupBy (Day/Week/Month), CategoryId?, PriorityId?, StatusId?, AssignedToId?
- **Output:** `TicketVolumeReportDto`
  - `TimeSeries[]` — each entry: Period (string), CreatedCount, ResolvedCount
  - `CategoryBreakdown[]` — CategoryName/NameAr, Count
  - `PriorityBreakdown[]` — PriorityName/NameAr, Count
  - `TotalCreated`, `TotalResolved`

#### SlaPerformanceReportQuery

- **Input:** DateRangeFilter, PriorityId?, CategoryId?
- **Output:** `SlaPerformanceReportDto`
  - `OverallFirstResponseCompliance` (decimal %)
  - `OverallResolutionCompliance` (decimal %)
  - `TimeSeries[]` — Period, FirstResponseOnTime, FirstResponseBreached, ResolutionOnTime, ResolutionBreached
  - `BreachDetails[]` — TicketId, TicketNumber, BreachType, PolicyName, DueAt, BreachedAt, MinutesLate

#### AgentPerformanceReportQuery

- **Input:** DateRangeFilter, AgentId?
- **Output:** `AgentPerformanceReportDto`
  - `Agents[]` — AgentId, AgentName, TicketsHandled, TicketsResolved, AvgResolutionMinutes, AvgFirstResponseMinutes, SlaCompliancePercent
  - `TopPerformer` — AgentId, AgentName (highest SLA compliance)

#### ChannelAnalyticsReportQuery

- **Input:** DateRangeFilter
- **Output:** `ChannelAnalyticsReportDto`
  - `ChannelBreakdown[]` — Channel (enum name), ConversationCount, MessageCount, AvgResponseMinutes
  - `TimeSeries[]` — Period, Channel, ConversationCount

#### AiUsageReportQuery

- **Input:** DateRangeFilter
- **Output:** `AiUsageReportDto`
  - `SuggestionsByType[]` — Type, TotalCount, AcceptedCount, RejectedCount, PendingCount, AcceptanceRate (decimal %)
  - `AvgConfidence` (decimal)
  - `TotalTokensUsed` (int)
  - `TimeSeries[]` — Period, SuggestionCount, AcceptanceRate

#### CsatReportQuery

- **Input:** DateRangeFilter, CategoryId?
- **Output:** `CsatReportDto`
  - `AverageRating` (decimal)
  - `TotalResponses` (int)
  - `RatingDistribution[]` — Rating (1-5), Count, Percentage
  - `RecentFeedback[]` — TicketId, TicketNumber, CustomerName, Rating, Comment, SubmittedAt
  - `TimeSeries[]` — Period, AverageRating, ResponseCount

### 4.4.3 Report Export Service

**Interface:** `IReportExportService` in `src/CustomerSupport.Domain/Interfaces/`

```csharp
public interface IReportExportService
{
    Task<byte[]> ExportCsvAsync(ReportExportData data);
    Task<byte[]> ExportExcelAsync(ReportExportData data);
    Task<byte[]> ExportPdfAsync(ReportExportData data);
}

public record ReportExportData(
    string Title,
    string TitleAr,
    string[] ColumnHeaders,
    string[] ColumnHeadersAr,
    List<string[]> Rows,
    DateTime GeneratedAt,
    string? DateRange = null);
```

**Implementation:** `src/CustomerSupport.Infrastructure/Services/Reports/ReportExportService.cs`
- CSV: `StreamWriter` with proper quoting/escaping
- Excel: ClosedXML — header row styled, auto-fit columns, sheet named after report title
- PDF: QuestPDF — header with report title + date range + generation timestamp, styled data table, footer with page numbers

### 4.4.4 Report Export API Endpoints

Each report query handler has a companion export endpoint:

```
GET api/v1/reports/ticket-volume?startDate=&endDate=&groupBy=&categoryId=&priorityId=&statusId=&assignedToId=
GET api/v1/reports/ticket-volume/export?format=csv|excel|pdf&...same filters...

GET api/v1/reports/sla-performance?startDate=&endDate=&priorityId=&categoryId=
GET api/v1/reports/sla-performance/export?format=csv|excel|pdf&...

GET api/v1/reports/agent-performance?startDate=&endDate=&agentId=
GET api/v1/reports/agent-performance/export?format=csv|excel|pdf&...

GET api/v1/reports/channel-analytics?startDate=&endDate=
GET api/v1/reports/channel-analytics/export?format=csv|excel|pdf&...

GET api/v1/reports/ai-usage?startDate=&endDate=
GET api/v1/reports/ai-usage/export?format=csv|excel|pdf&...

GET api/v1/reports/csat?startDate=&endDate=&categoryId=
GET api/v1/reports/csat/export?format=csv|excel|pdf&...
```

All behind `[Authorize(Policy = "Permission:reports.view")]`. Export endpoints additionally require `Permission:reports.export`.

### 4.4.5 CSAT Submission (Customer Portal)

```
POST api/v1/portal/tickets/{ticketId}/feedback
Body: { rating: int, comment?: string }
```

- Portal JWT authentication (existing `PortalAuthenticationScheme`)
- Validates: ticket exists, belongs to customer, has final status, no existing feedback
- Returns 201 Created

### 4.4.6 New Permissions

| Key | Module | Description |
|-----|--------|-------------|
| `reports.view` | Reports | View reports and analytics |
| `reports.export` | Reports | Export reports to CSV/Excel/PDF |
| `integrations.view` | Integrations | View integration configurations |
| `integrations.manage` | Integrations | Manage webhook subscriptions and ERP settings |

- SuperAdmin and Admin get all four
- Agent gets `reports.view` only

---

## P4.5 — Reports UI

### 4.5.1 Angular Feature Module

**Route:** `/admin/reports` lazy-loaded via `app.routes.ts`

**Sub-routes:**
```
/admin/reports                     → ReportsLandingComponent
/admin/reports/ticket-volume       → TicketVolumeReportComponent
/admin/reports/sla-performance     → SlaPerformanceReportComponent
/admin/reports/agent-performance   → AgentPerformanceReportComponent
/admin/reports/channel-analytics   → ChannelAnalyticsReportComponent
/admin/reports/ai-usage            → AiUsageReportComponent
/admin/reports/csat                → CsatReportComponent
```

### 4.5.2 Shared Report Components

**ReportDateRangeComponent** (`src/client/src/app/features/reports/shared/report-date-range/`)
- Material date range picker (`mat-date-range-input`)
- Preset buttons: Today, Last 7 Days, Last 30 Days, This Month, This Quarter
- Emits `(dateRangeChange)` with `{ startDate: string, endDate: string }`

**ReportExportBarComponent** (`src/client/src/app/features/reports/shared/report-export-bar/`)
- Three buttons: CSV, Excel, PDF
- Input: `exportUrl` (base URL), `params` (current filter params)
- Calls export endpoint via `window.open()` (file download) or HTTP GET with blob response
- Shows `mat-spinner` during download

**ReportChartCardComponent** (`src/client/src/app/features/reports/shared/report-chart-card/`)
- Material card wrapper with title, loading skeleton, content projection for chart
- Input: `title`, `loading`

### 4.5.3 Reports Service

**Location:** `src/client/src/app/features/reports/reports.service.ts`

Extends `ApiService`. One method per report query + one `exportReport(reportType, format, params)` method that triggers file download.

### 4.5.4 Reports Landing Page

Grid of 6 Material cards, one per report type. Each card shows:
- Icon, title (translated), brief description
- `[routerLink]` to the report page

### 4.5.5 Individual Report Pages

Each report page follows the same layout:
1. Title + description
2. `<app-report-date-range>` + report-specific filter dropdowns
3. `<app-report-chart-card>` sections with ngx-charts inside
4. `<app-report-export-bar>`
5. Material data table with sortable columns and pagination

**Chart types per report:**

| Report | Charts |
|--------|--------|
| Ticket Volume | `ngx-charts-line-chart` (created vs resolved trend), `ngx-charts-bar-vertical` (category breakdown), `ngx-charts-pie-chart` (priority distribution) |
| SLA Performance | `ngx-charts-gauge` (compliance %), `ngx-charts-line-chart` (compliance trend), data table (breach details) |
| Agent Performance | `ngx-charts-bar-horizontal` (tickets per agent), `ngx-charts-bar-vertical-2d` (avg times comparison), leaderboard table |
| Channel Analytics | `ngx-charts-pie-chart` (channel distribution), `ngx-charts-bar-vertical-stacked` (volume over time) |
| AI Usage | `ngx-charts-bar-vertical` (by type), `ngx-charts-gauge` (acceptance rate), `ngx-charts-line-chart` (token usage trend) |
| CSAT | `ngx-charts-gauge` (avg rating, max 5), `ngx-charts-bar-vertical` (rating distribution 1-5), recent feedback table |

### 4.5.6 CSAT Submission in Portal

Add a feedback prompt to the existing customer portal ticket detail view. When a ticket has final status and no feedback exists:
- Show a star rating component (1-5 stars using Material icons)
- Optional comment textarea
- Submit button → `POST api/v1/portal/tickets/{id}/feedback`

---

## P4.6 — Management Dashboard Enhancement

### 4.6.1 New Backend Queries

Added to `src/CustomerSupport.Application/Dashboard/`:

**GetTicketTrendsQuery**
- Input: `Days` (default 30)
- Output: `TicketTrendDto[]` — Date, CreatedCount, ResolvedCount

**GetCategoryDistributionQuery**
- Output: `CategoryDistributionDto[]` — CategoryId, CategoryName, CategoryNameAr, TicketCount

**GetPriorityBreakdownQuery**
- Output: `PriorityBreakdownDto[]` — PriorityId, PriorityName, PriorityNameAr, Level, TicketCount

**GetChannelVolumeQuery**
- Input: `Days` (default 30)
- Output: `ChannelVolumeDto[]` — Channel (enum), ConversationCount, Date

**GetRecentSlaBreachesQuery**
- Input: `Count` (default 10)
- Output: `SlaBreachDto[]` — TicketId, TicketNumber, BreachType, PolicyName, DueAt, BreachedAt, MinutesLate

### 4.6.2 Dashboard API Endpoints

Added to existing `DashboardController`:

```
GET api/v1/Dashboard/ticket-trends?days=30
GET api/v1/Dashboard/category-distribution
GET api/v1/Dashboard/priority-breakdown
GET api/v1/Dashboard/channel-volume?days=30
GET api/v1/Dashboard/recent-sla-breaches?count=10
```

All behind existing `Permission:dashboard.view`.

### 4.6.3 Dashboard Service Extension

Add 5 new methods to `dashboard.service.ts`:
- `getTicketTrends(days)`
- `getCategoryDistribution()`
- `getPriorityBreakdown()`
- `getChannelVolume(days)`
- `getRecentSlaBreaches(count)`

### 4.6.4 Dashboard Component Enhancement

Add below existing sections, in a 2-column responsive grid:

1. **Ticket Trends** — `ngx-charts-line-chart` (created vs resolved, last 30 days)
2. **Category Distribution** — `ngx-charts-pie-chart` (open tickets by category)
3. **Priority Breakdown** — `ngx-charts-bar-horizontal` (open tickets by priority, color-coded)
4. **Channel Volume** — `ngx-charts-bar-vertical-stacked` (conversations by channel)
5. **Recent SLA Breaches** — Material table with breach details and ticket link

---

## P4.7 — External Integrations

### 4.7.1 New Entities

**WebhookSubscription : BaseEntity, ITenantEntity**
```
TenantId          Guid
Name              string          (max 200)
Url               string          (max 2000, HTTPS required)
Secret            string          (max 500, for HMAC-SHA256 signing)
Events            string          (JSON array: ["ticket.created", "ticket.resolved", ...])
IsActive          bool
Headers           string?         (JSON object of extra headers)
```

Indexes: `(TenantId, IsActive)`, unique `(TenantId, Name)`

**WebhookDeliveryLog : BaseEntity, ITenantEntity**
```
TenantId          Guid
SubscriptionId    Guid            (FK → WebhookSubscription)
Event             string          (e.g. "ticket.created")
Payload           string          (JSON, max 64KB)
StatusCode        int?
ResponseBody      string?         (max 4000, truncated)
Attempt           int             (1-3)
Success           bool
ErrorMessage      string?
CreatedAt         DateTime
```

Index: `(SubscriptionId, CreatedAt DESC)`, `(TenantId, Event, CreatedAt DESC)`

### 4.7.2 Supported Webhook Events

| Event | Trigger | Payload |
|-------|---------|---------|
| `ticket.created` | TicketCreatedNotification | ticketId, ticketNumber, subject, categoryName, priorityName, customerName |
| `ticket.status_changed` | TicketStatusChangedNotification | ticketId, ticketNumber, oldStatus, newStatus |
| `ticket.resolved` | Ticket status → IsFinal | ticketId, ticketNumber, resolvedAt, resolutionMinutes |
| `ticket.escalated` | EscalationRule fires | ticketId, ticketNumber, escalationReason |
| `sla.breached` | SLA check detects breach | ticketId, ticketNumber, breachType, policyName, dueAt |
| `conversation.created` | ConversationCreatedNotification | conversationId, channel, customerName |
| `conversation.closed` | Conversation.Status → Closed | conversationId, channel, closedAt |

### 4.7.3 Webhook Dispatcher

**Interface:** `IWebhookDispatcher` in `src/CustomerSupport.Domain/Interfaces/`

```csharp
public interface IWebhookDispatcher
{
    Task DispatchAsync(Guid tenantId, string eventName, object payload);
}
```

**Implementation:** `src/CustomerSupport.Infrastructure/Services/Integrations/WebhookDispatcher.cs`

Flow:
1. Query active subscriptions for the tenant where `Events` JSON contains the event name
2. For each matching subscription:
   a. Serialize payload to JSON
   b. Compute HMAC-SHA256 signature using subscription's Secret
   c. POST to URL with headers: `Content-Type: application/json`, `X-Webhook-Signature: sha256=<hex>`, `X-Webhook-Event: <event>`, plus any custom headers
   d. Log delivery attempt to `WebhookDeliveryLog`
   e. On failure (non-2xx or exception): retry up to 3 times with exponential backoff (2s, 8s, 32s) via `Task.Delay`
   f. Each retry is a separate `WebhookDeliveryLog` entry

### 4.7.4 Webhook Event Handlers

MediatR `INotificationHandler` implementations that listen to existing domain notifications and call `IWebhookDispatcher`:

- `WebhookTicketCreatedHandler` → listens to `TicketCreatedNotification`
- `WebhookTicketStatusChangedHandler` → listens to `TicketStatusChangedNotification` (also fires `ticket.resolved` when new status IsFinal)
- `WebhookSlaBreachedHandler` → listens to a new `SlaBreachedNotification` (published by SLA check logic)
- `WebhookConversationHandler` → listens to `ConversationCreatedNotification` and `ConversationClosedNotification`

**New MediatR notifications required** (do not exist yet):
- `TicketStatusChangedNotification(Guid TicketId, Guid TenantId, Guid OldStatusId, Guid NewStatusId)` — published in UpdateTicketCommand when status changes
- `SlaBreachedNotification(Guid TicketId, Guid TenantId, string BreachType, Guid SlaPolicyId)` — published by SLA check logic when a breach is detected
- `ConversationClosedNotification(Guid ConversationId, Guid TenantId)` — published when conversation status changes to Closed
- `TicketEscalatedNotification(Guid TicketId, Guid TenantId, string Reason)` — published by escalation rule execution

**Existing notifications reused:** `TicketCreatedNotification`, `ConversationCreatedNotification`

### 4.7.5 Webhook Management API

**Controller:** `WebhookSubscriptionController` at `api/v1/webhooks/subscriptions`

```
GET     api/v1/webhooks/subscriptions                     → List subscriptions (paginated)
GET     api/v1/webhooks/subscriptions/{id}                → Get subscription details
POST    api/v1/webhooks/subscriptions                     → Create subscription
PUT     api/v1/webhooks/subscriptions/{id}                → Update subscription
DELETE  api/v1/webhooks/subscriptions/{id}                → Delete subscription
POST    api/v1/webhooks/subscriptions/{id}/test           → Send test ping event
GET     api/v1/webhooks/subscriptions/{id}/deliveries     → Get delivery log (paginated, newest first)
```

All behind `Permission:integrations.manage` except GET endpoints which require `Permission:integrations.view`.

### 4.7.6 ERP Integration Stub

**Interface:** `IErpConnector` in `src/CustomerSupport.Domain/Interfaces/`

```csharp
public interface IErpConnector
{
    Task<ErpSyncResult> SyncTicketAsync(ErpTicketData ticket);
    Task<ErpSyncResult> SyncCustomerAsync(ErpCustomerData customer);
    Task<ErpCustomerData?> GetCustomerByExternalIdAsync(string externalId);
}

public record ErpSyncResult(bool Success, string? ExternalId, string? ErrorMessage);
public record ErpTicketData(Guid TicketId, string TicketNumber, string Subject, string CustomerName, string Status, string Priority, DateTime CreatedAt, DateTime? ResolvedAt);
public record ErpCustomerData(string? ExternalId, string Name, string? Email, string? Phone, string? Company);
```

**MockErpConnector:** Logs calls via `ILogger`, returns `ErpSyncResult(true, Guid.NewGuid().ToString(), null)`.

**ErpSettings:**
```json
"ErpSettings": {
  "Provider": "Mock",
  "BaseUrl": "",
  "ApiKey": ""
}
```

DI registration: factory switch on `Provider` — "Mock" → `MockErpConnector`, anything else → throw at startup (no real connector yet, stub only).

**ERP Sync Trigger:** Listens to `ticket.created` and `ticket.resolved` webhook events (or directly to MediatR notifications). Manual sync endpoint:

```
POST api/v1/integrations/erp/sync-ticket/{ticketId}
POST api/v1/integrations/erp/sync-customer/{customerId}
GET  api/v1/integrations/erp/status
```

### 4.7.7 Integrations UI

**Route:** `/admin/integrations` lazy-loaded

**Components:**

**WebhookSubscriptionListComponent:**
- Material table: Name, URL, Events (chips), IsActive (toggle), Actions (edit/delete/test)
- "Add Subscription" button → opens dialog
- Filter by active/inactive

**WebhookSubscriptionDialogComponent:**
- Form: Name, URL (with HTTPS validation), Secret (auto-generate button), Events (multi-select checkboxes), Custom Headers (key-value pairs), IsActive toggle
- Create/Update modes

**WebhookDeliveryLogComponent:**
- Material table: Event, URL, Status Code, Success (icon), Attempt, Timestamp
- Expandable rows showing payload and response body
- Filter by subscription, success/failure, date range

**ErpStatusCardComponent:**
- Card showing: Provider, Connection Status (mock → always "Mock Mode"), last sync time
- Manual sync buttons (sync ticket/customer by ID)

---

## NuGet / npm Packages

### Backend (new)
- `ClosedXML` (latest stable) — Excel export
- `QuestPDF` (latest stable) — PDF export

### Frontend (new)
- `@swimlane/ngx-charts` (latest stable) — charting library

### No new packages needed for
- Webhook dispatch (built-in `HttpClient`)
- CSV export (built-in `StreamWriter`)
- HMAC signing (built-in `System.Security.Cryptography`)

---

## Database Migrations

1. `AddTicketFeedback` — TicketFeedback table + indexes
2. `AddWebhookEntities` — WebhookSubscription + WebhookDeliveryLog tables + indexes
3. Permission seed data: `reports.view`, `reports.export`, `integrations.view`, `integrations.manage`
4. Role-permission mappings update

---

## i18n Keys

Add to both `en.json` and `ar.json`:

- `reports.*` — report titles, descriptions, column headers, filter labels, export labels
- `dashboard.*` — new dashboard section titles (trends, distribution, etc.)
- `integrations.*` — webhook management labels, ERP status labels
- `csat.*` — rating labels, feedback prompt text
- `common.export`, `common.csv`, `common.excel`, `common.pdf`
