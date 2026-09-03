import { Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-channel-analytics-report',
  imports: [TranslateModule],
  template: `<h1>{{ 'reports.channelAnalytics.title' | translate }}</h1>`
})
export class ChannelAnalyticsReportComponent {}
