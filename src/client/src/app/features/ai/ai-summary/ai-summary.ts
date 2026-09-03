import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AiService } from '../ai.service';

@Component({
  selector: 'app-ai-summary',
  imports: [TranslateModule, MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>
          <mat-icon>summarize</mat-icon>
          {{ 'ai.summary' | translate }}
        </mat-card-title>
        <button mat-icon-button (click)="onGenerate()" [disabled]="loading">
          <mat-icon>refresh</mat-icon>
        </button>
      </mat-card-header>
      <mat-card-content>
        @if (loading) {
          <mat-spinner diameter="24"></mat-spinner>
        } @else if (summary) {
          <p class="summary-text">{{ summary }}</p>
        } @else {
          <p class="no-summary">{{ 'ai.noSummary' | translate }}</p>
          <button mat-raised-button color="primary" (click)="onGenerate()">
            <mat-icon>auto_awesome</mat-icon>
            {{ 'ai.generateSummary' | translate }}
          </button>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    mat-card-header { display: flex; align-items: center; }
    mat-card-header button { margin-inline-start: auto; }
    mat-card-title { display: flex; align-items: center; gap: 8px; }
    .summary-text { font-size: 14px; line-height: 1.6; white-space: pre-wrap; }
    .no-summary { color: rgba(0,0,0,0.6); font-size: 14px; }
  `]
})
export class AiSummaryComponent {
  private aiService = inject(AiService);

  @Input() ticketId = '';
  @Input() summary: string | null = null;
  @Output() summaryGenerated = new EventEmitter<string>();

  loading = false;

  onGenerate(): void {
    if (!this.ticketId) return;
    this.loading = true;
    this.aiService.summarize(this.ticketId).subscribe({
      next: result => {
        this.summary = result.summary;
        this.loading = false;
        this.summaryGenerated.emit(result.summary);
      },
      error: () => { this.loading = false; }
    });
  }
}
