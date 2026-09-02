import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { AssignmentService, AssignmentStrategy } from '../assignment.service';
import { TicketsService, TicketPriorityDto, TicketCategoryDto } from '../../tickets/tickets.service';
import { UsersService, UserDetail } from '../../users/users.service';

@Component({
  selector: 'app-assignment-rule-form',
  imports: [
    ReactiveFormsModule, TranslateModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule
  ],
  template: `
    <mat-card class="form-card">
      <mat-card-header>
        <mat-card-title>{{ (isEditMode ? 'assignment.editRule' : 'assignment.createRule') | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'assignment.name' | translate }}</mat-label>
            <input matInput formControlName="name" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'assignment.nameAr' | translate }}</mat-label>
            <input matInput formControlName="nameAr" dir="rtl" />
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
            <mat-label>{{ 'sla.priority' | translate }}</mat-label>
            <mat-select formControlName="priorityId">
              <mat-option [value]="null">{{ 'sla.allPriorities' | translate }}</mat-option>
              @for (priority of priorities; track priority.id) {
                <mat-option [value]="priority.id">{{ priority.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'assignment.strategy' | translate }}</mat-label>
            <mat-select formControlName="strategy">
              <mat-option value="RoundRobin">{{ 'assignment.roundRobin' | translate }}</mat-option>
              <mat-option value="LeastLoad">{{ 'assignment.leastLoad' | translate }}</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'assignment.agentPool' | translate }}</mat-label>
            <mat-select formControlName="agentPool" multiple>
              @for (user of users; track user.id) {
                <mat-option [value]="user.id">{{ user.fullName }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'assignment.order' | translate }}</mat-label>
            <input matInput type="number" formControlName="order" />
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
export class AssignmentRuleFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private assignmentService = inject(AssignmentService);
  private ticketsService = inject(TicketsService);
  private usersService = inject(UsersService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  saving = false;
  error: string | null = null;
  isEditMode = false;
  ruleId: string | null = null;

  priorities: TicketPriorityDto[] = [];
  categories: TicketCategoryDto[] = [];
  users: UserDetail[] = [];

  form = this.fb.group({
    name: ['', [Validators.required]],
    nameAr: ['', [Validators.required]],
    categoryId: [null as string | null],
    priorityId: [null as string | null],
    strategy: ['RoundRobin' as AssignmentStrategy, [Validators.required]],
    agentPool: [[] as string[]],
    order: [0, [Validators.required]]
  });

  ngOnInit(): void {
    this.ruleId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.ruleId;

    this.ticketsService.getPriorities().subscribe(priorities => this.priorities = priorities);
    this.ticketsService.getCategories().subscribe(categories => this.categories = categories);
    this.usersService.getUsers({ pageSize: 200 }).subscribe(result => this.users = result.items);

    if (this.isEditMode && this.ruleId) {
      this.assignmentService.getAssignmentRules().subscribe(rules => {
        const rule = rules.find(r => r.id === this.ruleId);
        if (rule) {
          this.form.patchValue({
            name: rule.name,
            nameAr: rule.nameAr,
            categoryId: rule.categoryId,
            priorityId: rule.priorityId,
            strategy: rule.strategy,
            agentPool: this.parseAgentPool(rule.agentPool),
            order: rule.order
          });
        }
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
      categoryId: value.categoryId,
      priorityId: value.priorityId,
      strategy: value.strategy!,
      agentPool: JSON.stringify(value.agentPool ?? []),
      order: value.order!
    };

    const result$ = this.isEditMode && this.ruleId
      ? this.assignmentService.updateAssignmentRule(this.ruleId, request)
      : this.assignmentService.createAssignmentRule(request);

    result$.subscribe({
      next: () => this.router.navigate(['/admin/assignment']),
      error: (err) => this.handleError(err)
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/assignment']);
  }

  private parseAgentPool(agentPool: string | null): string[] {
    if (!agentPool) return [];
    try {
      const parsed = JSON.parse(agentPool);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  private handleError(err: any): void {
    this.saving = false;
    this.error = err.error?.detail || err.error?.title || 'An error occurred';
  }
}
