import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface GapAnalysisResponse {
  missingTechnologies: string;
}

@Injectable({
  providedIn: 'root',
})
export class CareerMentorService {
  private readonly apiUrl = 'http://localhost:5019/api/CareerMentor/evaluate-gap';

  constructor(private http: HttpClient) {}

  evaluateGap(jobDescription: string, resumeText: string): Observable<GapAnalysisResponse> {
    return this.http.post<GapAnalysisResponse>(this.apiUrl, {
      jobDescription,
      resumeText,
    });
  }
}
