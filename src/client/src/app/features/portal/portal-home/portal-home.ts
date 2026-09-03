import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatListModule } from '@angular/material/list';
import { PortalTicketService, PortalTicketDto } from '../portal-ticket.service';
import { PortalKnowledgeService, PortalArticleDto } from '../portal-knowledge.service';
import { PortalAuthService } from '../portal-auth.service';

@Component({
  selector: 'app-portal-home',
  imports: [
    RouterLink, FormsModule, TranslateModule, DatePipe,
    MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatChipsModule, MatListModule
  ],
  template: `
    <div class="home-header">
      <h1>{{ 'portal.welcome' | translate: { name: userName } }}</h1>
    </div>

    <div class="quick-actions">
      <button mat-raised-button color="primary" routerLink="/portal/tickets/new">
        <mat-icon>add</mat-icon>
        {{ 'portal.submitTicket' | translate }}
      </button>
      <button mat-stroked-button routerLink="/portal/tickets">
        <mat-icon>confirmation_number</mat-icon>
        {{ 'portal.myTickets' | translate }}
      </button>
      <button mat-stroked-button routerLink="/portal/knowledge">
        <mat-icon>menu_book</mat-icon>
        {{ 'portal.browseKnowledge' | translate }}
      </button>
    </div>

    <mat-card class="search-card">
      <mat-card-content>
        <h2>{{ 'portal.searchHelp' | translate }}</h2>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ 'portal.searchPlaceholder' | translate }}</mat-label>
          <input matInput [(ngModel)]="searchTerm" (keyup.enter)="onSearch()" />
          <button mat-icon-button matSuffix (click)="onSearch()">
            <mat-icon>search</mat-icon>
          </button>
        </mat-form-field>

        @if (searchResults.length > 0) {
          <mat-nav-list>
            @for (article of searchResults; track article.id) {
              <a mat-list-item [routerLink]="['/portal/knowledge', article.id]">
                <span matListItemTitle>{{ article.title }}</span>
                <span matListItemLine>{{ article.categoryName }}</span>
              </a>
            }
          </mat-nav-list>
        } @else if (searched) {
          <p class="no-results">{{ 'portal.noResults' | translate }}</p>
        }
      </mat-card-content>
    </mat-card>

    <mat-card class="recent-tickets-card">
      <mat-card-header>
        <mat-card-title>{{ 'portal.recentTickets' | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        @if (recentTickets.length > 0) {
          <mat-nav-list>
            @for (ticket of recentTickets; track ticket.id) {
              <a mat-list-item [routerLink]="['/portal/tickets', ticket.id]">
                <span matListItemTitle>{{ ticket.subject }}</span>
                <span matListItemLine>
                  {{ ticket.ticketNumber }} · {{ ticket.createdAt | date: 'short' }}
                </span>
                <mat-chip color="primary" selected>{{ ticket.statusName }}</mat-chip>
              </a>
            }
          </mat-nav-list>
        } @else {
          <p class="no-results">{{ 'portal.noTickets' | translate }}</p>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .home-header { margin-block-end: 16px; }
    .quick-actions { display: flex; gap: 12px; margin-block-end: 24px; flex-wrap: wrap; }
    .search-card, .recent-tickets-card { margin-block-end: 16px; }
    .full-width { width: 100%; }
    .no-results { color: rgba(0,0,0,0.6); }
  `]
})
export class PortalHomeComponent implements OnInit {
  private ticketService = inject(PortalTicketService);
  private knowledgeService = inject(PortalKnowledgeService);
  private portalAuthService = inject(PortalAuthService);
  private router = inject(Router);

  userName = '';
  recentTickets: PortalTicketDto[] = [];
  searchTerm = '';
  searchResults: PortalArticleDto[] = [];
  searched = false;

  ngOnInit(): void {
    const user = this.portalAuthService.getCurrentUser();
    this.userName = user?.fullName ?? '';

    this.ticketService.getTickets(1, 5).subscribe(result => {
      this.recentTickets = result.items;
    });
  }

  onSearch(): void {
    if (!this.searchTerm.trim()) return;
    this.searched = true;
    this.knowledgeService.searchArticles(this.searchTerm.trim()).subscribe(result => {
      this.searchResults = result.items;
    });
  }
}
