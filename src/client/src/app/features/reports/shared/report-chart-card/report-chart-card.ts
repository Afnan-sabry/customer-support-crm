import { Component, input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-report-chart-card',
  imports: [TranslateModule, MatCardModule, MatProgressSpinnerModule],
  template: `
    <mat-card class="chart-card">
      <mat-card-header>
        <mat-card-title>{{ title() | translate }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        @if (loading()) {
          <div class="loading-container">
            <mat-spinner diameter="40"></mat-spinner>
          </div>
        } @else {
          <ng-content></ng-content>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .chart-card { margin-block-end: 16px; }
    .loading-container { display: flex; justify-content: center; padding: 40px 0; }
  `]
})
export class ReportChartCardComponent {
  title = input.required<string>();
  loading = input(false);
}
