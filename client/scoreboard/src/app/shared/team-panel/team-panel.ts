import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-team-panel',
  standalone: true,
  templateUrl: './team-panel.html',
  styleUrls: ['./team-panel.scss']
})
export class TeamPanelComponent {
  @Input() label = '';
  @Input() score = 0;
}
