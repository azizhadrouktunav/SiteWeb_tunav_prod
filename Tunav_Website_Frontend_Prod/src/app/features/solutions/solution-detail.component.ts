import {
  Component,
  DestroyRef,
  HostListener,
  OnInit,
  PLATFORM_ID,
  inject,
  signal,
} from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs/operators';
import { Solution } from '../../core/models/solution.model';
import { SolutionsApiService } from '../../core/services/solutions-api.service';
import { DemoRequestFormComponent } from '../../shared/demo-request-form/demo-request-form.component';

type SolutionModalType = 'demo' | 'appointment' | 'whatsapp';

interface AppointmentRequestForm {
  lastName: string;
  firstName: string;
  email: string;
  phone: string;
  company: string;
  message: string;
}

interface WhatsappConversationForm {
  phone: string;
}

@Component({
  selector: 'app-solution-detail',
  standalone: true,
  imports: [FormsModule, RouterLink, DemoRequestFormComponent],
  templateUrl: './solution-detail.component.html',
  styleUrl: './solution-detail.component.scss',
})
export class SolutionDetailComponent implements OnInit {
  readonly solution = signal<Solution | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly activeModal = signal<SolutionModalType | null>(null);

  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  private readonly contactEmail = 'contact@tunav.tn';
  private readonly tunavWhatsappNumber = '';

  appointmentForm: AppointmentRequestForm = this.createEmptyAppointmentForm();
  whatsappForm: WhatsappConversationForm = this.createEmptyWhatsappForm();

  appointmentErrors: Partial<Record<keyof AppointmentRequestForm, string>> = {};
  whatsappErrors: Partial<Record<keyof WhatsappConversationForm, string>> = {};

  constructor(
    private readonly route: ActivatedRoute,
    private readonly solutionsApi: SolutionsApiService,
    private readonly sanitizer: DomSanitizer
  ) {
    this.destroyRef.onDestroy(() => this.unlockBodyScroll());
  }

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = Number(params.get('id'));

          if (!Number.isInteger(id) || id <= 0) {
            throw new Error('Invalid solution id');
          }

          this.loading.set(true);
          this.error.set(null);
          this.solution.set(null);

          return this.solutionsApi.getById(id);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (solution) => {
          this.solution.set(solution);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Solution introuvable ou indisponible.');
          this.loading.set(false);
        },
      });
  }

  @HostListener('document:keydown.escape')
  handleEscapeKey(): void {
    if (this.activeModal()) {
      this.closeModal();
    }
  }

  getYoutubeEmbedUrl(solution: Solution): SafeResourceUrl | null {
    const embedId = this.extractYoutubeId(solution.youtubeUrl);
    if (!embedId) {
      return null;
    }

    return this.sanitizer.bypassSecurityTrustResourceUrl(
      `https://www.youtube-nocookie.com/embed/${embedId}`
    );
  }

  getVisibleSectors(solution: Solution): string[] {
    if (solution.sectors.length > 0) {
      return solution.sectors;
    }

    return solution.sectorName ? [solution.sectorName] : [];
  }

  getHeroDescription(solution: Solution): string | null {
    const description = this.normalizeDescription(solution.description);

    if (!description) {
      return null;
    }

    const firstParagraph = description
      .split(/\n{2,}/)[0]
      ?.replace(/\s+/g, ' ')
      .trim() ?? '';
    const firstSentenceMatch = firstParagraph.match(/^.*?[.!?](?:\s|$)/);
    const firstSentence = firstSentenceMatch?.[0]?.trim();

    if (firstSentence && firstSentence.length >= 60) {
      return firstSentence;
    }

    if (firstParagraph.length <= 160) {
      return firstParagraph;
    }

    return `${firstParagraph.slice(0, 157).trimEnd()}...`;
  }

  getAboutDescription(solution: Solution): string[] {
    const description = this.normalizeDescription(solution.description);

    if (!description) {
      return [];
    }

    const heroDescription = this.getHeroDescription(solution);
    const paragraphs = description
      .split(/\n{2,}/)
      .map((paragraph) => paragraph.trim())
      .filter((paragraph) => paragraph.length > 0);

    if (paragraphs.length === 1 && heroDescription === description) {
      return [];
    }

    return paragraphs;
  }

  openModal(modal: SolutionModalType): void {
    this.activeModal.set(modal);

    if (modal === 'appointment') {
      this.appointmentForm = this.createEmptyAppointmentForm();
      this.appointmentErrors = {};
    }

    if (modal === 'whatsapp') {
      this.whatsappForm = this.createEmptyWhatsappForm();
      this.whatsappErrors = {};
    }

    this.lockBodyScroll();
  }

  closeModal(): void {
    this.activeModal.set(null);
    this.unlockBodyScroll();
  }

  submitAppointmentRequest(solution: Solution): void {
    this.appointmentErrors = {};

    if (!this.hasValue(this.appointmentForm.lastName)) {
      this.appointmentErrors.lastName = 'Le nom est obligatoire.';
    }

    if (!this.hasValue(this.appointmentForm.firstName)) {
      this.appointmentErrors.firstName = 'Le prenom est obligatoire.';
    }

    if (!this.validateEmail(this.appointmentForm.email)) {
      this.appointmentErrors.email =
        'Veuillez saisir une adresse email valide.';
    }

    if (!this.hasValue(this.appointmentForm.phone)) {
      this.appointmentErrors.phone = 'Le numero de telephone est obligatoire.';
    }

    if (Object.keys(this.appointmentErrors).length > 0) {
      return;
    }

    const subject = `Demande de rendez-vous - ${solution.title}`;
    const body = [
      `Solution concernee : ${solution.title}`,
      `Nom : ${this.appointmentForm.lastName.trim()}`,
      `Prenom : ${this.appointmentForm.firstName.trim()}`,
      `Email : ${this.appointmentForm.email.trim()}`,
      `Telephone : ${this.appointmentForm.phone.trim()}`,
      this.hasValue(this.appointmentForm.company)
        ? `Entreprise : ${this.appointmentForm.company.trim()}`
        : '',
      this.hasValue(this.appointmentForm.message)
        ? `Message : ${this.appointmentForm.message.trim()}`
        : '',
    ].filter(Boolean);

    this.openMailto(subject, body);
    this.closeModal();
  }

  continueToWhatsapp(solution: Solution): void {
    this.whatsappErrors = {};

    if (!this.hasValue(this.whatsappForm.phone)) {
      this.whatsappErrors.phone = 'Le numero de telephone est obligatoire.';
    }

    if (Object.keys(this.whatsappErrors).length > 0) {
      return;
    }

    const businessNumber = this.tunavWhatsappNumber.replace(/\D/g, '');
    const whatsappMessage = encodeURIComponent(
      [
        'Bonjour Tunav,',
        `je souhaite demarrer une conversation pour la solution ${solution.title}.`,
        `Mon numero est : ${this.whatsappForm.phone.trim()}.`,
      ].join('\n')
    );

    const whatsappUrl = businessNumber
      ? `https://wa.me/${businessNumber}?text=${whatsappMessage}`
      : `https://wa.me/?text=${whatsappMessage}`;

    this.openExternalUrl(whatsappUrl);
    this.closeModal();
  }

  private extractYoutubeId(url: string | null): string | null {
    if (!url) {
      return null;
    }

    const trimmedUrl = url.trim();
    if (!trimmedUrl) {
      return null;
    }

    const patterns = [
      /youtu\.be\/([a-zA-Z0-9_-]{11})/,
      /youtube\.com\/watch\?v=([a-zA-Z0-9_-]{11})/,
      /youtube\.com\/embed\/([a-zA-Z0-9_-]{11})/,
      /youtube\.com\/shorts\/([a-zA-Z0-9_-]{11})/,
    ];

    for (const pattern of patterns) {
      const match = trimmedUrl.match(pattern);
      if (match?.[1]) {
        return match[1];
      }
    }

    return /^[a-zA-Z0-9_-]{11}$/.test(trimmedUrl) ? trimmedUrl : null;
  }

  private createEmptyAppointmentForm(): AppointmentRequestForm {
    return {
      lastName: '',
      firstName: '',
      email: '',
      phone: '',
      company: '',
      message: '',
    };
  }

  private createEmptyWhatsappForm(): WhatsappConversationForm {
    return {
      phone: '',
    };
  }

  private hasValue(value: string): boolean {
    return value.trim().length > 0;
  }

  private validateEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
  }

  private normalizeDescription(description: string | null | undefined): string {
    return (description ?? '')
      .replace(/\r\n/g, '\n')
      .trim();
  }

  private openMailto(subject: string, lines: string[]): void {
    if (!this.isBrowser) {
      return;
    }

    const mailtoUrl = `mailto:${this.contactEmail}?subject=${encodeURIComponent(
      subject
    )}&body=${encodeURIComponent(lines.join('\n'))}`;

    window.location.href = mailtoUrl;
  }

  private openExternalUrl(url: string): void {
    if (!this.isBrowser) {
      return;
    }

    window.open(url, '_blank', 'noopener,noreferrer');
  }

  private lockBodyScroll(): void {
    if (!this.isBrowser) {
      return;
    }

    this.document.body.style.overflow = 'hidden';
  }

  private unlockBodyScroll(): void {
    if (!this.isBrowser) {
      return;
    }

    this.document.body.style.overflow = '';
  }
}
