import { Component, computed, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { ApiService } from '../../../core/api';
import { RealtimeService } from '../../../core/realtime';
import { TeamPanelComponent } from '../../../shared/team-panel/team-panel';
import { TimerComponent } from '../../../shared/timer/timer';
import { QuarterIndicatorComponent } from '../../../shared/quarter-indicator/quarter-indicator';
import { FoulsPanelComponent } from '../../../shared/fouls-panel/fouls-panel';

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

  // ... imports arriba (igual que antes)
homeName = 'A TEAM';
awayName = 'B TEAM';

  async ngOnInit() {
    this.api.getMatch(this.matchId()).subscribe({
      next: (m: any) => {
        this.realtime.score.set({ home: m.homeScore, away: m.awayScore });
        this.homeName = m.homeTeam || 'A TEAM';
        this.awayName = m.awayTeam || 'B TEAM';
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
