import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { EscalationService, EscalationRuleDto } from '../escalation.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-escalation-rule-list',
  imports: [
    RouterLink, TranslateModule,
    MatTableModule, MatButtonModule, MatIconModule, MatChipsModule, MatDialogModule
  ],
  template: `
    <div class="list-header">
      <h1>{{ 'escalation.title' | translate }}</h1>
      <button mat-raised-button color="primary" routerLink="/admin/escalation/new">
        <mat-icon>add</mat-icon>
        {{ 'escalation.createRule' | translate }}
      </button>
    </div>

    <table mat-table [dataSource]="rules" class="mat-elevation-z2 full-width">
      <ng-container matColumnDef="name">
        <th mat-header-cell *matHeaderCellDef>{{ 'escalation.name' | translate }}</th>
        <td mat-cell *matCellDef="let rule">{{ rule.name }}</td>
      </ng-container>

      <ng-container matColumnDef="triggerType">
        <th mat-header-cell *matHeaderCellDef>{{ 'escalation.triggerType' | translate }}</th>
        <td mat-cell *matCellDef="let rule">
          {{ (rule.triggerType === 'FirstResponseBreached' ? 'escalation.firstResponseBreached' : 'escalation.resolutionBreached') | translate }}
        </td>
      </ng-container>

      <ng-container matColumnDef="triggerAfterMinutes">
        <th mat-header-cell *matHeaderCellDef>{{ 'escalation.triggerAfterMinutes' | translate }}</th>
        <td mat-cell *matCellDef="let rule">{{ rule.triggerAfterMinutes }}</td>
      </ng-container>

      <ng-container matColumnDef="actionType">
        <th mat-header-cell *matHeaderCellDef>{{ 'escalation.actionType' | translate }}</th>
        <td mat-cell *matCellDef="let rule">
          {{ (rule.actionType === 'Reassign' ? 'escalation.reassign' : 'escalation.changePriority') | translate }}
        </td>
      </ng-container>

      <ng-container matColumnDef="order">
        <th mat-header-cell *matHeaderCellDef>{{ 'escalation.order' | translate }}</th>
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
          <button mat-icon-button [routerLink]="['/admin/escalation', rule.id, 'edit']">
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
export class EscalationRuleListComponent implements OnInit {
  private escalationService = inject(EscalationService);
  private dialog = inject(MatDialog);
  private translate = inject(TranslateService);

  displayedColumns = ['name', 'triggerType', 'triggerAfterMinutes', 'actionType', 'order', 'isActive', 'actions'];
  rules: EscalationRuleDto[] = [];

  ngOnInit(): void {
    this.loadRules();
  }

  loadRules(): void {
    this.escalationService.getEscalationRules().subscribe(rules => {
      this.rules = rules;
    });
  }

  onDelete(rule: EscalationRuleDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: this.translate.instant('common.delete'),
        message: this.translate.instant('escalation.deleteConfirm')
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.escalationService.deleteEscalationRule(rule.id).subscribe(() => this.loadRules());
      }
    });
  }
}
