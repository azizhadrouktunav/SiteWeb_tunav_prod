import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  {
    path: 'news/blog/:id',
    renderMode: RenderMode.Server
  },
  {
    path: 'solutions/:id',
    renderMode: RenderMode.Server
  },
  {
    path: 'partners/:type',
    renderMode: RenderMode.Server
  },
  {
    path: '**',
    renderMode: RenderMode.Prerender
  }
];
