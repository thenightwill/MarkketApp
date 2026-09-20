import { Routes } from '@angular/router';
import { Shell } from './core/layout/shell';
import { authGuard, guestGuard } from './core/guards/guards';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register-page').then((m) => m.RegisterPage),
  },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'sales' },
      {
        path: 'sales',
        loadComponent: () => import('./features/sales/sales-page').then((m) => m.SalesPage),
      },
      {
        path: 'products',
        loadComponent: () => import('./features/products/products-page').then((m) => m.ProductsPage),
      },
      {
        path: 'inventory',
        loadComponent: () => import('./features/inventory/inventory-page').then((m) => m.InventoryPage),
      },
      {
        path: 'tasks',
        loadComponent: () => import('./features/tasks/tasks-page').then((m) => m.TasksPage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
