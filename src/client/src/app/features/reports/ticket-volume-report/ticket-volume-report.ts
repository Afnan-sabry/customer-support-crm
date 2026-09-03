import { Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-ticket-volume-report',
  imports: [TranslateModule],
  template: `<h1>{{ 'reports.ticketVolume.title' | translate }}</h1>`
})
export class TicketVolumeReportComponent {}
