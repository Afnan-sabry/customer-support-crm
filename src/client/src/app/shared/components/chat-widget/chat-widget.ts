import { Component, OnDestroy, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatBadgeModule } from '@angular/material/badge';
import { Subscription } from 'rxjs';
import { ChatHubService, HubMessage } from '../../../features/chat/chat-hub.service';
import { PortalApiService } from '../../../features/portal/portal-api.service';

interface StartChatResponse { id: string; }

@Component({
  selector: 'app-chat-widget',
  imports: [
    FormsModule, TranslateModule, DatePipe,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatCardModule, MatBadgeModule
  ],
  template: `
    @if (!open) {
      <button mat-fab color="primary" class="chat-fab" (click)="toggle()"
              [matBadge]="unread > 0 ? unread : null" matBadgeColor="warn">
        <mat-icon>chat</mat-icon>
      </button>
    } @else {
      <mat-card class="chat-panel">
        <div class="chat-panel-header">
          <span>{{ 'portal.chatTitle' | translate }}</span>
          <button mat-icon-button (click)="toggle()">
            <mat-icon>close</mat-icon>
          </button>
        </div>

        <div class="chat-panel-body">
          @if (!conversationId) {
            <div class="chat-start">
              <p>{{ 'portal.chatIntro' | translate }}</p>
              <button mat-raised-button color="primary" (click)="startChat()" [disabled]="connecting">
                {{ 'portal.chatStart' | translate }}
              </button>
            </div>
          } @else {
            <div class="chat-messages">
              @for (msg of messages; track msg.id) {
                <div class="message" [class.outbound]="msg.direction === 0" [class.inbound]="msg.direction === 1">
                  <div class="message-bubble">
                    <p>{{ msg.content }}</p>
                    <small>{{ msg.sentAt | date:'shortTime' }}</small>
                  </div>
                </div>
              }
              @if (waitingForAgent) {
                <p class="waiting-note">{{ 'portal.chatWaiting' | translate }}</p>
              }
            </div>

            <div class="chat-input">
              <mat-form-field appearance="outline" class="full-width">
                <input matInput [(ngModel)]="newMessage" (keyup.enter)="sendMessage()"
                       [placeholder]="'chat.typeMessage' | translate" />
              </mat-form-field>
              <button mat-icon-button color="primary" (click)="sendMessage()" [disabled]="!newMessage.trim()">
                <mat-icon>send</mat-icon>
              </button>
            </div>
          }
        </div>
      </mat-card>
    }
  `,
  styles: [`
    .chat-fab { position: fixed; inset-block-end: 24px; inset-inline-end: 24px; z-index: 1000; }
    .chat-panel {
      position: fixed; inset-block-end: 24px; inset-inline-end: 24px;
      width: 320px; height: 440px; display: flex; flex-direction: column; z-index: 1000;
      box-shadow: 0 4px 16px rgba(0,0,0,0.2);
    }
    .chat-panel-header {
      display: flex; justify-content: space-between; align-items: center;
      padding: 12px 16px; background: #1976d2; color: white; border-radius: 4px 4px 0 0;
    }
    .chat-panel-body { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
    .chat-start { padding: 16px; text-align: center; }
    .chat-messages { flex: 1; overflow-y: auto; padding: 12px; display: flex; flex-direction: column; gap: 8px; }
    .message { display: flex; }
    .message.outbound { justify-content: flex-end; }
    .message.inbound { justify-content: flex-start; }
    .message-bubble { max-width: 80%; padding: 8px 10px; border-radius: 10px; }
    .inbound .message-bubble { background: #f0f0f0; }
    .outbound .message-bubble { background: #e3f2fd; }
    .message-bubble p { margin: 0; word-break: break-word; }
    .message-bubble small { color: #999; font-size: 10px; }
    .waiting-note { color: #999; font-style: italic; font-size: 12px; text-align: center; }
    .chat-input { display: flex; align-items: center; padding: 8px; border-block-start: 1px solid #e0e0e0; }
    .full-width { flex: 1; }
  `]
})
export class ChatWidgetComponent extends PortalApiService implements OnDestroy {
  private chatHubService = inject(ChatHubService);
  private subscriptions: Subscription[] = [];

  open = false;
  connecting = false;
  waitingForAgent = false;
  unread = 0;
  conversationId: string | null = null;
  messages: HubMessage[] = [];
  newMessage = '';

  toggle(): void {
    this.open = !this.open;
    if (this.open) this.unread = 0;
  }

  startChat(): void {
    this.connecting = true;
    this.post<StartChatResponse>('/v1/portal/chat/start', {}).subscribe({
      next: (conversation) => {
        this.conversationId = conversation.id;
        this.connecting = false;
        this.waitingForAgent = true;
        this.chatHubService.connect().then(() => {
          this.subscriptions.push(
            this.chatHubService.messageReceived$.subscribe(msg => {
              if (msg.conversationId === this.conversationId) {
                this.messages.push(msg);
                this.waitingForAgent = false;
                if (!this.open) this.unread++;
              }
            }),
            this.chatHubService.chatEnded$.subscribe(id => {
              if (id === this.conversationId) {
                this.conversationId = null;
                this.messages = [];
              }
            })
          );
          this.chatHubService.joinChat(this.conversationId!);
        });
      },
      error: () => this.connecting = false
    });
  }

  sendMessage(): void {
    if (!this.newMessage.trim() || !this.conversationId) return;
    this.chatHubService.sendMessage(this.conversationId, this.newMessage.trim());
    this.newMessage = '';
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(s => s.unsubscribe());
    this.chatHubService.disconnect();
  }
}
