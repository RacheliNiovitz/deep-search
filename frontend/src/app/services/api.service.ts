import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Metadata, QueryDefinition, QueryResult, NlParseResult, SavedQuery
} from '../models/models';

/**
 * שכבת הגישה ל-API. כל הקומפוננטות עוברות דרכה - הן לא יודעות כתובות URL.
 * זו ההקבלה ל-Repository של צד השרת: ריכוז הגישה לנתונים במקום אחד.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);

  // בפיתוח: Angular רץ על 4200 וה-API על 5080. בפרודקשן הכל באותו origin -> "/api".
  private readonly base =
    window.location.hostname === 'localhost' && window.location.port === '4200'
      ? 'http://localhost:5080/api'
      : '/api';

  getMetadata(): Observable<Metadata> {
    return this.http.get<Metadata>(`${this.base}/metadata`);
  }

  execute(definition: QueryDefinition): Observable<QueryResult> {
    return this.http.post<QueryResult>(`${this.base}/queries/execute`, definition);
  }

  parse(question: string): Observable<NlParseResult> {
    return this.http.post<NlParseResult>(`${this.base}/nlp/parse`, { question });
  }

  saveQuery(name: string, definition: QueryDefinition): Observable<SavedQuery> {
    return this.http.post<SavedQuery>(`${this.base}/saved-queries`, { name, definition });
  }

  listSaved(): Observable<SavedQuery[]> {
    return this.http.get<SavedQuery[]>(`${this.base}/saved-queries`);
  }

  runSaved(id: number): Observable<QueryResult> {
    return this.http.post<QueryResult>(`${this.base}/saved-queries/${id}/run`, {});
  }
}
