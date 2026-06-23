import { Routes } from '@angular/router';
import { QueryBuilder } from './pages/query-builder/query-builder';
import { NlQuery } from './pages/nl-query/nl-query';
import { SavedQueries } from './pages/saved-queries/saved-queries';

export const routes: Routes = [
  { path: '', component: QueryBuilder },
  { path: 'nl', component: NlQuery },
  { path: 'saved', component: SavedQueries },
  { path: '**', redirectTo: '' }
];
