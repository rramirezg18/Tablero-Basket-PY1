import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = '/api';

  obtenerPartido(id: number) {
    return this.http.get<any>(`${this.base}/partidos/${id}`);
  }

  // ====== PUNTAJES ======
  crearPuntaje(id: number, body: { equipoId: number; puntos: 1|2|3; jugadorId?: number }) {
    return this.http.post(`${this.base}/partidos/${id}/score`, body);
  }
  ajustarPuntaje(id: number, body: { equipoId: number; delta: number }) {
    return this.http.post(`${this.base}/partidos/${id}/score/adjust`, body);
  }

  // ====== FALTAS ======
  agregarFalta(id: number, body: { equipoId: number; jugadorId?: number; tipo?: string }) {
    return this.http.post(`${this.base}/partidos/${id}/fouls`, body);
  }
  ajustarFalta(id: number, body: { equipoId: number; delta: number }) {
    return this.http.post(`${this.base}/partidos/${id}/fouls/adjust`, body);
  }

  // ====== TIMER ======
  startTimer(id: number, body?: { duracionPeriodoSegundos?: number }) {
    return this.http.post(`${this.base}/partidos/${id}/timer/start`, body ?? {});
  }
  pauseTimer(id: number)  { return this.http.post(`${this.base}/partidos/${id}/timer/pause`, {}); }
  resumeTimer(id: number) { return this.http.post(`${this.base}/partidos/${id}/timer/resume`, {}); }
  resetTimer(id: number)  { return this.http.post(`${this.base}/partidos/${id}/timer/reset`, {}); }

  // Quarter
  advanceQuarter(id: number) { return this.http.post(`${this.base}/partidos/${id}/quarters/advance`, {}); }
  autoAdvanceQuarter(id: number) { return this.http.post(`${this.base}/partidos/${id}/quarters/auto-advance`, {}); }

  // New game
  nuevoPartido(body: { nombreLocal: string; nombreVisitante: string; duracionPeriodoSegundos?: number }) {
    return this.http.post<any>(`${this.base}/partidos/new`, body);
  }
  nuevoPartidoPorEquipos(body: { equipoLocalId: number; equipoVisitanteId: number; duracionPeriodoSegundos?: number }) {
    return this.http.post<any>(`${this.base}/partidos/new-by-teams`, body);
  }

  // Equipos
  listarEquipos() {
    return this.http.get<Array<{ id: number; nombre: string; color?: string; cantidadJugadores: number }>>(
      `${this.base}/equipos`
    );
  }
  crearEquipo(body: { nombre: string; color?: string; jugadores: { numero?: number; nombre: string }[] }) {
    return this.http.post('/api/equipos', body);
  }

  obtenerPosiciones() {
    return this.http.get<Array<{ id: number; nombre: string; color?: string; victorias: number }>>(
      `${this.base}/posiciones`
    );
  }



}
