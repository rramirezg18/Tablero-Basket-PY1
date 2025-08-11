import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = '/api';

  getMatch(id: number) {
    return this.http.get<any>(`${this.base}/matches/${id}`);
  }

  createScore(id: number, body: { teamId: number; points: 1|2|3; playerId?: number }) {
    return this.http.post(`${this.base}/matches/${id}/score`, body);
  }

  adjustScore(id: number, body: { teamId: number; delta: number }) {
    return this.http.post(`${this.base}/matches/${id}/score/adjust`, body);
  }

  startTimer(id: number, body?: { quarterDurationSeconds?: number }) {
    return this.http.post(`${this.base}/matches/${id}/timer/start`, body ?? {});
  }

  pauseTimer(id: number)  { return this.http.post(`${this.base}/matches/${id}/timer/pause`, {}); }
  resumeTimer(id: number) { return this.http.post(`${this.base}/matches/${id}/timer/resume`, {}); }
  resetTimer(id: number)  { return this.http.post(`${this.base}/matches/${id}/timer/reset`, {}); }

  advanceQuarter(id: number) { return this.http.post(`${this.base}/matches/${id}/quarters/advance`, {}); }

  newGame(body: { homeName: string; awayName: string; quarterDurationSeconds?: number }) {
  return this.http.post<any>(`${this.base}/matches/new`, body);
}
}

