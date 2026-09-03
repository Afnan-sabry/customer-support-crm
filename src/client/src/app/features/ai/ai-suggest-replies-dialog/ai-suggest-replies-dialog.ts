import { Component, inject, OnInit } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AiService } from '../ai.service';

@Component({
  selector: 'app-ai-suggest-replies-dialog',
  imports: [TranslateModule, MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <h2 mat-dialog-title>
      <mat-icon>auto_awesome</mat-icon>
      {{ 'ai.suggestReplies' | translate }}
    </h2>
    <mat-dialog-content>
      @if (loading) {
        <div class="loading-container">
          <mat-spinner diameter="32"></mat-spinner>
          <p>{{ 'ai.generating' | translate }}</p>
        </div>
      } @else if (suggestions.length > 0) {
        @for (suggestion of suggestions; track $index) {
          <div class="reply-option">
            <p>{{ suggestion }}</p>
            <div class="reply-actions">
              <button mat-raised-button color="primary" (click)="onUse(suggestion)">
                <mat-icon>check</mat-icon> {{ 'ai.useReply' | translate }}
              </button>
            </div>
          </div>
        }
      } @else {
        <p>{{ 'ai.noSuggestions' | translate }}</p>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ 'common.close' | translate }}</button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 { display: flex; align-items: center; gap: 8px; }
    .loading-container { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 24px; }
    .reply-option { border: 1px solid rgba(0,0,0,0.12); border-radius: 8px; padding: 12px; margin-block-end: 12px; }
    .reply-option p { margin: 0 0 8px; font-size: 14px; line-height: 1.5; white-space: pre-wrap; }
    .reply-actions { display: flex; gap: 8px; justify-content: flex-end; }
  `]
})
export class AiSuggestRepliesDialogComponent implements OnInit {
  private aiService = inject(AiService);
  private dialogRef = inject(MatDialogRef<AiSuggestRepliesDialogComponent>);
  private data: { ticketId: string } = inject(MAT_DIALOG_DATA);

  suggestions: string[] = [];
  loading = true;

  ngOnInit(): void {
    this.aiService.suggestReplies(this.data.ticketId).subscribe({
      next: result => {
        this.suggestions = result.suggestions;
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  onUse(reply: string): void {
    this.dialogRef.close(reply);
  }
}
