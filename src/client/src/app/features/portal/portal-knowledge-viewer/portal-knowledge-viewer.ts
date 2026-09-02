import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { PortalKnowledgeService, PortalArticleDetailDto } from '../portal-knowledge.service';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-portal-knowledge-viewer',
  imports: [
    RouterLink, TranslateModule, DatePipe,
    MatCardModule, MatButtonModule, MatIconModule, MatChipsModule
  ],
  template: `
    @if (article) {
      <div class="viewer-header">
        <div>
          <h1>{{ languageService.isRtl() ? article.titleAr : article.title }}</h1>
          <mat-chip color="accent" selected>{{ article.categoryName }}</mat-chip>
        </div>
        <button mat-button routerLink="/portal/knowledge">
          <mat-icon>arrow_back</mat-icon>
          {{ 'common.back' | translate }}
        </button>
      </div>

      <mat-card class="content-card">
        <mat-card-content>
          <div class="article-content" [innerHTML]="languageService.isRtl() ? article.contentAr : article.content"></div>
        </mat-card-content>
      </mat-card>

      <p class="meta">
        {{ 'knowledge.viewCount' | translate }}: {{ article.viewCount }}
        &middot;
        {{ article.createdAt | date: 'short' }}
      </p>
    }
  `,
  styles: [`
    .viewer-header { display: flex; justify-content: space-between; align-items: flex-start; margin-block-end: 16px; }
    .content-card { margin-block-end: 16px; }
    .article-content { white-space: pre-wrap; }
    .meta { color: rgba(0,0,0,0.6); font-size: 12px; }
  `]
})
export class PortalKnowledgeViewerComponent implements OnInit {
  private knowledgeService = inject(PortalKnowledgeService);
  private route = inject(ActivatedRoute);
  protected languageService = inject(LanguageService);

  article: PortalArticleDetailDto | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.knowledgeService.getArticleById(id).subscribe(article => this.article = article);
    }
  }
}
