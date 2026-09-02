import { Injectable } from '@angular/core';
import { PortalApiService } from './portal-api.service';
import { PaginatedList } from '../../core/models/paginated-list.model';
import { Observable } from 'rxjs';

export interface PortalTicketDto {
  id: string; ticketNumber: string; subject: string;
  categoryName: string; priorityName: string; statusName: string;
  createdAt: string; updatedAt: string;
}

export interface PortalTicketDetailDto extends PortalTicketDto {
  description: string; comments: PortalCommentDto[];
}

export interface PortalCommentDto {
  id: string; content: string; authorName: string; createdAt: string; isAgent: boolean;
}

export interface PortalTicketRequest {
  categoryId: string; priorityId: string; subject: string; description: string;
}

export interface TicketCategoryDto { id: string; name: string; nameAr: string; }
export interface TicketPriorityDto { id: string; name: string; nameAr: string; }

@Injectable({ providedIn: 'root' })
export class PortalTicketService extends PortalApiService {
  getTickets(page = 1, pageSize = 20): Observable<PaginatedList<PortalTicketDto>> {
    return this.get<PaginatedList<PortalTicketDto>>('/v1/portal/tickets', { page, pageSize });
  }

  getTicketById(id: string): Observable<PortalTicketDetailDto> {
    return this.get<PortalTicketDetailDto>(`/v1/portal/tickets/${id}`);
  }

  submitTicket(request: PortalTicketRequest): Observable<PortalTicketDto> {
    return this.post<PortalTicketDto>('/v1/portal/tickets', request);
  }

  addComment(ticketId: string, content: string): Observable<PortalCommentDto> {
    return this.post<PortalCommentDto>(`/v1/portal/tickets/${ticketId}/comments`, { content });
  }

  getCategories(): Observable<TicketCategoryDto[]> {
    return this.get<TicketCategoryDto[]>('/v1/tickets/categories');
  }

  getPriorities(): Observable<TicketPriorityDto[]> {
    return this.get<TicketPriorityDto[]>('/v1/tickets/priorities');
  }
}
