import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTabsModule } from '@angular/material/tabs';
import { forkJoin } from 'rxjs';
import {
  TicketsService, TicketDetailDto, TicketStatusDto, TicketPriorityDto
} from '../tickets.service';
import { UsersService, UserDetail } from '../../users/users.service';

@Component({
  selector: 'app-ticket-detail',
  imports: [
    RouterLink, ReactiveFormsModule, TranslateModule, DatePipe,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatCheckboxModule, MatTabsModule
  ],
  template: `
    @if (ticket) {
      <div class="detail-header">
        <div>
          <h1>{{ ticket.ticketNumber }} &mdash; {{ ticket.subject }}</h1>
          <div class="header-chips">
            <mat-chip color="primary" selected>{{ ticket.statusName }}</mat-chip>
            <mat-chip color="accent" selected>{{ ticket.priorityName }}</mat-chip>
          </div>
        </div>
        <button mat-button routerLink="/admin/tickets">
          <mat-icon>arrow_back</mat-icon>
          {{ 'common.back' | translate }}
        </button>
      </div>

      <mat-card class="info-card">
        <mat-card-content>
          <div class="info-grid">
            <div class="info-item">
              <span class="label">{{ 'tickets.customer' | translate }}</span>
              <span class="value">{{ ticket.customerName }}</span>
            </div>
            <div class="info-item">
              <span class="label">{{ 'tickets.category' | translate }}</span>
              <span class="value">{{ ticket.categoryName }}</span>
            </div>
            <div class="info-item">
              <span class="label">{{ 'tickets.description' | translate }}</span>
              <span class="value">{{ ticket.description }}</span>
            </div>
            <div class="info-item">
              <span class="label">{{ 'tickets.createdAt' | translate }}</span>
              <span class="value">{{ ticket.createdAt | date: 'short' }}</span>
            </div>
            <div class="info-item">
              <span class="label">{{ 'tickets.updatedAt' | translate }}</span>
              <span class="value">{{ ticket.updatedAt | date: 'short' }}</span>
            </div>
          </div>
        </mat-card-content>
      </mat-card>

      <mat-card class="action-card">
        <mat-card-content>
          <div class="action-bar">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'tickets.changeStatus' | translate }}</mat-label>
              <mat-select [value]="ticket.statusId" (selectionChange)="onStatusChange($event.value)">
                @for (status of statuses; track status.id) {
                  <mat-option [value]="status.id">{{ status.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>{{ 'tickets.changePriority' | translate }}</mat-label>
              <mat-select [value]="ticket.priorityId" (selectionChange)="onPriorityChange($event.value)">
                @for (priority of priorities; track priority.id) {
                  <mat-option [value]="priority.id">{{ priority.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>{{ 'tickets.assign' | translate }}</mat-label>
              <mat-select [value]="ticket.assignedToId" (selectionChange)="onAssignChange($event.value)">
                <mat-option [value]="null">{{ 'tickets.unassigned' | translate }}</mat-option>
                @for (user of users; track user.id) {
                  <mat-option [value]="user.id">{{ user.fullName }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
          </div>
        </mat-card-content>
      </mat-card>

      <mat-tab-group>
        <mat-tab [label]="'tickets.comments' | translate">
          <div class="tab-content">
            @if (ticket.comments.length > 0) {
              @for (comment of ticket.comments; track comment.id) {
                <div class="comment">
                  <div class="comment-header">
                    <strong>{{ comment.userName }}</strong>
                    @if (comment.isInternal) {
                      <mat-chip color="warn" selected>{{ 'tickets.internalNote' | translate }}</mat-chip>
                    }
                    <span class="comment-date">{{ comment.createdAt | date: 'short' }}</span>
                  </div>
                  <p class="comment-content">{{ comment.content }}</p>
                </div>
              }
            } @else {
              <p>{{ 'tickets.noComments' | translate }}</p>
            }

            <form [formGroup]="commentForm" (ngSubmit)="onAddComment()" class="comment-form">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'tickets.commentContent' | translate }}</mat-label>
                <textarea matInput formControlName="content" rows="3"></textarea>
              </mat-form-field>
              <mat-checkbox formControlName="isInternal">{{ 'tickets.internalNote' | translate }}</mat-checkbox>
              <div class="form-actions">
                <button mat-raised-button color="primary" type="submit" [disabled]="commentForm.invalid || savingComment">
                  {{ 'tickets.addComment' | translate }}
                </button>
              </div>
            </form>
          </div>
        </mat-tab>

        <mat-tab [label]="'tickets.attachments' | translate">
          <div class="tab-content">
            @if (ticket.attachments.length > 0) {
              <table mat-table [dataSource]="ticket.attachments" class="full-width">
                <ng-container matColumnDef="fileName">
                  <th mat-header-cell *matHeaderCellDef>{{ 'tickets.fileName' | translate }}</th>
                  <td mat-cell *matCellDef="let attachment">{{ attachment.fileName }}</td>
                </ng-container>

                <ng-container matColumnDef="contentType">
                  <th mat-header-cell *matHeaderCellDef>{{ 'tickets.fileType' | translate }}</th>
                  <td mat-cell *matCellDef="let attachment">{{ attachment.contentType }}</td>
                </ng-container>

                <ng-container matColumnDef="fileSize">
                  <th mat-header-cell *matHeaderCellDef>{{ 'tickets.fileSize' | translate }}</th>
                  <td mat-cell *matCellDef="let attachment">{{ formatFileSize(attachment.fileSize) }}</td>
                </ng-container>

                <ng-container matColumnDef="createdAt">
                  <th mat-header-cell *matHeaderCellDef>{{ 'tickets.createdAt' | translate }}</th>
                  <td mat-cell *matCellDef="let attachment">{{ attachment.createdAt | date: 'short' }}</td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="attachmentColumns"></tr>
                <tr mat-row *matRowDef="let row; columns: attachmentColumns;"></tr>
              </table>
            } @else {
              <p>{{ 'tickets.noAttachments' | translate }}</p>
            }
          </div>
        </mat-tab>

        <mat-tab [label]="'tickets.history' | translate">
          <div class="tab-content">
            @if (sortedHistory.length > 0) {
              <table mat-table [dataSource]="sortedHistory" class="full-width">
                <ng-container matColumnDef="field">
                  <th mat-header-cell *matHeaderCellDef>{{ 'tickets.field' | translate }}</th>
                  <td mat-cell *matCellDef="let entry">{{ entry.field }}</td>
                </ng-container>

                <ng-container matColumnDef="oldValue">
                  <th mat-header-cell *matHeaderCellDef>{{ 'tickets.oldValue' | translate }}</th>
                  <td mat-cell *matCellDef="let entry">{{ entry.oldValue }}</td>
                </ng-container>

                <ng-container matColumnDef="newValue">
                  <th mat-header-cell *matHeaderCellDef>{{ 'tickets.newValue' | translate }}</th>
                  <td mat-cell *matCellDef="let entry">{{ entry.newValue }}</td>
                </ng-container>

                <ng-container matColumnDef="userName">
                  <th mat-header-cell *matHeaderCellDef>{{ 'tickets.changedBy' | translate }}</th>
                  <td mat-cell *matCellDef="let entry">{{ entry.userName }}</td>
                </ng-container>

                <ng-container matColumnDef="createdAt">
                  <th mat-header-cell *matHeaderCellDef>{{ 'tickets.changedAt' | translate }}</th>
                  <td mat-cell *matCellDef="let entry">{{ entry.createdAt | date: 'short' }}</td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="historyColumns"></tr>
                <tr mat-row *matRowDef="let row; columns: historyColumns;"></tr>
              </table>
            } @else {
              <p>{{ 'tickets.noHistory' | translate }}</p>
            }
          </div>
        </mat-tab>
      </mat-tab-group>
    }
  `,
  styles: [`
    .detail-header { display: flex; justify-content: space-between; align-items: flex-start; margin-block-end: 16px; }
    .header-chips { display: flex; gap: 8px; margin-block-start: 8px; }
    .info-card, .action-card { margin-block-end: 16px; }
    .info-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px; }
    .info-item { display: flex; flex-direction: column; gap: 4px; }
    .info-item .label { font-size: 12px; color: rgba(0,0,0,0.6); }
    .info-item .value { font-size: 14px; }
    .action-bar { display: flex; gap: 16px; flex-wrap: wrap; }
    .full-width { width: 100%; }
    .tab-content { padding: 16px 0; }
    .comment { border-block-end: 1px solid rgba(0,0,0,0.12); padding-block: 12px; }
    .comment-header { display: flex; align-items: center; gap: 8px; }
    .comment-date { margin-inline-start: auto; font-size: 12px; color: rgba(0,0,0,0.6); }
    .comment-content { margin-block-start: 4px; }
    .comment-form { display: flex; flex-direction: column; gap: 8px; margin-block-start: 16px; max-width: 600px; }
    .form-actions { display: flex; justify-content: flex-end; }
  `]
})
export class TicketDetailComponent implements OnInit {
  private ticketsService = inject(TicketsService);
  private usersService = inject(UsersService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  ticket: TicketDetailDto | null = null;
  statuses: TicketStatusDto[] = [];
  priorities: TicketPriorityDto[] = [];
  users: UserDetail[] = [];

  attachmentColumns = ['fileName', 'contentType', 'fileSize', 'createdAt'];
  historyColumns = ['field', 'oldValue', 'newValue', 'userName', 'createdAt'];

  savingComment = false;

  commentForm = this.fb.group({
    content: ['', [Validators.required]],
    isInternal: [false]
  });

  get sortedHistory() {
    if (!this.ticket) return [];
    return [...this.ticket.history].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    forkJoin({
      statuses: this.ticketsService.getStatuses(),
      priorities: this.ticketsService.getPriorities(),
      users: this.usersService.getUsers({ pageSize: 100 })
    }).subscribe(result => {
      this.statuses = result.statuses;
      this.priorities = result.priorities;
      this.users = result.users.items;
    });

    if (id) {
      this.loadTicket(id);
    }
  }

  loadTicket(id: string): void {
    this.ticketsService.getTicketById(id).subscribe(ticket => {
      this.ticket = ticket;
    });
  }

  onStatusChange(statusId: string): void {
    if (!this.ticket) return;
    this.ticketsService.updateStatus(this.ticket.id, statusId).subscribe(() => this.loadTicket(this.ticket!.id));
  }

  onPriorityChange(priorityId: string): void {
    if (!this.ticket) return;
    this.ticketsService.updatePriority(this.ticket.id, priorityId).subscribe(() => this.loadTicket(this.ticket!.id));
  }

  onAssignChange(assignedToId: string | null): void {
    if (!this.ticket) return;
    this.ticketsService.assignTicket(this.ticket.id, assignedToId).subscribe(() => this.loadTicket(this.ticket!.id));
  }

  onAddComment(): void {
    if (this.commentForm.invalid || !this.ticket) return;

    this.savingComment = true;
    const value = this.commentForm.getRawValue();

    this.ticketsService.addComment(this.ticket.id, value.content!, value.isInternal!).subscribe({
      next: () => {
        this.savingComment = false;
        this.commentForm.reset({ content: '', isInternal: false });
        this.loadTicket(this.ticket!.id);
      },
      error: () => {
        this.savingComment = false;
      }
    });
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
