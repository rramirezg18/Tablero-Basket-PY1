import { Component, computed, effect, inject, OnDestroy, PLATFORM_ID, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { ApiService } from '../../../core/api';
import { RealtimeService } from '../../../core/realtime';
import Swal from 'sweetalert2';

type Possession = 'none' | 'home' | 'away';

@Component({
  selector: 'app-control-panel',
  standalone: true,
  imports: [RouterLink, MatButtonModule],
  templateUrl: './control-panel.html',
  styleUrls: ['./control-panel.scss']
})
export class ControlPanelComponent implements OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  public rt = inject(RealtimeService);
  private api = inject(ApiService);
  private platformId = inject(PLATFORM_ID);

  // id de partido
  matchId = toSignal(this.route.paramMap.pipe(map(p => Number(p.get('id') ?? '1'))), { initialValue: 1 });

  // datos
  homeTeamId?: number;
  awayTeamId?: number;
  homeName = 'HOME';
  awayName = 'AWAY';

  // cuarto REAL desde el servicio
  period = computed(() => this.rt.quarter());

  // UI local
  possession = signal<Possession>('none');
  homeScore = signal(0);
  awayScore = signal(0);

  // habilitar anotación solo cuando corre el timer
  canScore = computed(() => (this.rt as any).timerRunning ? (this.rt as any).timerRunning() : true);

  // reloj mm:ss desde timeLeft()
  clock = computed(() => {
    const tl = (this.rt as any).timeLeft ? (this.rt as any).timeLeft() : 0;
    const m = Math.floor(tl / 60);
    const s = tl % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  });

  // auto-advance flags
  private prevSecs = -1;
  private armed = true;

  constructor() {
    // sincroniza marcador local
    effect(() => {
      const s = this.rt.score();
      this.homeScore.set(s.home);
      this.awayScore.set(s.away);
    });

    // FIN DE PARTIDO → SweetAlert con resultado
    effect(() => {
      const over = this.rt.gameOver?.();
      if (!over) return;
      this.showGameEndAlert(over.home, over.away, over.winner);
    });

    // AUTO-AVANCE: detecta transición >0 → 0 y reintenta si hay drift cliente/servidor
    effect(() => {
      const secs = this.rt.timeLeft();

      if (this.prevSecs > 0 && secs === 0 && this.armed) {
        this.armed = false;
        this.tryAutoAdvance(); // ✅ reintento corto
      }

      // re-armar cuando vuelve a haber tiempo (nuevo cuarto o reanudación)
      if (secs > 0) this.armed = true;

      this.prevSecs = secs;
    });

    // Carga inicial + conexión realtime
    effect((onCleanup) => {
      const id = this.matchId();
      if (!id) return;

      this.api.getMatch(id).subscribe({
        next: (m: any) => {
          this.homeTeamId = m.homeTeamId;
          this.awayTeamId = m.awayTeamId;
          this.homeName = m.homeTeam ?? 'HOME';
          this.awayName = m.awayTeam ?? 'AWAY';

          this.homeScore.set(m.homeScore ?? 0);
          this.awayScore.set(m.awayScore ?? 0);

          if (typeof m.quarter === 'number') this.rt.quarter.set(m.quarter);
          if (m.timer) this.rt.hydrateTimerFromSnapshot({ ...m.timer, quarter: m.quarter });
        },
        error: (e) => console.error('getMatch error', e)
      });

      let disposed = false;
      if (isPlatformBrowser(this.platformId)) {
        (async () => {
          try {
            await this.rt.disconnect();
            if (!disposed) await this.rt.connect(id);
          } catch (err) {
            console.error('SignalR connect error', err);
          }
        })();
      }

      onCleanup(() => {
        disposed = true;
        this.rt.disconnect();
      });
    });
  }

  ngOnDestroy(): void {
    this.rt.disconnect();
  }

  // === Reintento para oficializar el fin de cuarto en backend ===
  private tryAutoAdvance(retry = 0) {
    const id = this.matchId();
    this.api.autoAdvanceQuarter(id).subscribe({
      next: (res: any) => {
        // el backend ya subió el cuarto; el que terminó es (nuevo - 1)
        const ended = (res?.quarter ?? this.rt.quarter()) - 1;
        this.showQuarterEndAlert(ended); // el buzzer llega por SignalR
      },
      error: (e) => {
        if (retry < 8) { // ~2.4s máx (8 * 300ms)
          setTimeout(() => this.tryAutoAdvance(retry + 1), 300);
        } else {
          console.warn('autoAdvanceQuarter no confirmó el fin del cuarto', e);
        }
      }
    });
  }

  // === New Game ===
  newGame() {
    const home = (prompt('Nombre equipo local:', this.homeName) ?? '').trim();
    if (!home) return;
    const away = (prompt('Nombre equipo visitante:', this.awayName) ?? '').trim();
    if (!away) return;
    const mins = Number(prompt('Duración del período (minutos):', '10') ?? '10');
    const qsec = Number.isFinite(mins) && mins > 0 ? Math.round(mins * 60) : 600;

    this.api.newGame({ homeName: home, awayName: away, quarterDurationSeconds: qsec }).subscribe({
      next: (res: any) => this.router.navigate(['/control', res.matchId]),
      error: (e) => console.error('newGame error', e)
    });
  }

  // === puntos ===
  add(teamId: number | undefined, points: 1 | 2 | 3) {
    if (!teamId || !this.canScore()) return;
    this.api.createScore(this.matchId(), { teamId, points }).subscribe({
      error: (e) => console.error('createScore error', e)
    });
  }
  minus1(teamId: number | undefined) {
    if (!teamId || !this.canScore()) return;
    this.api.adjustScore(this.matchId(), { teamId, delta: -1 }).subscribe({
      error: (e) => console.error('adjustScore error', e)
    });
  }

  // === timer ===
  start()  {
    this.api.startTimer(this.matchId()).subscribe({
      next: async () => {
        if (!isPlatformBrowser(this.platformId)) return;
        const q = this.rt.quarter();
        const names = ['', 'Primer', 'Segundo', 'Tercer', 'Cuarto'];
        await Swal.fire({
          title: `Inicio del ${names[q] ?? q + 'º'} cuarto`,
          icon: 'success',
          position: 'top',
          timer: 1200,
          showConfirmButton: false
        });
      },
      error: (e) => console.error('startTimer error', e)
    });
  }

  stop()   { this.api.pauseTimer(this.matchId()).subscribe(); }
  resume() { this.api.resumeTimer(this.matchId()).subscribe(); }
  reset()  { this.api.resetTimer(this.matchId()).subscribe(); }
  timeout(sec: number) {
    this.api.startTimer(this.matchId(), { quarterDurationSeconds: sec }).subscribe();
  }

  // === periodo (real)
  periodMinus() { /* opcional: no retroceder para mantener consistencia */ }
  periodPlus()  {
    const ended = this.rt.quarter();
    this.api.advanceQuarter(this.matchId()).subscribe({
      next: async () => {
        await this.showQuarterEndAlert(ended);
      },
      error: (e) => console.error('advanceQuarter error', e)
    });
  }

  // === posesión (visual local)
  posLeft()  { this.possession.set('home'); }
  posNone()  { this.possession.set('none'); }
  posRight() { this.possession.set('away'); }

  // === SweetAlerts ===
  private async showQuarterStartAlert(q: number) {
    if (!isPlatformBrowser(this.platformId)) return;
    const names = ['', 'Primer', 'Segundo', 'Tercer', 'Cuarto'];
    await Swal.fire({
      title: `Inicio del ${names[q] ?? q + 'º'} cuarto`,
      icon: 'success',
      position: 'top',
      timer: 1200,
      showConfirmButton: false
    });
  }

  private async showQuarterEndAlert(endedQuarter: number) {
    if (!isPlatformBrowser(this.platformId)) return;
    const names = ['', 'Primer', 'Segundo', 'Tercer', 'Cuarto'];
    const title = endedQuarter >= 1 && endedQuarter <= 4
      ? `Fin del ${names[endedQuarter]} cuarto`
      : 'Fin de cuarto';

    await Swal.fire({
      title,
      icon: 'info',
      position: 'top',
      timer: 1600,
      timerProgressBar: true,
      showConfirmButton: false,
      backdrop: true,
      background: '#ffffff',
      color: '#111'
    });
  }

  private async showGameEndAlert(home: number, away: number, winner: 'home'|'away'|'draw') {
    if (!isPlatformBrowser(this.platformId)) return;
    let text = '';
    if (winner === 'draw') text = `Empate ${home} - ${away}`;
    else if (winner === 'home') text = `¡Ganó ${this.homeName}! ${home} - ${away}`;
    else text = `¡Ganó ${this.awayName}! ${away} - ${home}`;

    await Swal.fire({
      title: 'Fin del partido',
      text,
      icon: 'warning',
      position: 'top',
      showConfirmButton: true
    });
  }
}
