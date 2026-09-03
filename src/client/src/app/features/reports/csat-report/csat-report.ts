import { Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-csat-report',
  imports: [TranslateModule],
  template: `<h1>{{ 'reports.csat.title' | translate }}</h1>`
})
export class CsatReportComponent {}
