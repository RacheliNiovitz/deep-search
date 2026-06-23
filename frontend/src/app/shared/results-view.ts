import { Component, computed, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { QueryResult } from '../models/models';
import { BarChart, ChartModel } from './bar-chart';

interface Pivot {
  catKey: string;
  serKey: string;
  categories: string[];
  series: string[];
  cell: (cat: string, ser: string) => number | null;
}

/**
 * תצוגת תוצאה אחידה: ניסוח + טבלה + גרף.
 * כשיש בדיוק שני פילוחים, מציגה טבלת צומת (pivot) וגרף מקובץ בצבעים.
 */
@Component({
  selector: 'app-results-view',
  imports: [DecimalPipe, BarChart],
  template: `
    @if (result(); as r) {
      <div class="phrase">📊 {{ r.readablePhrase }}</div>

      <h4>טבלה</h4>
      @if (pivot(); as p) {
        <!-- טבלת צומת: שורה לכל קטגוריה ראשית, עמודה לכל ערך משני -->
        <table>
          <thead>
            <tr>
              <th>{{ headerLabel(p.catKey) }}</th>
              @for (s of p.series; track s) { <th>{{ s }}</th> }
            </tr>
          </thead>
          <tbody>
            @for (cat of p.categories; track cat) {
              <tr>
                <td>{{ cat }}</td>
                @for (s of p.series; track s) {
                  @let v = p.cell(cat, s);
                  <td class="value">
                    @if (v !== null) { {{ v | number:'1.0-2' }} } @else { — }
                  </td>
                }
              </tr>
            }
          </tbody>
        </table>
      } @else {
        <!-- טבלה שטוחה: 0 או פילוח אחד -->
        <table>
          <thead>
            <tr>
              @for (key of r.groupKeys; track key) { <th>{{ headerLabel(key) }}</th> }
              <th>ערך</th>
            </tr>
          </thead>
          <tbody>
            @for (row of r.rows; track $index) {
              <tr>
                @for (key of r.groupKeys; track key) { <td>{{ row.groups[key] }}</td> }
                <td class="value">{{ row.value | number:'1.0-2' }}</td>
              </tr>
            } @empty {
              <tr><td [attr.colspan]="r.groupKeys.length + 1">אין תוצאות</td></tr>
            }
          </tbody>
        </table>
      }

      <h4>גרף</h4>
      <app-bar-chart [model]="chartModel()" />
    }
  `,
  styles: [`
    .phrase { background: #eff6ff; border: 1px solid #bfdbfe; color: #1e3a8a;
              padding: 14px 16px; border-radius: 10px; font-size: 16px; margin-bottom: 18px; line-height: 1.5; }
    h4 { margin: 18px 0 8px; color: #334155; }
    table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 10px; overflow: hidden;
            box-shadow: 0 1px 3px rgba(0,0,0,.06); margin-bottom: 8px; }
    th, td { padding: 10px 14px; text-align: right; border-bottom: 1px solid #f1f5f9; }
    th { background: #f8fafc; color: #475569; font-weight: 600; }
    td.value { font-weight: 700; color: #1e3a8a; }
    tr:last-child td { border-bottom: none; }
  `]
})
export class ResultsView {
  result = input<QueryResult | null>(null);

  /** מחושב כשיש בדיוק שני פילוחים. */
  pivot = computed<Pivot | null>(() => {
    const r = this.result();
    if (!r || r.groupKeys.length !== 2) return null;

    const [k0, k1] = r.groupKeys;
    const distinct = (k: string) => new Set(r.rows.map(row => row.groups[k])).size;
    // הקטגוריה (שורות) = הממד עם יותר ערכים שונים (למשל ערים).
    // הסדרה (עמודות/צבעים) = הממד עם פחות ערכים (למשל מגדר = 2).
    const [catKey, serKey] = distinct(k0) >= distinct(k1) ? [k0, k1] : [k1, k0];

    const categories: string[] = [];
    const series: string[] = [];
    const map = new Map<string, Map<string, number>>();

    for (const row of r.rows) {
      const cat = row.groups[catKey];
      const ser = row.groups[serKey];
      if (!categories.includes(cat)) categories.push(cat);
      if (!series.includes(ser)) series.push(ser);
      if (!map.has(cat)) map.set(cat, new Map());
      map.get(cat)!.set(ser, row.value);
    }

    return {
      catKey, serKey, categories, series,
      cell: (c, s) => map.get(c)?.get(s) ?? null
    };
  });

  /** מודל הגרף - מקובץ אם pivot, אחרת פשוט. */
  chartModel = computed<ChartModel>(() => {
    const r = this.result();
    if (!r) return { categories: [], series: [''], values: [] };

    const p = this.pivot();
    if (p) {
      return {
        categories: p.categories,
        series: p.series,
        values: p.categories.map(c => p.series.map(s => p.cell(c, s) ?? 0))
      };
    }

    const labels = r.rows.map(row => Object.values(row.groups).join(' · ') || 'הכל');
    return { categories: labels, series: [''], values: r.rows.map(row => [row.value]) };
  });

  headerLabel(key: string): string {
    const map: { [k: string]: string } = {
      year: 'שנה', gender: 'מגדר', city: 'עיר', district: 'מחוז', sector: 'מגזר', agegroup: 'קבוצת גיל'
    };
    return map[key] ?? key;
  }
}
