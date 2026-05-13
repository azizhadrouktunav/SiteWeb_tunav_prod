import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreatePartnerRequestPayload,
  PartnerRequest,
  PartnerRequestSubmissionResponse,
} from '../models/partner-request.model';

@Injectable({
  providedIn: 'root',
})
export class PartnerRequestsApiService {
  private readonly apiOrigin = 'http://localhost:5057';
  private readonly baseUrl = `${this.apiOrigin}/api/partner-requests`;

  constructor(private readonly http: HttpClient) {}

  submitRequest(
    payload: CreatePartnerRequestPayload
  ): Observable<PartnerRequestSubmissionResponse> {
    return this.http.post<PartnerRequestSubmissionResponse>(this.baseUrl, payload);
  }

  getRequests(): Observable<PartnerRequest[]> {
    return this.http.get<PartnerRequest[]>(this.baseUrl);
  }
}
