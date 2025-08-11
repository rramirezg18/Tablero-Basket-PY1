import { Component, computed, inject } from '@angular/core';
import { RealtimeService } from '../../core/realtime';

@Component({
  selector: 'app-timer',
  standalone: true,
  templateUrl: './timer.html',
  styleUrls: ['./timer.scss']
})
export class TimerComponent {
  rt = inject(RealtimeService);
  display = computed(() => {
    const s = this.rt.timeLeft();
    const m = Math.floor(s / 60);
    const r = s % 60;
    return `${m.toString().padStart(2,'0')}:${r.toString().padStart(2,'0')}`;
  });
}
