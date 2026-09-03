import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { KnowledgeService, KnowledgeArticleDetailDto } from '../knowledge.service';

@Component({
  selector: 'app-knowledge-article',
  imports: [
    RouterLink, TranslateModule, DatePipe,
    MatCardModule, MatButtonModule, MatIconModule, MatChipsModule
  ],
  template: `
    @if (article) {
      <div class="detail-header">
        <div>
          <h1>{{ article.title }}</h1>
          <div class="header-chips">
            <mat-chip [color]="article.isPublished ? 'primary' : 'warn'" selected>
              {{ (article.isPublished ? 'knowledge.published' : 'knowledge.draft') | translate }}
            </mat-chip>
            <mat-chip color="accent" selected>{{ article.categoryName }}</mat-chip>
          </div>
        </div>
        <div class="header-actions">
          <button mat-icon-button [routerLink]="['/admin/knowledge', article.id, 'edit']">
            <mat-icon>edit</mat-icon>
          </button>
          <button mat-button routerLink="/admin/knowledge">
            <mat-icon>arrow_back</mat-icon>
            {{ 'common.back' | translate }}
          </button>
        </div>
      </div>

      <mat-card class="info-card">
        <mat-card-content>
          <div class="info-grid">
            <div class="info-item">
              <span class="label">{{ 'knowledge.viewCount' | translate }}</span>
              <span class="value">{{ article.viewCount }}</span>
            </div>
            <div class="info-item">
              <span class="label">{{ 'tickets.createdAt' | translate }}</span>
              <span class="value">{{ article.createdAt | date: 'short' }}</span>
            </div>
            <div class="info-item">
              <span class="label">{{ 'tickets.updatedAt' | translate }}</span>
              <span class="value">{{ article.updatedAt | date: 'short' }}</span>
            </div>
          </div>

          @if (article.tags) {
            <div class="tags">
              @for (tag of tagList; track tag) {
                <mat-chip>{{ tag }}</mat-chip>
              }
            </div>
          }
        </mat-card-content>
      </mat-card>

      <mat-card class="content-card">
        <mat-card-content>
          <p class="content">{{ article.content }}</p>
        </mat-card-content>
      </mat-card>
    }
  `,
  styles: [`
    .detail-header { display: flex; justify-content: space-between; align-items: flex-start; margin-block-end: 16px; }
    .header-chips { display: flex; gap: 8px; margin-block-start: 8px; }
    .header-actions { display: flex; gap: 4px; }
    .info-card, .content-card { margin-block-end: 16px; }
    .info-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px; }
    .info-item { display: flex; flex-direction: column; gap: 4px; }
    .info-item .label { font-size: 12px; color: rgba(0,0,0,0.6); }
    .info-item .value { font-size: 14px; }
    .tags { display: flex; gap: 8px; margin-block-start: 16px; flex-wrap: wrap; }
    .content { white-space: pre-wrap; }
  `]
})
export class KnowledgeArticleComponent implements OnInit {
  private knowledgeService = inject(KnowledgeService);
  private route = inject(ActivatedRoute);

  article: KnowledgeArticleDetailDto | null = null;

  get tagList(): string[] {
    if (!this.article?.tags) return [];
    return this.article.tags.split(',').map(t => t.trim()).filter(t => t.length > 0);
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.knowledgeService.getArticleById(id).subscribe(article => {
        this.article = article;
      });
    }
  }
}
