import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard').then((component) => component.Dashboard),
  },
  {
    path: 'history',
    loadComponent: () =>
      import('./features/history/history').then((component) => component.History),
  },
  {
    path: 'machine-learning',
    loadComponent: () =>
      import('./features/machine-learning/machine-learning').then(
        (component) => component.MachineLearning,
      ),
  },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' },
];
