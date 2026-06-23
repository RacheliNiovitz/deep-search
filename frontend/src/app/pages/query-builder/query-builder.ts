import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { Metadata, QueryDefinition, QueryResult, MetricType, GroupByField } from '../../models/models';
import { ResultsView } from '../../shared/results-view';

/**
 * מסך בונה השאילתות (דרישות 1-4): בחירת פרמטרים, הרצה, הצגת תוצאה, ושמירה.
 */
@Component({
  selector: 'app-query-builder',
  imports: [FormsModule, ResultsView],
  templateUrl: './query-builder.html',
  styleUrl: './query-builder.css'
})
export class QueryBuilder implements OnInit {
  private api = inject(ApiService);

  // נתונים אסינכרוניים כ-signals
  metadata = signal<Metadata | null>(null);
  result = signal<QueryResult | null>(null);
  error = signal<string | null>(null);
  loading = signal(false);
  savedMsg = signal<string | null>(null);

  // שגיאות ברמת השדה הבודד (key = שם השדה)
  fieldErrors = signal<Record<string, string>>({});

  // מצב הטופס
  metric: MetricType = 'Average';
  gender = '';
  ageMin: number | null = null;
  ageMax: number | null = null;
  cityId = '';
  districtId = '';
  sectorId = '';
  yearMode: 'single' | 'range' = 'range';   // שנה בודדת או טווח שנים
  singleYear: number | null = null;
  yearFrom: number | null = null;
  yearTo: number | null = null;
  selectedGroupBy: string[] = [];

  toggleGroupBy(value: string, checked: boolean): void {
    if (checked) {
      if (!this.selectedGroupBy.includes(value)) this.selectedGroupBy.push(value);
    } else {
      this.selectedGroupBy = this.selectedGroupBy.filter(v => v !== value);
    }
  }

  ngOnInit(): void {
    this.api.getMetadata().subscribe({
      next: m => this.metadata.set(m),
      error: () => this.error.set('שגיאה בטעינת המטא-דאטה. ודאי שהשרת רץ על פורט 5080.')
    });
  }

  /** הרכבת ה-QueryDefinition מתוך מצב הטופס. */
  private buildDefinition(): QueryDefinition {
    const single = this.yearMode === 'single';
    const yearFrom = single ? this.singleYear : this.yearFrom;
    const yearTo = single ? this.singleYear : this.yearTo;

    return {
      metric: this.metric,
      metricField: 'MonthlyIncome',
      filters: {
        gender: this.gender || null,
        ageMin: this.ageMin,
        ageMax: this.ageMax,
        cityId: this.cityId ? +this.cityId : null,
        districtId: this.districtId ? +this.districtId : null,
        sectorId: this.sectorId ? +this.sectorId : null,
        yearFrom,
        yearTo
      },
      groupBy: this.selectedGroupBy as GroupByField[]
    };
  }

  /**
   * ולידציה לכל שדה בנפרד. מעדכנת את fieldErrors ומחזירה true אם הכל תקין.
   * נקראת ביציאה משדה (blur) וגם לפני הרצה.
   */
  validateFields(): boolean {
    const errors: Record<string, string> = {};
    const currentYear = new Date().getFullYear();

    if (this.ageMin != null && (this.ageMin < 0 || this.ageMin > 120)) errors['ageMin'] = 'גיל חייב להיות בין 0 ל-120';
    if (this.ageMax != null && (this.ageMax < 0 || this.ageMax > 120)) errors['ageMax'] = 'גיל חייב להיות בין 0 ל-120';
    if (this.ageMin != null && this.ageMax != null && this.ageMin > this.ageMax) errors['ageMax'] = 'גיל מקסימלי קטן מהמינימלי';

    if (this.yearMode === 'single') {
      if (this.singleYear != null && (this.singleYear < 1900 || this.singleYear > currentYear)) errors['singleYear'] = `שנה בין 1900 ל-${currentYear}`;
    } else {
      if (this.yearFrom != null && (this.yearFrom < 1900 || this.yearFrom > currentYear)) errors['yearFrom'] = `שנה בין 1900 ל-${currentYear}`;
      if (this.yearTo != null && (this.yearTo < 1900 || this.yearTo > currentYear)) errors['yearTo'] = `שנה בין 1900 ל-${currentYear}`;
      if (this.yearFrom != null && this.yearTo != null && this.yearFrom > this.yearTo) errors['yearTo'] = 'שנת סיום מוקדמת מההתחלה';
    }

    this.fieldErrors.set(errors);
    return Object.keys(errors).length === 0;
  }

  run(): void {
    this.error.set(null);
    this.savedMsg.set(null);

    if (!this.validateFields()) return;

    this.loading.set(true);
    this.api.execute(this.buildDefinition()).subscribe({
      next: r => { this.result.set(r); this.loading.set(false); },
      error: e => { this.error.set(e?.error?.error ?? 'שגיאה בהרצת השאילתה'); this.loading.set(false); }
    });
  }

  // ----- מודל שמירת שאילתה (במקום window.prompt) -----
  showSaveModal = signal(false);
  saveName = '';

  openSaveModal(): void {
    if (!this.validateFields()) return;
    this.saveName = '';
    this.savedMsg.set(null);
    this.error.set(null);
    this.showSaveModal.set(true);
  }

  cancelSave(): void {
    this.showSaveModal.set(false);
  }

  confirmSave(): void {
    const name = this.saveName.trim();
    if (!name) return;
    this.api.saveQuery(name, this.buildDefinition()).subscribe({
      next: () => { this.savedMsg.set('✓ השאילתה נשמרה'); this.showSaveModal.set(false); },
      error: e => { this.error.set(e?.error?.error ?? 'שגיאה בשמירה'); this.showSaveModal.set(false); }
    });
  }
}
