import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { SavedQuery, QueryResult } from '../../models/models';
import { ResultsView } from '../../shared/results-view';

/**
 * מסך שאילתות שמורות (דרישה 4): רשימה + הרצה מחדש.
 */
@Component({
  selector: 'app-saved-queries',
  imports: [ResultsView, DatePipe],
  templateUrl: './saved-queries.html',
  styleUrl: './saved-queries.css'
})
export class SavedQueries implements OnInit {
  private api = inject(ApiService);

  items = signal<SavedQuery[]>([]);
  result = signal<QueryResult | null>(null);
  error = signal<string | null>(null);
  runningId = signal<number | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.listSaved().subscribe({
      next: list => this.items.set(list),
      error: () => this.error.set('שגיאה בטעינת הרשימה. יש לוודא שהשרת פועל.')
    });
  }

  run(item: SavedQuery): void {
    this.error.set(null);
    this.runningId.set(item.id);
    this.api.runSaved(item.id).subscribe({
      next: r => { this.result.set(r); this.runningId.set(null); },
      error: e => { this.error.set(e?.error?.error ?? 'שגיאה בהרצה'); this.runningId.set(null); }
    });
  }
}
