import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private hub?: signalR.HubConnection;
  private tick?: any;

  score = signal<{ home: number; away: number }>({ home: 0, away: 0 });

  timeLeft = signal(0);           // segundos restantes
  timerRunning = signal(false);
  private endsAt?: number;        // ms epoch

  // --- NUEVO: método reutilizable para arrancar el tick ---
  private startTick() {
    this.stopTick();
    this.tick = setInterval(() => {
      if (!this.timerRunning() || !this.endsAt) return;
      const secs = Math.max(0, Math.floor((this.endsAt - Date.now()) / 1000));
      this.timeLeft.set(secs);
      if (secs === 0) this.timerRunning.set(false);
    }, 200);
  }

  // --- NUEVO: hidratar el timer desde el snapshot del GET /api/matches/:id ---
  hydrateTimerFromSnapshot(snap?: { running: boolean; remainingSeconds: number; quarterEndsAtUtc?: string | null }) {
    if (!snap) return;
    this.timeLeft.set(snap.remainingSeconds ?? 0);

    if (snap.running && snap.quarterEndsAtUtc) {
      this.timerRunning.set(true);
      this.endsAt = new Date(snap.quarterEndsAtUtc).getTime();
      this.startTick();
    } else {
      this.timerRunning.set(false);
      this.endsAt = undefined;
      this.stopTick();
    }
  }

  async connect(matchId: number) {
    if (this.hub) return;

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`/hubs/score?matchId=${matchId}`)
      .withAutomaticReconnect()
      .build();

    this.hub.on('scoreUpdated', (s: { homeScore: number; awayScore: number }) => {
      this.score.set({ home: s.homeScore, away: s.awayScore });
    });

    // Eventos del timer (usan this.startTick())
    this.hub.on('timerStarted', (t: { quarterEndsAtUtc: string; remainingSeconds: number }) => {
      this.endsAt = new Date(t.quarterEndsAtUtc).getTime();
      this.timeLeft.set(t.remainingSeconds);
      this.timerRunning.set(true);
      this.startTick();
    });

    this.hub.on('timerPaused', (t: { remainingSeconds: number }) => {
      this.timeLeft.set(t.remainingSeconds);
      this.timerRunning.set(false);
      this.stopTick();
      this.endsAt = undefined;
    });

    this.hub.on('timerResumed', (t: { quarterEndsAtUtc: string; remainingSeconds: number }) => {
      this.endsAt = new Date(t.quarterEndsAtUtc).getTime();
      this.timeLeft.set(t.remainingSeconds);
      this.timerRunning.set(true);
      this.startTick();
    });

    this.hub.on('timerReset', (t: { remainingSeconds: number }) => {
      this.timeLeft.set(t.remainingSeconds);
      this.timerRunning.set(false);
      this.stopTick();
      this.endsAt = undefined;
    });

    await this.hub.start();
  }

  private stopTick() {
    if (this.tick) {
      clearInterval(this.tick);
      this.tick = undefined;
    }
  }

  async disconnect() {
    this.stopTick();
    if (this.hub) { await this.hub.stop(); this.hub = undefined; }
  }
}
