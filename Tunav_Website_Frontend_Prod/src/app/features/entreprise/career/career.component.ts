import {
  Component, OnInit, AfterViewInit, signal, computed,
  PLATFORM_ID, Inject
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

interface JobOffer {
  id: number;
  title: string;
  description?: string;
  requirements?: string;
  missions?: string;
  benefits?: string;
  process?: string;
  contractType: string;
  academicLevel: string;
  postType: string;
  location?: string;
  duration?: string;
  salary?: string;
  skills?: string;
  skillList: string[];
  deadline?: string;
  isActive: boolean;
  isArchived: boolean;
  isExpired: boolean;
  createdAt: string;
}

@Component({
  selector: 'app-career',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './career.component.html',
  styleUrl: './career.component.scss',
})
export class CareerComponent implements OnInit, AfterViewInit {
  private readonly api = 'http://localhost:5057';

  offers     = signal<JobOffer[]>([]);
  isLoading  = signal(true);
  selectedOffer = signal<JobOffer | null>(null);

  showDetail   = signal(false);
  showApply    = signal(false);
  showSuccess  = signal(false);
  isSubmitting = signal(false);
  submitError  = signal('');

  viewMode = signal<'list' | 'detail'>('list');

  filterContract  = '';
  filterAcademic  = '';
  filterPostType  = '';
  searchText      = '';

  form = {
    firstName: '', lastName: '', email: '', phone: '',
    cvFile: null as File | null,
    motivationFile: null as File | null,
  };
  cvFileName = '';
  motivationFileName = '';

  filteredOffers = computed(() => {
    let list = this.offers().filter(o => o.isActive && !o.isArchived);
    if (this.filterContract) list = list.filter(o => o.contractType === this.filterContract);
    if (this.filterAcademic) list = list.filter(o => o.academicLevel === this.filterAcademic);
    if (this.filterPostType) list = list.filter(o => o.postType === this.filterPostType);
    if (this.searchText.trim()) {
      const q = this.searchText.toLowerCase();
      list = list.filter(o =>
        o.title.toLowerCase().includes(q) ||
        o.description?.toLowerCase().includes(q) ||
        o.skillList.some(s => s.toLowerCase().includes(q))
      );
    }
    return list;
  });

  employCount = computed(() => this.offers().filter(o => o.contractType === 'CDI' || o.contractType === 'CDD').length);
  stageCount  = computed(() => this.offers().filter(o => o.postType === 'Stage' || o.postType === 'StageEte' || o.postType === 'PFE').length);

  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: object
  ) {}

  /** Parse les compétences depuis skills (string) ou skillList (tableau) */
  private parseSkillList(o: any): string[] {
    // Priorité 1 : champ skills (string sauvegardée depuis le backoffice)
    if (o.skills && typeof o.skills === 'string' && o.skills.trim()) {
      // Essayer de splitter sur virgule ou point-virgule
      const parts = o.skills.split(/[,;]+/).map((s: string) => s.trim()).filter(Boolean);
      // Si un seul élément → les compétences sont séparées par espace (pas de séparateur saisi)
      // On retourne quand même tel quel pour ne pas casser les noms multi-mots
      return parts;
    }

    // Priorité 2 : skillList tableau (si le backend le construit)
    const fromList: string[] = (o.skillList || [])
      .flatMap((s: string) => s.split(/[,;\n]+/))
      .map((s: string) => s.trim())
      .filter(Boolean);

    return fromList;
  }

  ngOnInit() {
    this.http.get<JobOffer[]>(`${this.api}/api/job-offers?isActive=true`).subscribe({
      next: offers => {
        const normalized = offers.map(o => ({
          ...o,
          skillList: this.parseSkillList(o)
        }));
        this.offers.set(normalized);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  ngAfterViewInit() {
    if (!isPlatformBrowser(this.platformId)) return;
    this.initReveal();
  }

  private initReveal() {
    const obs = new IntersectionObserver(entries => {
      entries.forEach(e => { if (e.isIntersecting) { e.target.classList.add('visible'); obs.unobserve(e.target); } });
    }, { threshold: 0.08 });
    setTimeout(() => document.querySelectorAll('.ev-reveal,.ev-reveal-scale,.ev-reveal-left').forEach(el => obs.observe(el)), 100);
  }

  openDetail(offer: JobOffer) {
    this.selectedOffer.set(offer);
    this.viewMode.set('detail');
    if (isPlatformBrowser(this.platformId)) {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  closeDetail() {
    this.viewMode.set('list');
    this.showDetail.set(false);
    if (isPlatformBrowser(this.platformId)) document.body.style.overflow = '';
  }

  openApply(offer?: JobOffer) {
    if (offer) this.selectedOffer.set(offer);
    this.showApply.set(true);
    this.showDetail.set(false);
    this.resetForm();
    if (isPlatformBrowser(this.platformId)) document.body.style.overflow = 'hidden';
  }

  closeApply() {
    this.showApply.set(false);
    if (isPlatformBrowser(this.platformId)) document.body.style.overflow = '';
  }

  closeSuccess() {
    this.showSuccess.set(false);
    if (isPlatformBrowser(this.platformId)) document.body.style.overflow = '';
  }

  onCvSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    if (file.type !== 'application/pdf') { this.submitError.set('Le CV doit être un fichier PDF.'); return; }
    if (file.size > 5_000_000) { this.submitError.set('Le CV ne doit pas dépasser 5 MB.'); return; }
    this.form.cvFile = file;
    this.cvFileName = file.name;
    this.submitError.set('');
  }

  onMotivationSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    const allowed = ['application/pdf', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
    if (!allowed.includes(file.type)) { this.submitError.set('La lettre de motivation doit être PDF, DOC ou DOCX.'); return; }
    if (file.size > 5_000_000) { this.submitError.set('La lettre de motivation ne doit pas dépasser 5 MB.'); return; }
    this.form.motivationFile = file;
    this.motivationFileName = file.name;
    this.submitError.set('');
  }

  async submitApplication() {
    if (!this.form.firstName || !this.form.lastName || !this.form.email) {
      this.submitError.set('Veuillez remplir tous les champs obligatoires.');
      return;
    }
    if (!this.form.cvFile) { this.submitError.set('Le CV est obligatoire.'); return; }
    if (!this.form.motivationFile) { this.submitError.set('La lettre de motivation est obligatoire.'); return; }

    this.isSubmitting.set(true);
    this.submitError.set('');

    const fd = new FormData();
    fd.append('jobOfferId', String(this.selectedOffer()!.id));
    fd.append('firstName', this.form.firstName);
    fd.append('lastName', this.form.lastName);
    fd.append('email', this.form.email);
    if (this.form.phone) fd.append('phone', this.form.phone);
    fd.append('cvFile', this.form.cvFile);
    fd.append('motivationLetterFile', this.form.motivationFile!);

    this.http.post(`${this.api}/api/job-applications`, fd).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.showApply.set(false);
        this.showSuccess.set(true);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.submitError.set(err?.error?.message || 'Une erreur est survenue. Veuillez réessayer.');
      }
    });
  }

  resetForm() {
    this.form = { firstName: '', lastName: '', email: '', phone: '', cvFile: null, motivationFile: null };
    this.cvFileName = '';
    this.motivationFileName = '';
    this.submitError.set('');
  }

  contractLabel(c: string) {
    const m: Record<string, string> = { CDI: 'CDI', CDD: 'CDD', Stage: 'Stage', Freelance: 'Freelance', Alternance: 'Alternance' };
    return m[c] || c;
  }

  postTypeLabel(p: string) {
    const m: Record<string, string> = { Emploi: 'Emploi', Stage: 'Stage', StageEte: "Stage d'été", PFE: 'PFE' };
    return m[p] || p;
  }

  contractClass(c: string) {
    const m: Record<string, string> = { CDI: 'emp-badge--cdi', CDD: 'emp-badge--cdd', Stage: 'emp-badge--stage', PFE: 'emp-badge--pfe', StageEte: 'emp-badge--stage-ete', Freelance: 'emp-badge--freelance', Alternance: 'emp-badge--alternance' };
    return m[c] || '';
  }

  formatDeadline(d?: string) {
    if (!d) return null;
    return new Date(d).toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  isDeadlineSoon(d?: string) {
    if (!d) return false;
    const diff = new Date(d).getTime() - Date.now();
    return diff > 0 && diff < 7 * 24 * 60 * 60 * 1000;
  }

  parseLines(text?: string): string[] {
    if (!text) return [];
    return text.split('\n').map(l => l.trim()).filter(Boolean);
  }

  getBenefits(offer: JobOffer): string[] { return this.parseLines(offer.benefits); }
  getProcess(offer: JobOffer): string[] { return this.parseLines(offer.process); }
  getStaticBenefits(offer: JobOffer): string[] { return this.getBenefits(offer); }
  getStaticProcess(offer: JobOffer): string[] { return this.getProcess(offer); }
}
