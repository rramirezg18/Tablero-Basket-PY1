import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'score/1', pathMatch: 'full' },
  {
    path: 'score/:id',
    loadComponent: () =>
      import('./features/scoreboard/scoreboard/scoreboard').then(m => m.ScoreboardComponent),
  },
  {
    path: 'control/:id',
    loadComponent: () =>
      import('./features/control/control-panel/control-panel').then(m => m.ControlPanelComponent),
  },
  { path: '**', redirectTo: 'score/1' }
];
