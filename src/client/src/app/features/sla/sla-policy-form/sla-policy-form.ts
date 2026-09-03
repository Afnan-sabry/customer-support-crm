import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { SlaService } from '../sla.service';
import { TicketsService, TicketPriorityDto, TicketCategoryDto } from '../../tickets/tickets.service';

@Component({
  selector: 'app-sla-policy-form',
  imports: [
    ReactiveFormsModule, TranslateModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule
  ],
  template: `
    <mat-card class="form-card">
      <mat-card-header>
        <mat-card-title>{{ (isEditMode ? 'sla.editPolicy' : 'sla.createPolicy') | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'sla.name' | translate }}</mat-label>
            <input matInput formControlName="name" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'sla.nameAr' | translate }}</mat-label>
            <input matInput formControlName="nameAr" dir="rtl" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'sla.priority' | translate }}</mat-label>
            <mat-select formControlName="priorityId">
              <mat-option [value]="null">{{ 'sla.allPriorities' | translate }}</mat-option>
              @for (priority of priorities; track priority.id) {
                <mat-option [value]="priority.id">{{ priority.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'sla.category' | translate }}</mat-label>
            <mat-select formControlName="categoryId">
              <mat-option [value]="null">{{ 'sla.allCategories' | translate }}</mat-option>
              @for (category of categories; track category.id) {
                <mat-option [value]="category.id">{{ category.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'sla.firstResponseMinutes' | translate }}</mat-label>
            <input matInput type="number" formControlName="firstResponseMinutes" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'sla.resolutionMinutes' | translate }}</mat-label>
            <input matInput type="number" formControlName="resolutionMinutes" />
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
    .form-card { max-width: 700px; margin: 0 auto; }
    .full-width { width: 100%; }
    .form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-block-start: 16px; }
    .error-message { color: #f44336; margin-block-end: 16px; font-size: 14px; }
  `]
})
export class SlaPolicyFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private slaService = inject(SlaService);
  private ticketsService = inject(TicketsService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  saving = false;
  error: string | null = null;
  isEditMode = false;
  policyId: string | null = null;

  priorities: TicketPriorityDto[] = [];
  categories: TicketCategoryDto[] = [];

  form = this.fb.group({
    name: ['', [Validators.required]],
    nameAr: ['', [Validators.required]],
    priorityId: [null as string | null],
    categoryId: [null as string | null],
    firstResponseMinutes: [60, [Validators.required, Validators.min(1)]],
    resolutionMinutes: [480, [Validators.required, Validators.min(1)]]
  });

  ngOnInit(): void {
    this.policyId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.policyId;

    this.ticketsService.getPriorities().subscribe(priorities => this.priorities = priorities);
    this.ticketsService.getCategories().subscribe(categories => this.categories = categories);

    if (this.isEditMode && this.policyId) {
      this.slaService.getSlaPolicyById(this.policyId).subscribe(policy => {
        this.form.patchValue({
          name: policy.name,
          nameAr: policy.nameAr,
          priorityId: policy.priorityId,
          categoryId: policy.categoryId,
          firstResponseMinutes: policy.firstResponseMinutes,
          resolutionMinutes: policy.resolutionMinutes
        });
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.saving = true;
    this.error = null;
    const value = this.form.getRawValue();

    const request = {
      name: value.name!,
      nameAr: value.nameAr!,
      priorityId: value.priorityId,
      categoryId: value.categoryId,
      firstResponseMinutes: value.firstResponseMinutes!,
      resolutionMinutes: value.resolutionMinutes!
    };

    const result$ = this.isEditMode && this.policyId
      ? this.slaService.updateSlaPolicy(this.policyId, request)
      : this.slaService.createSlaPolicy(request);

    result$.subscribe({
      next: () => this.router.navigate(['/admin/sla']),
      error: (err) => this.handleError(err)
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/sla']);
  }

  private handleError(err: any): void {
    this.saving = false;
    this.error = err.error?.detail || err.error?.title || 'An error occurred';
  }
}
