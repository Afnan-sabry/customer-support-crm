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
import { PortalTicketService, TicketCategoryDto, TicketPriorityDto } from '../portal-ticket.service';

@Component({
  selector: 'app-portal-ticket-form',
  imports: [
    ReactiveFormsModule, TranslateModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule
  ],
  template: `
    <mat-card class="form-card">
      <mat-card-header>
        <mat-card-title>{{ 'portal.submitTicket' | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="onSubmit()">
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
            <textarea matInput formControlName="description" rows="6"></textarea>
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
export class PortalTicketFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private ticketService = inject(PortalTicketService);
  private router = inject(Router);

  saving = false;
  error: string | null = null;

  categories: TicketCategoryDto[] = [];
  priorities: TicketPriorityDto[] = [];

  form = this.fb.group({
    categoryId: ['', [Validators.required]],
    priorityId: ['', [Validators.required]],
    subject: ['', [Validators.required]],
    description: ['', [Validators.required]]
  });

  ngOnInit(): void {
    forkJoin({
      categories: this.ticketService.getCategories(),
      priorities: this.ticketService.getPriorities()
    }).subscribe(result => {
      this.categories = result.categories;
      this.priorities = result.priorities;
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.saving = true;
    this.error = null;
    const value = this.form.getRawValue();

    this.ticketService.submitTicket({
      categoryId: value.categoryId!,
      priorityId: value.priorityId!,
      subject: value.subject!,
      description: value.description!
    }).subscribe({
      next: (ticket) => this.router.navigate(['/portal/tickets', ticket.id]),
      error: (err) => this.handleError(err)
    });
  }

  onCancel(): void {
    this.router.navigate(['/portal/tickets']);
  }

  private handleError(err: any): void {
    this.saving = false;
    this.error = err.error?.detail || err.error?.title || 'An error occurred';
  }
}
