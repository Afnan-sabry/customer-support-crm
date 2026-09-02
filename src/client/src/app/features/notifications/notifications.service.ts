import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { PaginatedList } from '../../core/models/paginated-list.model';
import { Observable } from 'rxjs';

export interface NotificationDto {
  id: string; title: string; titleAr: string;
  body: string; bodyAr: string; data: string | null;
  isRead: boolean; createdAt: string; readAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class NotificationsService extends ApiService {
  getNotifications(isRead?: boolean, page = 1, pageSize = 10): Observable<PaginatedList<NotificationDto>> {
    return this.get<PaginatedList<NotificationDto>>('/v1/notifications', { isRead, page, pageSize });
  }

  getUnreadCount(): Observable<number> {
    return this.get<number>('/v1/notifications/unread-count');
  }

  markAsRead(id: string): Observable<any> {
    return this.put<any>(`/v1/notifications/${id}/read`, {});
  }

  markAllAsRead(): Observable<any> {
    return this.put<any>('/v1/notifications/read-all', {});
  }
}
