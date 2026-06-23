import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { NlParseResult, QueryResult, QueryDefinition } from '../../models/models';
import { ResultsView } from '../../shared/results-view';

type ParseStatus = 'ok' | 'partial' | 'bad';

/**
 * מסך שאלה חופשית (דרישה 5): המשתמש כותב שאלה, המערכת מפרשת אותה
 * אוטומטית ביציאה מהשדה, מציגה חיווי "כך הבנתי", ומאפשרת להריץ.
 */
@Component({
  selector: 'app-nl-query',
  imports: [FormsModule, ResultsView],
  templateUrl: './nl-query.html',
  styleUrl: './nl-query.css'
})
export class NlQuery {
  private api = inject(ApiService);

  question = '';
  private lastParsed = '';
  parsed = signal<NlParseResult | null>(null);
  result = signal<QueryResult | null>(null);
  error = signal<string | null>(null);
  loading = signal(false);

  /** סטטוס הפירוש: הובן / הובן חלקית / לא הובן. */
  status = computed<ParseStatus | null>(() => {
    const p = this.parsed();
    if (!p) return null;
    if (p.warnings.length === 0) return 'ok';
    const f = p.definition.filters;
    const nothingMatched =
      !f.gender && !f.cityId && !f.sectorId && f.yearFrom == null && p.definition.groupBy.length === 0;
    return nothingMatched ? 'bad' : 'partial';
  });

  readonly examples = [
    'מהו השכר הממוצע של נשים חרדיות בירושלים בין השנים 2021-2024 לפי שנה',
    'הצג את שיעור התעסוקה לפי עיר ומגדר בשנים 2020-2024',
    'כמה אנשים בתל אביב לפי מגדר'
  ];

  /** מופעל ביציאה מהשדה: מפרש אוטומטית להצגת החיווי (לא מריץ). */
  onBlur(): void {
    const q = this.question.trim();
    if (!q || q === this.lastParsed) return;
    this.parse(q);
  }

  useExample(ex: string): void {
    this.question = ex;
    this.parse(ex);
  }

  /** מפרש את השאלה ומציג את החיווי (בלי להריץ). */
  private parse(q: string): void {
    this.lastParsed = q;
    this.error.set(null);
    this.loading.set(true);
    this.api.parse(q).subscribe({
      next: p => { this.parsed.set(p); this.loading.set(false); },
      error: e => { this.error.set(e?.error?.error ?? 'שגיאה בפירוש השאלה'); this.loading.set(false); }
    });
  }

  /** כפתור ההרצה (גלוי מההתחלה): מוודא פירוש עדכני ואז מריץ. */
  run(): void {
    const q = this.question.trim();
    if (!q) { this.error.set('יש להזין שאלה.'); return; }

    // אם כבר פירשנו את הטקסט הנוכחי - מריצים ישירות
    const current = this.parsed();
    if (current && this.lastParsed === q) {
      this.execute(current.definition);
      return;
    }

    // אחרת - מפרשים ואז מריצים
    this.lastParsed = q;
    this.error.set(null);
    this.loading.set(true);
    this.api.parse(q).subscribe({
      next: p => { this.parsed.set(p); this.execute(p.definition); },
      error: e => { this.error.set(e?.error?.error ?? 'שגיאה בפירוש השאלה'); this.loading.set(false); }
    });
  }

  private execute(definition: QueryDefinition): void {
    this.loading.set(true);
    this.api.execute(definition).subscribe({
      next: r => { this.result.set(r); this.loading.set(false); },
      error: e => { this.error.set(e?.error?.error ?? 'שגיאה בהרצה'); this.loading.set(false); }
    });
  }
}
