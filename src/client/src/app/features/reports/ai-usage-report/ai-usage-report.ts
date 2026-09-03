import { Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-ai-usage-report',
  imports: [TranslateModule],
  template: `<h1>{{ 'reports.aiUsage.title' | translate }}</h1>`
})
export class AiUsageReportComponent {}
