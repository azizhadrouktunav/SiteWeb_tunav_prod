import { Component, OnInit, signal, PLATFORM_ID, Inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { switchMap } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

interface BlogArticle {
  id: number;
  title: string;
  summary?: string;
  content: string;
  coverImageUrl?: string;
  youtubeUrl?: string;
  youtubeEmbedId?: string;
  sector?: string;
  publishedAt?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  categoryId: number;
  categoryName?: string;
  createdByName?: string;
}

@Component({
  selector: 'app-news-blog-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './news-blog-detail.component.html',
  styleUrl: './news-blog-detail.component.scss',
})
export class NewsBlogDetailComponent implements OnInit {
  article = signal<BlogArticle | null>(null);
  relatedArticles = signal<BlogArticle[]>([]);
  isLoading = signal(true);
  copied = signal(false);
  expanded = signal(false);

  private readonly api = environment.apiOrigin;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private sanitizer: DomSanitizer,
    @Inject(PLATFORM_ID) private platformId: object
  ) {}

  ngOnInit() {
    // SSR-safe
    if (!isPlatformBrowser(this.platformId)) return;

    // switchMap garantit le rechargement du contenu à chaque changement d'id
    this.route.paramMap.pipe(
      switchMap(params => {
        const id = params.get('id');
        if (!id) throw new Error('No id');
        this.isLoading.set(true);
        this.expanded.set(false);
        this.article.set(null);
        this.relatedArticles.set([]);
        if (isPlatformBrowser(this.platformId)) {
          window.scrollTo({ top: 0, behavior: 'smooth' });
        }
        return this.http.get<BlogArticle>(`${this.api}/api/blog-articles/${+id}`);
      })
    ).subscribe({
      next: (article) => {
        this.article.set(article);
        this.isLoading.set(false);
        this.loadRelated(article.categoryId, article.id);
      },
      error: (err) => {
        console.error('Erreur chargement article:', err);
        this.isLoading.set(false);
      }
    });
  }

  loadRelated(categoryId: number, excludeId: number) {
    this.http.get<BlogArticle[]>(`${this.api}/api/blog-articles?categoryId=${categoryId}&isActive=true`)
      .subscribe({
        next: (articles) => {
          this.relatedArticles.set(articles.filter(a => a.id !== excludeId).slice(0, 3));
        },
        error: (err) => console.error('Erreur chargement articles similaires:', err)
      });
  }

  // ✅ NOUVELLE MÉTHODE : Extraire l'ID YouTube depuis l'URL
  extractYoutubeId(url?: string): string | null {
    if (!url) return null;
    const val = url.trim();
    if (val.includes('youtu.be/')) {
      return val.split('youtu.be/').pop()?.split('?')[0] || null;
    }
    if (val.includes('v=')) {
      return val.split('v=').pop()?.split('&')[0] || null;
    }
    return val.length <= 20 ? val : null;
  }

  // ✅ NOUVELLE MÉTHODE : Obtenir l'URL embed YouTube sécurisée (appelée dans le template)
  getYoutubeEmbedUrl(article: BlogArticle): SafeResourceUrl | null {
    const embedId = this.extractYoutubeId(article.youtubeUrl);
    if (!embedId) return null;
    return this.sanitizer.bypassSecurityTrustResourceUrl(
      `https://www.youtube-nocookie.com/embed/${embedId}`
    );
  }

  // Sanitize YouTube URL — requis par Angular pour les iframes
  getYoutubeUrl(embedId: string): SafeResourceUrl {
    return this.sanitizer.bypassSecurityTrustResourceUrl(
      `https://www.youtube.com/embed/${embedId}`
    );
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('fr-FR', {
      day: 'numeric', month: 'long', year: 'numeric'
    });
  }

  readingTime(content: string): number {
    return Math.max(1, Math.ceil(content.split(' ').length / 200));
  }

  getTags(article: BlogArticle): string[] {
    // Retourne uniquement la categoryName comme tag — les secteurs sont affichés séparément
    if (!article.categoryName) return [];
    return [article.categoryName];
  }

  share() {
    if (isPlatformBrowser(this.platformId)) {
      navigator.clipboard.writeText(window.location.href).then(() => {
        this.copied.set(true);
        setTimeout(() => this.copied.set(false), 2000);
      });
    }
  }

  get contentParagraphs(): string[] {
    const a = this.article();
    if (!a) return [];
    return a.content.split('\n').filter(p => p.trim().length > 0);
  }

  get visibleParagraphs(): string[] {
    const all = this.contentParagraphs;
    return this.expanded() ? all : all.slice(0, 4);
  }

  getSectorTags(sector?: string): string[] {
    if (!sector) return [];
    return sector.split(',').map(s => s.trim()).filter(Boolean);
  }
}
