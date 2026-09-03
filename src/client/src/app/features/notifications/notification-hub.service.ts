import { Injectable, inject } from '@angular/core';
import { Subject } from 'rxjs';
import type { HubConnection } from '@microsoft/signalr';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class NotificationHubService {
  private connection: HubConnection | null = null;
  private authService = inject(AuthService);

  notificationReceived$ = new Subject<any>();

  async connect(): Promise<void> {
    if (this.connection) return;

    const signalR = await import('@microsoft/signalr');
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl.replace('/api', '')}/hubs/notifications`, {
        accessTokenFactory: () => this.authService.getToken() || ''
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('ReceiveNotification', (notification: any) =>
      this.notificationReceived$.next(notification));

    await this.connection.start();
  }

  async disconnect(): Promise<void> {
    await this.connection?.stop();
    this.connection = null;
  }
}
