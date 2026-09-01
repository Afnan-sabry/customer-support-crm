import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { CustomersService, CustomerDetailDto, CustomerContactDto } from '../customers.service';

@Component({
  selector: 'app-customer-detail',
  imports: [
    RouterLink, ReactiveFormsModule, TranslateModule,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatFormFieldModule, MatInputModule, MatCheckboxModule
  ],
  template: `
    @if (customer) {
      <div class="detail-header">
        <h1>{{ 'customers.customerDetail' | translate }}</h1>
        <div class="header-actions">
          <button mat-button routerLink="/admin/customers">
            <mat-icon>arrow_back</mat-icon>
            {{ 'common.back' | translate }}
          </button>
          <button mat-raised-button color="primary" [routerLink]="['/admin/customers', customer.id, 'edit']">
            <mat-icon>edit</mat-icon>
            {{ 'common.edit' | translate }}
          </button>
        </div>
      </div>

      <mat-card class="info-card">
        <mat-card-content>
          <div class="info-grid">
            <div class="info-item">
              <span class="label">{{ 'customers.name' | translate }}</span>
              <span class="value">{{ customer.name }}</span>
            </div>
            <div class="info-item">
              <span class="label">{{ 'customers.nameAr' | translate }}</span>
              <span class="value">{{ customer.nameAr }}</span>
            </div>
            <div class="info-item">
              <span class="label">{{ 'customers.email' | translate }}</span>
              <span class="value">{{ customer.email }}</span>
            </div>
            <div class="info-item">
              <span class="label">{{ 'customers.phone' | translate }}</span>
              <span class="value">{{ customer.phone }}</span>
            </div>
            <div class="info-item">
              <span class="label">{{ 'customers.company' | translate }}</span>
              <span class="value">{{ customer.company }}</span>
            </div>
            <div class="info-item">
              <span class="label">{{ 'customers.companyAr' | translate }}</span>
              <span class="value">{{ customer.companyAr }}</span>
            </div>
            <div class="info-item">
              <span class="label">{{ 'customers.address' | translate }}</span>
              <span class="value">{{ customer.address }}</span>
            </div>
            <div class="info-item">
              <span class="label">{{ 'common.actions' | translate }}</span>
              <mat-chip [color]="customer.isActive ? 'primary' : 'warn'" selected>
                {{ (customer.isActive ? 'customers.active' : 'customers.inactive') | translate }}
              </mat-chip>
            </div>
          </div>
        </mat-card-content>
      </mat-card>

      <mat-card class="contacts-card">
        <mat-card-header>
          <mat-card-title>{{ 'customers.contacts' | translate }}</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          @if (customer.contacts.length > 0) {
            <table mat-table [dataSource]="customer.contacts" class="full-width">
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef>{{ 'customers.contactName' | translate }}</th>
                <td mat-cell *matCellDef="let contact">{{ contact.name }}</td>
              </ng-container>

              <ng-container matColumnDef="email">
                <th mat-header-cell *matHeaderCellDef>{{ 'customers.email' | translate }}</th>
                <td mat-cell *matCellDef="let contact">{{ contact.email }}</td>
              </ng-container>

              <ng-container matColumnDef="phone">
                <th mat-header-cell *matHeaderCellDef>{{ 'customers.phone' | translate }}</th>
                <td mat-cell *matCellDef="let contact">{{ contact.phone }}</td>
              </ng-container>

              <ng-container matColumnDef="title">
                <th mat-header-cell *matHeaderCellDef>{{ 'customers.contactTitle' | translate }}</th>
                <td mat-cell *matCellDef="let contact">{{ contact.title }}</td>
              </ng-container>

              <ng-container matColumnDef="isPrimary">
                <th mat-header-cell *matHeaderCellDef>{{ 'customers.primaryContact' | translate }}</th>
                <td mat-cell *matCellDef="let contact">
                  @if (contact.isPrimary) {
                    <mat-chip color="primary" selected>{{ 'customers.primaryContact' | translate }}</mat-chip>
                  }
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="contactColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: contactColumns;"></tr>
            </table>
          } @else {
            <p>{{ 'customers.noContacts' | translate }}</p>
          }

          @if (!showContactForm) {
            <button mat-stroked-button color="primary" class="add-contact-btn" (click)="showContactForm = true">
              <mat-icon>add</mat-icon>
              {{ 'customers.addContact' | translate }}
            </button>
          } @else {
            <form [formGroup]="contactForm" (ngSubmit)="onAddContact()" class="contact-form">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'customers.contactName' | translate }}</mat-label>
                <input matInput formControlName="name" />
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'customers.contactNameAr' | translate }}</mat-label>
                <input matInput formControlName="nameAr" />
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'customers.email' | translate }}</mat-label>
                <input matInput formControlName="email" type="email" />
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'customers.phone' | translate }}</mat-label>
                <input matInput formControlName="phone" />
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'customers.contactTitle' | translate }}</mat-label>
                <input matInput formControlName="title" />
              </mat-form-field>

              <mat-checkbox formControlName="isPrimary">{{ 'customers.primaryContact' | translate }}</mat-checkbox>

              @if (contactError) {
                <p class="error-message">{{ contactError }}</p>
              }

              <div class="form-actions">
                <button mat-button type="button" (click)="cancelContactForm()">{{ 'common.cancel' | translate }}</button>
                <button mat-raised-button color="primary" type="submit" [disabled]="contactForm.invalid || savingContact">
                  {{ 'common.save' | translate }}
                </button>
              </div>
            </form>
          }
        </mat-card-content>
      </mat-card>
    }
  `,
  styles: [`
    .detail-header { display: flex; justify-content: space-between; align-items: center; margin-block-end: 16px; }
    .header-actions { display: flex; gap: 8px; }
    .info-card { margin-block-end: 16px; }
    .info-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px; }
    .info-item { display: flex; flex-direction: column; gap: 4px; }
    .info-item .label { font-size: 12px; color: rgba(0,0,0,0.6); }
    .info-item .value { font-size: 14px; }
    .full-width { width: 100%; }
    .add-contact-btn { margin-block-start: 16px; }
    .contact-form { display: flex; flex-direction: column; gap: 4px; margin-block-start: 16px; max-width: 500px; }
    .form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-block-start: 8px; }
    .error-message { color: #f44336; font-size: 14px; }
  `]
})
export class CustomerDetailComponent implements OnInit {
  private customersService = inject(CustomersService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  customer: CustomerDetailDto | null = null;
  contactColumns = ['name', 'email', 'phone', 'title', 'isPrimary'];
  showContactForm = false;
  savingContact = false;
  contactError: string | null = null;

  contactForm = this.fb.group({
    name: ['', [Validators.required]],
    nameAr: ['', [Validators.required]],
    email: ['', [Validators.email]],
    phone: [''],
    title: [''],
    isPrimary: [false]
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadCustomer(id);
    }
  }

  loadCustomer(id: string): void {
    this.customersService.getCustomerById(id).subscribe(customer => {
      this.customer = customer;
    });
  }

  onAddContact(): void {
    if (this.contactForm.invalid || !this.customer) return;

    this.savingContact = true;
    this.contactError = null;
    const value = this.contactForm.getRawValue();

    this.customersService.addContact(this.customer.id, {
      customerId: this.customer.id,
      name: value.name!,
      nameAr: value.nameAr!,
      email: value.email || undefined,
      phone: value.phone || undefined,
      title: value.title || undefined,
      isPrimary: value.isPrimary!
    }).subscribe({
      next: () => {
        this.savingContact = false;
        this.showContactForm = false;
        this.contactForm.reset({ name: '', nameAr: '', email: '', phone: '', title: '', isPrimary: false });
        this.loadCustomer(this.customer!.id);
      },
      error: (err) => {
        this.savingContact = false;
        this.contactError = err.error?.detail || err.error?.title || 'An error occurred';
      }
    });
  }

  cancelContactForm(): void {
    this.showContactForm = false;
    this.contactError = null;
    this.contactForm.reset({ name: '', nameAr: '', email: '', phone: '', title: '', isPrimary: false });
  }
}
