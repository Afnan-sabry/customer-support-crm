import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatBadgeModule } from '@angular/material/badge';
import { MatChipsModule } from '@angular/material/chips';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ChatService, ChatSessionDto } from '../chat.service';
import { ChatHubService, HubMessage } from '../chat-hub.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-chat-console',
  imports: [
    TranslateModule, FormsModule, DatePipe,
    MatListModule, MatIconModule, MatButtonModule, MatInputModule,
    MatFormFieldModule, MatBadgeModule, MatChipsModule
  ],
  template: `
    <div class="chat-layout">
      <div class="chat-sidebar">
        <h3>{{ 'chat.activeSessions' | translate }}</h3>
        <mat-nav-list>
          @for (session of sessions; track session.id) {
            <a mat-list-item [class.active]="selectedId === session.id"
               (click)="selectSession(session)">
              <mat-icon matListItemIcon>person</mat-icon>
              <span matListItemTitle>{{ session.customerName }}</span>
              <span matListItemLine>{{ session.subject || ('chat.noSubject' | translate) }}</span>
              @if (!session.assignedAgentId) {
                <mat-chip color="warn" selected>{{ 'chat.unassigned' | translate }}</mat-chip>
              }
            </a>
          }
          @if (sessions.length === 0) {
            <p class="no-sessions">{{ 'chat.noSessions' | translate }}</p>
          }
        </mat-nav-list>
      </div>

      <div class="chat-main">
        @if (selectedId) {
          <div class="chat-header">
            <h3>{{ selectedSession?.customerName }}</h3>
            <div class="chat-actions">
              @if (!selectedSession?.assignedAgentId) {
                <button mat-raised-button color="primary" (click)="acceptChat()">
                  {{ 'chat.accept' | translate }}
                </button>
              }
              <button mat-raised-button color="warn" (click)="endSelectedChat()">
                {{ 'chat.endChat' | translate }}
              </button>
            </div>
          </div>

          <div class="chat-messages" #messageContainer>
            @for (msg of messages; track msg.id) {
              <div class="message" [class.outbound]="msg.direction === 1" [class.inbound]="msg.direction === 0">
                <div class="message-bubble">
                  <p>{{ msg.content }}</p>
                  <small>{{ msg.sentAt | date:'shortTime' }}</small>
                </div>
              </div>
            }
            @if (isTyping) {
              <div class="typing-indicator">{{ 'chat.typing' | translate }}</div>
            }
          </div>

          <div class="chat-input">
            <mat-form-field appearance="outline" class="full-width">
              <input matInput [(ngModel)]="newMessage"
                     (keyup.enter)="sendMessage()"
                     [placeholder]="'chat.typeMessage' | translate" />
            </mat-form-field>
            <button mat-icon-button color="primary" (click)="sendMessage()" [disabled]="!newMessage.trim()">
              <mat-icon>send</mat-icon>
            </button>
          </div>
        } @else {
          <div class="no-chat-selected">
            <mat-icon>chat</mat-icon>
            <p>{{ 'chat.selectSession' | translate }}</p>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .chat-layout { display: flex; height: calc(100vh - 128px); gap: 0; }
    .chat-sidebar { width: 300px; border-inline-end: 1px solid #e0e0e0; overflow-y: auto; }
    .chat-sidebar h3 { padding: 16px; margin: 0; }
    .chat-main { flex: 1; display: flex; flex-direction: column; }
    .chat-header { display: flex; justify-content: space-between; align-items: center; padding: 16px; border-block-end: 1px solid #e0e0e0; }
    .chat-header h3 { margin: 0; }
    .chat-actions { display: flex; gap: 8px; }
    .chat-messages { flex: 1; overflow-y: auto; padding: 16px; display: flex; flex-direction: column; gap: 8px; }
    .message { display: flex; }
    .message.outbound { justify-content: flex-end; }
    .message.inbound { justify-content: flex-start; }
    .message-bubble { max-width: 70%; padding: 8px 12px; border-radius: 12px; }
    .inbound .message-bubble { background: #f0f0f0; }
    .outbound .message-bubble { background: #e3f2fd; }
    .message-bubble p { margin: 0; }
    .message-bubble small { color: #999; font-size: 11px; }
    .typing-indicator { color: #999; font-style: italic; padding: 4px; }
    .chat-input { display: flex; align-items: center; padding: 8px 16px; border-block-start: 1px solid #e0e0e0; }
    .full-width { flex: 1; }
    .no-chat-selected { display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100%; color: #999; }
    .no-chat-selected mat-icon { font-size: 64px; width: 64px; height: 64px; }
    .no-sessions { padding: 16px; color: #999; text-align: center; }
    .active { background-color: rgba(0, 0, 0, 0.04); }
  `]
})
export class ChatConsoleComponent implements OnInit, OnDestroy {
  private chatService = inject(ChatService);
  private chatHubService = inject(ChatHubService);
  private authService = inject(AuthService);
  private subscriptions: Subscription[] = [];

  sessions: ChatSessionDto[] = [];
  selectedId: string | null = null;
  selectedSession: ChatSessionDto | null = null;
  messages: HubMessage[] = [];
  newMessage = '';
  isTyping = false;

  ngOnInit(): void {
    this.loadSessions();
    this.chatHubService.connect().then(() => {
      this.subscriptions.push(
        this.chatHubService.messageReceived$.subscribe(msg => {
          if (msg.conversationId === this.selectedId) {
            this.messages.push(msg);
          }
          this.loadSessions();
        }),
        this.chatHubService.typingIndicator$.subscribe(data => {
          if (data.conversationId === this.selectedId) {
            this.isTyping = true;
            setTimeout(() => this.isTyping = false, 3000);
          }
        }),
        this.chatHubService.chatEnded$.subscribe(id => {
          if (id === this.selectedId) {
            this.selectedId = null;
            this.selectedSession = null;
            this.messages = [];
          }
          this.loadSessions();
        })
      );
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(s => s.unsubscribe());
    this.chatHubService.disconnect();
  }

  loadSessions(): void {
    this.chatService.getActiveSessions().subscribe(sessions => this.sessions = sessions as any);
  }

  selectSession(session: ChatSessionDto): void {
    this.selectedId = session.id;
    this.selectedSession = session;
    this.messages = [];
    this.chatHubService.joinChat(session.id);
  }

  sendMessage(): void {
    if (!this.newMessage.trim() || !this.selectedId) return;
    this.chatHubService.sendMessage(this.selectedId, this.newMessage.trim());
    this.newMessage = '';
  }

  acceptChat(): void {
    if (!this.selectedId) return;
    const user = this.authService.getCurrentUser();
    if (!user) return;
    this.chatService.assignToMe(this.selectedId, user.id).subscribe(() => this.loadSessions());
  }

  endSelectedChat(): void {
    if (!this.selectedId) return;
    this.chatHubService.endChat(this.selectedId);
  }
}
