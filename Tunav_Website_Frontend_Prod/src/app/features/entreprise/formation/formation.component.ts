import { Component, OnInit, AfterViewInit, signal, PLATFORM_ID, Inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

// ── Formulaire 1 : Demande de collaboration (partenaire formation) ────────────
interface CollabForm {
  organization: string;
  fullName: string;
  phone: string;
  email: string;
  address: string;
  message: string;
}

// ── Formulaire 2 : Demande de formation ──────────────────────────────────────
interface FormationForm {
  organization: string;
  fullName: string;
  phone: string;
  email: string;
  address: string;
  message: string;
  isHomologueMalek: boolean;
}

// ── Partenaire formation ──────────────────────────────────────────────────────
interface TrainingPartner {
  id: number;
  name: string;
  domain: string;
  icon?: string;
  imageUrl?: string;        // ← URL image optionnelle depuis le backoffice
  isActive: boolean;
  displayOrder: number;
}

// ── Témoignage ────────────────────────────────────────────────────────────────
interface Testimonial {
  id: number;
  authorName: string;
  authorRole: string;
  company?: string;
  avatar?: string;
  content: string;
  rating: number;
  isActive: boolean;
  displayOrder: number;
}

@Component({
  selector: 'app-pole-formation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './formation.component.html',
  styleUrl: './formation.component.scss',
})
export class PoleFormationComponent implements OnInit, AfterViewInit {

  private readonly API = environment.apiBaseUrl;

  // ── Data signals ─────────────────────────────────────────────────────────
  partners            = signal<TrainingPartner[]>([]);
  testimonials        = signal<Testimonial[]>([]);
  isLoadingPartners   = signal(true);
  isLoadingTestimonials = signal(true);

  // ── Formulaire 1 (Demande de collaboration) ──────────────────────────────
  showCollabModal   = signal(false);
  collabForm: CollabForm = this.emptyCollabForm();
  collabErrors: Record<string, string> = {};
  isSubmittingCollab = signal(false);
  collabSuccess      = signal(false);
  collabError        = signal('');
  collabFiles: File[] = [];

  // ── Formulaire 2 (Demande de formation) ──────────────────────────────────
  showFormationModal   = signal(false);
  formationForm: FormationForm = this.emptyFormationForm();
  formationErrors: Record<string, string> = {};
  isSubmittingFormation = signal(false);
  formationSuccess      = signal(false);
  formationError        = signal('');
  formationFiles: File[] = [];

  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: object
  ) {}

  ngOnInit() {
    this.loadPartners();
    this.loadTestimonials();
  }

  ngAfterViewInit() {
    if (!isPlatformBrowser(this.platformId)) return;
    const observer = new IntersectionObserver(entries => {
      entries.forEach(e => { if (e.isIntersecting) { e.target.classList.add('visible'); observer.unobserve(e.target); } });
    }, { threshold: 0.1 });
    setTimeout(() => {
      document.querySelectorAll('.pf-reveal').forEach(el => observer.observe(el));
    }, 100);
  }

  loadPartners() {
    this.isLoadingPartners.set(true);
    this.http.get<TrainingPartner[]>(`${this.API}/training-partners?isActive=true`).subscribe({
      next: (list) => { this.partners.set(list); this.isLoadingPartners.set(false); },
      error: () => { this.isLoadingPartners.set(false); }
    });
  }

  loadTestimonials() {
    this.isLoadingTestimonials.set(true);
    this.http.get<Testimonial[]>(`${this.API}/testimonials?isActive=true`).subscribe({
      next: (list) => { this.testimonials.set(list); this.isLoadingTestimonials.set(false); },
      error: () => { this.isLoadingTestimonials.set(false); }
    });
  }

  getStars(rating: number): string {
    return '★'.repeat(Math.min(5, Math.max(1, rating)));
  }

  /** Retourne une image Unsplash thématique selon le domaine du partenaire.
   *  Si le partenaire a une imageUrl définie dans le backoffice, elle est prioritaire. */
  getPartnerImage(partner: TrainingPartner): string {
    if (partner.imageUrl) return partner.imageUrl;
    const domain = (partner.domain || '').toLowerCase();
    if (domain.includes('data') || domain.includes('ia') || domain.includes('intelligence'))
      return 'https://images.unsplash.com/photo-1620712943543-bcc4688e7485?w=400&h=260&fit=crop';
    if (domain.includes('cloud') || domain.includes('devops') || domain.includes('linux'))
      return 'https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=400&h=260&fit=crop';
    if (domain.includes('web') || domain.includes('react') || domain.includes('angular') || domain.includes('front'))
      return 'https://images.unsplash.com/photo-1593720213428-28a5b9e94613?w=400&h=260&fit=crop';
    if (domain.includes('marketing') || domain.includes('digital') || domain.includes('communication'))
      return 'https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=400&h=260&fit=crop';
    if (domain.includes('projet') || domain.includes('management') || domain.includes('gestion'))
      return 'https://images.unsplash.com/photo-1507925921958-8a62f3d1a50d?w=400&h=260&fit=crop';
    if (domain.includes('comptab') || domain.includes('finance') || domain.includes('fiscal'))
      return 'https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=400&h=260&fit=crop';
    if (domain.includes('securit') || domain.includes('cyber') || domain.includes('réseau'))
      return 'https://images.unsplash.com/photo-1563986768494-4dee2763ff3f?w=400&h=260&fit=crop';
    if (domain.includes('langue') || domain.includes('anglais') || domain.includes('français'))
      return 'https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=400&h=260&fit=crop';
    // Fallback générique formation
    return 'https://images.unsplash.com/photo-1524178232363-1fb2b075b655?w=400&h=260&fit=crop';
  }

  // ══════════════════════════════════════════════════════════════════════════
  //  MODAL FORMULAIRE 1 — Demande de collaboration
  //  → enregistrée dans backoffice onglet "Demandes de collaboration"
  //    CollaborationType = "Collaboration"
  // ══════════════════════════════════════════════════════════════════════════

  openCollabModal() {
    this.collabForm   = this.emptyCollabForm();
    this.collabErrors = {};
    this.collabError.set('');
    this.collabSuccess.set(false);
    this.collabFiles  = [];
    this.showCollabModal.set(true);
    this.lockScroll();
  }

  closeCollabModal() {
    this.showCollabModal.set(false);
    this.collabSuccess.set(false);
    this.unlockScroll();
  }

  onCollabFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files) this.collabFiles = Array.from(input.files);
  }

  async submitCollab() {
    this.collabErrors = {};
    this.collabError.set('');

    if (!this.collabForm.organization?.trim()) this.collabErrors['organization'] = "Le nom de l'établissement est obligatoire.";
    if (!this.collabForm.fullName?.trim())     this.collabErrors['fullName']     = 'Le responsable est obligatoire.';
    if (!this.collabForm.phone?.trim())        this.collabErrors['phone']        = 'Le téléphone est obligatoire.';
    if (!this.collabForm.email?.trim())        this.collabErrors['email']        = "L'email est obligatoire.";
    if (!this.collabForm.message?.trim())      this.collabErrors['message']      = 'La description est obligatoire.';

    if (Object.keys(this.collabErrors).length) return;

    this.isSubmittingCollab.set(true);

    // Upload fichiers si présents
    let attachmentNames: string | null = null;
    if (this.collabFiles.length > 0) {
      try {
        const fd = new FormData();
        this.collabFiles.forEach(f => fd.append('files', f));
        const res: any = await this.http.post(`${this.API}/uploads/collaboration`, fd).toPromise();
        attachmentNames = res?.files?.join(',') || null;
      } catch {
        this.isSubmittingCollab.set(false);
        this.collabError.set("Erreur lors de l'envoi des fichiers.");
        return;
      }
    }

    const body = {
      eventId:           null,
      organization:      this.collabForm.organization.trim(),
      fullName:          this.collabForm.fullName.trim(),
      phone:             this.collabForm.phone.trim(),
      email:             this.collabForm.email.trim(),
      address:           this.collabForm.address?.trim() || null,
      message:           this.collabForm.message.trim(),
      attachmentNames,
      collaborationType: 'Collaboration',   // → onglet "Demandes de collaboration"
      isHomologueMalek:  null,
    };

    this.http.post(`${this.API}/collaborations`, body).subscribe({
      next: () => {
        this.isSubmittingCollab.set(false);
        this.collabSuccess.set(true);
      },
      error: (err) => {
        this.isSubmittingCollab.set(false);
        this.collabError.set(err?.error?.message || 'Une erreur est survenue. Veuillez réessayer.');
      }
    });
  }

  // ══════════════════════════════════════════════════════════════════════════
  //  MODAL FORMULAIRE 2 — Demande de formation
  //  → enregistrée dans backoffice onglet "Demandes de formation"
  //    CollaborationType = "DemandeFormation"
  // ══════════════════════════════════════════════════════════════════════════

  openFormationModal() {
    this.formationForm   = this.emptyFormationForm();
    this.formationErrors = {};
    this.formationError.set('');
    this.formationSuccess.set(false);
    this.formationFiles  = [];
    this.showFormationModal.set(true);
    this.lockScroll();
  }

  closeFormationModal() {
    this.showFormationModal.set(false);
    this.formationSuccess.set(false);
    this.unlockScroll();
  }

  onFormationFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files) this.formationFiles = Array.from(input.files);
  }

  async submitFormation() {
    this.formationErrors = {};
    this.formationError.set('');

    if (!this.formationForm.organization?.trim()) this.formationErrors['organization'] = "Le nom de l'établissement est obligatoire.";
    if (!this.formationForm.fullName?.trim())     this.formationErrors['fullName']     = 'Le responsable est obligatoire.';
    if (!this.formationForm.phone?.trim())        this.formationErrors['phone']        = 'Le téléphone est obligatoire.';
    if (!this.formationForm.email?.trim())        this.formationErrors['email']        = "L'email est obligatoire.";
    if (!this.formationForm.message?.trim())      this.formationErrors['message']      = 'La description de la formation est obligatoire.';

    if (Object.keys(this.formationErrors).length) return;

    this.isSubmittingFormation.set(true);

    // Upload fichiers si présents
    let attachmentNames: string | null = null;
    if (this.formationFiles.length > 0) {
      try {
        const fd = new FormData();
        this.formationFiles.forEach(f => fd.append('files', f));
        const res: any = await this.http.post(`${this.API}/uploads/collaboration`, fd).toPromise();
        attachmentNames = res?.files?.join(',') || null;
      } catch {
        this.isSubmittingFormation.set(false);
        this.formationError.set("Erreur lors de l'envoi des fichiers.");
        return;
      }
    }

    const body = {
      eventId:           null,
      organization:      this.formationForm.organization.trim(),
      fullName:          this.formationForm.fullName.trim(),
      phone:             this.formationForm.phone.trim(),
      email:             this.formationForm.email.trim(),
      address:           this.formationForm.address?.trim() || null,
      message:           this.formationForm.message.trim(),
      attachmentNames,
      collaborationType: 'DemandeFormation',  // → onglet "Demandes de formation"
      isHomologueMalek:  this.formationForm.isHomologueMalek,
    };

    this.http.post(`${this.API}/collaborations`, body).subscribe({
      next: () => {
        this.isSubmittingFormation.set(false);
        this.formationSuccess.set(true);
      },
      error: (err) => {
        this.isSubmittingFormation.set(false);
        this.formationError.set(err?.error?.message || 'Une erreur est survenue. Veuillez réessayer.');
      }
    });
  }

  // ── Helpers ──────────────────────────────────────────────────────────────

  private lockScroll() {
    if (isPlatformBrowser(this.platformId))
      document.body.style.overflow = 'hidden';
  }

  private unlockScroll() {
    if (isPlatformBrowser(this.platformId))
      document.body.style.overflow = '';
  }

  private emptyCollabForm(): CollabForm {
    return { organization: '', fullName: '', phone: '', email: '', address: '', message: '' };
  }

  private emptyFormationForm(): FormationForm {
    return { organization: '', fullName: '', phone: '', email: '', address: '', message: '', isHomologueMalek: false };
  }
}
