import { Component, OnInit, AfterViewInit, signal, PLATFORM_ID, Inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface EventDto {
  id: number;
  title: string;
  description: string | null;
  type: string;
  status: string;
  startDate: string;
  endDate: string | null;
  location: string | null;
  onlineLink: string | null;
  coverImageUrl: string | null;
  youtubeUrl: string | null;
  youtubeEmbedId: string | null;
  externalUrl: string | null;
  isArchived: boolean;
  isUpcoming: boolean;
  collaborationCount: number;
  registrationCount: number;   // ← AJOUT : nombre d'inscriptions
  participantCount?: number;
  isFeatured?: boolean;
}

interface CollabForm {
  organization: string;
  fullName: string;
  phone: string;
  email: string;
  address: string;
  message: string;
}

interface RegisterForm {
  fullName: string;
  email: string;
  phone: string;
  organization: string;
  message: string;
}

@Component({
  selector: 'app-news-event',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './news-event.component.html',
  styleUrl: './news-event.component.scss',
})
export class NewsEventComponent implements OnInit, AfterViewInit {

  private readonly API = 'http://localhost:5057/api';

  upcomingEvents    = signal<EventDto[]>([]);
  pastEvents        = signal<EventDto[]>([]);
  isLoadingUpcoming = signal(true);
  isLoadingPast     = signal(true);

  showModal      = signal(false);
  selectedEvent  = signal<EventDto | null>(null);
  isSubmitting   = signal(false);
  submitSuccess  = signal(false);
  submitError    = signal('');

  showRegisterModal = signal(false);
  registerEvent     = signal<EventDto | null>(null);
  isRegistering     = signal(false);
  registerSuccess   = signal(false);
  registerError     = signal('');
  registerForm: RegisterForm = this.emptyRegisterForm();
  registerErrors: Record<string, string> = {};

  form: CollabForm = this.emptyForm();
  fieldErrors: Record<string, string> = {};
  selectedFiles: File[] = [];

  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: object
  ) {}

  ngOnInit() {
    this.loadUpcoming();
    this.loadPast();
  }

  ngAfterViewInit() {
    if (!isPlatformBrowser(this.platformId)) return;
    setTimeout(() => this.initScrollReveal(), 120);
  }

  // ── SCROLL REVEAL (IntersectionObserver — inspiré AOS / michalsnik) ──────────
  private initScrollReveal() {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            entry.target.classList.add('visible');
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.1, rootMargin: '0px 0px -40px 0px' }
    );

    document.querySelectorAll('.ev-reveal, .ev-reveal-left, .ev-reveal-right, .ev-reveal-scale')
      .forEach(el => observer.observe(el));
  }

  private reinitReveal() {
    if (!isPlatformBrowser(this.platformId)) return;
    setTimeout(() => this.initScrollReveal(), 80);
  }

  // ── DATA ─────────────────────────────────────────────────────────────────────
  loadUpcoming() {
    this.isLoadingUpcoming.set(true);
    this.http.get<EventDto[]>(`${this.API}/events?status=Published&upcoming=true`).subscribe({
      next: (list) => {
        this.upcomingEvents.set(list.map((e, i) => ({ ...e, isFeatured: i === 0 })));
        this.isLoadingUpcoming.set(false);
        this.reinitReveal();
      },
      error: () => this.isLoadingUpcoming.set(false)
    });
  }

  loadPast() {
    this.isLoadingPast.set(true);
    this.http.get<EventDto[]>(`${this.API}/events?status=Archived`).subscribe({
      next: (list) => {
        this.pastEvents.set(list.slice(0, 6));
        this.isLoadingPast.set(false);
        this.reinitReveal();
      },
      error: () => this.isLoadingPast.set(false)
    });
  }

  // ── MODAL INSCRIPTION ─────────────────────────────────────────────────────────
  openRegisterModal(ev: EventDto) {
    this.registerEvent.set(ev);
    this.registerForm = this.emptyRegisterForm();
    this.registerErrors = {};
    this.registerError.set('');
    this.registerSuccess.set(false);
    this.showRegisterModal.set(true);
    this.lockScroll();
  }

  closeRegisterModal() {
    this.showRegisterModal.set(false);
    this.registerEvent.set(null);
    this.registerSuccess.set(false);
    this.unlockScroll();
  }

  submitRegister() {
    this.registerErrors = {};
    this.registerError.set('');

    if (!this.registerForm.fullName?.trim()) this.registerErrors['fullName'] = 'Le nom est obligatoire.';
    if (!this.registerForm.email?.trim())    this.registerErrors['email']    = "L'email est obligatoire.";
    if (!this.registerForm.phone?.trim())    this.registerErrors['phone']    = 'Le téléphone est obligatoire.';

    if (Object.keys(this.registerErrors).length) return;

    const ev = this.registerEvent();
    if (!ev) return;

    this.isRegistering.set(true);
    this.http.post(`${this.API}/registrations`, {
      eventId:      ev.id,
      fullName:     this.registerForm.fullName.trim(),
      email:        this.registerForm.email.trim(),
      phone:        this.registerForm.phone.trim(),
      organization: this.registerForm.organization?.trim() || null,
      message:      this.registerForm.message?.trim() || null,
    }).subscribe({
      next: () => {
        this.isRegistering.set(false);
        this.registerSuccess.set(true);
        // ← AJOUT : mettre à jour le compteur localement après inscription réussie
        this.upcomingEvents.update(events =>
          events.map(e => e.id === ev.id
            ? { ...e, registrationCount: e.registrationCount + 1 }
            : e
          )
        );
      },
      error: (err) => {
        this.isRegistering.set(false);
        this.registerError.set(err?.error?.message || 'Une erreur est survenue.');
      }
    });
  }

  // ── MODAL COLLABORATION ───────────────────────────────────────────────────────
  openCollabModal(ev: EventDto | null) {
    this.selectedEvent.set(ev);
    this.form = this.emptyForm();
    this.fieldErrors = {};
    this.submitError.set('');
    this.submitSuccess.set(false);
    this.selectedFiles = [];
    this.showModal.set(true);
    this.lockScroll();
  }

  closeModal() {
    this.showModal.set(false);
    this.selectedEvent.set(null);
    this.submitSuccess.set(false);
    this.unlockScroll();
  }

  async submitCollab() {
    this.fieldErrors = {};
    this.submitError.set('');

    if (!this.form.organization?.trim()) this.fieldErrors['organization'] = "Le nom de l'organisme est obligatoire.";
    if (!this.form.fullName?.trim())     this.fieldErrors['fullName']     = 'Le responsable est obligatoire.';
    if (!this.form.phone?.trim())        this.fieldErrors['phone']        = 'Le téléphone est obligatoire.';
    if (!this.form.email?.trim())        this.fieldErrors['email']        = "L'email est obligatoire.";
    if (!this.form.message?.trim())      this.fieldErrors['message']      = 'La description est obligatoire.';

    if (Object.keys(this.fieldErrors).length) return;

    this.isSubmitting.set(true);

    let attachmentNames: string | null = null;
    if (this.selectedFiles.length > 0) {
      try {
        const fd = new FormData();
        this.selectedFiles.forEach(f => fd.append('files', f));
        const res: any = await this.http.post(`${this.API}/uploads/collaboration`, fd).toPromise();
        attachmentNames = res?.files?.join(',') || null;
      } catch {
        this.isSubmitting.set(false);
        this.submitError.set("Erreur lors de l'envoi des fichiers.");
        return;
      }
    }

    this.http.post(`${this.API}/collaborations`, {
      eventId:           null,
      organization:      this.form.organization.trim(),
      fullName:          this.form.fullName.trim(),
      phone:             this.form.phone.trim(),
      email:             this.form.email.trim(),
      address:           this.form.address?.trim() || null,
      message:           this.form.message.trim(),
      attachmentNames,
      collaborationType: 'PropositionEvenement',
    }).subscribe({
      next: () => { this.isSubmitting.set(false); this.submitSuccess.set(true); },
      error: (err) => {
        this.isSubmitting.set(false);
        this.submitError.set(err?.error?.message || 'Une erreur est survenue.');
      }
    });
  }

  // ── HELPERS ──────────────────────────────────────────────────────────────────
  formatDate(dateStr: string | null): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('fr-FR', {
      day: '2-digit', month: 'long', year: 'numeric'
    });
  }

  formatTime(dateStr: string | null): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleTimeString('fr-FR', {
      hour: '2-digit', minute: '2-digit'
    });
  }

  getTypeLabel(type: string): string {
    const map: Record<string, string> = {
      Salon: 'Salon', Conference: 'Conférence',
      Partenariat: 'Partenariat', Interne: 'Interne', Autre: 'Autre'
    };
    return map[type] ?? type;
  }

  getTypeBadgeClass(type: string): Record<string, boolean> {
    return {
      'ev-type-salon':       type === 'Salon',
      'ev-type-conference':  type === 'Conference',
      'ev-type-partenariat': type === 'Partenariat',
      'ev-type-interne':     type === 'Interne',
      'ev-type-autre':       type === 'Autre',
      'ev-type-webinar':     type === 'Webinar',
      'ev-type-workshop':    type === 'Workshop',
      'ev-type-forum':       type === 'Forum',
    };
  }

  onFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.selectedFiles = Array.from(input.files);
      const el = document.getElementById('fileName');
      if (el) el.textContent = this.selectedFiles.map(f => f.name).join(', ');
    }
  }

  private lockScroll() {
    if (isPlatformBrowser(this.platformId)) document.body.style.overflow = 'hidden';
  }

  private unlockScroll() {
    if (isPlatformBrowser(this.platformId)) document.body.style.overflow = '';
  }

  private emptyRegisterForm(): RegisterForm {
    return { fullName: '', email: '', phone: '', organization: '', message: '' };
  }

  private emptyForm(): CollabForm {
    return { organization: '', fullName: '', phone: '', email: '', address: '', message: '' };
  }
}
