import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { Observable } from 'rxjs';

export interface ChatSessionDto {
  id: string;
  customerId: string;
  customerName: string;
  status: number;
  subject: string | null;
  assignedAgentId: string | null;
  assignedAgentName: string | null;
  messageCount: number;
  createdAt: string;
  lastMessagePreview?: string;
}

export interface ChatMessageDto {
  id: string;
  conversationId: string;
  direction: number;
  senderType: number;
  senderId: string | null;
  content: string;
  sentAt: string;
}

@Injectable({ providedIn: 'root' })
export class ChatService extends ApiService {
  getActiveSessions(): Observable<ChatSessionDto[]> {
    return this.get<ChatSessionDto[]>('/v1/conversations', {
      channel: 2, // LiveChat
      status: 0   // Active
    });
  }

  assignToMe(conversationId: string, agentId: string): Observable<any> {
    return this.put<any>(`/v1/conversations/${conversationId}/assign`, { conversationId, agentId });
  }

  endChat(conversationId: string): Observable<any> {
    return this.put<any>(`/v1/conversations/${conversationId}/close`, {});
  }
}
