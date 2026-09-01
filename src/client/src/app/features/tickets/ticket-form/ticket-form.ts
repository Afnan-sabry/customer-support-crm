import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { forkJoin } from 'rxjs';
import { TicketsService, TicketCategoryDto, TicketPriorityDto } from '../tickets.service';
import { CustomersService, CustomerDto } from '../../customers/customers.service';

@Component({
  selector: 'app-ticket-form',
  imports: [
    ReactiveFormsModule, TranslateModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule
  ],
  template: `
    <mat-card class="form-card">
      <mat-card-header>
        <mat-card-title>{{ 'tickets.createTicket' | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'tickets.customer' | translate }}</mat-label>
            <mat-select formControlName="customerId">
              @for (customer of customers; track customer.id) {
                <mat-option [value]="customer.id">{{ customer.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'tickets.category' | translate }}</mat-label>
            <mat-select formControlName="categoryId">
              @for (category of categories; track category.id) {
                <mat-option [value]="category.id">{{ category.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'tickets.priority' | translate }}</mat-label>
            <mat-select formControlName="priorityId">
              @for (priority of priorities; track priority.id) {
                <mat-option [value]="priority.id">{{ priority.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'tickets.subject' | translate }}</mat-label>
            <input matInput formControlName="subject" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'tickets.description' | translate }}</mat-label>
            <textarea matInput formControlName="description" rows="5"></textarea>
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
export class TicketFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private ticketsService = inject(TicketsService);
  private customersService = inject(CustomersService);
  private router = inject(Router);

  saving = false;
  error: string | null = null;

  customers: CustomerDto[] = [];
  categories: TicketCategoryDto[] = [];
  priorities: TicketPriorityDto[] = [];

  form = this.fb.group({
    customerId: ['', [Validators.required]],
    categoryId: ['', [Validators.required]],
    priorityId: ['', [Validators.required]],
    subject: ['', [Validators.required]],
    description: ['', [Validators.required]]
  });

  ngOnInit(): void {
    forkJoin({
      customers: this.customersService.getCustomers({ pageSize: 100 }),
      categories: this.ticketsService.getCategories(),
      priorities: this.ticketsService.getPriorities()
    }).subscribe(result => {
      this.customers = result.customers.items;
      this.categories = result.categories;
      this.priorities = result.priorities;
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.saving = true;
    this.error = null;
    const value = this.form.getRawValue();

    this.ticketsService.createTicket({
      customerId: value.customerId!,
      categoryId: value.categoryId!,
      priorityId: value.priorityId!,
      subject: value.subject!,
      description: value.description!
    }).subscribe({
      next: (ticket) => this.router.navigate(['/admin/tickets', ticket.id]),
      error: (err) => this.handleError(err)
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/tickets']);
  }

  private handleError(err: any): void {
    this.saving = false;
    this.error = err.error?.detail || err.error?.title || 'An error occurred';
  }
}
