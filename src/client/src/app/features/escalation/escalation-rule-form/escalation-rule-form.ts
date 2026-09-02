import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { EscalationService, EscalationActionType, EscalationTriggerType } from '../escalation.service';
import { TicketsService, TicketPriorityDto, TicketCategoryDto } from '../../tickets/tickets.service';
import { UsersService, UserDetail } from '../../users/users.service';

@Component({
  selector: 'app-escalation-rule-form',
  imports: [
    ReactiveFormsModule, TranslateModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule
  ],
  template: `
    <mat-card class="form-card">
      <mat-card-header>
        <mat-card-title>{{ (isEditMode ? 'escalation.editRule' : 'escalation.createRule') | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'escalation.name' | translate }}</mat-label>
            <input matInput formControlName="name" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'escalation.nameAr' | translate }}</mat-label>
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
            <mat-label>{{ 'escalation.triggerType' | translate }}</mat-label>
            <mat-select formControlName="triggerType">
              <mat-option value="FirstResponseBreached">{{ 'escalation.firstResponseBreached' | translate }}</mat-option>
              <mat-option value="ResolutionBreached">{{ 'escalation.resolutionBreached' | translate }}</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'escalation.triggerAfterMinutes' | translate }}</mat-label>
            <input matInput type="number" formControlName="triggerAfterMinutes" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'escalation.actionType' | translate }}</mat-label>
            <mat-select formControlName="actionType">
              <mat-option value="Reassign">{{ 'escalation.reassign' | translate }}</mat-option>
              <mat-option value="ChangePriority">{{ 'escalation.changePriority' | translate }}</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'escalation.actionTarget' | translate }}</mat-label>
            @if (form.value.actionType === 'ChangePriority') {
              <mat-select formControlName="actionTarget">
                @for (priority of priorities; track priority.id) {
                  <mat-option [value]="priority.id">{{ priority.name }}</mat-option>
                }
              </mat-select>
            } @else {
              <mat-select formControlName="actionTarget">
                @for (user of users; track user.id) {
                  <mat-option [value]="user.id">{{ user.fullName }}</mat-option>
                }
              </mat-select>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'escalation.order' | translate }}</mat-label>
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
export class EscalationRuleFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private escalationService = inject(EscalationService);
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
    priorityId: [null as string | null],
    categoryId: [null as string | null],
    triggerType: ['FirstResponseBreached' as EscalationTriggerType, [Validators.required]],
    triggerAfterMinutes: [30, [Validators.required, Validators.min(1)]],
    actionType: ['Reassign' as EscalationActionType, [Validators.required]],
    actionTarget: [null as string | null],
    order: [0, [Validators.required]]
  });

  ngOnInit(): void {
    this.ruleId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.ruleId;

    this.ticketsService.getPriorities().subscribe(priorities => this.priorities = priorities);
    this.ticketsService.getCategories().subscribe(categories => this.categories = categories);
    this.usersService.getUsers({ pageSize: 200 }).subscribe(result => this.users = result.items);

    if (this.isEditMode && this.ruleId) {
      this.escalationService.getEscalationRules().subscribe(rules => {
        const rule = rules.find(r => r.id === this.ruleId);
        if (rule) {
          this.form.patchValue({
            name: rule.name,
            nameAr: rule.nameAr,
            priorityId: rule.priorityId,
            categoryId: rule.categoryId,
            triggerType: rule.triggerType,
            triggerAfterMinutes: rule.triggerAfterMinutes,
            actionType: rule.actionType,
            actionTarget: rule.actionTarget,
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
      priorityId: value.priorityId,
      categoryId: value.categoryId,
      triggerType: value.triggerType!,
      triggerAfterMinutes: value.triggerAfterMinutes!,
      actionType: value.actionType!,
      actionTarget: value.actionTarget,
      order: value.order!
    };

    const result$ = this.isEditMode && this.ruleId
      ? this.escalationService.updateEscalationRule(this.ruleId, request)
      : this.escalationService.createEscalationRule(request);

    result$.subscribe({
      next: () => this.router.navigate(['/admin/escalation']),
      error: (err) => this.handleError(err)
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/escalation']);
  }

  private handleError(err: any): void {
    this.saving = false;
    this.error = err.error?.detail || err.error?.title || 'An error occurred';
  }
}
