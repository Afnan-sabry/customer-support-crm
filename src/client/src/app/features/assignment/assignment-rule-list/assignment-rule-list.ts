import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AssignmentService, AssignmentRuleDto } from '../assignment.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-assignment-rule-list',
  imports: [
    RouterLink, TranslateModule,
    MatTableModule, MatButtonModule, MatIconModule, MatChipsModule, MatDialogModule
  ],
  template: `
    <div class="list-header">
      <h1>{{ 'assignment.title' | translate }}</h1>
      <button mat-raised-button color="primary" routerLink="/admin/assignment/new">
        <mat-icon>add</mat-icon>
        {{ 'assignment.createRule' | translate }}
      </button>
    </div>

    <table mat-table [dataSource]="rules" class="mat-elevation-z2 full-width">
      <ng-container matColumnDef="name">
        <th mat-header-cell *matHeaderCellDef>{{ 'assignment.name' | translate }}</th>
        <td mat-cell *matCellDef="let rule">{{ rule.name }}</td>
      </ng-container>

      <ng-container matColumnDef="categoryName">
        <th mat-header-cell *matHeaderCellDef>{{ 'sla.category' | translate }}</th>
        <td mat-cell *matCellDef="let rule">{{ rule.categoryName || '-' }}</td>
      </ng-container>

      <ng-container matColumnDef="priorityName">
        <th mat-header-cell *matHeaderCellDef>{{ 'sla.priority' | translate }}</th>
        <td mat-cell *matCellDef="let rule">{{ rule.priorityName || '-' }}</td>
      </ng-container>

      <ng-container matColumnDef="strategy">
        <th mat-header-cell *matHeaderCellDef>{{ 'assignment.strategy' | translate }}</th>
        <td mat-cell *matCellDef="let rule">
          {{ (rule.strategy === 'RoundRobin' ? 'assignment.roundRobin' : 'assignment.leastLoad') | translate }}
        </td>
      </ng-container>

      <ng-container matColumnDef="agentPool">
        <th mat-header-cell *matHeaderCellDef>{{ 'assignment.agentPool' | translate }}</th>
        <td mat-cell *matCellDef="let rule">{{ agentCount(rule) }}</td>
      </ng-container>

      <ng-container matColumnDef="order">
        <th mat-header-cell *matHeaderCellDef>{{ 'assignment.order' | translate }}</th>
        <td mat-cell *matCellDef="let rule">{{ rule.order }}</td>
      </ng-container>

      <ng-container matColumnDef="isActive">
        <th mat-header-cell *matHeaderCellDef>{{ 'sla.active' | translate }}</th>
        <td mat-cell *matCellDef="let rule">
          <mat-chip [color]="rule.isActive ? 'primary' : 'warn'" selected>
            {{ (rule.isActive ? 'sla.active' : 'sla.inactive') | translate }}
          </mat-chip>
        </td>
      </ng-container>

      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef>{{ 'common.actions' | translate }}</th>
        <td mat-cell *matCellDef="let rule">
          <button mat-icon-button [routerLink]="['/admin/assignment', rule.id, 'edit']">
            <mat-icon>edit</mat-icon>
          </button>
          <button mat-icon-button color="warn" (click)="onDelete(rule)">
            <mat-icon>delete</mat-icon>
          </button>
        </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
    </table>
  `,
  styles: [`
    .list-header { display: flex; justify-content: space-between; align-items: center; margin-block-end: 16px; }
    .full-width { width: 100%; }
  `]
})
export class AssignmentRuleListComponent implements OnInit {
  private assignmentService = inject(AssignmentService);
  private dialog = inject(MatDialog);
  private translate = inject(TranslateService);

  displayedColumns = ['name', 'categoryName', 'priorityName', 'strategy', 'agentPool', 'order', 'isActive', 'actions'];
  rules: AssignmentRuleDto[] = [];

  ngOnInit(): void {
    this.loadRules();
  }

  loadRules(): void {
    this.assignmentService.getAssignmentRules().subscribe(rules => {
      this.rules = rules;
    });
  }

  agentCount(rule: AssignmentRuleDto): number {
    if (!rule.agentPool) return 0;
    try {
      const parsed = JSON.parse(rule.agentPool);
      return Array.isArray(parsed) ? parsed.length : 0;
    } catch {
      return 0;
    }
  }

  onDelete(rule: AssignmentRuleDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: this.translate.instant('common.delete'),
        message: this.translate.instant('assignment.deleteConfirm')
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.assignmentService.deleteAssignmentRule(rule.id).subscribe(() => this.loadRules());
      }
    });
  }
}
