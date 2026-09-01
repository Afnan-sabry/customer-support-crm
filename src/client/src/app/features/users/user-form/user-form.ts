import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { UsersService } from '../users.service';
import { RolesService, RoleDto } from '../../roles/roles.service';

@Component({
  selector: 'app-user-form',
  imports: [
    ReactiveFormsModule, TranslateModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatChipsModule, MatIconModule, MatSlideToggleModule
  ],
  template: `
    <mat-card class="form-card">
      <mat-card-header>
        <mat-card-title>{{ (isEditMode ? 'users.editUser' : 'users.createUser') | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'users.email' | translate }}</mat-label>
            <input matInput formControlName="email" type="email" />
          </mat-form-field>

          @if (!isEditMode) {
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>{{ 'users.password' | translate }}</mat-label>
              <input matInput formControlName="password" type="password" />
            </mat-form-field>
          }

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'users.fullName' | translate }}</mat-label>
            <input matInput formControlName="fullName" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'users.fullNameAr' | translate }}</mat-label>
            <input matInput formControlName="fullNameAr" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'users.phone' | translate }}</mat-label>
            <input matInput formControlName="phone" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'users.language' | translate }}</mat-label>
            <mat-select formControlName="preferredLanguage">
              <mat-option value="en">English</mat-option>
              <mat-option value="ar">العربية</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'users.roles' | translate }}</mat-label>
            <mat-select formControlName="roleNames" multiple>
              @for (role of availableRoles; track role.id) {
                <mat-option [value]="role.name">{{ role.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          @if (isEditMode) {
            <div class="active-toggle">
              <mat-slide-toggle formControlName="isActive">{{ 'users.active' | translate }}</mat-slide-toggle>
            </div>
          }

          @if (error) {
            <p class="error-message">{{ error }}</p>
          }

          <div class="form-actions">
            <button mat-button type="button" (click)="onCancel()">{{ 'common.cancel' | translate }}</button>
            <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || saving">
              {{ 'common.save' | translate }}
            </button>
          </div>
        </form>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .form-card { max-width: 600px; margin: 0 auto; }
    .full-width { width: 100%; }
    .active-toggle { margin-block-end: 16px; }
    .form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-block-start: 16px; }
    .error-message { color: #f44336; margin-block-end: 16px; font-size: 14px; }
  `]
})
export class UserFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private usersService = inject(UsersService);
  private rolesService = inject(RolesService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  isEditMode = false;
  userId: string | null = null;
  availableRoles: RoleDto[] = [];
  saving = false;
  error: string | null = null;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    fullName: ['', [Validators.required]],
    fullNameAr: ['', [Validators.required]],
    phone: [''],
    preferredLanguage: ['en', [Validators.required]],
    roleNames: [[] as string[]],
    isActive: [true]
  });

  ngOnInit(): void {
    this.rolesService.getRoles().subscribe(roles => this.availableRoles = roles);

    this.userId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.userId;

    if (this.isEditMode) {
      this.form.get('password')?.clearValidators();
      this.form.get('password')?.updateValueAndValidity();
      this.usersService.getUserById(this.userId!).subscribe(user => {
        this.form.patchValue({
          email: user.email,
          fullName: user.fullName,
          fullNameAr: user.fullNameAr,
          phone: user.phone ?? '',
          preferredLanguage: user.preferredLanguage,
          roleNames: user.roles,
          isActive: user.isActive
        });
        this.form.get('email')?.disable();
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.saving = true;
    this.error = null;
    const value = this.form.getRawValue();

    if (this.isEditMode) {
      this.usersService.updateUser(this.userId!, {
        fullName: value.fullName!,
        fullNameAr: value.fullNameAr!,
        phone: value.phone || undefined,
        preferredLanguage: value.preferredLanguage!,
        isActive: value.isActive!,
        roleNames: value.roleNames!
      }).subscribe({
        next: () => this.router.navigate(['/admin/users']),
        error: (err) => this.handleError(err)
      });
    } else {
      this.usersService.createUser({
        email: value.email!,
        password: value.password!,
        fullName: value.fullName!,
        fullNameAr: value.fullNameAr!,
        phone: value.phone || undefined,
        preferredLanguage: value.preferredLanguage!,
        roleNames: value.roleNames!
      }).subscribe({
        next: () => this.router.navigate(['/admin/users']),
        error: (err) => this.handleError(err)
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/admin/users']);
  }

  private handleError(err: any): void {
    this.saving = false;
    this.error = err.error?.detail || err.error?.title || 'An error occurred';
  }
}
