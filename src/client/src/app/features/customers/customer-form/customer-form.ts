import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { CustomersService } from '../customers.service';

@Component({
  selector: 'app-customer-form',
  imports: [
    ReactiveFormsModule, TranslateModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule
  ],
  template: `
    <mat-card class="form-card">
      <mat-card-header>
        <mat-card-title>{{ (isEditMode ? 'customers.editCustomer' : 'customers.createCustomer') | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'customers.name' | translate }}</mat-label>
            <input matInput formControlName="name" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'customers.nameAr' | translate }}</mat-label>
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
            <mat-label>{{ 'customers.company' | translate }}</mat-label>
            <input matInput formControlName="company" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'customers.companyAr' | translate }}</mat-label>
            <input matInput formControlName="companyAr" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'customers.address' | translate }}</mat-label>
            <textarea matInput formControlName="address" rows="3"></textarea>
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
  `,
  styles: [`
    .form-card { max-width: 600px; margin: 0 auto; }
    .full-width { width: 100%; }
    .form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-block-start: 16px; }
    .error-message { color: #f44336; margin-block-end: 16px; font-size: 14px; }
  `]
})
export class CustomerFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private customersService = inject(CustomersService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  isEditMode = false;
  customerId: string | null = null;
  saving = false;
  error: string | null = null;

  form = this.fb.group({
    name: ['', [Validators.required]],
    nameAr: ['', [Validators.required]],
    email: ['', [Validators.email]],
    phone: [''],
    company: [''],
    companyAr: [''],
    address: ['']
  });

  ngOnInit(): void {
    this.customerId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.customerId;

    if (this.isEditMode) {
      this.customersService.getCustomerById(this.customerId!).subscribe(customer => {
        this.form.patchValue({
          name: customer.name,
          nameAr: customer.nameAr,
          email: customer.email ?? '',
          phone: customer.phone ?? '',
          company: customer.company ?? '',
          companyAr: customer.companyAr ?? '',
          address: customer.address ?? ''
        });
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.saving = true;
    this.error = null;
    const value = this.form.getRawValue();

    if (this.isEditMode) {
      this.customersService.updateCustomer(this.customerId!, {
        id: this.customerId!,
        name: value.name!,
        nameAr: value.nameAr!,
        email: value.email || undefined,
        phone: value.phone || undefined,
        company: value.company || undefined,
        companyAr: value.companyAr || undefined,
        address: value.address || undefined
      }).subscribe({
        next: () => this.router.navigate(['/admin/customers']),
        error: (err) => this.handleError(err)
      });
    } else {
      this.customersService.createCustomer({
        name: value.name!,
        nameAr: value.nameAr!,
        email: value.email || undefined,
        phone: value.phone || undefined,
        company: value.company || undefined,
        companyAr: value.companyAr || undefined,
        address: value.address || undefined
      }).subscribe({
        next: () => this.router.navigate(['/admin/customers']),
        error: (err) => this.handleError(err)
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/admin/customers']);
  }

  private handleError(err: any): void {
    this.saving = false;
    this.error = err.error?.detail || err.error?.title || 'An error occurred';
  }
}
