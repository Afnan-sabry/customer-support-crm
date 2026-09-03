import { Component, input, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ReportsService } from '../../reports.service';

@Component({
  selector: 'app-report-export-bar',
  imports: [TranslateModule, MatButtonModule, MatIconModule],
  template: `
    <div class="export-bar">
      <button mat-stroked-button (click)="doExport('csv')">
        <mat-icon>description</mat-icon> CSV
      </button>
      <button mat-stroked-button (click)="doExport('excel')">
        <mat-icon>table_chart</mat-icon> Excel
      </button>
      <button mat-stroked-button (click)="doExport('pdf')">
        <mat-icon>picture_as_pdf</mat-icon> PDF
      </button>
    </div>
  `,
  styles: [`
    .export-bar { display: flex; gap: 8px; margin-block: 12px; }
  `]
})
export class ReportExportBarComponent {
  reportType = input.required<string>();
  params = input<Record<string, any>>({});

  private reportsService = inject(ReportsService);

  doExport(format: string): void {
    this.reportsService.exportReport(this.reportType(), format, this.params());
  }
}
