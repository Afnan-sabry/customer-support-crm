# Phase 0 — Project Init Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Scaffold the full project structure for a Customer Support CRM with .NET 10 Clean Architecture backend and Angular 20 frontend — no database tables, no migrations, no business logic.

**Architecture:** Clean Architecture backend (Domain → Application → Infrastructure → API) with Angular 20 frontend. Backend and frontend are two independent tracks that can be built in parallel. The backend produces a running API with Swagger, health check, and JWT auth middleware wired but no Identity tables. The frontend produces an Angular shell with Material, i18n, RTL support, and layout shells.

**Tech Stack:** .NET 10, ASP.NET Core 10 Web API, EF Core 10 (packages only), Serilog, FluentValidation, MediatR, Swagger (Swashbuckle), Angular 20, Angular Material, ngx-translate

**Spec:** `docs/superpowers/specs/2026-08-31-customer-support-crm-design.md`

## Global Constraints

- .NET 10 (`net10.0` TFM)
- Angular 20 (CLI 20.3.10)
- No database tables or EF Core migrations
- No domain entities or business logic
- No Identity database tables
- No seed data
- No business API endpoints or UI screens
- SQL Server connection string in config only (not used yet)
- JWT auth middleware wired but no token issuance
- All bilingual UI keys use paired `en.json` / `ar.json`
- IIS deployment target (no Docker in Phase 0)

---

## File Map

### Backend

```
src/
  CustomerSupport.sln
  CustomerSupport.Domain/
    CustomerSupport.Domain.csproj
    DependencyInjection.cs
  CustomerSupport.Application/
    CustomerSupport.Application.csproj
    DependencyInjection.cs
  CustomerSupport.Infrastructure/
    CustomerSupport.Infrastructure.csproj
    DependencyInjection.cs
    Persistence/
      AppDbContext.cs
  CustomerSupport.API/
    CustomerSupport.API.csproj
    Program.cs
    appsettings.json
    appsettings.Development.json
    Middleware/
      ExceptionHandlingMiddleware.cs
    web.config
```

### Frontend

```
src/
  client/                          (Angular project root)
    angular.json
    package.json
    src/
      app/
        app.ts
        app.routes.ts
        app.config.ts
        core/
          services/
            auth.service.ts
            language.service.ts
          interceptors/
            auth.interceptor.ts
            error.interceptor.ts
          guards/
            auth.guard.ts
        shared/
          components/
            .gitkeep
          pipes/
            .gitkeep
          directives/
            .gitkeep
        features/
          .gitkeep
        layouts/
          admin-layout/
            admin-layout.ts
          portal-layout/
            portal-layout.ts
      assets/
        i18n/
          en.json
          ar.json
      environments/
        environment.ts
        environment.development.ts
      styles.scss
```

---

## Task 1: .NET Solution Scaffolding

**Covers:** P0.1 — Solution file, 4 projects, references, .NET 10 target framework

**Files:**
- Create: `src/CustomerSupport.sln`
- Create: `src/CustomerSupport.Domain/CustomerSupport.Domain.csproj`
- Create: `src/CustomerSupport.Application/CustomerSupport.Application.csproj`
- Create: `src/CustomerSupport.Infrastructure/CustomerSupport.Infrastructure.csproj`
- Create: `src/CustomerSupport.API/CustomerSupport.API.csproj`

**Interfaces:**
- Consumes: nothing (first task)
- Produces: Solution with 4 projects, project references wired, `dotnet build` passes

- [ ] **Step 1: Create solution and projects**

```powershell
cd src
dotnet new sln -n CustomerSupport
dotnet new classlib -n CustomerSupport.Domain --framework net10.0
dotnet new classlib -n CustomerSupport.Application --framework net10.0
dotnet new classlib -n CustomerSupport.Infrastructure --framework net10.0
dotnet new webapi -n CustomerSupport.API --framework net10.0 --no-https false
```

- [ ] **Step 2: Add projects to solution**

```powershell
cd src
dotnet sln add CustomerSupport.Domain/CustomerSupport.Domain.csproj
dotnet sln add CustomerSupport.Application/CustomerSupport.Application.csproj
dotnet sln add CustomerSupport.Infrastructure/CustomerSupport.Infrastructure.csproj
dotnet sln add CustomerSupport.API/CustomerSupport.API.csproj
```

- [ ] **Step 3: Wire project references (Clean Architecture)**

```powershell
cd src
dotnet add CustomerSupport.Application/CustomerSupport.Application.csproj reference CustomerSupport.Domain/CustomerSupport.Domain.csproj
dotnet add CustomerSupport.Infrastructure/CustomerSupport.Infrastructure.csproj reference CustomerSupport.Application/CustomerSupport.Application.csproj
dotnet add CustomerSupport.API/CustomerSupport.API.csproj reference CustomerSupport.Application/CustomerSupport.Application.csproj
dotnet add CustomerSupport.API/CustomerSupport.API.csproj reference CustomerSupport.Infrastructure/CustomerSupport.Infrastructure.csproj
```

- [ ] **Step 4: Configure each .csproj**

Enable nullable reference types and implicit usings in all 4 `.csproj` files. Each should contain:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

For the API project, also ensure:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

- [ ] **Step 5: Delete auto-generated placeholder files**

Remove `Class1.cs` from Domain, Application, Infrastructure. Remove the auto-generated `WeatherForecast` controller and class from API.

- [ ] **Step 6: Add .gitignore for .NET**

Create `src/.gitignore` using `dotnet new gitignore` in the `src/` directory.

- [ ] **Step 7: Verify build**

```powershell
cd src
dotnet build CustomerSupport.sln
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 8: Commit**

```bash
git add src/
git commit -m "chore: scaffold .NET 10 solution with Clean Architecture projects"
```

---

## Task 2: Backend NuGet Packages

**Covers:** Install all Phase 0 NuGet packages across projects (no code yet — just package references)

**Files:**
- Modify: `src/CustomerSupport.Application/CustomerSupport.Application.csproj`
- Modify: `src/CustomerSupport.Infrastructure/CustomerSupport.Infrastructure.csproj`
- Modify: `src/CustomerSupport.API/CustomerSupport.API.csproj`

**Interfaces:**
- Consumes: Solution from Task 1
- Produces: All NuGet packages installed, `dotnet restore` and `dotnet build` pass

- [ ] **Step 1: Install Application layer packages**

```powershell
cd src/CustomerSupport.Application
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
```

- [ ] **Step 2: Install Infrastructure layer packages**

```powershell
cd src/CustomerSupport.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

- [ ] **Step 3: Install API layer packages**

```powershell
cd src/CustomerSupport.API
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
dotnet add package Swashbuckle.AspNetCore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

- [ ] **Step 4: Verify restore and build**

```powershell
cd src
dotnet restore CustomerSupport.sln
dotnet build CustomerSupport.sln
```

Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add src/
git commit -m "chore: install NuGet packages for all layers"
```

---

## Task 3: DI Registration Pattern & Layer Shells

**Covers:** P0.2 partial — DependencyInjection extension methods per layer, empty shell classes

**Files:**
- Create: `src/CustomerSupport.Domain/DependencyInjection.cs`
- Create: `src/CustomerSupport.Application/DependencyInjection.cs`
- Create: `src/CustomerSupport.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: NuGet packages from Task 2
- Produces: `AddDomainServices()`, `AddApplicationServices()`, `AddInfrastructureServices(IConfiguration)` extension methods on `IServiceCollection`

- [ ] **Step 1: Create Domain DI registration**

Create `src/CustomerSupport.Domain/DependencyInjection.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace CustomerSupport.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        return services;
    }
}
```

- [ ] **Step 2: Create Application DI registration**

Create `src/CustomerSupport.Application/DependencyInjection.cs`:

```csharp
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerSupport.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
```

- [ ] **Step 3: Create Infrastructure DI registration**

Create `src/CustomerSupport.Infrastructure/DependencyInjection.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CustomerSupport.Infrastructure.Persistence;

namespace CustomerSupport.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
```

- [ ] **Step 4: Create empty AppDbContext shell**

Create `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
```

- [ ] **Step 5: Verify build**

```powershell
cd src
dotnet build CustomerSupport.sln
```

Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add src/
git commit -m "feat: add DI registration pattern and empty AppDbContext shell"
```

---

## Task 4: Configuration & Logging

**Covers:** P0.3 — Serilog setup, appsettings structure, connection string placeholder, JWT settings

**Files:**
- Create: `src/CustomerSupport.API/appsettings.json`
- Create: `src/CustomerSupport.API/appsettings.Development.json`

**Interfaces:**
- Consumes: API project from Task 1, Serilog package from Task 2
- Produces: Configuration files with `ConnectionStrings:DefaultConnection`, `JwtSettings` section, Serilog config

- [ ] **Step 1: Write appsettings.json**

Replace `src/CustomerSupport.API/appsettings.json` with:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=CustomerSupportCRM;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  },
  "JwtSettings": {
    "Secret": "CHANGE-THIS-TO-A-SECURE-KEY-AT-LEAST-32-CHARS-LONG",
    "Issuer": "CustomerSupportCRM",
    "Audience": "CustomerSupportCRM",
    "ExpiryInMinutes": 60,
    "RefreshTokenExpiryInDays": 7
  },
  "CorsSettings": {
    "AllowedOrigins": [ "http://localhost:4200" ]
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 2: Write appsettings.Development.json**

Replace `src/CustomerSupport.API/appsettings.Development.json` with:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    }
  }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/CustomerSupport.API/appsettings*.json
git commit -m "feat: add configuration structure with Serilog, JWT, and CORS settings"
```

---

## Task 5: Program.cs — Middleware Pipeline & Health Check

**Covers:** P0.2 + P0.4 — Full Program.cs composition root, exception handling middleware, CORS, Swagger, health check, JWT auth middleware, Serilog bootstrap

**Files:**
- Modify: `src/CustomerSupport.API/Program.cs`
- Create: `src/CustomerSupport.API/Middleware/ExceptionHandlingMiddleware.cs`
- Create: `src/CustomerSupport.API/web.config` (IIS)

**Interfaces:**
- Consumes: DI registrations from Task 3, config from Task 4, all NuGet packages from Task 2
- Produces: Running API at `https://localhost:5001` with `/health`, `/swagger`, global error handling, JWT auth middleware, CORS

- [ ] **Step 1: Create ExceptionHandlingMiddleware**

Create `src/CustomerSupport.API/Middleware/ExceptionHandlingMiddleware.cs`:

```csharp
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = context.Response.StatusCode,
            Title = "An unexpected error occurred.",
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
```

- [ ] **Step 2: Write Program.cs**

Replace `src/CustomerSupport.API/Program.cs` with:

```csharp
using System.Text;
using CustomerSupport.Application;
using CustomerSupport.Domain;
using CustomerSupport.Infrastructure;
using CustomerSupport.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Layer DI registrations
builder.Services.AddDomainServices();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// JWT Authentication
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
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!))
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        var origins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Customer Support CRM API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

- [ ] **Step 3: Create web.config for IIS**

Create `src/CustomerSupport.API/web.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\CustomerSupport.API.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

- [ ] **Step 4: Verify build**

```powershell
cd src
dotnet build CustomerSupport.sln
```

Expected: Build succeeded.

- [ ] **Step 5: Verify the API starts and health check responds**

```powershell
cd src/CustomerSupport.API
dotnet run --urls "http://localhost:5000" &
# Wait a few seconds, then:
curl http://localhost:5000/health
```

Expected: `Healthy`

Stop the process after verification.

- [ ] **Step 6: Commit**

```bash
git add src/
git commit -m "feat: add Program.cs with middleware pipeline, JWT auth, Swagger, and health check"
```

---

## Task 6: Angular Project Initialization

**Covers:** P0.5 — Angular 20 project, Angular Material, folder structure, environment config, proxy

**Files:**
- Create: `src/client/` (entire Angular project)
- Create: `src/client/proxy.conf.json`

**Interfaces:**
- Consumes: nothing (independent frontend track)
- Produces: Angular 20 app that runs with `ng serve`, Material theme applied, folder structure created

- [ ] **Step 1: Create Angular project**

```powershell
cd src
ng new client --style=scss --ssr=false --skip-git=true
```

- [ ] **Step 2: Install Angular Material**

```powershell
cd src/client
ng add @angular/material --skip-confirmation
```

- [ ] **Step 3: Create folder structure**

Create the following directories and `.gitkeep` files:

```
src/client/src/app/core/services/.gitkeep
src/client/src/app/core/interceptors/.gitkeep
src/client/src/app/core/guards/.gitkeep
src/client/src/app/shared/components/.gitkeep
src/client/src/app/shared/pipes/.gitkeep
src/client/src/app/shared/directives/.gitkeep
src/client/src/app/features/.gitkeep
src/client/src/app/layouts/.gitkeep
```

- [ ] **Step 4: Create environment files**

Create `src/client/src/environments/environment.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: '/api'
};
```

Create `src/client/src/environments/environment.development.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'
};
```

- [ ] **Step 5: Create proxy config for development**

Create `src/client/proxy.conf.json`:

```json
{
  "/api": {
    "target": "http://localhost:5000",
    "secure": false,
    "changeOrigin": true
  }
}
```

Update `angular.json` to use the proxy in the `serve` target under `development` configuration:

```json
"proxyConfig": "proxy.conf.json"
```

- [ ] **Step 6: Verify Angular app starts**

```powershell
cd src/client
ng serve
```

Expected: App compiles and serves at `http://localhost:4200`.

- [ ] **Step 7: Commit**

```bash
git add src/client/
git commit -m "feat: scaffold Angular 20 project with Material, folder structure, and proxy config"
```

---

## Task 7: Angular i18n & RTL Foundation

**Covers:** P0.6 — ngx-translate, translation files, LanguageService, RTL/LTR binding, bidirectional CSS

**Files:**
- Modify: `src/client/src/app/app.config.ts`
- Modify: `src/client/src/app/app.ts`
- Create: `src/client/src/app/core/services/language.service.ts`
- Create: `src/client/src/assets/i18n/en.json`
- Create: `src/client/src/assets/i18n/ar.json`
- Modify: `src/client/src/styles.scss`

**Interfaces:**
- Consumes: Angular project from Task 6
- Produces: `LanguageService` with `switchLanguage(lang: 'en' | 'ar')`, `getCurrentLanguage(): string`, document `dir` attribute toggles RTL/LTR, `TranslateModule` available app-wide

- [ ] **Step 1: Install ngx-translate**

```powershell
cd src/client
npm install @ngx-translate/core @ngx-translate/http-loader
```

- [ ] **Step 2: Create translation files**

Create `src/client/src/assets/i18n/en.json`:

```json
{
  "app": {
    "title": "Customer Support CRM",
    "loading": "Loading...",
    "error": "An error occurred"
  },
  "nav": {
    "dashboard": "Dashboard",
    "tickets": "Tickets",
    "customers": "Customers",
    "knowledgeBase": "Knowledge Base",
    "reports": "Reports",
    "settings": "Settings"
  },
  "common": {
    "save": "Save",
    "cancel": "Cancel",
    "delete": "Delete",
    "edit": "Edit",
    "create": "Create",
    "search": "Search",
    "filter": "Filter",
    "actions": "Actions",
    "confirm": "Confirm",
    "back": "Back",
    "next": "Next",
    "yes": "Yes",
    "no": "No"
  },
  "auth": {
    "login": "Login",
    "logout": "Logout",
    "email": "Email",
    "password": "Password"
  }
}
```

Create `src/client/src/assets/i18n/ar.json`:

```json
{
  "app": {
    "title": "نظام دعم العملاء",
    "loading": "جاري التحميل...",
    "error": "حدث خطأ"
  },
  "nav": {
    "dashboard": "لوحة التحكم",
    "tickets": "التذاكر",
    "customers": "العملاء",
    "knowledgeBase": "قاعدة المعرفة",
    "reports": "التقارير",
    "settings": "الإعدادات"
  },
  "common": {
    "save": "حفظ",
    "cancel": "إلغاء",
    "delete": "حذف",
    "edit": "تعديل",
    "create": "إنشاء",
    "search": "بحث",
    "filter": "تصفية",
    "actions": "إجراءات",
    "confirm": "تأكيد",
    "back": "رجوع",
    "next": "التالي",
    "yes": "نعم",
    "no": "لا"
  },
  "auth": {
    "login": "تسجيل الدخول",
    "logout": "تسجيل الخروج",
    "email": "البريد الإلكتروني",
    "password": "كلمة المرور"
  }
}
```

- [ ] **Step 3: Create LanguageService**

Create `src/client/src/app/core/services/language.service.ts`:

```typescript
import { Injectable, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { DOCUMENT } from '@angular/common';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private translate = inject(TranslateService);
  private document = inject(DOCUMENT);

  private readonly STORAGE_KEY = 'crm-language';
  private readonly RTL_LANGUAGES = ['ar'];

  init(): void {
    this.translate.addLangs(['en', 'ar']);
    this.translate.setDefaultLang('en');

    const saved = localStorage.getItem(this.STORAGE_KEY);
    const lang = saved && ['en', 'ar'].includes(saved) ? saved : 'en';
    this.switchLanguage(lang as 'en' | 'ar');
  }

  switchLanguage(lang: 'en' | 'ar'): void {
    this.translate.use(lang);
    localStorage.setItem(this.STORAGE_KEY, lang);

    const dir = this.RTL_LANGUAGES.includes(lang) ? 'rtl' : 'ltr';
    this.document.documentElement.setAttribute('dir', dir);
    this.document.documentElement.setAttribute('lang', lang);
  }

  getCurrentLanguage(): string {
    return this.translate.currentLang || 'en';
  }

  isRtl(): boolean {
    return this.RTL_LANGUAGES.includes(this.getCurrentLanguage());
  }
}
```

- [ ] **Step 4: Configure ngx-translate in app.config.ts**

Update `src/client/src/app/app.config.ts` to include the translate module providers:

```typescript
import { ApplicationConfig, provideZoneChangeDetection, importProvidersFrom, APP_INITIALIZER, inject } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi, HttpClient } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { routes } from './app.routes';
import { LanguageService } from './core/services/language.service';

function httpLoaderFactory(http: HttpClient): TranslateHttpLoader {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

function initLanguage(): () => void {
  const languageService = inject(LanguageService);
  return () => languageService.init();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    provideAnimationsAsync(),
    importProvidersFrom(
      TranslateModule.forRoot({
        loader: {
          provide: TranslateLoader,
          useFactory: httpLoaderFactory,
          deps: [HttpClient]
        }
      })
    ),
    {
      provide: APP_INITIALIZER,
      useFactory: initLanguage,
      multi: true
    }
  ]
};
```

- [ ] **Step 5: Add RTL-aware global styles**

Append to `src/client/src/styles.scss`:

```scss
// RTL/LTR Foundation
html {
  &[dir='rtl'] {
    direction: rtl;
    text-align: right;

    .mat-mdc-form-field {
      direction: rtl;
    }
  }

  &[dir='ltr'] {
    direction: ltr;
    text-align: left;
  }
}

// Use CSS logical properties throughout the app:
// margin-inline-start instead of margin-left
// padding-inline-end instead of padding-right
// inset-inline-start instead of left
// border-inline-start instead of border-left
```

- [ ] **Step 6: Verify translations load**

Update `src/client/src/app/app.ts` to display a translated title as a smoke test:

```typescript
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, TranslateModule],
  template: `
    <h1>{{ 'app.title' | translate }}</h1>
    <router-outlet />
  `
})
export class App {}
```

Run `ng serve` and verify "Customer Support CRM" renders.

- [ ] **Step 7: Commit**

```bash
git add src/client/
git commit -m "feat: add i18n with ngx-translate (Arabic/English) and RTL/LTR foundation"
```

---

## Task 8: Angular Layouts & Core Services

**Covers:** P0.7 — AdminLayout, PortalLayout, routing with lazy-load, AuthService skeleton, interceptors, guards

**Files:**
- Create: `src/client/src/app/layouts/admin-layout/admin-layout.ts`
- Create: `src/client/src/app/layouts/portal-layout/portal-layout.ts`
- Create: `src/client/src/app/core/services/auth.service.ts`
- Create: `src/client/src/app/core/interceptors/auth.interceptor.ts`
- Create: `src/client/src/app/core/interceptors/error.interceptor.ts`
- Create: `src/client/src/app/core/guards/auth.guard.ts`
- Modify: `src/client/src/app/app.routes.ts`
- Modify: `src/client/src/app/app.ts`
- Modify: `src/client/src/app/app.config.ts`

**Interfaces:**
- Consumes: LanguageService from Task 7, Angular Material from Task 6
- Produces: `AdminLayoutComponent` with sidebar/header shell, `PortalLayoutComponent` shell, `AuthService` with `login()`, `logout()`, `isAuthenticated()`, `getToken()` stubs, `authInterceptor` that attaches JWT, `authGuard` that checks `AuthService.isAuthenticated()`

- [ ] **Step 1: Create AuthService skeleton**

Create `src/client/src/app/core/services/auth.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'crm-access-token';
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());

  isAuthenticated$: Observable<boolean> = this.isAuthenticatedSubject.asObservable();

  login(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
    this.isAuthenticatedSubject.next(true);
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this.isAuthenticatedSubject.next(false);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    return this.hasToken();
  }

  private hasToken(): boolean {
    return !!localStorage.getItem(this.TOKEN_KEY);
  }
}
```

- [ ] **Step 2: Create auth interceptor**

Create `src/client/src/app/core/interceptors/auth.interceptor.ts`:

```typescript
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  return next(req);
};
```

- [ ] **Step 3: Create error interceptor**

Create `src/client/src/app/core/interceptors/error.interceptor.ts`:

```typescript
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError(error => {
      if (error.status === 401) {
        authService.logout();
        router.navigate(['/login']);
      }
      return throwError(() => error);
    })
  );
};
```

- [ ] **Step 4: Create auth guard**

Create `src/client/src/app/core/guards/auth.guard.ts`:

```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};
```

- [ ] **Step 5: Create AdminLayoutComponent**

Create `src/client/src/app/layouts/admin-layout/admin-layout.ts`:

```typescript
import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { LanguageService } from '../../core/services/language.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-admin-layout',
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, TranslateModule,
    MatSidenavModule, MatToolbarModule, MatListModule, MatIconModule, MatButtonModule
  ],
  template: `
    <mat-sidenav-container class="admin-container">
      <mat-sidenav mode="side" opened class="admin-sidenav">
        <div class="sidenav-header">
          <h2>{{ 'app.title' | translate }}</h2>
        </div>
        <mat-nav-list>
          <a mat-list-item routerLink="/admin/dashboard" routerLinkActive="active">
            <mat-icon matListItemIcon>dashboard</mat-icon>
            <span>{{ 'nav.dashboard' | translate }}</span>
          </a>
          <a mat-list-item routerLink="/admin/tickets" routerLinkActive="active">
            <mat-icon matListItemIcon>confirmation_number</mat-icon>
            <span>{{ 'nav.tickets' | translate }}</span>
          </a>
          <a mat-list-item routerLink="/admin/customers" routerLinkActive="active">
            <mat-icon matListItemIcon>people</mat-icon>
            <span>{{ 'nav.customers' | translate }}</span>
          </a>
          <a mat-list-item routerLink="/admin/knowledge-base" routerLinkActive="active">
            <mat-icon matListItemIcon>menu_book</mat-icon>
            <span>{{ 'nav.knowledgeBase' | translate }}</span>
          </a>
          <a mat-list-item routerLink="/admin/reports" routerLinkActive="active">
            <mat-icon matListItemIcon>bar_chart</mat-icon>
            <span>{{ 'nav.reports' | translate }}</span>
          </a>
          <a mat-list-item routerLink="/admin/settings" routerLinkActive="active">
            <mat-icon matListItemIcon>settings</mat-icon>
            <span>{{ 'nav.settings' | translate }}</span>
          </a>
        </mat-nav-list>
      </mat-sidenav>

      <mat-sidenav-content>
        <mat-toolbar color="primary">
          <span class="spacer"></span>
          <button mat-icon-button (click)="toggleLanguage()">
            <mat-icon>language</mat-icon>
          </button>
          <button mat-icon-button (click)="logout()">
            <mat-icon>logout</mat-icon>
          </button>
        </mat-toolbar>
        <main class="admin-content">
          <router-outlet />
        </main>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [`
    .admin-container { height: 100vh; }
    .admin-sidenav { width: 260px; }
    .sidenav-header { padding: 16px; text-align: center; }
    .spacer { flex: 1 1 auto; }
    .admin-content { padding: 24px; }
    .active { background-color: rgba(0, 0, 0, 0.04); }
  `]
})
export class AdminLayoutComponent {
  private languageService = inject(LanguageService);
  private authService = inject(AuthService);

  toggleLanguage(): void {
    const current = this.languageService.getCurrentLanguage();
    this.languageService.switchLanguage(current === 'en' ? 'ar' : 'en');
  }

  logout(): void {
    this.authService.logout();
  }
}
```

- [ ] **Step 6: Create PortalLayoutComponent**

Create `src/client/src/app/layouts/portal-layout/portal-layout.ts`:

```typescript
import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { LanguageService } from '../../core/services/language.service';

@Component({
  selector: 'app-portal-layout',
  imports: [RouterOutlet, TranslateModule, MatToolbarModule, MatButtonModule, MatIconModule],
  template: `
    <mat-toolbar color="primary">
      <span>{{ 'app.title' | translate }}</span>
      <span class="spacer"></span>
      <button mat-icon-button (click)="toggleLanguage()">
        <mat-icon>language</mat-icon>
      </button>
    </mat-toolbar>
    <main class="portal-content">
      <router-outlet />
    </main>
  `,
  styles: [`
    .spacer { flex: 1 1 auto; }
    .portal-content { padding: 24px; max-width: 960px; margin-inline: auto; }
  `]
})
export class PortalLayoutComponent {
  private languageService = inject(LanguageService);

  toggleLanguage(): void {
    const current = this.languageService.getCurrentLanguage();
    this.languageService.switchLanguage(current === 'en' ? 'ar' : 'en');
  }
}
```

- [ ] **Step 7: Configure routes with lazy-load structure**

Replace `src/client/src/app/app.routes.ts`:

```typescript
import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'admin',
    loadComponent: () => import('./layouts/admin-layout/admin-layout').then(m => m.AdminLayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      // Feature routes will be added in Phase 1+
    ]
  },
  {
    path: 'portal',
    loadComponent: () => import('./layouts/portal-layout/portal-layout').then(m => m.PortalLayoutComponent),
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      // Portal routes will be added in Phase 3
    ]
  },
  {
    path: 'login',
    loadComponent: () => import('./layouts/portal-layout/portal-layout').then(m => m.PortalLayoutComponent),
    // Login page will be added in Phase 1
  },
  { path: '', redirectTo: '/admin', pathMatch: 'full' },
  { path: '**', redirectTo: '/admin' }
];
```

- [ ] **Step 8: Update app.config.ts with interceptors**

Update `src/client/src/app/app.config.ts` to register the functional interceptors:

```typescript
import { ApplicationConfig, provideZoneChangeDetection, importProvidersFrom, APP_INITIALIZER, inject } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { routes } from './app.routes';
import { LanguageService } from './core/services/language.service';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';

function httpLoaderFactory(http: HttpClient): TranslateHttpLoader {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

function initLanguage(): () => void {
  const languageService = inject(LanguageService);
  return () => languageService.init();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    provideAnimationsAsync(),
    importProvidersFrom(
      TranslateModule.forRoot({
        loader: {
          provide: TranslateLoader,
          useFactory: httpLoaderFactory,
          deps: [HttpClient]
        }
      })
    ),
    {
      provide: APP_INITIALIZER,
      useFactory: initLanguage,
      multi: true
    }
  ]
};
```

- [ ] **Step 9: Update app.ts to be a simple router host**

Replace `src/client/src/app/app.ts`:

```typescript
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `<router-outlet />`
})
export class App {}
```

- [ ] **Step 10: Verify Angular app compiles and serves**

```powershell
cd src/client
ng serve
```

Expected: Compiles successfully. Navigating to `http://localhost:4200` redirects to `/admin` which redirects to `/login` (auth guard).

- [ ] **Step 11: Commit**

```bash
git add src/client/
git commit -m "feat: add admin/portal layouts, routing, auth service, interceptors, and guards"
```

---

## Dependency Graph

```
Backend track:              Frontend track:
Task 1 (Solution)           Task 6 (Angular Init)
  ↓                           ↓
Task 2 (NuGet)              Task 7 (i18n & RTL)
  ↓                           ↓
Task 3 (DI & DbContext)     Task 8 (Layouts & Core)
  ↓
Task 4 (Config)
  ↓
Task 5 (Program.cs)

Backend and Frontend tracks are fully independent.
```

## Verification Checklist

After all 8 tasks complete:

- [ ] `dotnet build src/CustomerSupport.sln` succeeds with 0 errors
- [ ] `GET http://localhost:5000/health` returns `Healthy`
- [ ] Swagger UI accessible at `http://localhost:5000/swagger`
- [ ] `ng serve` in `src/client/` compiles with 0 errors
- [ ] App renders at `http://localhost:4200`
- [ ] Language toggle switches between English (LTR) and Arabic (RTL)
- [ ] No database tables or migrations exist
- [ ] No business API endpoints exist beyond `/health`
