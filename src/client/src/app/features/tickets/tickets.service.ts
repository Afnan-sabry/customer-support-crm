import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { PaginatedList } from '../../core/models/paginated-list.model';
import { Observable } from 'rxjs';

export interface TicketDto {
  id: string;
  ticketNumber: string;
  subject: string;
  customerName: string;
  categoryName: string;
  priorityName: string;
  statusName: string;
  assignedToName: string | null;
  createdAt: string;
}

export interface TicketCommentDto {
  id: string;
  userId: string;
  userName: string;
  content: string;
  isInternal: boolean;
  createdAt: string;
}

export interface TicketAttachmentDto {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  createdAt: string;
}

export interface TicketHistoryDto {
  id: string;
  userName: string | null;
  field: string;
  oldValue: string | null;
  newValue: string | null;
  createdAt: string;
}

export interface TicketDetailDto {
  id: string;
  ticketNumber: string;
  customerId: string;
  customerName: string;
  categoryId: string;
  categoryName: string;
  priorityId: string;
  priorityName: string;
  statusId: string;
  statusName: string;
  assignedToId: string | null;
  assignedToName: string | null;
  subject: string;
  description: string;
  createdAt: string;
  updatedAt: string;
  comments: TicketCommentDto[];
  attachments: TicketAttachmentDto[];
  history: TicketHistoryDto[];
}

export interface TicketCategoryDto {
  id: string;
  name: string;
  nameAr: string;
}

export interface TicketPriorityDto {
  id: string;
  name: string;
  nameAr: string;
  level: number;
}

export interface TicketStatusDto {
  id: string;
  name: string;
  nameAr: string;
  order: number;
  isFinal: boolean;
}

export interface CreateTicketRequest {
  customerId: string;
  categoryId: string;
  priorityId: string;
  subject: string;
  description: string;
}

export interface TicketFilterParams {
  statusId?: string;
  priorityId?: string;
  categoryId?: string;
  assignedToId?: string;
  customerId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateAttachmentRequest {
  ticketId: string;
  fileName: string;
  filePath: string;
  contentType: string;
  fileSize: number;
}

@Injectable({ providedIn: 'root' })
export class TicketsService extends ApiService {
  getTickets(params?: TicketFilterParams): Observable<PaginatedList<TicketDto>> {
    return this.get<PaginatedList<TicketDto>>('/v1/tickets', params);
  }

  getTicketById(id: string): Observable<TicketDetailDto> {
    return this.get<TicketDetailDto>(`/v1/tickets/${id}`);
  }

  createTicket(request: CreateTicketRequest): Observable<TicketDto> {
    return this.post<TicketDto>('/v1/tickets', request);
  }

  updateStatus(ticketId: string, statusId: string): Observable<void> {
    return this.put<void>(`/v1/tickets/${ticketId}/status`, { ticketId, statusId });
  }

  updatePriority(ticketId: string, priorityId: string): Observable<void> {
    return this.put<void>(`/v1/tickets/${ticketId}/priority`, { ticketId, priorityId });
  }

  assignTicket(ticketId: string, assignedToId: string | null): Observable<void> {
    return this.put<void>(`/v1/tickets/${ticketId}/assign`, { ticketId, assignedToId });
  }

  addComment(ticketId: string, content: string, isInternal: boolean): Observable<TicketCommentDto> {
    return this.post<TicketCommentDto>(`/v1/tickets/${ticketId}/comments`, { ticketId, content, isInternal });
  }

  addAttachment(ticketId: string, request: CreateAttachmentRequest): Observable<TicketAttachmentDto> {
    return this.post<TicketAttachmentDto>(`/v1/tickets/${ticketId}/attachments`, request);
  }

  getCategories(): Observable<TicketCategoryDto[]> {
    return this.get<TicketCategoryDto[]>('/v1/tickets/categories');
  }

  getPriorities(): Observable<TicketPriorityDto[]> {
    return this.get<TicketPriorityDto[]>('/v1/tickets/priorities');
  }

  getStatuses(): Observable<TicketStatusDto[]> {
    return this.get<TicketStatusDto[]>('/v1/tickets/statuses');
  }
}
