import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { PaginatedList } from '../../core/models/paginated-list.model';
import { Observable } from 'rxjs';

export interface SlaPolicyDto {
  id: string;
  name: string;
  nameAr: string;
  priorityId: string | null;
  priorityName: string | null;
  categoryId: string | null;
  categoryName: string | null;
  firstResponseMinutes: number;
  resolutionMinutes: number;
  isActive: boolean;
}

export interface SlaPolicyRequest {
  name: string;
  nameAr: string;
  priorityId?: string | null;
  categoryId?: string | null;
  firstResponseMinutes: number;
  resolutionMinutes: number;
}

export interface SlaPolicyFilterParams {
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class SlaService extends ApiService {
  getSlaPolicies(params?: SlaPolicyFilterParams): Observable<PaginatedList<SlaPolicyDto>> {
    return this.get<PaginatedList<SlaPolicyDto>>('/v1/Sla', params);
  }

  getSlaPolicyById(id: string): Observable<SlaPolicyDto> {
    return this.get<SlaPolicyDto>(`/v1/Sla/${id}`);
  }

  createSlaPolicy(request: SlaPolicyRequest): Observable<SlaPolicyDto> {
    return this.post<SlaPolicyDto>('/v1/Sla', request);
  }

  updateSlaPolicy(id: string, request: SlaPolicyRequest): Observable<SlaPolicyDto> {
    return this.put<SlaPolicyDto>(`/v1/Sla/${id}`, { id, ...request });
  }

  deleteSlaPolicy(id: string): Observable<void> {
    return this.delete<void>(`/v1/Sla/${id}`);
  }
}
