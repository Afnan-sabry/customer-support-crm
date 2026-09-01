import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { RolesService, RoleDto } from '../roles.service';

@Component({
  selector: 'app-role-list',
  imports: [
    RouterLink, TranslateModule,
    MatTableModule, MatButtonModule, MatIconModule, MatChipsModule
  ],
  template: `
    <div class="role-list-header">
      <h1>{{ 'roles.title' | translate }}</h1>
      <button mat-raised-button color="primary" routerLink="/admin/roles/new">
        <mat-icon>add</mat-icon>
        {{ 'roles.createRole' | translate }}
      </button>
    </div>

    <table mat-table [dataSource]="roles" class="mat-elevation-z2 full-width">
      <ng-container matColumnDef="name">
        <th mat-header-cell *matHeaderCellDef>{{ 'roles.name' | translate }}</th>
        <td mat-cell *matCellDef="let role">{{ role.name }}</td>
      </ng-container>

      <ng-container matColumnDef="nameAr">
        <th mat-header-cell *matHeaderCellDef>{{ 'roles.nameAr' | translate }}</th>
        <td mat-cell *matCellDef="let role">{{ role.nameAr }}</td>
      </ng-container>

      <ng-container matColumnDef="isSystem">
        <th mat-header-cell *matHeaderCellDef>{{ 'roles.system' | translate }}</th>
        <td mat-cell *matCellDef="let role">
          @if (role.isSystem) {
            <mat-chip color="primary" selected>{{ 'roles.system' | translate }}</mat-chip>
          }
        </td>
      </ng-container>

      <ng-container matColumnDef="permissionsCount">
        <th mat-header-cell *matHeaderCellDef>{{ 'roles.permissions' | translate }}</th>
        <td mat-cell *matCellDef="let role">{{ role.permissions.length }}</td>
      </ng-container>

      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef>{{ 'common.actions' | translate }}</th>
        <td mat-cell *matCellDef="let role">
          <button mat-icon-button [routerLink]="['/admin/roles', role.id, 'edit']">
            <mat-icon>edit</mat-icon>
          </button>
          <button mat-icon-button [routerLink]="['/admin/roles', role.id, 'permissions']">
            <mat-icon>lock</mat-icon>
          </button>
        </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
    </table>
  `,
  styles: [`
    .role-list-header { display: flex; justify-content: space-between; align-items: center; margin-block-end: 16px; }
    .full-width { width: 100%; }
  `]
})
export class RoleListComponent implements OnInit {
  private rolesService = inject(RolesService);

  displayedColumns = ['name', 'nameAr', 'isSystem', 'permissionsCount', 'actions'];
  roles: RoleDto[] = [];

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles(): void {
    this.rolesService.getRoles().subscribe(roles => this.roles = roles);
  }
}
