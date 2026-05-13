import { Component, OnInit, AfterViewInit, signal, PLATFORM_ID, Inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface NewsletterDto {
  id: number;
  title: string;
  summary: string | null;
  tableOfContents: string | null;
  tocLines: string[];
  coverImageUrl: string | null;
  pdfUrl: string | null;
  publishedAt: string;
  isActive: boolean;
}

@Component({
  selector: 'app-news-newsletters',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './news-newsletters.component.html',
  styleUrl: './news-newsletters.component.scss',
})
export class NewsNewslettersComponent implements OnInit, AfterViewInit {

  private readonly API = environment.apiBaseUrl;

  newsletters   = signal<NewsletterDto[]>([]);
  isLoading     = signal(true);

  // Abonnement
  email         = '';
  phone         = '';
  subSuccess    = signal(false);
  subError      = signal('');
  isSubscribing = signal(false);

  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: object
  ) {}

  ngOnInit() {
    this.http.get<NewsletterDto[]>(`${this.API}/newsletters?active=true`).subscribe({
      next: list => { this.newsletters.set(list); this.isLoading.set(false); },
      error: ()  => this.isLoading.set(false)
    });
  }

  ngAfterViewInit() {
    if (!isPlatformBrowser(this.platformId)) return;
    setTimeout(() => this.initReveal(), 100);
  }

  private initReveal() {
    const obs = new IntersectionObserver(entries => {
      entries.forEach(e => { if (e.isIntersecting) { e.target.classList.add('visible'); obs.unobserve(e.target); } });
    }, { threshold: 0.08 });
    document.querySelectorAll('.nl-reveal').forEach(el => obs.observe(el));
  }

  subscribe() {
    this.subError.set('');
    if (!this.email.trim()) { this.subError.set("L'email est obligatoire."); return; }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.email)) { this.subError.set("Email invalide."); return; }

    this.isSubscribing.set(true);

    this.http.post(`${this.API}/newsletter-subscribers`, {
      email: this.email.trim(),
      phone: this.phone.trim() || null
    }).subscribe({
      next: () => {
        this.isSubscribing.set(false);
        this.subSuccess.set(true);
        this.email = '';
        this.phone = '';
      },
      error: (err) => {
        this.isSubscribing.set(false);
        // Si déjà abonné → message spécifique
        const msg = err?.error?.message || "Une erreur est survenue. Veuillez réessayer.";
        this.subError.set(msg);
      }
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('fr-FR', {
      day: '2-digit', month: 'long', year: 'numeric'
    });
  }

  formatMonth(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('fr-FR', { month: 'long', year: 'numeric' });
  }

  downloadPdf(pdfUrl: string, title: string) {
    // Construire l'URL complète si c'est un chemin relatif (ex: /uploads/fichier.pdf)
    const fullUrl = pdfUrl.startsWith('http')
      ? pdfUrl
      : `${environment.apiOrigin}${pdfUrl}`;

    // Nom du fichier à télécharger
    const fileName = title
      .normalize('NFD').replace(/[\u0300-\u036f]/g, '') // enlever accents
      .replace(/[^a-zA-Z0-9\s-]/g, '')
      .trim()
      .replace(/\s+/g, '_')
      + '.pdf';

    // Téléchargement via fetch pour forcer le download même si même domaine
    fetch(fullUrl)
      .then(res => {
        if (!res.ok) throw new Error('Erreur téléchargement');
        return res.blob();
      })
      .then(blob => {
        const url = window.URL.createObjectURL(blob);
        const a   = document.createElement('a');
        a.href     = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      })
      .catch(() => {
        // Fallback : ouvrir dans un nouvel onglet
        window.open(fullUrl, '_blank');
      });
  }
}
