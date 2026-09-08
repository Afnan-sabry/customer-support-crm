import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

const AVAILABLE_EVENTS = [
  'ticket.created', 'ticket.status_changed', 'ticket.resolved',
  'ticket.escalated', 'sla.breached', 'conversation.created', 'conversation.closed'
];

@Component({
  selector: 'app-webhook-subscription-dialog',
  imports: [
    FormsModule, TranslateModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatCheckboxModule, MatButtonModule, MatSlideToggleModule
  ],
  template: `
    <h2 mat-dialog-title>{{ (data ? 'integrations.editSubscription' : 'integrations.createSubscription') | translate }}</h2>
    <mat-dialog-content>
      <mat-form-field class="full-width">
        <mat-label>{{ 'integrations.name' | translate }}</mat-label>
        <input matInput [(ngModel)]="form.name" required>
      </mat-form-field>

      <mat-form-field class="full-width">
        <mat-label>{{ 'integrations.url' | translate }}</mat-label>
        <input matInput [(ngModel)]="form.url" required placeholder="https://">
      </mat-form-field>

      <mat-form-field class="full-width">
        <mat-label>{{ 'integrations.secret' | translate }}</mat-label>
        <input matInput [(ngModel)]="form.secret" required>
      </mat-form-field>
      <button mat-stroked-button type="button" (click)="generateSecret()" class="generate-btn">
        {{ 'integrations.generateSecret' | translate }}
      </button>

      <div class="events-section">
        <label>{{ 'integrations.events' | translate }}</label>
        @for (event of availableEvents; track event) {
          <mat-checkbox [(ngModel)]="selectedEvents[event]">{{ event }}</mat-checkbox>
        }
      </div>

      <mat-slide-toggle [(ngModel)]="form.isActive">
        {{ 'integrations.active' | translate }}
      </mat-slide-toggle>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ 'common.cancel' | translate }}</button>
      <button mat-flat-button color="primary" (click)="save()" [disabled]="!isValid()">
        {{ 'common.save' | translate }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width { width: 100%; }
    .events-section { margin: 16px 0; display: flex; flex-direction: column; gap: 4px; }
    .events-section label { font-weight: 500; margin-bottom: 8px; }
    .generate-btn { margin-bottom: 16px; }
    mat-dialog-content { min-width: 400px; }
  `]
})
export class WebhookSubscriptionDialogComponent {
  private dialogRef = inject(MatDialogRef);
  data = inject(MAT_DIALOG_DATA, { optional: true }) as any;

  availableEvents = AVAILABLE_EVENTS;
  selectedEvents: Record<string, boolean> = {};

  form = {
    name: '',
    url: '',
    secret: '',
    isActive: true
  };

  constructor() {
    if (this.data) {
      this.form.name = this.data.name;
      this.form.url = this.data.url;
      this.form.secret = '';
      this.form.isActive = this.data.isActive;
      for (const event of this.data.events || []) {
        this.selectedEvents[event] = true;
      }
    }
  }

  generateSecret(): void {
    const array = new Uint8Array(32);
    crypto.getRandomValues(array);
    this.form.secret = Array.from(array, b => b.toString(16).padStart(2, '0')).join('');
  }

  isValid(): boolean {
    return !!this.form.name && !!this.form.url && (!!this.form.secret || !!this.data) &&
      Object.values(this.selectedEvents).some(v => v);
  }

  save(): void {
    const events = Object.entries(this.selectedEvents)
      .filter(([, v]) => v).map(([k]) => k);
    this.dialogRef.close({ ...this.form, events });
  }
}
