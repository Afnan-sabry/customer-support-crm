import { Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-sla-performance-report',
  imports: [TranslateModule],
  template: `<h1>{{ 'reports.slaPerformance.title' | translate }}</h1>`
})
export class SlaPerformanceReportComponent {}
