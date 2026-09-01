import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonToggleModule, MatButtonToggleChange } from '@angular/material/button-toggle';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { CustomersService, CustomerDto } from '../customers.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';

type ActiveFilter = 'all' | 'active' | 'inactive';

@Component({
  selector: 'app-customer-list',
  imports: [
    RouterLink, TranslateModule,
    MatTableModule, MatPaginatorModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatChipsModule, MatButtonToggleModule, MatDialogModule
  ],
  template: `
    <div class="customer-list-header">
      <h1>{{ 'customers.title' | translate }}</h1>
      <button mat-raised-button color="primary" routerLink="/admin/customers/new">
        <mat-icon>add</mat-icon>
        {{ 'customers.createCustomer' | translate }}
      </button>
    </div>

    <div class="filters">
      <mat-form-field appearance="outline" class="search-field">
        <mat-label>{{ 'common.search' | translate }}</mat-label>
        <input matInput (keyup)="onSearchInput($event)" [value]="search" />
        <mat-icon matSuffix>search</mat-icon>
      </mat-form-field>

      <mat-button-toggle-group [value]="activeFilter" (change)="onActiveFilterChange($event)">
        <mat-button-toggle value="all">{{ 'common.filter' | translate }}</mat-button-toggle>
        <mat-button-toggle value="active">{{ 'customers.active' | translate }}</mat-button-toggle>
        <mat-button-toggle value="inactive">{{ 'customers.inactive' | translate }}</mat-button-toggle>
      </mat-button-toggle-group>
    </div>

    <table mat-table [dataSource]="customers" class="mat-elevation-z2 full-width">
      <ng-container matColumnDef="name">
        <th mat-header-cell *matHeaderCellDef>{{ 'customers.name' | translate }}</th>
        <td mat-cell *matCellDef="let customer">{{ customer.name }}</td>
      </ng-container>

      <ng-container matColumnDef="nameAr">
        <th mat-header-cell *matHeaderCellDef>{{ 'customers.nameAr' | translate }}</th>
        <td mat-cell *matCellDef="let customer">{{ customer.nameAr }}</td>
      </ng-container>

      <ng-container matColumnDef="email">
        <th mat-header-cell *matHeaderCellDef>{{ 'customers.email' | translate }}</th>
        <td mat-cell *matCellDef="let customer">{{ customer.email }}</td>
      </ng-container>

      <ng-container matColumnDef="phone">
        <th mat-header-cell *matHeaderCellDef>{{ 'customers.phone' | translate }}</th>
        <td mat-cell *matCellDef="let customer">{{ customer.phone }}</td>
      </ng-container>

      <ng-container matColumnDef="company">
        <th mat-header-cell *matHeaderCellDef>{{ 'customers.company' | translate }}</th>
        <td mat-cell *matCellDef="let customer">{{ customer.company }}</td>
      </ng-container>

      <ng-container matColumnDef="isActive">
        <th mat-header-cell *matHeaderCellDef>{{ 'customers.active' | translate }}</th>
        <td mat-cell *matCellDef="let customer">
          <mat-chip [color]="customer.isActive ? 'primary' : 'warn'" selected>
            {{ (customer.isActive ? 'customers.active' : 'customers.inactive') | translate }}
          </mat-chip>
        </td>
      </ng-container>

      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef>{{ 'common.actions' | translate }}</th>
        <td mat-cell *matCellDef="let customer">
          <button mat-icon-button [routerLink]="['/admin/customers', customer.id]">
            <mat-icon>visibility</mat-icon>
          </button>
          <button mat-icon-button [routerLink]="['/admin/customers', customer.id, 'edit']">
            <mat-icon>edit</mat-icon>
          </button>
          <button mat-icon-button color="warn" (click)="onDelete(customer)">
            <mat-icon>delete</mat-icon>
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
    .customer-list-header { display: flex; justify-content: space-between; align-items: center; margin-block-end: 16px; }
    .filters { display: flex; align-items: center; gap: 16px; margin-block-end: 16px; flex-wrap: wrap; }
    .search-field { width: 100%; max-width: 400px; }
    .full-width { width: 100%; }
  `]
})
export class CustomerListComponent implements OnInit {
  private customersService = inject(CustomersService);
  private dialog = inject(MatDialog);
  private translate = inject(TranslateService);

  displayedColumns = ['name', 'nameAr', 'email', 'phone', 'company', 'isActive', 'actions'];
  customers: CustomerDto[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  search = '';
  activeFilter: ActiveFilter = 'all';

  private searchSubject = new Subject<string>();

  ngOnInit(): void {
    this.searchSubject.pipe(debounceTime(300), distinctUntilChanged()).subscribe(value => {
      this.search = value;
      this.page = 1;
      this.loadCustomers();
    });
    this.loadCustomers();
  }

  loadCustomers(): void {
    const isActive = this.activeFilter === 'all' ? undefined : this.activeFilter === 'active';
    this.customersService.getCustomers({
      search: this.search,
      isActive,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe(result => {
      this.customers = result.items;
      this.totalCount = result.totalCount;
      this.page = result.page;
      this.pageSize = result.pageSize;
    });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchSubject.next(value);
  }

  onActiveFilterChange(event: MatButtonToggleChange): void {
    this.activeFilter = event.value;
    this.page = 1;
    this.loadCustomers();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadCustomers();
  }

  onDelete(customer: CustomerDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: this.translate.instant('common.delete'),
        message: this.translate.instant('customers.deleteConfirm')
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.customersService.deleteCustomer(customer.id).subscribe(() => this.loadCustomers());
      }
    });
  }
}
