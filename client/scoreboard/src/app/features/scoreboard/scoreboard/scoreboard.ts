import { Component, computed, effect, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { ApiService } from '../../../core/api';
import { RealtimeService } from '../../../core/realtime';
import { TeamPanelComponent } from '../../../shared/team-panel/team-panel';
import { TimerComponent } from '../../../shared/timer/timer';
import { QuarterIndicatorComponent } from '../../../shared/quarter-indicator/quarter-indicator';
import { FoulsPanelComponent } from '../../../shared/fouls-panel/fouls-panel';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-scoreboard',
  standalone: true,
  imports: [RouterLink, MatButtonModule, TeamPanelComponent, TimerComponent, QuarterIndicatorComponent, FoulsPanelComponent],
  templateUrl: './scoreboard.html',
  styleUrls: ['./scoreboard.scss']
})
export class ScoreboardComponent {
  private route = inject(ActivatedRoute);
  private api = inject(ApiService);
  private platformId = inject(PLATFORM_ID);
  realtime = inject(RealtimeService);

  matchId = computed(() => Number(this.route.snapshot.paramMap.get('id') ?? '1'));

  homeName = 'A TEAM';
  awayName = 'B TEAM';

  constructor() {
    effect(() => {
      const over = this.realtime.gameOver();
      if (!over || !isPlatformBrowser(this.platformId)) return;
      const text = over.winner === 'draw'
        ? `Empate ${over.home} - ${over.away}`
        : over.winner === 'home'
          ? `¡Ganó ${this.homeName}! ${over.home} - ${over.away}`
          : `¡Ganó ${this.awayName}! ${over.away} - ${over.home}`;
      Swal.fire({ title: 'Fin del partido', text, icon: 'warning', position: 'top', showConfirmButton: true });
    });
  }

  async ngOnInit() {
    this.api.obtenerPartido(this.matchId()).subscribe({
      next: (m: any) => {
        this.realtime.score.set({ home: m.puntajeLocal, away: m.puntajeVisitante });
        this.homeName = m.equipoLocal || 'A TEAM';
        this.awayName = m.equipoVisitante || 'B TEAM';
        this.realtime.hydrateTimerFromSnapshot(m.timer);
        this.realtime.hydrateFoulsFromSnapshot({ home: m.faltasLocal, away: m.faltasVisitante });
      }
    });
    if (isPlatformBrowser(this.platformId)) {
      await this.realtime.connect(this.matchId());
    }
  }

  ngOnDestroy() {
    if (isPlatformBrowser(this.platformId)) {
      this.realtime.disconnect();
    }
  }
}
