import { Component, computed, effect, inject, OnDestroy, PLATFORM_ID, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import Swal from 'sweetalert2';
import { StandingsDialogComponent } from '../../standings/standings-dialog';


import { ApiService } from '../../../core/api';
import { RealtimeService } from '../../../core/realtime';

// Diálogos
import { NewGameDialogComponent } from '../../matches/new-game-dialog';
import { RegisterTeamDialogComponent } from '../../teams/register-team-dialog';

type Possession = 'none' | 'home' | 'away';

@Component({
  selector: 'app-control-panel',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatDialogModule],
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

  // Id partido desde ruta
  matchId = toSignal(this.route.paramMap.pipe(map(p => Number(p.get('id') ?? '1'))), { initialValue: 1 });

  // Datos del partido
  homeTeamId?: number;
  awayTeamId?: number;
  homeName = 'HOME';
  awayName = 'AWAY';

  // Cuarto real desde RealtimeService
  period = computed(() => this.rt.quarter());

  // UI local
  possession = signal<Possession>('none');
  homeScore = signal(0);
  awayScore = signal(0);

  // Faltas desde RealtimeService
  homeFouls = computed(() => this.rt.fouls().home);
  awayFouls = computed(() => this.rt.fouls().away);

  // Sólo se puede anotar cuando corre el timer
  canScore = computed(() => this.rt.timerRunning());

  // Reloj mm:ss desde timeLeft()
  clock = computed(() => {
    const tl = this.rt.timeoutRunning() ? this.rt.timeoutLeft() : this.rt.timeLeft();
    const m = Math.floor(tl / 60), s = tl % 60;
    return `${m.toString().padStart(2,'0')}:${s.toString().padStart(2,'0')}`;
  });

  // Auto-advance helpers
  private prevSecs = -1;
  private armed = true;
  private zeroGuardUntil = 0;   // ventana para ignorar 0 tras stop/reset
  private prevRunning = false;  // estado anterior del timer

  constructor() {
    // Sincroniza marcador local
    effect(() => {
      const s = this.rt.score();
      this.homeScore.set(s.home);
      this.awayScore.set(s.away);
    });

    // Fin de partido → SweetAlert con resultado
    effect(() => {
      const over = this.rt.gameOver?.();
      if (!over) return;
      this.showGameEndAlert(over.home, over.away, over.winner);
    });

    // AUTO-AVANCE con guardia para Stop/Reset
    effect(() => {
      const secs = this.rt.timeLeft();
      const running = this.rt.timerRunning();
      const guardActive = Date.now() < this.zeroGuardUntil;

      // Sólo auto-avanzar si veníamos corriendo y llegamos naturalmente a 0
      if (!guardActive && this.prevSecs > 0 && secs === 0 && this.prevRunning) {
        this.tryAutoAdvance();
      }

      if (secs > 0) this.armed = true;

      this.prevSecs = secs;
      this.prevRunning = running;
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

          // hidratar faltas si el GET las trae (opcional)
          if (m.fouls) this.rt.hydrateFoulsFromSnapshot(m.fouls);
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

      onCleanup(() => { disposed = true; this.rt.disconnect(); });
    });
  }

  ngOnDestroy(): void { this.rt.disconnect(); }

  // === Reintento para oficializar fin de cuarto en backend ===
  private tryAutoAdvance(retry = 0) {
    const id = this.matchId();
    this.api.autoAdvanceQuarter(id).subscribe({
      next: (res: any) => {
        // el backend ya subió el cuarto; el que terminó es (nuevo - 1)
        const ended = (res?.quarter ?? this.rt.quarter()) - 1;
        this.showQuarterEndAlert(ended); // buzzer llega por SignalR
      },
      error: (e) => {
        if (retry < 8) {
          setTimeout(() => this.tryAutoAdvance(retry + 1), 300);
        } else {
          console.warn('autoAdvanceQuarter no confirmó el fin del cuarto', e);
        }
      }
    });
  }

  // === Flujo antiguo (manual por nombres)
  newGame() {
    const home = (prompt('Nombre equipo local:', this.homeName) ?? '').trim();
    if (!home) return;
    const away = (prompt('Nombre equipo visitante:', this.awayName) ?? '').trim();
    if (!away) return;
    const mins = Number(prompt('Duración del período (minutos):', '10') ?? '10');
    const qsec = Number.isFinite(mins) && mins > 0 ? Math.round(mins * 60) : 600;

    this.api.newGame({ homeName: home, awayName: away, quarterDurationSeconds: qsec })
      .subscribe({ next: (res: any) => this.router.navigate(['/control', res.matchId]) });
  }

  // === Puntos
  add(teamId: number | undefined, points: 1 | 2 | 3) {
    if (!teamId || !this.canScore()) return;
    this.api.createScore(this.matchId(), { teamId, points }).subscribe();
  }
  minus1(teamId: number | undefined) {
    if (!teamId || !this.canScore()) return;
    this.api.adjustScore(this.matchId(), { teamId, delta: -1 }).subscribe();
  }

  // === Faltas
  foul(teamId: number | undefined, delta: 1 | -1) {
    if (!teamId) return;
    this.api.adjustFoul(this.matchId(), { teamId, delta }).subscribe();
  }

  // === Timer
  start() {
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
      }
    });
  }

  stop() {
    // Evita que el 0 provocado por pausa dispare auto-advance
    this.armed = false;
    this.prevSecs = 0;
    this.zeroGuardUntil = Date.now() + 1500; // ignora 0 por 1.5s
    this.api.pauseTimer(this.matchId()).subscribe();
  }

  resume() { this.api.resumeTimer(this.matchId()).subscribe(); }

  reset() {
    // Evita que el 0 provocado por reset dispare auto-advance
    this.armed = false;
    this.prevSecs = 0;
    this.zeroGuardUntil = Date.now() + 1500; // ignora 0 por 1.5s
    this.api.resetTimer(this.matchId()).subscribe();
  }

  timeout(sec: number) {
    if (this.rt.timeoutRunning()) return;
    this.stop();
    this.rt.startTimeout(sec, () => this.resume());
  }

  // === Periodo (real)
  periodMinus() { /* mantener consistente: no retroceder */ }
  periodPlus()  {
    const ended = this.rt.quarter();
    this.api.advanceQuarter(this.matchId()).subscribe({
      next: async () => { await this.showQuarterEndAlert(ended); }
    });
  }

  // === Posesión (visual local)
  posLeft()  { this.possession.set('home'); }
  posNone()  { this.possession.set('none'); }
  posRight() { this.possession.set('away'); }

  // ==== D I Á L O G O S  ====

  // Registrar equipo
  registerTeam() {
    const ref = this.dialog.open(RegisterTeamDialogComponent, {
      width: '520px',
      disableClose: true
    });
    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.api.createTeam(result).subscribe({
        next: async (res: any) => {
          await Swal.fire({
            icon: 'success',
            title: `Team "${res?.name ?? result.name}" created`,
            timer: 1200, showConfirmButton: false, position: 'top'
          });
        },
        error: async (e) => {
          await Swal.fire({ icon: 'error', title: 'Error creating team', text: e?.error ?? e?.message ?? 'Unknown error' });
        }
      });
    });
  }

  // Nuevo partido con equipos registrados
  newGameFromRegistered() {
    const ref = this.dialog.open(NewGameDialogComponent, {
      width: '520px',
      disableClose: true
    });
    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.api.newGameByTeams(result).subscribe({
        next: (res: any) => this.router.navigate(['/control', res.matchId]),
        error: async (e) => {
          await Swal.fire({ icon: 'error', title: 'Error creating match', text: e?.error ?? e?.message ?? 'Unknown error' });
        }
      });
    });
  }

  // ==== Alerts ====
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
    let text = winner === 'draw' ? `Empate ${home} - ${away}` :
               winner === 'home' ? `¡Ganó ${this.homeName}! ${home} - ${away}` :
                                   `¡Ganó ${this.awayName}! ${away} - ${home}`;
    await Swal.fire({ title: 'Fin del partido', text, icon: 'warning', position: 'top', showConfirmButton: true });
  }

  showStandings() {
    this.api.getStandings().subscribe({
      next: (rows) => {
        this.dialog.open(StandingsDialogComponent, {
          width: '520px',
          data: { rows }
        });
      },
      error: (e) => {
        console.error('getStandings error', e);
        // Si usas SweetAlert:
        // Swal.fire({ icon: 'error', title: 'Error cargando standings', text: e?.error ?? e?.message ?? 'Unknown error' });
      }
    });
  }





}
