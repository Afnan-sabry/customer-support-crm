import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { PortalKnowledgeService, PortalArticleDto } from '../portal-knowledge.service';

@Component({
  selector: 'app-portal-knowledge-list',
  imports: [
    RouterLink, FormsModule, TranslateModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatIconModule, MatChipsModule, MatPaginatorModule
  ],
  template: `
    <h1>{{ 'portal.knowledgeTitle' | translate }}</h1>

    <mat-form-field appearance="outline" class="search-field">
      <mat-label>{{ 'knowledge.search' | translate }}</mat-label>
      <input matInput [(ngModel)]="search" (ngModelChange)="onSearchChange($event)" />
      <mat-icon matSuffix>search</mat-icon>
    </mat-form-field>

    <div class="article-grid">
      @for (article of articles; track article.id) {
        <mat-card class="article-card" [routerLink]="['/portal/knowledge', article.id]">
          <mat-card-content>
            <h3>{{ article.title }}</h3>
            <mat-chip color="accent" selected>{{ article.categoryName }}</mat-chip>
            <p class="views">{{ 'knowledge.viewCount' | translate }}: {{ article.viewCount }}</p>
          </mat-card-content>
        </mat-card>
      }
    </div>

    @if (articles.length === 0) {
      <p class="no-results">{{ 'knowledge.noArticles' | translate }}</p>
    }

    <mat-paginator
      [length]="totalCount"
      [pageSize]="pageSize"
      [pageIndex]="page - 1"
      [pageSizeOptions]="[10, 20, 50]"
      (page)="onPageChange($event)">
    </mat-paginator>
  `,
  styles: [`
    .search-field { width: 100%; max-width: 400px; margin-block-end: 16px; }
    .article-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 16px; margin-block-end: 16px; }
    .article-card { cursor: pointer; }
    .article-card h3 { margin-block-start: 0; }
    .views { color: rgba(0,0,0,0.6); font-size: 12px; margin-block-end: 0; margin-block-start: 8px; }
    .no-results { color: rgba(0,0,0,0.6); }
  `]
})
export class PortalKnowledgeListComponent implements OnInit {
  private knowledgeService = inject(PortalKnowledgeService);

  articles: PortalArticleDto[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  search = '';

  private searchSubject = new Subject<string>();

  ngOnInit(): void {
    this.searchSubject.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => {
      this.page = 1;
      this.loadArticles();
    });
    this.loadArticles();
  }

  onSearchChange(value: string): void {
    this.search = value;
    this.searchSubject.next(value);
  }

  loadArticles(): void {
    this.knowledgeService.searchArticles(this.search || undefined, undefined, this.page, this.pageSize)
      .subscribe(result => {
        this.articles = result.items;
        this.totalCount = result.totalCount;
        this.page = result.page;
        this.pageSize = result.pageSize;
      });
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadArticles();
  }
}
