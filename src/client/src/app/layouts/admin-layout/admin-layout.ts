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
          <a mat-list-item routerLink="/admin/knowledge" routerLinkActive="active">
            <mat-icon matListItemIcon>menu_book</mat-icon>
            <span>{{ 'nav.knowledgeBase' | translate }}</span>
          </a>
          <a mat-list-item routerLink="/admin/reports" routerLinkActive="active">
            <mat-icon matListItemIcon>bar_chart</mat-icon>
            <span>{{ 'nav.reports' | translate }}</span>
          </a>
          <a mat-list-item routerLink="/admin/sla" routerLinkActive="active">
            <mat-icon matListItemIcon>timer</mat-icon>
            <span>{{ 'sla.title' | translate }}</span>
          </a>
          <a mat-list-item routerLink="/admin/escalation" routerLinkActive="active">
            <mat-icon matListItemIcon>trending_up</mat-icon>
            <span>{{ 'escalation.title' | translate }}</span>
          </a>
          <a mat-list-item routerLink="/admin/assignment" routerLinkActive="active">
            <mat-icon matListItemIcon>assignment_ind</mat-icon>
            <span>{{ 'assignment.title' | translate }}</span>
          </a>
          <a mat-list-item routerLink="/admin/users" routerLinkActive="active">
            <mat-icon matListItemIcon>manage_accounts</mat-icon>
            <span>{{ 'nav.users' | translate }}</span>
          </a>
          <a mat-list-item routerLink="/admin/roles" routerLinkActive="active">
            <mat-icon matListItemIcon>admin_panel_settings</mat-icon>
            <span>{{ 'nav.roles' | translate }}</span>
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
