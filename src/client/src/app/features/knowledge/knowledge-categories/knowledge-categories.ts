import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { KnowledgeService, KnowledgeCategoryDto } from '../knowledge.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-knowledge-categories',
  imports: [
    RouterLink, ReactiveFormsModule, TranslateModule,
    MatTableModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule, MatDialogModule
  ],
  template: `
    <div class="categories-header">
      <h1>{{ 'knowledge.manageCategories' | translate }}</h1>
      <button mat-button routerLink="/admin/knowledge">
        <mat-icon>arrow_back</mat-icon>
        {{ 'common.back' | translate }}
      </button>
    </div>

    <mat-card class="form-card">
      <mat-card-header>
        <mat-card-title>{{ (editingId ? 'common.edit' : 'knowledge.addCategory') | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'knowledge.categoryName' | translate }}</mat-label>
            <input matInput formControlName="name" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'knowledge.categoryNameAr' | translate }}</mat-label>
            <input matInput formControlName="nameAr" dir="rtl" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'knowledge.parentCategory' | translate }}</mat-label>
            <mat-select formControlName="parentCategoryId">
              <mat-option [value]="null">{{ 'common.no' | translate }}</mat-option>
              @for (category of parentOptions; track category.id) {
                <mat-option [value]="category.id">{{ category.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ 'knowledge.order' | translate }}</mat-label>
            <input matInput type="number" formControlName="order" />
          </mat-form-field>

          @if (error) {
            <p class="error-message">{{ error }}</p>
          }

          <div class="form-actions">
            @if (editingId) {
              <button mat-button type="button" (click)="onCancelEdit()">{{ 'common.cancel' | translate }}</button>
            }
            <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || saving">
              {{ 'common.save' | translate }}
            </button>
          </div>
        </form>
      </mat-card-content>
    </mat-card>

    <table mat-table [dataSource]="categories" class="mat-elevation-z2 full-width">
      <ng-container matColumnDef="name">
        <th mat-header-cell *matHeaderCellDef>{{ 'knowledge.categoryName' | translate }}</th>
        <td mat-cell *matCellDef="let category">{{ category.name }}</td>
      </ng-container>

      <ng-container matColumnDef="nameAr">
        <th mat-header-cell *matHeaderCellDef>{{ 'knowledge.categoryNameAr' | translate }}</th>
        <td mat-cell *matCellDef="let category">{{ category.nameAr }}</td>
      </ng-container>

      <ng-container matColumnDef="order">
        <th mat-header-cell *matHeaderCellDef>{{ 'knowledge.order' | translate }}</th>
        <td mat-cell *matCellDef="let category">{{ category.order }}</td>
      </ng-container>

      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef>{{ 'common.actions' | translate }}</th>
        <td mat-cell *matCellDef="let category">
          <button mat-icon-button (click)="onEdit(category)">
            <mat-icon>edit</mat-icon>
          </button>
          <button mat-icon-button color="warn" (click)="onDelete(category)">
            <mat-icon>delete</mat-icon>
          </button>
        </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
    </table>
  `,
  styles: [`
    .categories-header { display: flex; justify-content: space-between; align-items: center; margin-block-end: 16px; }
    .form-card { max-width: 500px; margin-block-end: 16px; }
    .full-width { width: 100%; }
    .form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-block-start: 16px; }
    .error-message { color: #f44336; margin-block-end: 16px; font-size: 14px; }
  `]
})
export class KnowledgeCategoriesComponent implements OnInit {
  private fb = inject(FormBuilder);
  private knowledgeService = inject(KnowledgeService);
  private dialog = inject(MatDialog);
  private translate = inject(TranslateService);

  displayedColumns = ['name', 'nameAr', 'order', 'actions'];
  categories: KnowledgeCategoryDto[] = [];
  editingId: string | null = null;
  saving = false;
  error: string | null = null;

  form = this.fb.group({
    name: ['', [Validators.required]],
    nameAr: ['', [Validators.required]],
    parentCategoryId: [null as string | null],
    order: [0, [Validators.required]]
  });

  get parentOptions(): KnowledgeCategoryDto[] {
    return this.categories.filter(c => c.id !== this.editingId);
  }

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.knowledgeService.getCategories().subscribe(categories => {
      this.categories = categories;
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.saving = true;
    this.error = null;
    const value = this.form.getRawValue();

    const request = {
      name: value.name!,
      nameAr: value.nameAr!,
      parentCategoryId: value.parentCategoryId,
      order: value.order!
    };

    const result$ = this.editingId
      ? this.knowledgeService.updateCategory(this.editingId, request)
      : this.knowledgeService.createCategory(request);

    result$.subscribe({
      next: () => {
        this.saving = false;
        this.resetForm();
        this.loadCategories();
      },
      error: (err) => this.handleError(err)
    });
  }

  onEdit(category: KnowledgeCategoryDto): void {
    this.editingId = category.id;
    this.form.patchValue({
      name: category.name,
      nameAr: category.nameAr,
      parentCategoryId: category.parentCategoryId,
      order: category.order
    });
  }

  onCancelEdit(): void {
    this.resetForm();
  }

  onDelete(category: KnowledgeCategoryDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: this.translate.instant('common.delete'),
        message: this.translate.instant('knowledge.deleteCategoryConfirm')
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.knowledgeService.deleteCategory(category.id).subscribe(() => this.loadCategories());
      }
    });
  }

  private resetForm(): void {
    this.editingId = null;
    this.form.reset({ name: '', nameAr: '', parentCategoryId: null, order: 0 });
  }

  private handleError(err: any): void {
    this.saving = false;
    this.error = err.error?.detail || err.error?.title || 'An error occurred';
  }
}
