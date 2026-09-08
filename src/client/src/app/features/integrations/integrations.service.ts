import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { Observable } from 'rxjs';

export interface WebhookSubscriptionDto {
  id: string;
  name: string;
  url: string;
  events: string[];
  isActive: boolean;
  createdAt: string;
}

export interface WebhookDeliveryLogDto {
  id: string;
  event: string;
  statusCode: number | null;
  success: boolean;
  attempt: number;
  errorMessage: string | null;
  createdAt: string;
}

export interface CreateWebhookSubscription {
  name: string;
  url: string;
  secret: string;
  events: string[];
  isActive: boolean;
  headers?: Record<string, string>;
}

export interface ErpStatusDto {
  provider: string;
  connected: string;
}

@Injectable({ providedIn: 'root' })
export class IntegrationsService extends ApiService {
  getSubscriptions(): Observable<WebhookSubscriptionDto[]> {
    return this.get<WebhookSubscriptionDto[]>('/v1/webhooks/subscriptions');
  }

  getSubscription(id: string): Observable<WebhookSubscriptionDto> {
    return this.get<WebhookSubscriptionDto>(`/v1/webhooks/subscriptions/${id}`);
  }

  createSubscription(data: CreateWebhookSubscription): Observable<WebhookSubscriptionDto> {
    return this.post<WebhookSubscriptionDto>('/v1/webhooks/subscriptions', data);
  }

  updateSubscription(id: string, data: any): Observable<any> {
    return this.put<any>(`/v1/webhooks/subscriptions/${id}`, { id, ...data });
  }

  deleteSubscription(id: string): Observable<any> {
    return this.delete<any>(`/v1/webhooks/subscriptions/${id}`);
  }

  testSubscription(id: string): Observable<any> {
    return this.post<any>(`/v1/webhooks/subscriptions/${id}/test`, {});
  }

  getDeliveryLogs(subscriptionId: string, page = 1, pageSize = 20): Observable<WebhookDeliveryLogDto[]> {
    return this.get<WebhookDeliveryLogDto[]>(`/v1/webhooks/subscriptions/${subscriptionId}/deliveries`, { page, pageSize });
  }

  getErpStatus(): Observable<ErpStatusDto> {
    return this.get<ErpStatusDto>('/v1/integrations/erp/status');
  }

  syncTicket(ticketId: string): Observable<any> {
    return this.post<any>(`/v1/integrations/erp/sync-ticket/${ticketId}`, {});
  }

  syncCustomer(customerId: string): Observable<any> {
    return this.post<any>(`/v1/integrations/erp/sync-customer/${customerId}`, {});
  }
}
