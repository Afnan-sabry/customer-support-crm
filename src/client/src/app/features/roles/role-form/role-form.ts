import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { RolesService, RoleDto, PermissionDto } from '../roles.service';

@Component({
  selector: 'app-role-form',
  imports: [
    ReactiveFormsModule, TranslateModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatCheckboxModule
  ],
  template: `
    @if (isPermissionsMode) {
      <mat-card class="form-card permissions-card">
        <mat-card-header>
          <mat-card-title>{{ 'roles.assignPermissions' | translate }} - {{ role?.name }}</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          @for (module of moduleKeys; track module) {
            <div class="module-group">
              <h3>{{ module }}</h3>
              @for (permission of permissionsByModule[module]; track permission.id) {
                <div class="permission-row">
                  <mat-checkbox
                    [checked]="selectedPermissionIds.has(permission.id)"
                    (change)="onPermissionToggle(permission.id, $event.checked)">
                    {{ permission.description || permission.key }}
                  </mat-checkbox>
                </div>
              }
            </div>
          }

          @if (error) {
            <p class="error-message">{{ error }}</p>
          }

          <div class="form-actions">
            <button mat-button type="button" (click)="onCancel()">{{ 'common.cancel' | translate }}</button>
            <button mat-raised-button color="primary" type="button" (click)="onSavePermissions()" [disabled]="saving">
              {{ 'common.save' | translate }}
            </button>
          </div>
        </mat-card-content>
      </mat-card>
    } @else {
      <mat-card class="form-card">
        <mat-card-header>
          <mat-card-title>{{ (isEditMode ? 'roles.editRole' : 'roles.createRole') | translate }}</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="onSubmit()">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>{{ 'roles.name' | translate }}</mat-label>
              <input matInput formControlName="name" />
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>{{ 'roles.nameAr' | translate }}</mat-label>
              <input matInput formControlName="nameAr" />
            </mat-form-field>

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
    }
  `,
  styles: [`
    .form-card { max-width: 600px; margin: 0 auto; }
    .permissions-card { max-width: 800px; }
    .full-width { width: 100%; }
    .module-group { margin-block-end: 16px; }
    .module-group h3 { text-transform: capitalize; margin-block-end: 8px; }
    .permission-row { margin-block-end: 4px; }
    .form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-block-start: 16px; }
    .error-message { color: #f44336; margin-block-end: 16px; font-size: 14px; }
  `]
})
export class RoleFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private rolesService = inject(RolesService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  isEditMode = false;
  isPermissionsMode = false;
  roleId: string | null = null;
  role: RoleDto | null = null;
  saving = false;
  error: string | null = null;

  allPermissions: PermissionDto[] = [];
  permissionsByModule: Record<string, PermissionDto[]> = {};
  moduleKeys: string[] = [];
  selectedPermissionIds = new Set<string>();

  form = this.fb.group({
    name: ['', [Validators.required]],
    nameAr: ['', [Validators.required]]
  });

  ngOnInit(): void {
    this.roleId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.roleId;
    this.isPermissionsMode = this.route.snapshot.url.some(segment => segment.path === 'permissions');

    if (this.isPermissionsMode) {
      this.loadPermissionsMode();
    } else if (this.isEditMode) {
      this.rolesService.getRoles().subscribe(roles => {
        const found = roles.find(r => r.id === this.roleId);
        if (found) {
          this.role = found;
          this.form.patchValue({ name: found.name, nameAr: found.nameAr });
        }
      });
    }
  }

  private loadPermissionsMode(): void {
    this.rolesService.getPermissions().subscribe(permissions => {
      this.allPermissions = permissions;
      this.permissionsByModule = {};
      for (const permission of permissions) {
        if (!this.permissionsByModule[permission.module]) {
          this.permissionsByModule[permission.module] = [];
        }
        this.permissionsByModule[permission.module].push(permission);
      }
      this.moduleKeys = Object.keys(this.permissionsByModule);
    });

    this.rolesService.getRoles().subscribe(roles => {
      const found = roles.find(r => r.id === this.roleId);
      if (found) {
        this.role = found;
        this.selectedPermissionIds = new Set(found.permissions.map(p => p.id));
      }
    });
  }

  onPermissionToggle(permissionId: string, checked: boolean): void {
    if (checked) {
      this.selectedPermissionIds.add(permissionId);
    } else {
      this.selectedPermissionIds.delete(permissionId);
    }
  }

  onSavePermissions(): void {
    if (!this.roleId) return;
    this.saving = true;
    this.error = null;
    this.rolesService.assignPermissions(this.roleId, Array.from(this.selectedPermissionIds)).subscribe({
      next: () => this.router.navigate(['/admin/roles']),
      error: (err) => this.handleError(err)
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.saving = true;
    this.error = null;
    const value = this.form.getRawValue();

    if (this.isEditMode) {
      this.rolesService.updateRole(this.roleId!, {
        roleId: this.roleId!,
        name: value.name!,
        nameAr: value.nameAr!
      }).subscribe({
        next: () => this.router.navigate(['/admin/roles']),
        error: (err) => this.handleError(err)
      });
    } else {
      this.rolesService.createRole({
        name: value.name!,
        nameAr: value.nameAr!
      }).subscribe({
        next: () => this.router.navigate(['/admin/roles']),
        error: (err) => this.handleError(err)
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/admin/roles']);
  }

  private handleError(err: any): void {
    this.saving = false;
    this.error = err.error?.detail || err.error?.title || 'An error occurred';
  }
}
