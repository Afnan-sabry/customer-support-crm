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
