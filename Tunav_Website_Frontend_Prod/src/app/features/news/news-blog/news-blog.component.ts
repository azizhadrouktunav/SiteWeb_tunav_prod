import { Component, OnInit, AfterViewInit, signal, computed, PLATFORM_ID, Inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

interface BlogCategory {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
}

interface BlogArticle {
  id: number;
  title: string;
  summary?: string;
  content: string;
  coverImageUrl?: string;
  youtubeUrl?: string;
  sector?: string;
  publishedAt?: string;
  isActive: boolean;
  createdAt: string;
  categoryId: number;
  categoryName?: string;
  createdByName?: string;
}

@Component({
  selector: 'app-news-blog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './news-blog.component.html',
  styleUrl: './news-blog.component.scss',
})
export class NewsBlogComponent implements OnInit, AfterViewInit {
  articles = signal<BlogArticle[]>([]);
  categories = signal<BlogCategory[]>([]);
  isLoading = signal(true);

  selectedCategory = '';
  searchText = '';

  // Computed signal pour les articles filtrés
  filteredArticles = computed(() => {
    const search = this.searchText.toLowerCase().trim();
    const articlesList = this.articles();
    
    if (!search) return articlesList;
    return articlesList.filter(a =>
      a.title.toLowerCase().includes(search) ||
      (a.summary?.toLowerCase().includes(search)) ||
      (a.categoryName?.toLowerCase().includes(search))
    );
  });

  private readonly api = 'http://localhost:5057';

  constructor(
    private http: HttpClient,
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: object
  ) {}

  navigateToArticle(id: number) {
    this.router.navigate(['/news/blog', id]);
  }

  ngOnInit() {
    if (!isPlatformBrowser(this.platformId)) return;

    this.http.get<BlogCategory[]>(`${this.api}/api/blog-categories`).subscribe({
      next: (cats) => this.categories.set(cats.filter(c => c.isActive)),
      error: (err) => console.error('Erreur chargement catégories:', err)
    });

    this.loadArticles();
  }

  ngAfterViewInit() {
    if (!isPlatformBrowser(this.platformId)) return;
    this.initRevealObserver();
  }

  private initRevealObserver() {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            entry.target.classList.add('visible');
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.1 }
    );

    // Petit délai pour s'assurer que le DOM est prêt
    setTimeout(() => {
      document.querySelectorAll('.blog-reveal, .blog-section-reveal').forEach(el => {
        observer.observe(el);
      });
    }, 100);
  }

  loadArticles() {
    this.isLoading.set(true);
    let url = `${this.api}/api/blog-articles?isActive=true`;
    if (this.selectedCategory) url += `&categoryId=${this.selectedCategory}`;

    this.http.get<BlogArticle[]>(url).subscribe({
      next: (articles) => {
        console.log('Articles chargés:', articles);
        this.articles.set(articles);
        this.isLoading.set(false);
        // Re-init observer après rendu des nouvelles cartes
        if (isPlatformBrowser(this.platformId)) {
          setTimeout(() => this.initRevealObserver(), 100);
        }
      },
      error: (err) => {
        console.error('Erreur chargement articles:', err);
        this.isLoading.set(false);
      }
    });
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '';
    try {
      return new Date(dateStr).toLocaleDateString('fr-FR', {
        day: 'numeric', month: 'long', year: 'numeric'
      });
    } catch {
      return '';
    }
  }

  readingTime(content: string): number {
    if (!content) return 1;
    return Math.ceil(content.split(' ').length / 200);
  }

  getSectorTags(sector?: string): string[] {
    if (!sector) return [];
    return sector.split(',').map(s => s.trim()).filter(Boolean);
  }
}
