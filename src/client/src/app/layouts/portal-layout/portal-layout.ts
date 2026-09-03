import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { LanguageService } from '../../core/services/language.service';
import { PortalAuthService } from '../../features/portal/portal-auth.service';
import { ChatWidgetComponent } from '../../shared/components/chat-widget/chat-widget';

@Component({
  selector: 'app-portal-layout',
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, TranslateModule,
    MatToolbarModule, MatButtonModule, MatIconModule, MatMenuModule, ChatWidgetComponent
  ],
  template: `
    <mat-toolbar color="primary">
      <span class="brand">{{ 'portal.title' | translate }}</span>
      @if (isAuthenticated) {
        <nav class="nav-links">
          <a mat-button routerLink="/portal/home" routerLinkActive="active-link">
            {{ 'portal.navHome' | translate }}
          </a>
          <a mat-button routerLink="/portal/tickets" routerLinkActive="active-link">
            {{ 'portal.navTickets' | translate }}
          </a>
          <a mat-button routerLink="/portal/knowledge" routerLinkActive="active-link">
            {{ 'portal.navKnowledge' | translate }}
          </a>
        </nav>
      }
      <span class="spacer"></span>
      <button mat-icon-button (click)="toggleLanguage()">
        <mat-icon>language</mat-icon>
      </button>
      @if (isAuthenticated) {
        <button mat-icon-button [matMenuTriggerFor]="userMenu">
          <mat-icon>account_circle</mat-icon>
        </button>
        <mat-menu #userMenu="matMenu">
          <button mat-menu-item routerLink="/portal/profile">
            <mat-icon>person</mat-icon>
            <span>{{ 'portal.navProfile' | translate }}</span>
          </button>
          <button mat-menu-item (click)="logout()">
            <mat-icon>logout</mat-icon>
            <span>{{ 'auth.logout' | translate }}</span>
          </button>
        </mat-menu>
      }
    </mat-toolbar>
    <main class="portal-content">
      <router-outlet />
    </main>
    @if (isAuthenticated) {
      <app-chat-widget />
    }
  `,
  styles: [`
    .brand { font-weight: 500; }
    .nav-links { display: flex; gap: 4px; margin-inline-start: 24px; }
    .active-link { background: rgba(255,255,255,0.15); }
    .spacer { flex: 1 1 auto; }
    .portal-content { padding: 24px; max-width: 960px; margin-inline: auto; }
  `]
})
export class PortalLayoutComponent {
  private languageService = inject(LanguageService);
  private portalAuthService = inject(PortalAuthService);

  get isAuthenticated(): boolean {
    return this.portalAuthService.isAuthenticated();
  }

  toggleLanguage(): void {
    const current = this.languageService.getCurrentLanguage();
    this.languageService.switchLanguage(current === 'en' ? 'ar' : 'en');
  }

  logout(): void {
    this.portalAuthService.logout();
  }
}
