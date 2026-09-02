import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { PortalApiService } from '../portal-api.service';
import { PortalAuthService, PortalUserInfo } from '../portal-auth.service';

interface PortalProfileDto {
  id: string; email: string; fullName: string; fullNameAr: string; phone: string | null; customerId: string;
}

@Component({
  selector: 'app-portal-profile',
  imports: [
    ReactiveFormsModule, TranslateModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule
  ],
  template: `
    <mat-card class="form-card">
      <mat-card-header>
        <mat-card-title>{{ 'portal.profileTitle' | translate }}</mat-card-title>
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
            <mat-label>{{ 'portal.phone' | translate }}</mat-label>
            <input matInput formControlName="phone" />
          </mat-form-field>
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'portal.newPassword' | translate }}</mat-label>
            <input matInput formControlName="newPassword" type="password" [placeholder]="'portal.newPasswordHint' | translate" />
          </mat-form-field>

          @if (success) {
            <p class="success-message">{{ 'portal.profileUpdated' | translate }}</p>
          }
          @if (error) {
            <p class="error-message">{{ error }}</p>
          }

          <div class="form-actions">
            <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || saving">
              {{ 'common.save' | translate }}
            </button>
          </div>
        </form>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .form-card { max-width: 500px; margin: 0 auto; }
    .full-width { width: 100%; }
    .form-actions { display: flex; justify-content: flex-end; margin-block-start: 16px; }
    .error-message { color: #f44336; font-size: 14px; }
    .success-message { color: #4caf50; font-size: 14px; }
  `]
})
export class PortalProfileComponent extends PortalApiService implements OnInit {
  private fb = inject(FormBuilder);
  private portalAuthService = inject(PortalAuthService);

  saving = false;
  success = false;
  error: string | null = null;

  form = this.fb.group({
    fullName: ['', [Validators.required]],
    fullNameAr: ['', [Validators.required]],
    phone: [''],
    newPassword: ['']
  });

  ngOnInit(): void {
    this.get<PortalProfileDto>('/v1/portal/profile').subscribe(profile => {
      this.form.patchValue({
        fullName: profile.fullName,
        fullNameAr: profile.fullNameAr,
        phone: profile.phone || ''
      });
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.saving = true;
    this.success = false;
    this.error = null;
    const value = this.form.getRawValue();

    this.put<any>('/v1/portal/profile', {
      fullName: value.fullName,
      fullNameAr: value.fullNameAr,
      phone: value.phone || null,
      newPassword: value.newPassword || null
    }).subscribe({
      next: () => {
        this.saving = false;
        this.success = true;
        const current = this.portalAuthService.getCurrentUser();
        if (current) {
          const updated: PortalUserInfo = {
            ...current,
            fullName: value.fullName!,
            fullNameAr: value.fullNameAr!,
            phone: value.phone || null
          };
          this.portalAuthService.updateStoredUser(updated);
        }
        this.form.patchValue({ newPassword: '' });
      },
      error: (err) => {
        this.saving = false;
        this.error = err.error?.detail || err.error?.title || 'Failed to update profile';
      }
    });
  }
}
