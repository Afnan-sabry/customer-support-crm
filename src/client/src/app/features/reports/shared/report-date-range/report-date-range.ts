import { Component, output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatNativeDateModule } from '@angular/material/core';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'app-report-date-range',
  imports: [
    FormsModule, TranslateModule,
    MatFormFieldModule, MatDatepickerModule, MatInputModule,
    MatButtonModule, MatNativeDateModule, MatChipsModule
  ],
  template: `
    <div class="date-range-container">
      <div class="presets">
        @for (preset of presets; track preset.label) {
          <button mat-stroked-button (click)="applyPreset(preset)">
            {{ preset.label | translate }}
          </button>
        }
      </div>
      <mat-form-field>
        <mat-label>{{ 'reports.dateRange' | translate }}</mat-label>
        <mat-date-range-input [rangePicker]="picker">
          <input matStartDate [(ngModel)]="startDate" (dateChange)="emitChange()">
          <input matEndDate [(ngModel)]="endDate" (dateChange)="emitChange()">
        </mat-date-range-input>
        <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
        <mat-date-range-picker #picker></mat-date-range-picker>
      </mat-form-field>
    </div>
  `,
  styles: [`
    .date-range-container { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; margin-block-end: 16px; }
    .presets { display: flex; gap: 8px; flex-wrap: wrap; }
    .presets button { font-size: 12px; }
  `]
})
export class ReportDateRangeComponent {
  dateRangeChange = output<{ startDate: string; endDate: string }>();

  startDate: Date | null = null;
  endDate: Date | null = null;

  presets = [
    { label: 'reports.presets.today', days: 0 },
    { label: 'reports.presets.last7', days: 7 },
    { label: 'reports.presets.last30', days: 30 },
    { label: 'reports.presets.thisMonth', days: -1 },
    { label: 'reports.presets.thisQuarter', days: -2 },
  ];

  constructor() {
    this.applyPreset(this.presets[2]);
  }

  applyPreset(preset: { label: string; days: number }): void {
    const now = new Date();
    if (preset.days === -1) {
      this.startDate = new Date(now.getFullYear(), now.getMonth(), 1);
      this.endDate = now;
    } else if (preset.days === -2) {
      const qMonth = Math.floor(now.getMonth() / 3) * 3;
      this.startDate = new Date(now.getFullYear(), qMonth, 1);
      this.endDate = now;
    } else if (preset.days === 0) {
      this.startDate = new Date(now.getFullYear(), now.getMonth(), now.getDate());
      this.endDate = now;
    } else {
      this.startDate = new Date(now.getTime() - preset.days * 86400000);
      this.endDate = now;
    }
    this.emitChange();
  }

  emitChange(): void {
    if (this.startDate && this.endDate) {
      this.dateRangeChange.emit({
        startDate: this.startDate.toISOString().split('T')[0],
        endDate: this.endDate.toISOString().split('T')[0]
      });
    }
  }
}
