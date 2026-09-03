import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { Subscription } from 'rxjs';
import { NotificationsService, NotificationDto } from '../notifications.service';
import { NotificationHubService } from '../notification-hub.service';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-notification-bell',
  imports: [
    DatePipe, TranslateModule, MatIconModule, MatButtonModule,
    MatBadgeModule, MatMenuModule, MatListModule, MatDividerModule
  ],
  template: `
    <button mat-icon-button [matMenuTriggerFor]="notificationMenu"
            [matBadge]="unreadCount > 0 ? unreadCount : null" matBadgeColor="warn" matBadgeSize="small">
      <mat-icon>notifications</mat-icon>
    </button>

    <mat-menu #notificationMenu="matMenu" class="notification-menu">
      <div class="notification-header" (click)="$event.stopPropagation()">
        <span>{{ 'notifications.title' | translate }}</span>
        @if (unreadCount > 0) {
          <button mat-button color="primary" (click)="markAllRead()">
            {{ 'notifications.markAllRead' | translate }}
          </button>
        }
      </div>
      <mat-divider></mat-divider>
      @for (n of notifications; track n.id) {
        <button mat-menu-item [class.unread]="!n.isRead" (click)="onNotificationClick(n)">
          <div class="notification-item">
            <strong>{{ isArabic ? n.titleAr : n.title }}</strong>
            <small>{{ n.createdAt | date:'short' }}</small>
          </div>
        </button>
      }
      @if (notifications.length === 0) {
        <div class="no-notifications" (click)="$event.stopPropagation()">
          {{ 'notifications.noNotifications' | translate }}
        </div>
      }
    </mat-menu>
  `,
  styles: [`
    .notification-header { display: flex; justify-content: space-between; align-items: center; padding: 8px 16px; }
    .notification-item { display: flex; flex-direction: column; }
    .notification-item strong { font-size: 13px; }
    .notification-item small { color: #999; font-size: 11px; }
    .unread { background-color: rgba(25, 118, 210, 0.04); }
    .no-notifications { padding: 16px; text-align: center; color: #999; }
  `]
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  private notificationsService = inject(NotificationsService);
  private notificationHub = inject(NotificationHubService);
  private languageService = inject(LanguageService);
  private subscription?: Subscription;

  notifications: NotificationDto[] = [];
  unreadCount = 0;
  get isArabic(): boolean { return this.languageService.getCurrentLanguage() === 'ar'; }

  ngOnInit(): void {
    this.loadNotifications();
    this.notificationHub.connect().then(() => {
      this.subscription = this.notificationHub.notificationReceived$.subscribe(() => {
        this.loadNotifications();
      });
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
    this.notificationHub.disconnect();
  }

  loadNotifications(): void {
    this.notificationsService.getNotifications(undefined, 1, 10).subscribe(result => {
      this.notifications = result.items;
    });
    this.notificationsService.getUnreadCount().subscribe(count => {
      this.unreadCount = count;
    });
  }

  onNotificationClick(notification: NotificationDto): void {
    if (!notification.isRead) {
      this.notificationsService.markAsRead(notification.id).subscribe(() => this.loadNotifications());
    }
  }

  markAllRead(): void {
    this.notificationsService.markAllAsRead().subscribe(() => this.loadNotifications());
  }
}
