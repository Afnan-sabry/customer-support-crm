import { Component, input, inject, OnChanges } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { IntegrationsService, WebhookDeliveryLogDto } from '../integrations.service';

@Component({
  selector: 'app-webhook-delivery-log',
  imports: [TranslateModule, DatePipe, MatTableModule, MatIconModule, MatButtonModule],
  template: `
    @if (subscriptionId()) {
      <h3>{{ 'integrations.deliveryLog' | translate }}</h3>
      <table mat-table [dataSource]="logs" class="full-width">
        <ng-container matColumnDef="event">
          <th mat-header-cell *matHeaderCellDef>{{ 'integrations.event' | translate }}</th>
          <td mat-cell *matCellDef="let log">{{ log.event }}</td>
        </ng-container>
        <ng-container matColumnDef="statusCode">
          <th mat-header-cell *matHeaderCellDef>{{ 'integrations.statusCode' | translate }}</th>
          <td mat-cell *matCellDef="let log">{{ log.statusCode ?? '-' }}</td>
        </ng-container>
        <ng-container matColumnDef="success">
          <th mat-header-cell *matHeaderCellDef>{{ 'integrations.success' | translate }}</th>
          <td mat-cell *matCellDef="let log">
            <mat-icon [class]="log.success ? 'success-icon' : 'error-icon'">
              {{ log.success ? 'check_circle' : 'error' }}
            </mat-icon>
          </td>
        </ng-container>
        <ng-container matColumnDef="attempt">
          <th mat-header-cell *matHeaderCellDef>{{ 'integrations.attempt' | translate }}</th>
          <td mat-cell *matCellDef="let log">{{ log.attempt }}</td>
        </ng-container>
        <ng-container matColumnDef="createdAt">
          <th mat-header-cell *matHeaderCellDef>{{ 'integrations.timestamp' | translate }}</th>
          <td mat-cell *matCellDef="let log">{{ log.createdAt | date: 'short' }}</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="columns"></tr>
        <tr mat-row *matRowDef="let row; columns: columns;"></tr>
      </table>
      <button mat-stroked-button (click)="loadMore()">{{ 'integrations.loadMore' | translate }}</button>
    }
  `,
  styles: [`
    .full-width { width: 100%; }
    .success-icon { color: #4caf50; }
    .error-icon { color: #f44336; }
  `]
})
export class WebhookDeliveryLogComponent implements OnChanges {
  subscriptionId = input<string | null>(null);

  private integrationsService = inject(IntegrationsService);
  logs: WebhookDeliveryLogDto[] = [];
  columns = ['event', 'statusCode', 'success', 'attempt', 'createdAt'];
  page = 1;

  ngOnChanges(): void {
    if (this.subscriptionId()) {
      this.page = 1;
      this.logs = [];
      this.loadLogs();
    }
  }

  loadMore(): void {
    this.page++;
    this.loadLogs();
  }

  private loadLogs(): void {
    const id = this.subscriptionId();
    if (!id) return;
    this.integrationsService.getDeliveryLogs(id, this.page).subscribe(data => {
      this.logs = [...this.logs, ...data];
    });
  }
}
