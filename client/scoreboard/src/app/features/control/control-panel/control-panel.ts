import { Component, computed, effect, inject, OnDestroy, PLATFORM_ID, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { ApiService } from '../../../core/api';
import { RealtimeService } from '../../../core/realtime';

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
  public rt = inject(RealtimeService);     // público para usar rt.timerRunning() en la plantilla
  private api = inject(ApiService);
  private platformId = inject(PLATFORM_ID);

  // Id de la ruta como señal (se actualiza si navegas a otro /control/:id)
  matchId = toSignal(
    this.route.paramMap.pipe(map(p => Number(p.get('id') ?? '1'))),
    { initialValue: 1 }
  );

  // datos del partido
  homeTeamId?: number;
  awayTeamId?: number;
  homeName = 'HOME';
  awayName = 'AWAY';
  period = signal(1);
  possession = signal<Possession>('none');

  // scores (mostrados en esta vista; se alimentan desde RealtimeService)
  homeScore = signal(0);
  awayScore = signal(0);

  // solo se puede anotar cuando el timer corre
  canScore = computed(() => {
    // si aún no tienes timerRunning en RealtimeService, deja true para no bloquear
    return (this.rt as any).timerRunning ? (this.rt as any).timerRunning() : true;
  });

  // reloj mm:ss leyendo timeLeft() del RealtimeService
  clock = computed(() => {
    const tl = (this.rt as any).timeLeft ? (this.rt as any).timeLeft() : 0;
    const m = Math.floor(tl / 60);
    const s = tl % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  });

  constructor() {
    // Sincroniza el marcador local con la señal global del RealtimeService
    effect(() => {
      const s = this.rt.score();
      this.homeScore.set(s.home);
      this.awayScore.set(s.away);
    });

    // Reacciona a cambios de matchId: carga estado y conecta SignalR (solo en navegador)
    effect((onCleanup) => {
      const id = this.matchId();
      if (!id) return;

      // Carga inicial del estado del partido
      this.api.getMatch(id).subscribe({
        next: (m: any) => {
          this.homeTeamId = m.homeTeamId;
          this.awayTeamId = m.awayTeamId;
          this.homeName = m.homeTeam ?? 'HOME';
          this.awayName = m.awayTeam ?? 'AWAY';
          this.period.set(m.quarter ?? 1);
          this.homeScore.set(m.homeScore ?? 0);
          this.awayScore.set(m.awayScore ?? 0);
        },
        error: (e) => console.error('getMatch error', e)
      });

      // Conexión tiempo real
      let disposed = false;
      if (isPlatformBrowser(this.platformId)) {
        (async () => {
          try {
            await this.rt.disconnect();
            if (!disposed) {
              await this.rt.connect(id);
            }
          } catch (err) {
            console.error('SignalR connect error', err);
          }
        })();
      }

      // cleanup al cambiar de partido o destruir el componente
      onCleanup(() => {
        disposed = true;
        this.rt.disconnect();
      });
    });
  }

  ngOnDestroy(): void {
    // Por si se destruye la vista
    this.rt.disconnect();
  }

  // === New Game ===
  newGame() {
    const home = (prompt('Nombre equipo local:', this.homeName) ?? '').trim();
    if (!home) return;
    const away = (prompt('Nombre equipo visitante:', this.awayName) ?? '').trim();
    if (!away) return;
    const mins = Number(prompt('Duración del período (minutos):', '10') ?? '10');
    const qsec = Number.isFinite(mins) && mins > 0 ? Math.round(mins * 60) : 600;

    this.api.newGame({ homeName: home, awayName: away, quarterDurationSeconds: qsec })
      .subscribe({
        next: (res: any) => {
          // navega al nuevo partido; los effects recargarán datos y conectarán SignalR
          this.router.navigate(['/control', res.matchId]);
        },
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
  start()  { this.api.startTimer(this.matchId()).subscribe(); }
  stop()   { this.api.pauseTimer(this.matchId()).subscribe(); }
  resume() { this.api.resumeTimer(this.matchId()).subscribe(); }
  reset()  { this.api.resetTimer(this.matchId()).subscribe(); }

  // atajos de timeout: arranca contador con X segundos
  timeout(sec: number) {
    this.api.startTimer(this.matchId(), { quarterDurationSeconds: sec }).subscribe();
  }

  // periodo
  periodMinus() { this.period.update(p => Math.max(1, p - 1)); /* si quieres persistirlo, añade endpoint */ }
  periodPlus()  { this.api.advanceQuarter(this.matchId()).subscribe(); this.period.update(p => p + 1); }

  // posesión (visual local)
  posLeft()  { this.possession.set('home'); }
  posNone()  { this.possession.set('none'); }
  posRight() { this.possession.set('away'); }
}
