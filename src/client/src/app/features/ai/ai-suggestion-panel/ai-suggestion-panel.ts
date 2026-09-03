import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { AiService, AiSuggestionDto } from '../ai.service';

@Component({
  selector: 'app-ai-suggestion-panel',
  imports: [DatePipe, TranslateModule, MatCardModule, MatButtonModule, MatIconModule, MatChipsModule],
  template: `
    @if (suggestions.length > 0) {
      <mat-card>
        <mat-card-header>
          <mat-card-title>
            <mat-icon>psychology</mat-icon>
            {{ 'ai.suggestions' | translate }}
          </mat-card-title>
        </mat-card-header>
        <mat-card-content>
          @for (suggestion of suggestions; track suggestion.id) {
            <div class="suggestion-item">
              <div class="suggestion-header">
                <mat-chip>{{ suggestion.type }}</mat-chip>
                @if (suggestion.confidence !== null) {
                  <mat-chip [color]="getConfidenceColor(suggestion.confidence)" selected>
                    {{ (suggestion.confidence * 100).toFixed(0) }}%
                  </mat-chip>
                }
                <span class="suggestion-status">{{ suggestion.status }}</span>
                <span class="suggestion-date">{{ suggestion.createdAt | date: 'short' }}</span>
              </div>
              <p class="suggestion-output">{{ suggestion.output }}</p>
              @if (suggestion.status === 'Pending') {
                <div class="suggestion-actions">
                  <button mat-raised-button color="primary" (click)="onAccept(suggestion.id)">
                    <mat-icon>check</mat-icon> {{ 'ai.accept' | translate }}
                  </button>
                  <button mat-button color="warn" (click)="onReject(suggestion.id)">
                    <mat-icon>close</mat-icon> {{ 'ai.reject' | translate }}
                  </button>
                </div>
              }
            </div>
          }
        </mat-card-content>
      </mat-card>
    }
  `,
  styles: [`
    .suggestion-item { border-block-end: 1px solid rgba(0,0,0,0.12); padding-block: 12px; }
    .suggestion-item:last-child { border-block-end: none; }
    .suggestion-header { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
    .suggestion-status { font-size: 12px; font-weight: 500; }
    .suggestion-date { margin-inline-start: auto; font-size: 12px; color: rgba(0,0,0,0.6); }
    .suggestion-output { margin-block: 8px; font-size: 14px; white-space: pre-wrap; }
    .suggestion-actions { display: flex; gap: 8px; }
    mat-card-title { display: flex; align-items: center; gap: 8px; }
  `]
})
export class AiSuggestionPanelComponent {
  private aiService = inject(AiService);

  @Input() suggestions: AiSuggestionDto[] = [];
  @Output() updated = new EventEmitter<void>();

  getConfidenceColor(confidence: number): 'primary' | 'accent' | 'warn' {
    if (confidence >= 0.8) return 'primary';
    if (confidence >= 0.5) return 'accent';
    return 'warn';
  }

  onAccept(suggestionId: string): void {
    this.aiService.acceptSuggestion(suggestionId).subscribe(() => this.updated.emit());
  }

  onReject(suggestionId: string): void {
    this.aiService.rejectSuggestion(suggestionId).subscribe(() => this.updated.emit());
  }
}
