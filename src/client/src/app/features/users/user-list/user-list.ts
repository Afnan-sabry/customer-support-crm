import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { UsersService, UserDetail } from '../users.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-user-list',
  imports: [
    RouterLink, TranslateModule,
    MatTableModule, MatPaginatorModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatChipsModule, MatDialogModule
  ],
  template: `
    <div class="user-list-header">
      <h1>{{ 'users.title' | translate }}</h1>
      <button mat-raised-button color="primary" routerLink="/admin/users/new">
        <mat-icon>add</mat-icon>
        {{ 'users.createUser' | translate }}
      </button>
    </div>

    <mat-form-field appearance="outline" class="search-field">
      <mat-label>{{ 'common.search' | translate }}</mat-label>
      <input matInput (keyup)="onSearchInput($event)" [value]="search" />
      <mat-icon matSuffix>search</mat-icon>
    </mat-form-field>

    <table mat-table [dataSource]="users" class="mat-elevation-z2 full-width">
      <ng-container matColumnDef="fullName">
        <th mat-header-cell *matHeaderCellDef>{{ 'users.fullName' | translate }}</th>
        <td mat-cell *matCellDef="let user">{{ user.fullName }}</td>
      </ng-container>

      <ng-container matColumnDef="email">
        <th mat-header-cell *matHeaderCellDef>{{ 'users.email' | translate }}</th>
        <td mat-cell *matCellDef="let user">{{ user.email }}</td>
      </ng-container>

      <ng-container matColumnDef="roles">
        <th mat-header-cell *matHeaderCellDef>{{ 'users.roles' | translate }}</th>
        <td mat-cell *matCellDef="let user">{{ user.roles.join(', ') }}</td>
      </ng-container>

      <ng-container matColumnDef="isActive">
        <th mat-header-cell *matHeaderCellDef>{{ 'common.actions' | translate }}</th>
        <td mat-cell *matCellDef="let user">
          <mat-chip [color]="user.isActive ? 'primary' : 'warn'" selected>
            {{ (user.isActive ? 'users.active' : 'users.inactive') | translate }}
          </mat-chip>
        </td>
      </ng-container>

      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef>{{ 'common.actions' | translate }}</th>
        <td mat-cell *matCellDef="let user">
          <button mat-icon-button [routerLink]="['/admin/users', user.id, 'edit']">
            <mat-icon>edit</mat-icon>
          </button>
          <button mat-icon-button color="warn" (click)="onDeactivate(user)" [disabled]="!user.isActive">
            <mat-icon>block</mat-icon>
          </button>
        </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
    </table>

    <mat-paginator
      [length]="totalCount"
      [pageSize]="pageSize"
      [pageIndex]="page - 1"
      [pageSizeOptions]="[10, 20, 50]"
      (page)="onPageChange($event)">
    </mat-paginator>
  `,
  styles: [`
    .user-list-header { display: flex; justify-content: space-between; align-items: center; margin-block-end: 16px; }
    .search-field { width: 100%; max-width: 400px; margin-block-end: 16px; }
    .full-width { width: 100%; }
  `]
})
export class UserListComponent implements OnInit {
  private usersService = inject(UsersService);
  private dialog = inject(MatDialog);
  private translate = inject(TranslateService);

  displayedColumns = ['fullName', 'email', 'roles', 'isActive', 'actions'];
  users: UserDetail[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  search = '';

  private searchSubject = new Subject<string>();

  ngOnInit(): void {
    this.searchSubject.pipe(debounceTime(300), distinctUntilChanged()).subscribe(value => {
      this.search = value;
      this.page = 1;
      this.loadUsers();
    });
    this.loadUsers();
  }

  loadUsers(): void {
    this.usersService.getUsers({ search: this.search, page: this.page, pageSize: this.pageSize }).subscribe(result => {
      this.users = result.items;
      this.totalCount = result.totalCount;
      this.page = result.page;
      this.pageSize = result.pageSize;
    });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchSubject.next(value);
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadUsers();
  }

  onDeactivate(user: UserDetail): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: this.translate.instant('users.deactivate'),
        message: this.translate.instant('users.deactivateConfirm')
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.usersService.deactivateUser(user.id).subscribe(() => this.loadUsers());
      }
    });
  }
}
