import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateCustomPackRequestPayload,
  CustomPackRequestSubmissionResponse,
  PackCatalogSolution,
} from '../models/pack.model';

@Injectable({
  providedIn: 'root',
})
export class PacksApiService {
  private readonly apiOrigin = 'http://localhost:5057';
  private readonly packsUrl = `${this.apiOrigin}/api/packs`;
  private readonly customPackRequestsUrl = `${this.apiOrigin}/api/custom-pack-requests`;

  constructor(private readonly http: HttpClient) {}

  getCatalog(): Observable<PackCatalogSolution[]> {
    return this.http.get<PackCatalogSolution[]>(`${this.packsUrl}/catalog`);
  }

  submitCustomPackRequest(
    payload: CreateCustomPackRequestPayload
  ): Observable<CustomPackRequestSubmissionResponse> {
    return this.http.post<CustomPackRequestSubmissionResponse>(
      this.customPackRequestsUrl,
      payload
    );
  }
}
