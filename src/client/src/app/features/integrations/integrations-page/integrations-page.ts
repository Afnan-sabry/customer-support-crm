import { Component, inject } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { IntegrationsService, WebhookSubscriptionDto, ErpStatusDto } from '../integrations.service';
import { WebhookSubscriptionDialogComponent } from '../webhook-subscription-dialog/webhook-subscription-dialog';
import { WebhookDeliveryLogComponent } from '../webhook-delivery-log/webhook-delivery-log';

@Component({
  selector: 'app-integrations-page',
  imports: [
    TranslateModule, DatePipe,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule,
    MatSlideToggleModule, MatChipsModule,
    WebhookDeliveryLogComponent
  ],
  template: `
    <h1>{{ 'integrations.title' | translate }}</h1>

    <mat-card class="erp-card">
      <mat-card-header>
        <mat-card-title>{{ 'integrations.erpStatus' | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        @if (erpStatus) {
          <p><strong>{{ 'integrations.provider' | translate }}:</strong> {{ erpStatus.provider }}</p>
          <p><strong>{{ 'integrations.connectionStatus' | translate }}:</strong> {{ erpStatus.connected }}</p>
        }
      </mat-card-content>
    </mat-card>

    <mat-card>
      <mat-card-header>
        <mat-card-title>{{ 'integrations.webhookSubscriptions' | translate }}</mat-card-title>
        <button mat-flat-button color="primary" (click)="openDialog()">
          <mat-icon>add</mat-icon> {{ 'integrations.createSubscription' | translate }}
        </button>
      </mat-card-header>
      <mat-card-content>
        <table mat-table [dataSource]="subscriptions" class="full-width">
          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef>{{ 'integrations.name' | translate }}</th>
            <td mat-cell *matCellDef="let s">{{ s.name }}</td>
          </ng-container>
          <ng-container matColumnDef="url">
            <th mat-header-cell *matHeaderCellDef>{{ 'integrations.url' | translate }}</th>
            <td mat-cell *matCellDef="let s">{{ s.url }}</td>
          </ng-container>
          <ng-container matColumnDef="events">
            <th mat-header-cell *matHeaderCellDef>{{ 'integrations.events' | translate }}</th>
            <td mat-cell *matCellDef="let s">
              <mat-chip-set>
                @for (event of s.events; track event) {
                  <mat-chip>{{ event }}</mat-chip>
                }
              </mat-chip-set>
            </td>
          </ng-container>
          <ng-container matColumnDef="isActive">
            <th mat-header-cell *matHeaderCellDef>{{ 'integrations.active' | translate }}</th>
            <td mat-cell *matCellDef="let s">
              <mat-icon [class]="s.isActive ? 'active-icon' : 'inactive-icon'">
                {{ s.isActive ? 'check_circle' : 'cancel' }}
              </mat-icon>
            </td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef>{{ 'common.actions' | translate }}</th>
            <td mat-cell *matCellDef="let s">
              <button mat-icon-button (click)="openDialog(s)"><mat-icon>edit</mat-icon></button>
              <button mat-icon-button (click)="testWebhook(s.id)"><mat-icon>send</mat-icon></button>
              <button mat-icon-button (click)="viewLogs(s.id)"><mat-icon>list</mat-icon></button>
              <button mat-icon-button color="warn" (click)="deleteSubscription(s.id)"><mat-icon>delete</mat-icon></button>
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns;"></tr>
        </table>
      </mat-card-content>
    </mat-card>

    <app-webhook-delivery-log [subscriptionId]="selectedSubscriptionId" />
  `,
  styles: [`
    .full-width { width: 100%; }
    .erp-card { margin-block-end: 16px; }
    mat-card-header { display: flex; justify-content: space-between; align-items: center; }
    .active-icon { color: #4caf50; }
    .inactive-icon { color: #9e9e9e; }
  `]
})
export class IntegrationsPageComponent {
  private integrationsService = inject(IntegrationsService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private translate = inject(TranslateService);

  subscriptions: WebhookSubscriptionDto[] = [];
  erpStatus: ErpStatusDto | null = null;
  selectedSubscriptionId: string | null = null;
  columns = ['name', 'url', 'events', 'isActive', 'actions'];

  ngOnInit(): void {
    this.loadSubscriptions();
    this.integrationsService.getErpStatus().subscribe(s => this.erpStatus = s);
  }

  loadSubscriptions(): void {
    this.integrationsService.getSubscriptions().subscribe(s => this.subscriptions = s);
  }

  openDialog(existing?: WebhookSubscriptionDto): void {
    const ref = this.dialog.open(WebhookSubscriptionDialogComponent, { data: existing || null });
    ref.afterClosed().subscribe(result => {
      if (!result) return;
      if (existing) {
        this.integrationsService.updateSubscription(existing.id, result).subscribe(() => this.loadSubscriptions());
      } else {
        this.integrationsService.createSubscription(result).subscribe(() => this.loadSubscriptions());
      }
    });
  }

  testWebhook(id: string): void {
    this.integrationsService.testSubscription(id).subscribe(() => {
      this.snackBar.open(this.translate.instant('integrations.testSent'), '', { duration: 3000 });
    });
  }

  viewLogs(id: string): void {
    this.selectedSubscriptionId = this.selectedSubscriptionId === id ? null : id;
  }

  deleteSubscription(id: string): void {
    this.integrationsService.deleteSubscription(id).subscribe(() => this.loadSubscriptions());
  }
}
