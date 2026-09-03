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

@Component({
  selector: 'app-portal-register',
  imports: [
    ReactiveFormsModule, RouterLink, TranslateModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  template: `
    <div class="register-container">
      <mat-card class="register-card">
        <mat-card-header>
          <mat-card-title>{{ 'portal.registerTitle' | translate }}</mat-card-title>
          <mat-card-subtitle>{{ 'portal.registerSubtitle' | translate }}</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="onSubmit()">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>{{ 'portal.fullName' | translate }}</mat-label>
              <input matInput formControlName="fullName" />
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>{{ 'portal.fullNameAr' | translate }}</mat-label>
              <input matInput formControlName="fullNameAr" />
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>{{ 'portal.email' | translate }}</mat-label>
              <input matInput formControlName="email" type="email" />
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>{{ 'portal.phone' | translate }}</mat-label>
              <input matInput formControlName="phone" />
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
                {{ 'portal.register' | translate }}
              }
            </button>
          </form>
        </mat-card-content>
        <mat-card-actions align="end">
          <a mat-button routerLink="/portal/login">{{ 'portal.haveAccount' | translate }}</a>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .register-container {
      display: flex; justify-content: center; align-items: center;
      min-height: calc(100vh - 64px); background-color: #f5f5f5; padding: 24px 0;
    }
    .register-card { max-width: 440px; width: 100%; padding: 24px; }
    .full-width { width: 100%; }
    .error-message { color: #f44336; margin-block-end: 16px; font-size: 14px; }
  `]
})
export class PortalRegisterComponent {
  private fb = inject(FormBuilder);
  private portalAuthService = inject(PortalAuthService);
  private router = inject(Router);

  form = this.fb.group({
    fullName: ['', [Validators.required]],
    fullNameAr: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  hidePassword = true;
  loading = false;
  error: string | null = null;

  onSubmit(): void {
    if (this.form.invalid) return;

    this.loading = true;
    this.error = null;
    const value = this.form.getRawValue();

    this.portalAuthService.register({
      email: value.email!,
      password: value.password!,
      fullName: value.fullName!,
      fullNameAr: value.fullNameAr!,
      phone: value.phone || undefined
    }).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/portal/home']);
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.detail || err.error?.title || 'Registration failed';
      }
    });
  }
}
