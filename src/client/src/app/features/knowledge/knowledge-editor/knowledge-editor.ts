import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { KnowledgeService, KnowledgeCategoryDto } from '../knowledge.service';

@Component({
  selector: 'app-knowledge-editor',
  imports: [
    ReactiveFormsModule, TranslateModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatCheckboxModule
  ],
  template: `
    <mat-card class="form-card">
      <mat-card-header>
        <mat-card-title>{{ (isEditMode ? 'knowledge.editArticle' : 'knowledge.createArticle') | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'knowledge.articleTitle' | translate }}</mat-label>
            <input matInput formControlName="title" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'knowledge.articleTitleAr' | translate }}</mat-label>
            <input matInput formControlName="titleAr" dir="rtl" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'knowledge.category' | translate }}</mat-label>
            <mat-select formControlName="categoryId">
              @for (category of categories; track category.id) {
                <mat-option [value]="category.id">{{ category.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'knowledge.content' | translate }}</mat-label>
            <textarea matInput formControlName="content" rows="8"></textarea>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'knowledge.contentAr' | translate }}</mat-label>
            <textarea matInput formControlName="contentAr" rows="8" dir="rtl"></textarea>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'knowledge.tags' | translate }}</mat-label>
            <input matInput formControlName="tags" />
          </mat-form-field>

          <mat-checkbox formControlName="isPublished">{{ 'knowledge.published' | translate }}</mat-checkbox>

          @if (error) {
            <p class="error-message">{{ error }}</p>
          }

          <div class="form-actions">
            <button mat-button type="button" (click)="onCancel()">{{ 'common.cancel' | translate }}</button>
            <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || saving">
              {{ 'common.save' | translate }}
            </button>
          </div>
        </form>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .form-card { max-width: 700px; margin: 0 auto; }
    .full-width { width: 100%; }
    .form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-block-start: 16px; }
    .error-message { color: #f44336; margin-block-end: 16px; font-size: 14px; }
  `]
})
export class KnowledgeEditorComponent implements OnInit {
  private fb = inject(FormBuilder);
  private knowledgeService = inject(KnowledgeService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  saving = false;
  error: string | null = null;
  isEditMode = false;
  articleId: string | null = null;

  categories: KnowledgeCategoryDto[] = [];

  form = this.fb.group({
    title: ['', [Validators.required]],
    titleAr: ['', [Validators.required]],
    categoryId: ['', [Validators.required]],
    content: ['', [Validators.required]],
    contentAr: ['', [Validators.required]],
    tags: [''],
    isPublished: [false]
  });

  ngOnInit(): void {
    this.articleId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.articleId;

    this.knowledgeService.getCategories(true).subscribe(categories => {
      this.categories = categories;
    });

    if (this.isEditMode && this.articleId) {
      this.knowledgeService.getArticleById(this.articleId).subscribe(article => {
        this.form.patchValue({
          title: article.title,
          titleAr: article.titleAr,
          categoryId: article.categoryId,
          content: article.content,
          contentAr: article.contentAr,
          tags: article.tags ?? '',
          isPublished: article.isPublished
        });
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.saving = true;
    this.error = null;
    const value = this.form.getRawValue();

    const request = {
      title: value.title!,
      titleAr: value.titleAr!,
      content: value.content!,
      contentAr: value.contentAr!,
      categoryId: value.categoryId!,
      tags: value.tags ?? '',
      isPublished: value.isPublished!
    };

    const result$ = this.isEditMode && this.articleId
      ? this.knowledgeService.updateArticle(this.articleId, request)
      : this.knowledgeService.createArticle(request);

    result$.subscribe({
      next: (article) => this.router.navigate(['/admin/knowledge', article.id]),
      error: (err) => this.handleError(err)
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/knowledge']);
  }

  private handleError(err: any): void {
    this.saving = false;
    this.error = err.error?.detail || err.error?.title || 'An error occurred';
  }
}
