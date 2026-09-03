import { Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-agent-performance-report',
  imports: [TranslateModule],
  template: `<h1>{{ 'reports.agentPerformance.title' | translate }}</h1>`
})
export class AgentPerformanceReportComponent {}
