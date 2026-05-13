import { isPlatformBrowser, CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, AfterViewInit, signal, PLATFORM_ID, Inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

interface Client { id: number; name: string; logoUrl?: string; isActive: boolean; }
interface Partner { id: number; name: string; logoUrl?: string; contactPerson?: string; country?: string; description?: string; isActive: boolean; }
interface BlogArticle { id: number; title: string; summary?: string; coverImageUrl?: string; publishedAt?: string; createdAt: string; categoryName?: string; isActive: boolean; }

interface SolutionFunctionality { title: string; description: string; }
interface Solution {
  id: number; title: string; description: string;
  coverImageUrl?: string; youtubeUrl?: string;
  functionalitiesJson?: string; isActive: boolean;
}

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

interface IndustrySector {
  id: number;
  title: string;
  description: string;
  imageUrl: string;
  displayOrder: number;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent implements OnInit, AfterViewInit {
  private readonly api = 'http://localhost:5057';

  // ── Hero ──────────────────────────────────────────────────────────────────
  activeSlide = 0;
  slides = [
    {
      tag: 'GPS TRACKING',
      title: 'Accelerate your digital<br><span>transformation with GPS</span><br>tracking',
      subtitle: 'We support businesses and individuals with smart GPS tracking and fleet management solutions designed to optimize their performance.',
      image: 'https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=600&h=420&fit=crop',
    },
    {
      tag: 'FLEET MANAGEMENT',
      title: 'Gérez votre flotte<br><span>en temps réel</span><br>avec TUNAV',
      subtitle: 'Suivi GPS avancé, alertes intelligentes et rapports détaillés pour une gestion optimale de vos véhicules et actifs.',
      image: 'https://images.unsplash.com/photo-1566576912321-d58ddd7a6088?w=600&h=420&fit=crop',
    },
    {
      tag: 'IOT SOLUTIONS',
      title: 'Technologies IoT<br><span>de pointe pour</span><br>l\'Afrique du Nord',
      subtitle: 'Solutions innovantes adaptées aux besoins spécifiques du marché tunisien et africain depuis plus de 20 ans.',
      image: 'https://images.unsplash.com/photo-1518770660439-4636190af475?w=600&h=420&fit=crop',
    },
  ];

  // ── À Propos ──────────────────────────────────────────────────────────────
  readonly values = [
    { icon: '💡', iconBg: '#4f6ef7', title: 'Innovation', description: 'Nous repoussons constamment les limites de la technologie pour offrir les solutions les plus avancées du marché.', image: 'https://images.unsplash.com/photo-1581091226825-a6a2a5aee158?w=400&h=200&fit=crop' },
    { icon: '✨', iconBg: '#a855f7', title: 'Excellence',  description: 'La qualité et la satisfaction client sont au cœur de tout ce que nous faisons, sans compromis.', image: 'https://images.unsplash.com/photo-1552664730-d307ca884978?w=400&h=200&fit=crop' },
    { icon: '❤️', iconBg: '#f43f5e', title: 'Intégrité',  description: "Nous construisons des relations durables basées sur la confiance, la transparence et l'honnêteté.", image: 'https://images.unsplash.com/photo-1521737604893-d14cc237f11d?w=400&h=200&fit=crop' },
    { icon: '👥', iconBg: '#22c55e', title: 'Collaboration', description: "Le travail d'équipe et le partenariat avec nos clients sont essentiels à notre succès commun.", image: 'https://images.unsplash.com/photo-1518770660439-4636190af475?w=400&h=200&fit=crop' },
  ];
  readonly teamStats = [
    { value: '50+',  label: 'Collaborateurs Experts' },
    { value: '15+',  label: "Années d'Expérience" },
    { value: '200+', label: 'Projets Réalisés' },
    { value: '98%',  label: 'Satisfaction Client' },
  ];

  // ── Secteurs d'Activité (carousel — données depuis API backoffice) ────────
  activeSector     = 0;
  sectors          = signal<{ title: string; desc: string; image: string }[]>([]);
  isLoadingSectors = signal(true);

  getVisibleSectors() {
    const list = this.sectors();
    if (!list.length) return [];
    const r = [];
    for (let i = 0; i < 3; i++) r.push(list[(this.activeSector + i) % list.length]);
    return r;
  }
  prevSector() {
    const l = this.sectors().length;
    if (l) this.activeSector = (this.activeSector - 1 + l) % l;
  }
  nextSector() {
    const l = this.sectors().length;
    if (l) this.activeSector = (this.activeSector + 1) % l;
  }

  // ── Solutions depuis API ──────────────────────────────────────────────────
  solutions = signal<Solution[]>([]);
  activeSolution = 0;
  prevSolution() { const l = this.solutions().length; if (l) this.activeSolution = (this.activeSolution - 1 + l) % l; }
  nextSolution() { const l = this.solutions().length; if (l) this.activeSolution = (this.activeSolution + 1) % l; }

  getFunctionalities(sol: Solution): SolutionFunctionality[] {
    if (!sol.functionalitiesJson) return [];
    try { return JSON.parse(sol.functionalitiesJson); } catch { return []; }
  }

  getSafeYoutubeUrl(url?: string): SafeResourceUrl {
    if (!url) return this.sanitizer.bypassSecurityTrustResourceUrl('');
    let id = url;
    if (url.includes('youtu.be/')) id = url.split('youtu.be/').pop()?.split('?')[0] || url;
    else if (url.includes('v=')) id = url.split('v=').pop()?.split('&')[0] || url;
    return this.sanitizer.bypassSecurityTrustResourceUrl(`https://www.youtube-nocookie.com/embed/${id}`);
  }

  getSolutionIcon(title: string): string {
    const t = title.toLowerCase();
    if (t.includes('rfid')) return '📡';
    if (t.includes('trace') || t.includes('gps')) return '🛰️';
    if (t.includes('disjoncteur') || t.includes('électrique')) return '⚡';
    if (t.includes('fuel') || t.includes('carburant')) return '⛽';
    if (t.includes('dashcam') || t.includes('camera')) return '📷';
    return '🔧';
  }

  // ── Pourquoi TUNAV ────────────────────────────────────────────────────────
  whyReasons = [
    { icon: '🏆', color: 'linear-gradient(135deg,#1a2a6c,#2940a0)', title: '20+ ans d\'expertise', desc: 'Leader reconnu en solutions IoT et GPS en Afrique du Nord depuis 2004.' },
    { icon: '🚀', color: 'linear-gradient(135deg,#00c8d4,#0099cc)', title: 'Innovation continue', desc: 'Technologies de pointe constamment mises à jour pour rester à la frontière de l\'innovation.' },
    { icon: '🛡️', color: 'linear-gradient(135deg,#22c55e,#16a34a)', title: 'Fiabilité prouvée', desc: '98% de satisfaction client et plus de 10 000 appareils déployés avec succès.' },
    { icon: '🤝', color: 'linear-gradient(135deg,#f59e0b,#d97706)', title: 'Support 24/7', desc: 'Notre équipe d\'experts est disponible à toute heure pour vous accompagner.' },
    { icon: '🌍', color: 'linear-gradient(135deg,#8b5cf6,#6d28d9)', title: 'Couverture régionale', desc: 'Présence et expertise couvrant la Tunisie et l\'ensemble de l\'Afrique du Nord.' },
    { icon: '💡', color: 'linear-gradient(135deg,#f43f5e,#e11d48)', title: 'Solutions sur mesure', desc: 'Chaque solution est adaptée aux besoins spécifiques de votre secteur.' },
  ];

  // ── Solutions Placeholder ────────────────────────────────────────────────
  readonly solutionsPlaceholder = [
    { icon: '🛰️', title: 'GPS Tracking', desc: 'Suivi en temps réel de vos véhicules et actifs mobiles.' },
    { icon: '📡', title: 'RFID & IoT', desc: 'Identification et connectivité intelligente pour vos équipements.' },
    { icon: '⛽', title: 'Gestion Carburant', desc: 'Contrôle et optimisation de la consommation de carburant.' },
    { icon: '📷', title: 'DashCam', desc: 'Surveillance vidéo embarquée pour la sécurité de votre flotte.' },
    { icon: '⚡', title: 'Disjoncteur Électronique', desc: 'Protection intelligente de vos installations électriques.' },
    { icon: '🔧', title: 'Maintenance Prédictive', desc: "Anticipez les pannes grâce à l'analyse des données IoT." },
  ];

  // ── Signals ───────────────────────────────────────────────────────────────
  clients          = signal<Client[]>([]);
  isLoadingClients = signal(true);
  partners         = signal<Partner[]>([]);
  isLoadingPartners = signal(true);
  activePartner    = 0;
  blogArticles     = signal<BlogArticle[]>([]);
  isLoadingBlog    = signal(true);
  homeContactSuccess    = signal(false);
  homeContactSubmitting = signal(false);
  homeContactError      = signal('');
  homeForm = { firstName: '', lastName: '', email: '', phone: '', subject: '', message: '', consent: false };

  // ── Team (fusionné depuis about.component.ts) ─────────────────────────────
  teamMembers      = signal<TeamMember[]>([]);
  isLoadingTeam    = signal(true);
  currentTeamSlide = signal(0);

  constructor(
    private http: HttpClient,
    private router: Router,
    private sanitizer: DomSanitizer,
    @Inject(PLATFORM_ID) private platformId: object
  ) {}

  ngOnInit() {
    if (!isPlatformBrowser(this.platformId)) return;

    this.http.get<Client[]>(`${this.api}/api/clients?isActive=true`).subscribe({ next: (d: Client[]) => { this.clients.set(d); this.isLoadingClients.set(false); }, error: () => this.isLoadingClients.set(false) });
    this.http.get<Partner[]>(`${this.api}/api/partners?isActive=true`).subscribe({ next: (d: Partner[]) => { this.partners.set(d); this.isLoadingPartners.set(false); }, error: () => this.isLoadingPartners.set(false) });
    this.http.get<Solution[]>(`${this.api}/api/solutions?isActive=true`).subscribe({ next: (d: Solution[]) => this.solutions.set(d), error: () => { } });
    this.http.get<BlogArticle[]>(`${this.api}/api/blog-articles?isActive=true`).subscribe({
      next: (d: BlogArticle[]) => { this.blogArticles.set(d.slice(0, 6)); this.isLoadingBlog.set(false); if (isPlatformBrowser(this.platformId)) setTimeout(() => this.initRevealObserver(), 200); },
      error: () => this.isLoadingBlog.set(false)
    });
    this.http.get<TeamMember[]>(`${this.api}/api/team-members?isActiveOnly=true`).subscribe({
      next: (d: TeamMember[]) => { this.teamMembers.set(d); this.isLoadingTeam.set(false); },
      error: () => this.isLoadingTeam.set(false),
    });

    // ── Chargement des secteurs depuis le backoffice ───────────────────────
    this.http.get<IndustrySector[]>(`${this.api}/api/industry-sectors`).subscribe({
      next: (data: IndustrySector[]) => {
        this.sectors.set(data.map(s => ({
          title: s.title,
          desc:  s.description,
          image: s.imageUrl || 'https://images.unsplash.com/photo-1518770660439-4636190af475?w=500&h=300&fit=crop'
        })));
        this.isLoadingSectors.set(false);
      },
      error: () => {
        // Fallback sur des données par défaut si l'API est indisponible
        this.sectors.set([
          { title: 'SANTÉ & MÉDICAL',         desc: "Solutions sécurisées pour la gestion des données patients, l'optimisation des flux et la conformité réglementaire.",  image: 'https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=500&h=300&fit=crop' },
          { title: 'INDUSTRIE & MANUFACTURE',  desc: 'Automatisation de la chaîne de production, maintenance prédictive et suivi logistique en temps réel.',                image: 'https://images.unsplash.com/photo-1581092160562-40aa08e78837?w=500&h=300&fit=crop' },
          { title: 'SERVICES FINANCIERS',      desc: "Plateformes robustes pour les transactions sécurisées, l'analyse de risques et l'expérience client digitale.",        image: 'https://images.unsplash.com/photo-1454165804606-c3d57bc86b40?w=500&h=300&fit=crop' },
          { title: 'TRANSPORT & LOGISTIQUE',   desc: 'Optimisation des routes, suivi de flotte en temps réel et gestion efficace de la supply chain.',                     image: 'https://images.unsplash.com/photo-1601584115197-04ecc0da31d7?w=500&h=300&fit=crop' },
          { title: 'ADMINISTRATION PUBLIQUE',  desc: "Digitalisation des services publics et gestion intelligente des actifs de l'État.",                                   image: 'https://images.unsplash.com/photo-1568992687947-868a62a9f521?w=500&h=300&fit=crop' },
          { title: 'ÉNERGIE & UTILITIES',      desc: 'Surveillance et contrôle des infrastructures énergétiques avec des solutions IoT avancées.',                         image: 'https://images.unsplash.com/photo-1473341304170-971dccb5ac1e?w=500&h=300&fit=crop' },
          { title: 'AGRICULTURE',              desc: 'Agriculture de précision grâce aux capteurs IoT et au suivi en temps réel des équipements agricoles.',                image: 'https://images.unsplash.com/photo-1625246333195-78d9c38ad449?w=500&h=300&fit=crop' },
        ]);
        this.isLoadingSectors.set(false);
      }
    });

    setInterval(() => { this.activeSlide = (this.activeSlide + 1) % this.slides.length; }, 5000);
  }

  ngAfterViewInit() {
    if (!isPlatformBrowser(this.platformId)) return;
    this.initRevealObserver();
    this.initRippleButtons();
  }

  private initRippleButtons(): void {
    const buttons = document.querySelectorAll<HTMLElement>('.hero-btn');
    buttons.forEach(btn => {
      btn.addEventListener('click', (e: MouseEvent) => {
        const rect = btn.getBoundingClientRect();
        const rx = ((e.clientX - rect.left) / rect.width) * 100;
        const ry = ((e.clientY - rect.top) / rect.height) * 100;
        btn.style.setProperty('--rx', `${rx}%`);
        btn.style.setProperty('--ry', `${ry}%`);
        btn.classList.remove('ripple-active');
        void btn.offsetWidth; // force reflow pour relancer l'animation
        btn.classList.add('ripple-active');
        // Nettoyer la classe après la fin de l'animation
        setTimeout(() => btn.classList.remove('ripple-active'), 650);
      });
    });
  }

  private initRevealObserver() {
    const obs = new IntersectionObserver(entries => {
      entries.forEach(e => { if (e.isIntersecting) { e.target.classList.add('visible'); obs.unobserve(e.target); } });
    }, { threshold: 0.05 });
    document.querySelectorAll('.reveal,.reveal-left,.reveal-right,.reveal-scale,.home-blog-reveal,.home-blog-section-reveal').forEach(el => obs.observe(el));
  }

  scrollToContact(event: Event) {
    event.preventDefault();
    const el = document.getElementById('contact');
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  prevPartner() { const p = this.partners(); if (p.length) this.activePartner = (this.activePartner - 1 + p.length) % p.length; }
  nextPartner() { const p = this.partners(); if (p.length) this.activePartner = (this.activePartner + 1) % p.length; }
  getInitials(name: string): string { return name.split(' ').map(w => w[0] || '').join('').toUpperCase().slice(0, 2); }

  navigateToBlog(id: number) { this.router.navigate(['/news/blog', id]); }
  formatBlogDate(dateStr?: string): string {
    if (!dateStr) return '';
    try { return new Date(dateStr).toLocaleDateString('fr-FR', { day: 'numeric', month: 'short', year: 'numeric' }); } catch { return ''; }
  }

  submitHomeContact() {
    const f = this.homeForm;
    if (!f.firstName || !f.lastName || !f.email || !f.subject || !f.message) { this.homeContactError.set('Veuillez remplir tous les champs obligatoires.'); return; }
    if (!f.consent) { this.homeContactError.set('Vous devez accepter la politique de confidentialité.'); return; }
    this.homeContactSubmitting.set(true); this.homeContactError.set('');
    this.http.post(`${this.api}/api/contact`, { ...f }).subscribe({
      next: () => { this.homeContactSubmitting.set(false); this.homeContactSuccess.set(true); this.homeForm = { firstName: '', lastName: '', email: '', phone: '', subject: '', message: '', consent: false }; },
      error: (err: unknown) => {
        const message =
          typeof err === 'object' && err !== null && 'error' in err
            ? (err as { error?: { message?: string } }).error?.message
            : undefined;
        this.homeContactSubmitting.set(false);
        this.homeContactError.set(message || 'Une erreur est survenue.');
      },
    });
  }

  // ── Méthodes Team (fusionnées depuis about.component.ts) ─────────────────
  getMemberInitials(firstName: string, lastName: string): string {
    return ((firstName?.[0] || '') + (lastName?.[0] || '')).toUpperCase();
  }

  prevTeamSlide() {
    const m = this.teamMembers();
    if (m.length <= 1) return;
    this.currentTeamSlide.update(s => (s - 1 + m.length) % m.length);
  }

  nextTeamSlide() {
    const m = this.teamMembers();
    if (m.length <= 1) return;
    this.currentTeamSlide.update(s => (s + 1) % m.length);
  }

  getVisibleMembers(): TeamMember[] {
    const m = this.teamMembers();
    if (!m.length) return [];
    const visibleCount = Math.min(3, m.length);
    const start = this.currentTeamSlide();
    const result: TeamMember[] = [];
    for (let i = 0; i < visibleCount; i++) {
      const index = (start + i) % m.length;
      if (!result.find(r => r.id === m[index].id)) {
        result.push(m[index]);
      }
    }
    return result;
  }

  get totalTeamSlides(): number {
    return Math.max(0, this.teamMembers().length - 2);
  }
}
