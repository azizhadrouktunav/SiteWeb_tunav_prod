import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Solution, SolutionType } from '../models/solution.model';
import { environment } from '../../../environments/environment';

export interface SolutionFilters {
  type?: SolutionType;
  sector?: string;
  search?: string;
}

@Injectable({
  providedIn: 'root',
})
export class SolutionsApiService {
  private readonly apiOrigin = environment.apiOrigin;
  private readonly baseUrl = `${this.apiOrigin}/api/solutions`;

  constructor(private readonly http: HttpClient) {}

  getActiveSolutions(filters: SolutionFilters = {}): Observable<Solution[]> {
    let params = new HttpParams().set('isActiveOnly', 'true');

    if (filters.type) {
      params = params.set('type', filters.type);
    }

    if (filters.sector) {
      params = params.set('sector', filters.sector);
    }

    if (filters.search) {
      params = params.set('search', filters.search);
    }

    return this.http.get<Solution[]>(this.baseUrl, { params });
  }

  getActiveByType(type: SolutionType): Observable<Solution[]> {
    return this.getActiveSolutions({ type });
  }

  getActiveBySector(sector: string): Observable<Solution[]> {
    return this.getActiveSolutions({ sector });
  }

  searchActiveSolutions(search: string): Observable<Solution[]> {
    return this.getActiveSolutions({ search });
  }

  getSectors(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/sectors`);
  }

  getById(id: number): Observable<Solution> {
    return this.http.get<Solution>(`${this.baseUrl}/${id}`);
  }

  getByType(type: SolutionType): Observable<Solution[]> {
    return this.http.get<Solution[]>(`${this.baseUrl}/type/${type}`);
  }

  resolveMediaUrl(url: string | null): string | null {
    if (!url) {
      return null;
    }

    if (/^https?:\/\//i.test(url)) {
      return url;
    }

    if (url.startsWith('/uploads/')) {
      return `${this.apiOrigin}${url}`;
    }

    return url;
  }
}
