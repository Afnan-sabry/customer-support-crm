# Frontend i18n Translation Fixes

**Date:** 2026-09-06
**Status:** Implemented
**Scope:** 8 translation defects across the Angular frontend (ngx-translate + Angular locale)

## Problem

The application supports English and Arabic via `@ngx-translate/core`, but multiple i18n issues caused the Arabic experience to break or fall back to English in several areas. The issues ranged from infrastructure (translations not loaded before first render) to content (hardcoded English strings, untranslated reference data names) to presentation (missing Arabic font, locale-unaware date formatting).

## Issues and Fixes

### 1. APP_INITIALIZER Does Not Await Translation Loading

**Problem:** The `initLanguage` factory in `app.config.ts` returned `() => void`. Angular's `APP_INITIALIZER` only blocks bootstrap if the returned function produces a `Promise` or `Observable`. Translations loaded asynchronously after first render, causing a flash of raw translation keys (e.g., `app.title`).

**Root cause:** `LanguageService.init()` called `this.translate.use(lang)` but discarded the returned `Observable`.

**Fix:**
- `LanguageService.init()` now returns `Observable<unknown>` from `translate.use()`
- `switchLanguage()` delegates to a shared `applyLanguage()` method that sets `dir`, `lang`, localStorage, and returns the observable
- `initLanguage` factory signature changed to `() => Observable<unknown>`

**Files:** `language.service.ts`, `app.config.ts`

### 2. Hardcoded English Error Fallback Strings

**Problem:** 15 error handlers across 14 component files used hardcoded English strings as fallbacks:
```typescript
this.error = err.error?.detail || err.error?.title || 'An error occurred';
```
When the UI language was Arabic, these errors displayed in English.

**Fix:** Each component now injects `TranslateService` and uses `this.translate.instant(...)` with the appropriate translation key:

| Hardcoded String | Translation Key | Count |
|---|---|---|
| `'An error occurred'` | `app.error` | 11 |
| `'Login failed'` | `auth.loginFailed` | 1 |
| `'Login failed'` (portal) | `portal.loginFailed` | 1 |
| `'Failed to update profile'` | `portal.profileUpdateFailed` | 1 |
| `'Registration failed'` | `portal.registrationFailed` | 1 |

**Files:** 14 component files across `features/auth`, `features/portal`, `features/tickets`, `features/customers`, `features/users`, `features/roles`, `features/knowledge`, `features/sla`, `features/escalation`, `features/assignment`

### 3. Server-Side Reference Data Names Not Translated

**Problem:** Dropdown options and display chips for statuses, priorities, and categories rendered `status.name`, `priority.name`, etc. — always the English `name` field. The server already returns `nameAr` alongside `name` in the reference data DTOs (`TicketStatusDto`, `TicketPriorityDto`, `TicketCategoryDto`), but the client ignored it.

**Fix:** Created `LocalizedNamePipe` (`shared/pipes/localized-name.pipe.ts`) — an impure pipe that reads `TranslateService.currentLang` and returns `item.nameAr` for Arabic, `item.name` otherwise:
```typescript
@Pipe({ name: 'localizedName', pure: false })
export class LocalizedNamePipe implements PipeTransform {
  transform(item: { name: string; nameAr: string } | null): string {
    if (!item) return '';
    return this.translate.currentLang === 'ar' ? item.nameAr : item.name;
  }
}
```

Applied in templates:
```html
<!-- Dropdowns -->
<mat-option [value]="status.id">{{ status | localizedName }}</mat-option>

<!-- Display chips (via lookup) -->
<mat-chip>{{ statusItem | localizedName }}</mat-chip>
```

For ticket-detail chips, added `statusItem` and `priorityItem` getters that look up the reference data object by ID from the loaded arrays.

**Limitation:** List views that display `ticket.statusName` / `ticket.priorityName` (plain strings from the server DTO without a corresponding reference object) are not yet translated. A future fix should either have the API return both `statusName` and `statusNameAr`, or have the client look up the name from cached reference data.

**Files:** `localized-name.pipe.ts` (new), `ticket-detail.ts`, `ticket-list.ts`, `ticket-form.ts`, `sla-policy-form.ts`, `sla-policy-list.ts`, `escalation-rule-form.ts`, `assignment-rule-form.ts`, `portal-ticket-form.ts`

### 4. Date Pipe Ignores Locale

**Problem:** Angular's `DatePipe` uses `LOCALE_ID` to determine date formatting. No `LOCALE_ID` provider was configured, so it defaulted to `en-US` regardless of the selected language. Dates like `9/6/26, 2:30 PM` never switched to Arabic format.

**Fix:**
- Registered Arabic locale data: `registerLocaleData(localeAr)` at module level
- Added `LOCALE_ID` provider that reads from `LanguageService.getCurrentLanguage()`

**Note:** `LOCALE_ID` is injection-time only — it does not dynamically update when the user switches language mid-session. A full page reload (which `switchLanguage` does not trigger) is needed for date formatting to reflect the new locale. This is acceptable because language switching is infrequent.

**Files:** `app.config.ts`

### 5. No Arabic Web Font

**Problem:** Only Roboto was loaded via Google Fonts. Roboto has minimal Arabic glyph support, so Arabic text fell back to the browser's default sans-serif, causing inconsistent typography.

**Fix:**
- Added Noto Sans Arabic (weights 300, 400, 500) to the Google Fonts link in `index.html`
- Added `"Noto Sans Arabic"` to the `font-family` stack in `styles.scss`

**Files:** `index.html`, `styles.scss`

### 6. Hardcoded `lang` and `title` in index.html

**Problem:**
- `<html lang="en">` was hardcoded. Although `LanguageService` updates the `lang` attribute at runtime, the initial HTML did not match the user's saved preference during the FOUC window (before translations loaded).
- `<title>Client</title>` was a meaningless placeholder.

**Fix:**
- Added `dir="ltr"` to the initial `<html>` tag (runtime still overrides both `lang` and `dir`)
- Changed `<title>` to `Customer Support CRM`

**Files:** `index.html`

### 7. Language Toggle Button Missing Label

**Problem:** The admin and portal layout toolbars used an icon-only button (`mat-icon-button` with `language` icon) for the language toggle. Users could not tell which language was currently active or what clicking would switch to.

**Fix:** Changed to `mat-button` with a text label showing the opposite language name:
- When in English: shows "العربية"
- When in Arabic: shows "English"

Added a `languageLabel` getter to both `AdminLayoutComponent` and `PortalLayoutComponent`.

**Files:** `admin-layout.ts`, `portal-layout.ts`

### 8. Missing Translation Keys

**Problem:** The new error fallback translations referenced keys that did not exist in the locale files.

**Fix:** Added to both `en.json` and `ar.json`:

| Key | English | Arabic |
|---|---|---|
| `auth.loginFailed` | Login failed | فشل تسجيل الدخول |
| `portal.loginFailed` | Login failed | فشل تسجيل الدخول |
| `portal.registrationFailed` | Registration failed | فشل التسجيل |
| `portal.profileUpdateFailed` | Failed to update profile | فشل تحديث الملف الشخصي |

**Note:** `app.error` ("An error occurred" / "حدث خطأ") already existed.

**Files:** `en.json`, `ar.json`

## Conventions for Future Work

### Adding New User-Facing Strings
- Never hardcode English strings in TypeScript code. Use `this.translate.instant('key')` for programmatic strings and `{{ 'key' | translate }}` in templates.
- Add the key to both `en.json` and `ar.json` simultaneously.

### Displaying Reference Data Names
- Use the `localizedName` pipe for any object that has both `name` and `nameAr` fields.
- Import `LocalizedNamePipe` in the component's `imports` array.

### New Components with Error Handling
- Inject `TranslateService` and use `this.translate.instant('app.error')` as the fallback — never a hardcoded string.

## Known Remaining Gaps

1. **List-view display names** — `ticket.statusName`, `ticket.priorityName`, `ticket.categoryName` in list/table views are plain strings from the server DTO. These need either a server-side `Accept-Language`-aware response or client-side reference data lookup to translate.
2. **LOCALE_ID is static** — Date formatting only reflects the locale set at injection time. Switching language mid-session does not update date formats without a reload.
3. **Ticket history field names and values** — The history table shows raw field names (e.g., "Status", "Priority") and values from the server, untranslated.
