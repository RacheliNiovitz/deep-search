import { Component, computed, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';

/** מודל אחיד לגרף: קטגוריות (ציר X), סדרות (מקרא/צבעים), וערכים [קטגוריה][סדרה]. */
export interface ChartModel {
  categories: string[];
  series: string[];       // אורך 1 = גרף פשוט; אורך >1 = מקובץ בצבעים
  values: number[][];
}

/**
 * גרף עמודות. תומך גם בגרף פשוט (סדרה אחת) וגם במקובץ (כמה סדרות בצבעים),
 * עבור תוצאות עם שני פילוחים. ללא ספריות חיצוניות.
 */
@Component({
  selector: 'app-bar-chart',
  imports: [DecimalPipe],
  template: `
    <div class="wrap">
      @if (model().series.length > 1) {
        <div class="legend">
          @for (s of model().series; track $index; let i = $index) {
            <span class="leg"><i [style.background]="color(i)"></i>{{ s }}</span>
          }
        </div>
      }

      <div class="chart">
        @for (cat of model().categories; track $index; let ci = $index) {
          <div class="group">
            <div class="bars">
              @for (s of model().series; track $index; let si = $index) {
                <div class="bar" [style.height.%]="heightPct(model().values[ci][si])"
                     [style.background]="color(si)" [title]="model().values[ci][si]">
                  <span class="val">{{ model().values[ci][si] | number:'1.0-0' }}</span>
                </div>
              }
            </div>
            <div class="lbl">{{ cat }}</div>
          </div>
        } @empty {
          <p class="empty">אין נתונים להצגה</p>
        }
      </div>
    </div>
  `,
  styles: [`
    .wrap { border: 1px solid #e2e8f0; border-radius: 10px; background: #fff; padding: 12px; }
    .legend { display: flex; gap: 16px; flex-wrap: wrap; margin-bottom: 8px; padding-bottom: 8px; border-bottom: 1px solid #f1f5f9; }
    .leg { display: inline-flex; align-items: center; gap: 6px; font-size: 13px; color: #475569; }
    .leg i { width: 14px; height: 14px; border-radius: 3px; display: inline-block; }
    .chart { display: flex; align-items: stretch; gap: 18px; height: 260px; padding: 24px 6px 4px; overflow-x: auto; }
    .group { display: flex; flex-direction: column; min-width: 60px; }
    .bars { flex: 1; display: flex; align-items: flex-end; justify-content: center; gap: 4px; }
    .bar { width: 38px; border-radius: 6px 6px 0 0; min-height: 4px; transition: height .3s; position: relative; }
    .val { position: absolute; top: -19px; left: 50%; transform: translateX(-50%); font-size: 11px; color: #1e3a8a; white-space: nowrap; font-weight: 700; }
    .lbl { margin-top: 8px; font-size: 12px; color: #475569; text-align: center; }
    .empty { color: #94a3b8; margin: auto; }
  `]
})
export class BarChart {
  model = input.required<ChartModel>();

  private readonly palette = ['#2b5fd0', '#f59e0b', '#16a34a', '#db2777', '#0891b2', '#7c3aed'];
  private max = computed(() => Math.max(1, ...this.model().values.flat()));

  heightPct(value: number): number {
    return (value / this.max()) * 100;
  }

  color(index: number): string {
    return this.palette[index % this.palette.length];
  }
}
