import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = '/api';

  // === Matches ===
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

  advanceQuarter(id: number) {
    return this.http.post(`${this.base}/matches/${id}/quarters/advance`, {});
  }

  autoAdvanceQuarter(id: number) {
    return this.http.post(`${this.base}/matches/${id}/quarters/auto-advance`, {});
  }

  // Crear partido SOLO con equipos registrados
  newGameByTeams(body: { homeTeamId: number; awayTeamId: number; quarterDurationSeconds: number }) {
    return this.http.post<any>(`${this.base}/matches/new-by-teams`, body);
  }

  // === Teams ===
  listTeams() {
    return this.http.get<Array<{ id: number; name: string; color?: string; playersCount: number }>>(
      `${this.base}/teams`
    );
  }

  createTeam(body: { name: string; color?: string; players: { number?: number; name: string }[] }) {
    return this.http.post<any>(`${this.base}/teams`, body);
  }
}
