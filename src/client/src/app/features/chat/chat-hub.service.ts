import { Injectable, inject } from '@angular/core';
import { Subject } from 'rxjs';
import type { HubConnection } from '@microsoft/signalr';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../../environments/environment';

export interface HubMessage {
  id: string;
  conversationId: string;
  direction: number;
  senderType: number;
  senderId: string | null;
  content: string;
  sentAt: string;
}

@Injectable({ providedIn: 'root' })
export class ChatHubService {
  private connection: HubConnection | null = null;
  private authService = inject(AuthService);

  messageReceived$ = new Subject<HubMessage>();
  typingIndicator$ = new Subject<{ conversationId: string; userId: string }>();
  chatEnded$ = new Subject<string>();

  async connect(): Promise<void> {
    if (this.connection) return;

    const signalR = await import('@microsoft/signalr');
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl.replace('/api', '')}/hubs/chat`, {
        accessTokenFactory: () => this.authService.getToken() || ''
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('ReceiveMessage', (msg: HubMessage) => this.messageReceived$.next(msg));
    this.connection.on('TypingIndicator', (data: any) => this.typingIndicator$.next(data));
    this.connection.on('ChatEnded', (id: string) => this.chatEnded$.next(id));

    await this.connection.start();
  }

  async joinChat(conversationId: string): Promise<void> {
    await this.connection?.invoke('JoinChat', conversationId);
  }

  async sendMessage(conversationId: string, content: string): Promise<void> {
    await this.connection?.invoke('SendMessage', conversationId, content);
  }

  async sendTypingIndicator(conversationId: string): Promise<void> {
    await this.connection?.invoke('SendTypingIndicator', conversationId);
  }

  async endChat(conversationId: string): Promise<void> {
    await this.connection?.invoke('EndChat', conversationId);
  }

  async disconnect(): Promise<void> {
    await this.connection?.stop();
    this.connection = null;
  }
}
