import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PortalAuthService } from '../portal-auth.service';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-portal-login',
  imports: [
    ReactiveFormsModule, RouterLink, TranslateModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  template: `
    <div class="login-container">
      <mat-card class="login-card">
        <mat-card-header>
          <mat-card-title>{{ 'portal.title' | translate }}</mat-card-title>
          <mat-card-subtitle>{{ 'portal.loginSubtitle' | translate }}</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="onSubmit()">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>{{ 'portal.email' | translate }}</mat-label>
              <input matInput formControlName="email" type="email" />
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>{{ 'portal.password' | translate }}</mat-label>
              <input matInput formControlName="password" [type]="hidePassword ? 'password' : 'text'" />
              <button mat-icon-button matSuffix type="button" (click)="hidePassword = !hidePassword">
                <mat-icon>{{ hidePassword ? 'visibility_off' : 'visibility' }}</mat-icon>
              </button>
            </mat-form-field>
            @if (error) {
              <p class="error-message">{{ error }}</p>
            }
            <button mat-raised-button color="primary" type="submit" class="full-width" [disabled]="form.invalid || loading">
              @if (loading) {
                <mat-spinner diameter="20"></mat-spinner>
              } @else {
                {{ 'portal.login' | translate }}
              }
            </button>
          </form>
        </mat-card-content>
        <mat-card-actions align="end" class="actions-row">
          <a mat-button routerLink="/portal/register">{{ 'portal.needAccount' | translate }}</a>
          <button mat-button type="button" (click)="toggleLanguage()">
            <mat-icon>language</mat-icon>
            {{ languageService.getCurrentLanguage() === 'en' ? 'العربية' : 'English' }}
          </button>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .login-container {
      display: flex; justify-content: center; align-items: center;
      min-height: calc(100vh - 64px); background-color: #f5f5f5;
    }
    .login-card { max-width: 400px; width: 100%; padding: 24px; }
    .full-width { width: 100%; }
    .actions-row { display: flex; justify-content: space-between; }
    .error-message { color: #f44336; margin-block-end: 16px; font-size: 14px; }
  `]
})
export class PortalLoginComponent {
  private fb = inject(FormBuilder);
  private portalAuthService = inject(PortalAuthService);
  private router = inject(Router);
  protected languageService = inject(LanguageService);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  hidePassword = true;
  loading = false;
  error: string | null = null;

  onSubmit(): void {
    if (this.form.invalid) return;

    this.loading = true;
    this.error = null;

    this.portalAuthService.login({
      email: this.form.value.email!,
      password: this.form.value.password!
    }).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/portal/home']);
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.detail || err.error?.title || 'Login failed';
      }
    });
  }

  toggleLanguage(): void {
    const current = this.languageService.getCurrentLanguage();
    this.languageService.switchLanguage(current === 'en' ? 'ar' : 'en');
  }
}
