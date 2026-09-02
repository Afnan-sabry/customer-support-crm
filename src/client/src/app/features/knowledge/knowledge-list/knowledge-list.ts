import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { KnowledgeService, KnowledgeArticleDto, KnowledgeCategoryDto } from '../knowledge.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-knowledge-list',
  imports: [
    RouterLink, TranslateModule, DatePipe,
    MatTableModule, MatPaginatorModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule, MatChipsModule, MatDialogModule
  ],
  template: `
    <div class="knowledge-list-header">
      <h1>{{ 'knowledge.title' | translate }}</h1>
      <div class="header-actions">
        <button mat-button routerLink="/admin/knowledge/categories">
          <mat-icon>category</mat-icon>
          {{ 'knowledge.manageCategories' | translate }}
        </button>
        <button mat-raised-button color="primary" routerLink="/admin/knowledge/new">
          <mat-icon>add</mat-icon>
          {{ 'knowledge.createArticle' | translate }}
        </button>
      </div>
    </div>

    <div class="filters">
      <mat-form-field appearance="outline" class="search-field">
        <mat-label>{{ 'knowledge.search' | translate }}</mat-label>
        <input matInput (keyup)="onSearchInput($event)" [value]="search" />
        <mat-icon matSuffix>search</mat-icon>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>{{ 'knowledge.category' | translate }}</mat-label>
        <mat-select [value]="categoryId" (selectionChange)="onCategoryChange($event.value)">
          <mat-option [value]="null">{{ 'common.filter' | translate }}</mat-option>
          @for (category of categories; track category.id) {
            <mat-option [value]="category.id">{{ category.name }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
    </div>

    @if (articles.length > 0) {
      <table mat-table [dataSource]="articles" class="mat-elevation-z2 full-width">
        <ng-container matColumnDef="title">
          <th mat-header-cell *matHeaderCellDef>{{ 'knowledge.articleTitle' | translate }}</th>
          <td mat-cell *matCellDef="let article">{{ article.title }}</td>
        </ng-container>

        <ng-container matColumnDef="categoryName">
          <th mat-header-cell *matHeaderCellDef>{{ 'knowledge.category' | translate }}</th>
          <td mat-cell *matCellDef="let article">{{ article.categoryName }}</td>
        </ng-container>

        <ng-container matColumnDef="isPublished">
          <th mat-header-cell *matHeaderCellDef>{{ 'knowledge.published' | translate }}</th>
          <td mat-cell *matCellDef="let article">
            <mat-chip [color]="article.isPublished ? 'primary' : 'warn'" selected>
              {{ (article.isPublished ? 'knowledge.published' : 'knowledge.draft') | translate }}
            </mat-chip>
          </td>
        </ng-container>

        <ng-container matColumnDef="viewCount">
          <th mat-header-cell *matHeaderCellDef>{{ 'knowledge.viewCount' | translate }}</th>
          <td mat-cell *matCellDef="let article">{{ article.viewCount }}</td>
        </ng-container>

        <ng-container matColumnDef="createdAt">
          <th mat-header-cell *matHeaderCellDef>{{ 'tickets.createdAt' | translate }}</th>
          <td mat-cell *matCellDef="let article">{{ article.createdAt | date: 'short' }}</td>
        </ng-container>

        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef>{{ 'common.actions' | translate }}</th>
          <td mat-cell *matCellDef="let article">
            <button mat-icon-button [routerLink]="['/admin/knowledge', article.id]">
              <mat-icon>visibility</mat-icon>
            </button>
            <button mat-icon-button [routerLink]="['/admin/knowledge', article.id, 'edit']">
              <mat-icon>edit</mat-icon>
            </button>
            <button mat-icon-button color="warn" (click)="onDelete(article)">
              <mat-icon>delete</mat-icon>
            </button>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
      </table>

      <mat-paginator
        [length]="totalCount"
        [pageSize]="pageSize"
        [pageIndex]="page - 1"
        [pageSizeOptions]="[10, 20, 50]"
        (page)="onPageChange($event)">
      </mat-paginator>
    } @else {
      <p>{{ 'knowledge.noArticles' | translate }}</p>
    }
  `,
  styles: [`
    .knowledge-list-header { display: flex; justify-content: space-between; align-items: center; margin-block-end: 16px; }
    .header-actions { display: flex; gap: 8px; }
    .filters { display: flex; align-items: center; gap: 16px; margin-block-end: 16px; flex-wrap: wrap; }
    .search-field { width: 100%; max-width: 300px; }
    .full-width { width: 100%; }
  `]
})
export class KnowledgeListComponent implements OnInit {
  private knowledgeService = inject(KnowledgeService);
  private dialog = inject(MatDialog);
  private translate = inject(TranslateService);

  displayedColumns = ['title', 'categoryName', 'isPublished', 'viewCount', 'createdAt', 'actions'];
  articles: KnowledgeArticleDto[] = [];
  categories: KnowledgeCategoryDto[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  search = '';
  categoryId: string | null = null;

  private searchSubject = new Subject<string>();

  ngOnInit(): void {
    this.searchSubject.pipe(debounceTime(300), distinctUntilChanged()).subscribe(value => {
      this.search = value;
      this.page = 1;
      this.loadArticles();
    });

    this.knowledgeService.getCategories(true).subscribe(categories => {
      this.categories = categories;
    });

    this.loadArticles();
  }

  loadArticles(): void {
    if (this.search) {
      this.knowledgeService.searchArticles(this.search, this.page, this.pageSize).subscribe(result => {
        this.applyResult(result);
      });
      return;
    }

    this.knowledgeService.getArticles({
      categoryId: this.categoryId ?? undefined,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe(result => {
      this.applyResult(result);
    });
  }

  private applyResult(result: { items: KnowledgeArticleDto[]; totalCount: number; page: number; pageSize: number }): void {
    this.articles = result.items;
    this.totalCount = result.totalCount;
    this.page = result.page;
    this.pageSize = result.pageSize;
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchSubject.next(value);
  }

  onCategoryChange(categoryId: string | null): void {
    this.categoryId = categoryId;
    this.page = 1;
    this.loadArticles();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadArticles();
  }

  onDelete(article: KnowledgeArticleDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: this.translate.instant('common.delete'),
        message: this.translate.instant('knowledge.deleteConfirm')
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.knowledgeService.deleteArticle(article.id).subscribe(() => this.loadArticles());
      }
    });
  }
}
