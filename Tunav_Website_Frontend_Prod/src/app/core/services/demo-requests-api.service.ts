import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateDemoRequestPayload,
  DemoRequest,
  DemoRequestSubmissionResponse,
} from '../models/demo-request.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class DemoRequestsApiService {
  private readonly apiOrigin = environment.apiOrigin;
  private readonly baseUrl = `${this.apiOrigin}/api/demo-requests`;

  constructor(private readonly http: HttpClient) {}

  submitRequest(
    payload: CreateDemoRequestPayload
  ): Observable<DemoRequestSubmissionResponse> {
    return this.http.post<DemoRequestSubmissionResponse>(this.baseUrl, payload);
  }

  getRequests(): Observable<DemoRequest[]> {
    return this.http.get<DemoRequest[]>(this.baseUrl);
  }
}
