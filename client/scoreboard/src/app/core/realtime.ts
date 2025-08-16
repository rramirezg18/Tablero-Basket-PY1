import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private hub?: signalR.HubConnection;
  private tick?: any;

  // Evitar conectar en SSR
  private readonly isBrowser =
    typeof window !== 'undefined' && typeof document !== 'undefined';

  // ==== estado público ====
  score = signal<{ home: number; away: number }>({ home: 0, away: 0 });
  timeLeft = signal(0);                 // segundos del cuarto
  timerRunning = signal(false);
  quarter = signal(1);                  // cuarto actual (1..4)

  // Fin de partido: null mientras no haya terminado
  gameOver = signal<{ home: number; away: number; winner: 'home'|'away'|'draw' } | null>(null);

  // ==== reloj interno ====
  private endsAt?: number;              // epoch en ms para terminar el cuarto

  // ==== audio ====
  private audioCtx?: AudioContext;

  // Método público para “buzzer” desde UI
  public beep() { this.playBuzzer(); }

  private playBuzzer() {
    if (!this.isBrowser) return;
    try {
      this.audioCtx ??= new (window.AudioContext || (window as any).webkitAudioContext)();
      const osc = this.audioCtx.createOscillator();
      const gain = this.audioCtx.createGain();
      osc.type = 'square';
      osc.frequency.value = 880;
      gain.gain.setValueAtTime(0.06, this.audioCtx.currentTime);
      osc.connect(gain); gain.connect(this.audioCtx.destination);
      osc.start();
      osc.stop(this.audioCtx.currentTime + 0.35);
    } catch {
      /* ignorar si el navegador bloquea audio sin interacción previa */
    }
  }

  private startTick() {
    this.stopTick();
    this.tick = setInterval(() => {
      if (!this.timerRunning() || !this.endsAt) return;
      const secs = Math.max(0, Math.floor((this.endsAt - Date.now()) / 1000));
      this.timeLeft.set(secs);
      if (secs === 0) {
        this.timerRunning.set(false);
        this.stopTick();
        this.endsAt = undefined;
      }
    }, 200);
  }
  private stopTick() {
    if (this.tick) { clearInterval(this.tick); this.tick = undefined; }
  }

  // Hidrata desde GET /api/matches/:id (usa remainingSeconds para evitar TZ)
  hydrateTimerFromSnapshot(snap?: {
    running: boolean;
    remainingSeconds: number;
    quarterEndsAtUtc?: string | null;
    quarter?: number;
  }) {
    if (!snap) return;
    if (typeof snap.quarter === 'number') this.quarter.set(snap.quarter);

    const secs = snap.remainingSeconds ?? 0;
    this.timeLeft.set(secs);

    if (snap.running && secs > 0) {
      this.timerRunning.set(true);
      this.endsAt = Date.now() + secs * 1000;
      this.startTick();
    } else {
      this.timerRunning.set(false);
      this.endsAt = undefined;
      this.stopTick();
    }
  }

  async connect(matchId: number) {
    if (!this.isBrowser) return;
    if (this.hub) return;

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`/hubs/score?matchId=${matchId}`)
      .withAutomaticReconnect()
      .build();

    // Puntuación
    this.hub.on('scoreUpdated', (s: { homeScore: number; awayScore: number }) => {
      this.score.set({ home: s.homeScore, away: s.awayScore });
    });

    // Timer (siempre con remainingSeconds)
    this.hub.on('timerStarted', (t: { quarterEndsAtUtc: string; remainingSeconds: number }) => {
      this.timeLeft.set(t.remainingSeconds);
      this.timerRunning.set(true);
      this.endsAt = Date.now() + t.remainingSeconds * 1000;
      this.startTick();
    });
    this.hub.on('timerPaused', (t: { remainingSeconds: number }) => {
      this.timeLeft.set(t.remainingSeconds);
      this.timerRunning.set(false);
      this.stopTick();
      this.endsAt = undefined;
    });
    this.hub.on('timerResumed', (t: { quarterEndsAtUtc: string; remainingSeconds: number }) => {
      this.timeLeft.set(t.remainingSeconds);
      this.timerRunning.set(true);
      this.endsAt = Date.now() + t.remainingSeconds * 1000;
      this.startTick();
    });
    this.hub.on('timerReset', (t: { remainingSeconds: number }) => {
      this.timeLeft.set(t.remainingSeconds);
      this.timerRunning.set(false);
      this.stopTick();
      this.endsAt = undefined;
    });

    // Cuarto actual
    // === Quarter (evento del backend) ===
    this.hub.on('quarterChanged', (p: { quarter: number }) => {
      console.debug('[SignalR] quarterChanged ->', p?.quarter); // DEBUG
      if (typeof p.quarter === 'number') this.quarter.set(p.quarter);
    });



    // Buzzer desde backend (inicio/fin cuarto o fin partido)
    this.hub.on('buzzer', (_: any) => {
      this.playBuzzer();
    });


    // Cuarto actual (evento del backend)
  this.hub.on('quarterChanged', (p: { quarter: number }) => {
    console.debug('[SignalR] quarterChanged ->', p?.quarter); // 👈 DEBUG
    if (typeof p.quarter === 'number') this.quarter.set(p.quarter);
  });

  


    // Fin de partido
    this.hub.on('gameEnded', (p: { home: number; away: number; winner: 'home'|'away'|'draw' }) => {
      this.gameOver.set(p);
    });

    await this.hub.start();
  }

  async disconnect() {
    this.stopTick();
    if (this.hub) { await this.hub.stop(); this.hub = undefined; }
  }
}
