import { Routes } from '@angular/router';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { HomeComponent } from './features/home/home.component';
import { CareerComponent } from './features/entreprise/career/career.component';
import { PartnersComponent } from './features/partners/partners.component';
import { NewsBlogComponent } from './features/news/news-blog/news-blog.component';
import { NewsEventComponent } from './features/news/news-event/news-event.component';
import { NewsNewslettersComponent } from './features/news/news-newsletters/news-newsletters.component';
import { AboutComponent } from './features/entreprise/about/about.component';
import { PoleFormationComponent } from './features/entreprise/formation/formation.component';  // ← CHANGÉ
import { ContactComponent } from './features/entreprise/contact/contact.component';
import { DemoRequestComponent } from './features/demo-request/demo-request.component';
import { TestRubriqueComponent } from './features/test-rubrique/test-rubrique.component';
import { NewsBlogDetailComponent } from './features/news/news-blog/news-blog-detail.component';

export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', component: HomeComponent },
      {
        path: 'solutions',
        loadComponent: () =>
          import('./features/solutions/solutions.component').then(
            (module) => module.SolutionsComponent
          ),
      },
      {
        path: 'solutions/:id',
        loadComponent: () =>
          import('./features/solutions/solution-detail.component').then(
            (module) => module.SolutionDetailComponent
          ),
      },
      {
        path: 'packs',
        loadComponent: () =>
          import('./features/packs/packs.component').then(
            (module) => module.PacksComponent
          ),
      },
      { path: 'career', component: CareerComponent },
      { path: 'news/blog', component: NewsBlogComponent },
      { path: 'news/blog/:id', component: NewsBlogDetailComponent },
      { path: 'news/event', component: NewsEventComponent },
      { path: 'news/newsletters', component: NewsNewslettersComponent },
      { path: 'about', component: AboutComponent },
      { path: 'formation', component: PoleFormationComponent },  // ← CHANGÉ
      { path: 'contact', component: ContactComponent },
      { path: 'partners', component: PartnersComponent },
      { path: 'partners/:type', component: PartnersComponent },
      { path: 'demo-request', component: DemoRequestComponent },
      { path: 'test-rubrique', component: TestRubriqueComponent },
    ],
  },
  { path: '**', redirectTo: 'home' },
];
