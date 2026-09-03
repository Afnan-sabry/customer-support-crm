import { Injectable } from '@angular/core';
import { PortalApiService } from './portal-api.service';
import { PaginatedList } from '../../core/models/paginated-list.model';
import { Observable } from 'rxjs';

export interface PortalArticleDto {
  id: string; title: string; titleAr: string;
  categoryName: string; tags: string | null; viewCount: number; createdAt: string;
}

export interface PortalArticleDetailDto extends PortalArticleDto {
  content: string; contentAr: string;
}

@Injectable({ providedIn: 'root' })
export class PortalKnowledgeService extends PortalApiService {
  searchArticles(term?: string, categoryId?: string, page = 1, pageSize = 20): Observable<PaginatedList<PortalArticleDto>> {
    return this.get<PaginatedList<PortalArticleDto>>('/v1/portal/knowledge', { term, categoryId, page, pageSize });
  }

  getArticleById(id: string): Observable<PortalArticleDetailDto> {
    return this.get<PortalArticleDetailDto>(`/v1/portal/knowledge/${id}`);
  }
}
