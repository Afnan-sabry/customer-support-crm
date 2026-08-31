# Customer Support CRM — System Design Specification

**Date:** 2026-08-31
**Status:** Approved
**Author:** Afnan Sabry / Claude

---

## 1. Overview

A multi-tenant Customer Support CRM supporting Arabic and English, multi-department and multi-branch operations, with ticketing, communication channels, AI-powered features, a customer self-service portal, and management dashboards.

## 2. Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 10 Web API |
| Frontend | Angular 20, Angular Material |
| Database | SQL Server (EF Core, Code-First) |
| Architecture | Clean Architecture (Domain → Application → Infrastructure → API) |
| Auth | ASP.NET Core Identity + JWT |
| Real-time | SignalR |
| AI | Azure OpenAI (GPT-4) |
| i18n | ngx-translate (Arabic/English), RTL/LTR support |
| Logging | Serilog |
| Deployment | IIS (on-premises) |

## 3. Architecture

### Backend Project Structure

```
src/
  CustomerSupport.Domain/          — Entities, value objects, enums, domain interfaces
  CustomerSupport.Application/     — Use cases, DTOs, validators (FluentValidation), CQRS (MediatR)
  CustomerSupport.Infrastructure/  — EF Core DbContext, repositories, external service clients
  CustomerSupport.API/             — Controllers, middleware, SignalR hubs, composition root
```

### Frontend Project Structure

```
src/app/
  core/          — Auth, guards, interceptors, base services
  shared/        — Reusable components, pipes, directives
  features/      — Feature modules (lazy-loaded)
  layouts/       — AdminLayout, PortalLayout
```

### Multi-Tenancy

- Single database with `TenantId` column on every business table
- EF Core global query filters enforce tenant isolation
- Tenant resolved from JWT claim via middleware

### Bilingual Support

- Paired columns for translatable fields: `Name` + `NameAr`
- Frontend selects column based on active locale
- RTL/LTR controlled via `dir` attribute on root element, CSS logical properties

## 4. Phased Delivery

### Phase 0 — Project Init (Structure Only)

Establishes technical foundation. No database tables, no migrations, no business logic.

**Backend:**
- Solution structure with Clean Architecture project boundaries
- DI composition root with layer registration extension methods
- Global exception handling middleware (ProblemDetails)
- CORS, Swagger/OpenAPI, health check endpoint
- Serilog logging (console + file)
- Configuration structure (appsettings, connection string, JWT settings)
- EF Core packages installed, empty DbContext shell (no entities, no migrations)
- JWT authentication middleware wired (no Identity tables)

**Frontend:**
- Angular 20 project with Angular Material
- Folder structure: core/, shared/, features/, layouts/
- ngx-translate with en.json/ar.json
- RTL/LTR foundation
- AdminLayout and PortalLayout shells
- AuthService, HttpInterceptor, LanguageService skeletons
- Lazy-load routing structure with empty feature routes

**Explicit exclusions:** No database tables, no EF Core migrations, no domain entities, no business APIs, no business UI, no seed data.

### Phase 1 — Core

Security & Administration, Customer Management, Ticket Management.

**Plans:**

**P1.1 — Tenant Domain & Persistence**
- Tenant entity, TenantId global query filter in EF Core
- Tenant table migration, seed default tenant
- Tenant resolution middleware (from JWT claim)

**P1.2 — Identity & Authentication**
- ASP.NET Core Identity setup with User entity (extends IdentityUser)
- Identity tables migration
- Login/Register endpoints, JWT token generation, refresh tokens

**P1.3 — Roles & Permissions**
- Role entity (extends IdentityRole), Permission entity, RolePermission join
- Migration for roles/permissions tables
- Permission-based authorization policy provider
- Seed system roles (SuperAdmin, Admin, Agent)

**P1.4 — Audit Log Infrastructure**
- AuditLog entity and table
- EF Core SaveChanges interceptor to auto-capture mutations
- Audit log query endpoint (admin only)

**P1.5 — Angular Authentication**
- Login page, auth service wired to real API
- JWT storage, auto-attach interceptor
- Route guards (authenticated, role-based)
- Language switcher functional

**P1.6 — User & Role Management UI**
- User list, create, edit, deactivate pages
- Role list, create, edit with permission assignment
- User-role assignment

**P1.7 — Customer Domain Foundation**
- Customer entity, CustomerContact entity
- Migration, repository interface and implementation
- Customer validation rules (FluentValidation)

**P1.8 — Customer CRUD API**
- Create, Read, Update, Delete (soft) endpoints
- Customer search and filtering
- Customer contacts sub-resource endpoints
- Pagination support

**P1.9 — Customer Management UI**
- Customer list with search/filter/pagination
- Customer create/edit form
- Customer detail page with contacts

**P1.10 — Ticket Domain Foundation**
- Ticket, TicketCategory, TicketPriority, TicketStatus entities
- TicketComment, TicketAttachment, TicketHistory entities
- Migrations, repository interfaces
- Reference data seed (default categories, priorities, statuses)

**P1.11 — Ticket Lifecycle API**
- Create ticket, update status, assign agent, change priority
- Add comment (internal/public), add attachment
- Ticket history auto-recording on state changes
- Ticket list with filtering, single ticket detail

**P1.12 — Ticket Management UI**
- Ticket list with filters, search, pagination
- Create ticket form (select customer, category, priority)
- Ticket detail page (comments thread, attachments, history timeline)
- Status transitions, agent assignment

**Dependencies:**
- P1.1 first, then P1.2/P1.4/P1.7 can parallel
- P1.7 → P1.8 → P1.9 (customer track) parallel with P1.2 → P1.3 → P1.6 (auth track)
- P1.10 depends on P1.7 (FK to Customer)
- P1.5 depends on P1.2 + Phase 0 frontend

### Phase 2 — Agent Productivity

SLA & Automation, Agent Dashboard, Knowledge Base.

**Plans:**

**P2.1 — SLA Policy Domain** — SlaPolicy entity, CRUD API, per-priority/category targets
**P2.2 — SLA Tracking & Breach Detection** — Background service to monitor SLA, flag breaches
**P2.3 — Escalation Rules** — EscalationRule entity, auto-escalate on breach, notification trigger
**P2.4 — Auto-Assignment Rules** — AssignmentRule entity, round-robin/load-based assignment
**P2.5 — Agent Dashboard API** — Aggregated endpoints: my tickets, SLA status, workload stats
**P2.6 — Agent Dashboard UI** — Dashboard page with ticket queue, SLA indicators, quick actions
**P2.7 — Knowledge Base Backend** — KnowledgeArticle, KnowledgeCategory, CRUD, full-text search
**P2.8 — Knowledge Base UI** — Article list/search, viewer, admin editor

**Dependencies:**
- P2.1–P2.4 (SLA/automation) parallel with P2.7–P2.8 (knowledge base)
- P2.5–P2.6 depend on both tracks

### Phase 3 — Channels & Portal

Communication Channels, Customer Portal.

**Plans:**

**P3.1 — Conversation & Message Domain** — Unified channel abstraction, entities
**P3.2 — Email Channel Integration** — Inbound/outbound email, link to tickets
**P3.3 — WhatsApp Channel Integration** — WhatsApp Business API client
**P3.4 — Live Chat (SignalR)** — Real-time chat hub, agent-customer sessions
**P3.5 — SMS Channel Integration** — SMS provider client
**P3.6 — Customer Portal Backend** — PortalUser auth, submit ticket, track status, feedback
**P3.7 — Customer Portal UI** — Public portal: ticket submission, status tracking, FAQ access
**P3.8 — Notification Infrastructure** — Unified notifications (in-app SignalR, email, SMS)

**Dependencies:**
- P3.1 first, then P3.2/P3.3/P3.4/P3.5 (each channel independent)
- P3.6–P3.7 parallel with channels

### Phase 4 — Intelligence & Insights

AI Features, Reports & Dashboards, Integrations.

**Plans:**

**P4.1 — AI Service Foundation** — Azure OpenAI client wrapper, prompt templates, rate limiting
**P4.2 — AI Ticket Features** — Auto-categorization, summarization, suggested replies
**P4.3 — AI Chatbot** — Customer-facing chatbot, knowledge base-aware, escalation
**P4.4 — Reports Domain** — Report definitions, query builders, aggregation services
**P4.5 — Reports UI** — Ticket reports, SLA performance, agent performance, CSAT
**P4.6 — Management Dashboards** — Executive overview, department comparison, trends
**P4.7 — External Integrations** — Integration config, ERP connector, webhooks

**Dependencies:**
- P4.1 first, then P4.2/P4.3 parallel
- P4.4 → P4.5 → P4.6 (reports track, parallel with AI track)

## 5. Data Model Principles

- Every business table has `TenantId` enforced via EF Core global query filters
- Bilingual fields: paired `Name` + `NameAr` columns
- Soft deletes via `IsActive` on reference data
- JSON columns for flexible data (Tags, audit OldValues/NewValues)
- Phase 2–4 entities extend Phase 1 via foreign keys without modifying existing tables

## 6. API Conventions

- RESTful endpoints: `api/{version}/{resource}`
- Consistent response envelope with ProblemDetails for errors
- Pagination via `?page=1&pageSize=20` returning `{ items, totalCount, page, pageSize }`
- JWT Bearer authentication on all endpoints except login/register
- Permission-based authorization via policy attributes

## 7. Security

- JWT tokens with short expiry + refresh token rotation
- Role-based + permission-based access control
- Tenant isolation at query filter level (defense in depth)
- Audit logging on all mutations
- Input validation via FluentValidation
- CORS restricted to known origins
- Rate limiting on auth endpoints

## 8. Plan Summary

| Phase | Plans | Scope |
|-------|-------|-------|
| Phase 0 | 7 | Project scaffolding, no business logic |
| Phase 1 | 12 | Auth, customers, tickets |
| Phase 2 | 8 | SLA, dashboard, knowledge base |
| Phase 3 | 8 | Channels, portal, notifications |
| Phase 4 | 7 | AI, reports, integrations |
| **Total** | **42** | |
