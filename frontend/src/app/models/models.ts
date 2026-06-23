// מודלים תואמים ל-DTOs של צד השרת (camelCase כפי שמגיע ב-JSON).

export type MetricType = 'Average' | 'Count' | 'Sum' | 'EmploymentRate';
export type GroupByField = 'Year' | 'Gender' | 'City' | 'Sector' | 'AgeGroup';

export interface QueryFilters {
  gender?: string | null;
  ageMin?: number | null;
  ageMax?: number | null;
  cityId?: number | null;
  districtId?: number | null;
  sectorId?: number | null;
  yearFrom?: number | null;
  yearTo?: number | null;
}

export interface QueryDefinition {
  metric: MetricType;
  metricField: string;
  filters: QueryFilters;
  groupBy: GroupByField[];
}

export interface QueryResultRow {
  groups: { [key: string]: string };
  value: number;
}

export interface QueryResult {
  readablePhrase: string;
  groupKeys: string[];
  rows: QueryResultRow[];
}

export interface Option {
  value: string;
  label: string;
}

export interface Metadata {
  cities: Option[];
  districts: Option[];
  sectors: Option[];
  genders: Option[];
  metrics: Option[];
  groupByFields: Option[];
}

export interface NlParseResult {
  definition: QueryDefinition;
  interpretation: string;
  warnings: string[];
}

export interface SavedQuery {
  id: number;
  name: string;
  createdAt: string;
}
