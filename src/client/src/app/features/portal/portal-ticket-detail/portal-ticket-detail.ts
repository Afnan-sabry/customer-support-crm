import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { PortalTicketService, PortalTicketDetailDto } from '../portal-ticket.service';

@Component({
  selector: 'app-portal-ticket-detail',
  imports: [
    RouterLink, ReactiveFormsModule, FormsModule, TranslateModule, DatePipe,
    MatCardModule, MatButtonModule, MatIconModule, MatChipsModule, MatFormFieldModule, MatInputModule
  ],
  template: `
    @if (ticket) {
      <div class="detail-header">
        <div>
          <h1>{{ ticket.subject }}</h1>
          <div class="header-chips">
            <span class="ticket-number">{{ ticket.ticketNumber }}</span>
            <mat-chip color="primary" selected>{{ ticket.statusName }}</mat-chip>
            <mat-chip [color]="priorityColor(ticket.priorityName)" selected>{{ ticket.priorityName }}</mat-chip>
            <mat-chip>{{ ticket.categoryName }}</mat-chip>
          </div>
        </div>
        <button mat-button routerLink="/portal/tickets">
          <mat-icon>arrow_back</mat-icon>
          {{ 'common.back' | translate }}
        </button>
      </div>

      <mat-card class="description-card">
        <mat-card-content>
          <p class="description">{{ ticket.description }}</p>
        </mat-card-content>
      </mat-card>

      <mat-card class="comments-card">
        <mat-card-header>
          <mat-card-title>{{ 'tickets.comments' | translate }}</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          @for (comment of ticket.comments; track comment.id) {
            <div class="comment" [class.agent-comment]="comment.isAgent">
              <div class="comment-header">
                <strong>{{ comment.authorName }}</strong>
                <span class="comment-date">{{ comment.createdAt | date: 'short' }}</span>
              </div>
              <p class="comment-content">{{ comment.content }}</p>
            </div>
          } @empty {
            <p class="no-comments">{{ 'tickets.noComments' | translate }}</p>
          }

          <form [formGroup]="commentForm" (ngSubmit)="onAddComment()" class="comment-form">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>{{ 'tickets.commentContent' | translate }}</mat-label>
              <textarea matInput formControlName="content" rows="3"></textarea>
            </mat-form-field>
            <button mat-raised-button color="primary" type="submit" [disabled]="commentForm.invalid || posting">
              {{ 'tickets.addComment' | translate }}
            </button>
          </form>
        </mat-card-content>
      </mat-card>

      @if (isFinalStatus(ticket.statusName)) {
        <mat-card class="feedback-card">
          <mat-card-header>
            <mat-card-title>{{ 'reports.csat.submitFeedback' | translate }}</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            @if (feedbackSubmitted) {
              <p class="feedback-thanks">{{ 'reports.csat.feedbackSubmitted' | translate }}</p>
            } @else {
              <div class="star-rating">
                @for (star of [1, 2, 3, 4, 5]; track star) {
                  <button mat-icon-button type="button" (click)="setRating(star)" [attr.aria-label]="star + ' stars'">
                    <mat-icon>{{ star <= feedbackRating ? 'star' : 'star_border' }}</mat-icon>
                  </button>
                }
              </div>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'reports.csat.feedbackPlaceholder' | translate }}</mat-label>
                <textarea matInput [(ngModel)]="feedbackComment" rows="3" [ngModelOptions]="{ standalone: true }"></textarea>
              </mat-form-field>
              <button mat-raised-button color="primary" type="button"
                [disabled]="feedbackRating === 0 || submittingFeedback"
                (click)="onSubmitFeedback()">
                {{ 'reports.csat.submitFeedback' | translate }}
              </button>
            }
          </mat-card-content>
        </mat-card>
      }
    }
  `,
  styles: [`
    .detail-header { display: flex; justify-content: space-between; align-items: flex-start; margin-block-end: 16px; }
    .header-chips { display: flex; align-items: center; gap: 8px; margin-block-start: 8px; flex-wrap: wrap; }
    .ticket-number { color: rgba(0,0,0,0.6); font-size: 14px; }
    .description-card, .comments-card { margin-block-end: 16px; }
    .description { white-space: pre-wrap; }
    .comment { padding: 12px; border-radius: 8px; background: #f5f5f5; margin-block-end: 12px; }
    .comment.agent-comment { background: #e3f2fd; }
    .comment-header { display: flex; justify-content: space-between; margin-block-end: 4px; }
    .comment-date { color: rgba(0,0,0,0.6); font-size: 12px; }
    .comment-content { margin: 0; white-space: pre-wrap; }
    .no-comments { color: rgba(0,0,0,0.6); }
    .comment-form { margin-block-start: 16px; }
    .full-width { width: 100%; }
    .feedback-card { margin-block-end: 16px; }
    .star-rating { display: flex; gap: 4px; margin-block-end: 8px; }
    .star-rating mat-icon { color: #fbc02d; }
    .feedback-thanks { color: #2e7d32; font-weight: 500; }
  `]
})
export class PortalTicketDetailComponent implements OnInit {
  private ticketService = inject(PortalTicketService);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  ticket: PortalTicketDetailDto | null = null;
  posting = false;

  feedbackRating = 0;
  feedbackComment = '';
  feedbackSubmitted = false;
  submittingFeedback = false;

  commentForm = this.fb.group({
    content: ['', [Validators.required]]
  });

  ngOnInit(): void {
    this.loadTicket();
  }

  loadTicket(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.ticketService.getTicketById(id).subscribe(ticket => this.ticket = ticket);
  }

  onAddComment(): void {
    if (this.commentForm.invalid || !this.ticket) return;
    this.posting = true;
    const content = this.commentForm.value.content!;
    this.ticketService.addComment(this.ticket.id, content).subscribe({
      next: () => {
        this.posting = false;
        this.commentForm.reset();
        this.loadTicket();
      },
      error: () => this.posting = false
    });
  }

  isFinalStatus(statusName: string): boolean {
    const lower = (statusName || '').toLowerCase();
    return lower.includes('closed') || lower.includes('resolved') || lower.includes('مغلق') || lower.includes('تم الحل');
  }

  setRating(star: number): void {
    this.feedbackRating = star;
  }

  onSubmitFeedback(): void {
    if (!this.ticket || this.feedbackRating === 0) return;
    this.submittingFeedback = true;
    this.ticketService.submitFeedback(this.ticket.id, this.feedbackRating, this.feedbackComment || null).subscribe({
      next: () => {
        this.submittingFeedback = false;
        this.feedbackSubmitted = true;
      },
      error: () => { this.submittingFeedback = false; }
    });
  }

  priorityColor(priorityName: string): 'primary' | 'accent' | 'warn' {
    const lower = (priorityName || '').toLowerCase();
    if (lower.includes('high') || lower.includes('urgent') || lower.includes('عالي') || lower.includes('عاجل')) {
      return 'warn';
    }
    if (lower.includes('medium') || lower.includes('متوسط')) {
      return 'accent';
    }
    return 'primary';
  }
}
