import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TestRubrique } from '../models/test-rubrique.model';

@Injectable({
  providedIn: 'root',
})
export class TestRubriqueApiService {
  /**
   * URL de base de l'API backend.
   *
   * D'après ton `launchSettings.json`, ton backend tourne en HTTPS sur :
   *   https://localhost:7110
   * et en HTTP sur :
   *   http://localhost:5057
   *
   * On utilise ici l'URL HTTPS. Si tu lances le backend en HTTP,
   * remplace simplement `https://localhost:7110` par `http://localhost:5057`.
   */
  private readonly baseUrl = 'https://localhost:7110/api/test-rubriques';

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<TestRubrique[]> {
    return this.http.get<TestRubrique[]>(this.baseUrl);
  }

  getById(id: number): Observable<TestRubrique> {
    return this.http.get<TestRubrique>(`${this.baseUrl}/${id}`);
  }

  create(payload: Omit<TestRubrique, 'id'>): Observable<TestRubrique> {
    return this.http.post<TestRubrique>(this.baseUrl, payload);
  }

  update(id: number, payload: Partial<TestRubrique>): Observable<TestRubrique> {
    return this.http.put<TestRubrique>(`${this.baseUrl}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}

