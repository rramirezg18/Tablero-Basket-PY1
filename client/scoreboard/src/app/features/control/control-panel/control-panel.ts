import { Component, computed, effect, inject, OnDestroy, PLATFORM_ID, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';
import Swal from 'sweetalert2';

import { ApiService } from '../../../core/api';
import { RealtimeService } from '../../../core/realtime';
import { NewGameDialogComponent } from '../../matches/new-game-dialog';
import { RegisterTeamDialogComponent } from '../../teams/register-team-dialog';


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
  private dialog = inject(MatDialog);

  // id de partido
  matchId = toSignal(this.route.paramMap.pipe(map(p => Number(p.get('id') ?? '1'))), { initialValue: 1 });

  // datos
  homeTeamId?: number;
  awayTeamId?: number;
  homeName = 'HOME';
  awayName = 'AWAY';

  // cuarto real
  period = computed(() => this.rt.quarter());

  // UI local
  possession = signal<Possession>('none');
  homeScore = signal(0);
  awayScore = signal(0);

  canScore = computed(() => (this.rt as any).timerRunning ? (this.rt as any).timerRunning() : true);

  clock = computed(() => {
    const tl = (this.rt as any).timeLeft ? (this.rt as any).timeLeft() : 0;
    const m = Math.floor(tl / 60);
    const s = tl % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  });

  // Deshabilitar "Start" cuando corre el reloj o el juego ya terminó
  // El partido SOLO se considera terminado cuando llega el evento "gameEnded"
  isFinished = computed(() => {
    return !!(this.rt as any).gameOver?.(); // true solo tras recibir 'gameEnded'
  });



  private prevSecs = -1;
  private armed = true;

  constructor() {
    // sincroniza marcador local
    effect(() => {
      const s = this.rt.score();
      this.homeScore.set(s.home);
      this.awayScore.set(s.away);
    });

    // fin de partido
    effect(() => {
      const over = this.rt.gameOver?.();
      if (!over) return;
      this.showGameEndAlert(over.home, over.away, over.winner);
    });

    // auto-advance con reintento
    effect(() => {
      const secs = this.rt.timeLeft();
      if (this.prevSecs > 0 && secs === 0 && this.armed) {
        this.armed = false;
        this.tryAutoAdvance();
      }
      if (secs > 0) this.armed = true;
      this.prevSecs = secs;
    });

    // carga inicial + realtime
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

  // === New Game (único flujo) ===
  async newGame() {
    const ref = this.dialog.open(NewGameDialogComponent, { autoFocus: true });
    const data = await ref.afterClosed().toPromise();
    if (!data) return;

    try {
      const res = await firstValueFrom(
        this.api.newGameByTeams({
          homeTeamId: data.homeTeamId,
          awayTeamId: data.awayTeamId,
          quarterDurationSeconds: data.quarterDurationSeconds
        })
      );
      this.router.navigate(['/control', res.matchId]);
    } catch (e: any) {
      await Swal.fire({
        icon: 'error',
        title: 'Error creating match',
        text: e?.error ?? e?.message ?? 'Unknown error'
      });
    }
  }

  // === Registrar equipo (dialog) ===
  async registerTeam() {
    const ref = this.dialog.open(RegisterTeamDialogComponent, { autoFocus: true });
    const form = await ref.afterClosed().toPromise();
    if (!form) return;

    try {
      const res = await firstValueFrom(this.api.createTeam({
        name: form.name,
        color: form.color,
        players: form.players
      }));
      await Swal.fire({
        icon: 'success',
        title: `Team "${res.name}" created`,
        timer: 1200,
        showConfirmButton: false,
        position: 'top'
      });
    } catch (e: any) {
      await Swal.fire({
        icon: 'error',
        title: 'Error creating team',
        text: e?.error ?? e?.message ?? 'Unknown error'
      });
    }
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
  periodMinus() { /* sin retroceder para consistencia */ }
  periodPlus()  {
    const ended = this.rt.quarter();
    this.api.advanceQuarter(this.matchId()).subscribe({
      next: async () => { await this.showQuarterEndAlert(ended); },
      error: (e) => console.error('advanceQuarter error', e)
    });
  }

  // === posesión (visual local)
  posLeft()  { this.possession.set('home'); }
  posNone()  { this.possession.set('none'); }
  posRight() { this.possession.set('away'); }

  // === auto-advance helper ===
  private tryAutoAdvance(retry = 0) {
    const id = this.matchId();
    this.api.autoAdvanceQuarter(id).subscribe({
      next: (res: any) => {
        const ended = (res?.quarter ?? this.rt.quarter()) - 1;
        this.showQuarterEndAlert(ended);
      },
      error: (e) => {
        if (retry < 8) setTimeout(() => this.tryAutoAdvance(retry + 1), 300);
        else console.warn('autoAdvanceQuarter no confirmó el fin del cuarto', e);
      }
    });
  }

  // === alerts ===
  private async showQuarterEndAlert(endedQuarter: number) {
    if (!isPlatformBrowser(this.platformId)) return;
    const names = ['', 'Primer', 'Segundo', 'Tercer', 'Cuarto'];
    const title = endedQuarter >= 1 && endedQuarter <= 4 ? `Fin del ${names[endedQuarter]} cuarto` : 'Fin de cuarto';
    await Swal.fire({
      title, icon: 'info', position: 'top', timer: 1600, timerProgressBar: true,
      showConfirmButton: false, backdrop: true, background: '#ffffff', color: '#111'
    });
  }

  private async showGameEndAlert(home: number, away: number, winner: 'home'|'away'|'draw') {
    if (!isPlatformBrowser(this.platformId)) return;
    let text = '';
    if (winner === 'draw') text = `Empate ${home} - ${away}`;
    else if (winner === 'home') text = `¡Ganó ${this.homeName}! ${home} - ${away}`;
    else text = `¡Ganó ${this.awayName}! ${away} - ${home}`;
    await Swal.fire({ title: 'Fin del partido', text, icon: 'warning', position: 'top', showConfirmButton: true });
  }
}
