import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { PaginatedList } from '../../core/models/paginated-list.model';
import { Observable } from 'rxjs';

export interface KnowledgeCategoryDto {
  id: string;
  name: string;
  nameAr: string;
  parentCategoryId: string | null;
  order: number;
  isActive: boolean;
}

export interface KnowledgeArticleDto {
  id: string;
  title: string;
  titleAr: string;
  categoryId: string;
  categoryName: string;
  tags: string | null;
  isPublished: boolean;
  viewCount: number;
  createdAt: string;
}

export interface KnowledgeArticleDetailDto extends KnowledgeArticleDto {
  content: string;
  contentAr: string;
  updatedAt: string;
}

export interface CreateCategoryRequest {
  name: string;
  nameAr: string;
  parentCategoryId?: string | null;
  description?: string;
  descriptionAr?: string;
  order?: number;
}

export interface UpdateCategoryRequest extends CreateCategoryRequest {
  isActive?: boolean;
}

export interface CreateArticleRequest {
  title: string;
  titleAr: string;
  content: string;
  contentAr: string;
  categoryId: string;
  tags?: string;
  isPublished: boolean;
}

export interface UpdateArticleRequest extends CreateArticleRequest {}

export interface ArticleFilterParams {
  categoryId?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class KnowledgeService extends ApiService {
  getCategories(isActive?: boolean): Observable<KnowledgeCategoryDto[]> {
    return this.get<KnowledgeCategoryDto[]>('/v1/knowledge/categories', { isActive });
  }

  createCategory(request: CreateCategoryRequest): Observable<KnowledgeCategoryDto> {
    return this.post<KnowledgeCategoryDto>('/v1/knowledge/categories', request);
  }

  updateCategory(id: string, request: UpdateCategoryRequest): Observable<KnowledgeCategoryDto> {
    return this.put<KnowledgeCategoryDto>(`/v1/knowledge/categories/${id}`, request);
  }

  deleteCategory(id: string): Observable<void> {
    return this.delete<void>(`/v1/knowledge/categories/${id}`);
  }

  getArticles(params?: ArticleFilterParams): Observable<PaginatedList<KnowledgeArticleDto>> {
    return this.get<PaginatedList<KnowledgeArticleDto>>('/v1/knowledge/articles', params);
  }

  getArticleById(id: string): Observable<KnowledgeArticleDetailDto> {
    return this.get<KnowledgeArticleDetailDto>(`/v1/knowledge/articles/${id}`);
  }

  searchArticles(term: string, page = 1, pageSize = 20): Observable<PaginatedList<KnowledgeArticleDto>> {
    return this.get<PaginatedList<KnowledgeArticleDto>>('/v1/knowledge/articles/search', { term, page, pageSize });
  }

  createArticle(request: CreateArticleRequest): Observable<KnowledgeArticleDto> {
    return this.post<KnowledgeArticleDto>('/v1/knowledge/articles', request);
  }

  updateArticle(id: string, request: UpdateArticleRequest): Observable<KnowledgeArticleDto> {
    return this.put<KnowledgeArticleDto>(`/v1/knowledge/articles/${id}`, request);
  }

  deleteArticle(id: string): Observable<void> {
    return this.delete<void>(`/v1/knowledge/articles/${id}`);
  }
}
