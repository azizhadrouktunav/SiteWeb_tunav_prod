import { Component, OnInit, AfterViewInit, signal, PLATFORM_ID, Inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';


interface TeamMember {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  position: string;
  description: string;
  photoUrl: string | null;
  linkedInUrl: string | null;
  email: string | null;
  displayOrder: number;
  isActive: boolean;
}

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './about.component.html',
  styleUrl: './about.component.scss',
})
export class AboutComponent implements OnInit, AfterViewInit {
  teamMembers = signal<TeamMember[]>([]);
  isLoadingTeam = signal(true);
  currentSlide = signal(0);

  readonly values = [
    {
      icon: '💡', iconBg: '#4f6ef7', title: 'Innovation',
      description: 'Nous repoussons constamment les limites de la technologie pour offrir les solutions les plus avancées du marché.',
      image: 'https://images.unsplash.com/photo-1581091226825-a6a2a5aee158?w=400&h=200&fit=crop'
    },
    {
      icon: '✨', iconBg: '#a855f7', title: 'Excellence',
      description: 'La qualité et la satisfaction client sont au cœur de tout ce que nous faisons, sans compromis.',
      image: 'https://images.unsplash.com/photo-1552664730-d307ca884978?w=400&h=200&fit=crop'
    },
    {
      icon: '❤️', iconBg: '#f43f5e', title: 'Intégrité',
      description: "Nous construisons des relations durables basées sur la confiance, la transparence et l'honnêteté.",
      image: 'https://images.unsplash.com/photo-1521737604893-d14cc237f11d?w=400&h=200&fit=crop'
    },
    {
      icon: '👥', iconBg: '#22c55e', title: 'Collaboration',
      description: "Le travail d'équipe et le partenariat avec nos clients sont essentiels à notre succès commun.",
      image: 'https://images.unsplash.com/photo-1518770660439-4636190af475?w=400&h=200&fit=crop'
    },
  ];

  readonly teamStats = [
    { value: '50+',  label: 'Collaborateurs Experts' },
    { value: '15+',  label: "Années d'Expérience" },
    { value: '200+', label: 'Projets Réalisés' },
    { value: '98%',  label: 'Satisfaction Client' },
  ];

  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: object
  ) {}

  ngAfterViewInit() {
    if (!isPlatformBrowser(this.platformId)) return;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            entry.target.classList.add('visible');
            observer.unobserve(entry.target); // anime une seule fois
          }
        });
      },
      { threshold: 0.12 }
    );

    // Observer tous les éléments avec classe reveal*
    const targets = document.querySelectorAll(
      '.reveal, .reveal-left, .reveal-right, .reveal-scale'
    );
    targets.forEach(el => observer.observe(el));
  }

  ngOnInit() {
    this.http.get<TeamMember[]>('http://localhost:5057/api/team-members?isActiveOnly=true')
      .subscribe({
        next: (members) => { this.teamMembers.set(members); this.isLoadingTeam.set(false); },
        error: () => { this.isLoadingTeam.set(false); }
      });
  }

  getInitials(firstName: string, lastName: string): string {
    return ((firstName?.[0] || '') + (lastName?.[0] || '')).toUpperCase();
  }

  prevSlide() {
    const m = this.teamMembers();
    if (m.length <= 1) return;
    this.currentSlide.update(s => (s - 1 + m.length) % m.length);
  }

  nextSlide() {
    const m = this.teamMembers();
    if (m.length <= 1) return;
    this.currentSlide.update(s => (s + 1) % m.length);
  }

  // ── Retourne seulement les membres disponibles (max 3) sans dupliquer ──────
  getVisibleMembers(): TeamMember[] {
    const m = this.teamMembers();
    if (!m.length) return [];

    const visibleCount = Math.min(3, m.length); // max 3, sans dépasser la taille réelle
    const start = this.currentSlide();
    const result: TeamMember[] = [];

    for (let i = 0; i < visibleCount; i++) {
      const index = (start + i) % m.length;
      // Évite les doublons si moins de 3 membres
      if (!result.find(r => r.id === m[index].id)) {
        result.push(m[index]);
      }
    }

    return result;
  }

  // Nombre total de slides possibles
  get totalSlides(): number {
    return Math.max(0, this.teamMembers().length - 2);
  }

  get canNavigate(): boolean {
    return this.teamMembers().length > 3;
  }
}
