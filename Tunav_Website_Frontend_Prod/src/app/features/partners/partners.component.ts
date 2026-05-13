import {
  Component,
  DestroyRef,
  OnInit,
  PLATFORM_ID,
  computed,
  inject,
  signal,
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PartnerRequestsApiService } from '../../core/services/partner-requests-api.service';
import {
  CreatePartnerRequestPayload,
  PartnerRequestPersonType,
  PartnerRequestType,
} from '../../core/models/partner-request.model';

type PartnerTypeKey = 'franchise' | 'revendeur' | 'commissionnaire';
type PersonType = 'physique' | 'morale' | '';
type PartnerIcon = 'store' | 'gift' | 'percent';
type SolutionIcon = 'rfid' | 'grid' | 'bolt' | 'fuel' | 'pin' | 'camera';

interface PartnerType {
  key: PartnerTypeKey;
  title: string;
  actionLabel: string;
  description: string;
  benefits: string[];
  icon: PartnerIcon;
  documentUrl?: string | null;
}

interface SolutionOption {
  key: string;
  title: string;
  icon: SolutionIcon;
}

interface PartnerRequestForm {
  fullName: string;
  email: string;
  phone: string;
  company: string;
  city: string;
  personType: PersonType;
  selectedSolutions: string[];
}

const PARTNER_TYPES: PartnerType[] = [
  {
    key: 'franchise',
    title: 'Franchise',
    actionLabel: 'Choisir Franchise',
    description:
      'Partenaire exploitant nos solutions et notre marque selon un modele de franchise defini.',
    benefits: [
      'Utilisation de la marque TUNAV IT',
      'Support complet',
      'Formation incluse',
      'Exclusivite territoriale',
    ],
    icon: 'store',
    documentUrl: '/assets/documents/partenariat-tunav.pdf',
  },
  {
    key: 'revendeur',
    title: 'Revendeur',
    actionLabel: 'Choisir Revendeur',
    description:
      'Partenaire souhaitant vendre nos solutions sous sa propre marque.',
    benefits: [
      'White label disponible',
      'Votre propre marque',
      'Support technique',
      'Tarifs preferentiels',
    ],
    icon: 'gift',
    documentUrl: '/assets/documents/partenariat-tunav.pdf',
  },
  {
    key: 'commissionnaire',
    title: 'Commissionnaire',
    actionLabel: 'Choisir Commissionnaire',
    description:
      "Personne ou entite disposant d'un portefeuille clients et souhaitant vendre une ou plusieurs solutions.",
    benefits: [
      'Commission attractive',
      "Pas d'investissement",
      'Portefeuille existant',
      'Flexibilite totale',
    ],
    icon: 'percent',
    documentUrl: '/assets/documents/partenariat-tunav.pdf',
  },
];

const SOLUTION_OPTIONS: SolutionOption[] = [
  { key: 'tagit-rfid', title: 'TAGIT RFID', icon: 'rfid' },
  { key: 'easytrace-360', title: 'EasyTrace 360', icon: 'grid' },
  {
    key: 'disjoncteur-intelligent',
    title: 'Disjoncteur Intelligent',
    icon: 'bolt',
  },
  { key: 'fuel-rescue', title: 'FUEL RESCUE', icon: 'fuel' },
  { key: 'easy-trace-v3', title: 'EASY TRACE V3', icon: 'pin' },
  { key: 'dashcam', title: 'DashCam', icon: 'camera' },
];

@Component({
  selector: 'app-partners',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './partners.component.html',
  styleUrl: './partners.component.scss',
})
export class PartnersComponent implements OnInit {
  readonly partnerTypes = PARTNER_TYPES;
  readonly solutionOptions = SOLUTION_OPTIONS;
  readonly selectedTypeKey = signal<PartnerTypeKey | null>(null);
  readonly formErrors = signal<Record<string, string>>({});
  readonly isSubmitting = signal(false);
  readonly submitSuccessMessage = signal('');
  readonly submitErrorMessage = signal('');
  readonly activeType = computed(
    () =>
      this.partnerTypes.find((type) => type.key === this.selectedTypeKey()) ??
      null
  );

  private readonly destroyRef = inject(DestroyRef);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  private readonly contactEmail = 'contact@tunav.tn';

  form: PartnerRequestForm = this.createEmptyForm();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly partnerRequestsApi: PartnerRequestsApiService
  ) {}

  ngOnInit(): void {
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const rawType = params.get('type');

        if (!rawType) {
          this.selectedTypeKey.set(null);
          this.form = this.createEmptyForm();
          this.formErrors.set({});
          this.resetSubmitState();
          return;
        }

        if (!this.isPartnerTypeKey(rawType)) {
          this.router.navigate(['/partners'], { replaceUrl: true });
          return;
        }

        this.selectedTypeKey.set(rawType);
        this.form = this.createEmptyForm();
        this.formErrors.set({});
        this.resetSubmitState();
      });
  }

  openPartnerType(type: PartnerTypeKey): void {
    this.resetSubmitState();
    this.router.navigate(['/partners', type]);
  }

  openPartnerDocument(type: PartnerType): void {
    if (type.documentUrl) {
      this.openExternalUrl(type.documentUrl, false);
      return;
    }

    const subject = `Demande du document de partenariat - ${type.title}`;
    const body = [
      'Bonjour TUNAV,',
      '',
      `Je souhaite recevoir le document de partenariat pour le profil ${type.title}.`,
      'Merci.',
    ];

    this.openMailto(subject, body);
  }

  backToTypes(): void {
    this.resetSubmitState();
    this.router.navigate(['/partners']);
  }

  toggleSolution(solutionKey: string): void {
    this.clearFeedback();
    const selectedSolutions = this.form.selectedSolutions.includes(solutionKey)
      ? this.form.selectedSolutions.filter((key) => key !== solutionKey)
      : [...this.form.selectedSolutions, solutionKey];

    this.form = {
      ...this.form,
      selectedSolutions,
    };

    if (selectedSolutions.length > 0) {
      this.clearError('selectedSolutions');
    }
  }

  isSolutionSelected(solutionKey: string): boolean {
    return this.form.selectedSolutions.includes(solutionKey);
  }

  submitRequest(type: PartnerType): void {
    this.clearFeedback();
    const errors: Record<string, string> = {};

    if (!this.hasValue(this.form.fullName)) {
      errors['fullName'] = 'Le nom complet est obligatoire.';
    }

    if (!this.validateEmail(this.form.email)) {
      errors['email'] = 'Veuillez saisir une adresse e-mail valide.';
    }

    if (!this.hasValue(this.form.phone)) {
      errors['phone'] = 'Le numero de telephone est obligatoire.';
    }

    if (!this.hasValue(this.form.city)) {
      errors['city'] = 'La ville est obligatoire.';
    }

    if (!this.form.personType) {
      errors['personType'] = 'Veuillez choisir un type de personne.';
    }

    if (this.form.selectedSolutions.length === 0) {
      errors['selectedSolutions'] =
        'Selectionnez au moins une solution qui vous interesse.';
    }

    this.formErrors.set(errors);

    if (Object.keys(errors).length > 0) {
      return;
    }

    const payload = this.buildRequestPayload(type);

    this.isSubmitting.set(true);

    this.partnerRequestsApi.submitRequest(payload).subscribe({
      next: (response) => {
        this.isSubmitting.set(false);
        this.form = this.createEmptyForm();
        this.formErrors.set({});
        this.submitSuccessMessage.set(
          response.message ||
            'Votre demande a bien ete recue et sera examinee par notre equipe.'
        );
      },
      error: (error) => {
        this.isSubmitting.set(false);
        this.submitErrorMessage.set(
          error?.error?.message ||
            'Une erreur est survenue lors de l envoi de votre demande.'
        );
      },
    });
  }

  clearError(field: string): void {
    this.clearFeedback();
    const currentErrors = { ...this.formErrors() };
    delete currentErrors[field];
    this.formErrors.set(currentErrors);
  }

  private buildRequestPayload(type: PartnerType): CreatePartnerRequestPayload {
    const selectedSolutionTitles = this.solutionOptions
      .filter((solution) => this.form.selectedSolutions.includes(solution.key))
      .map((solution) => solution.title);

    return {
      partnerType: this.toApiPartnerType(type.key),
      fullName: this.form.fullName.trim(),
      email: this.form.email.trim(),
      phone: this.form.phone.trim(),
      company: this.hasValue(this.form.company) ? this.form.company.trim() : null,
      city: this.form.city.trim(),
      personType: this.toApiPersonType(
        this.form.personType as Exclude<PersonType, ''>
      ),
      selectedSolutions: selectedSolutionTitles,
    };
  }

  private createEmptyForm(): PartnerRequestForm {
    return {
      fullName: '',
      email: '',
      phone: '',
      company: '',
      city: '',
      personType: '',
      selectedSolutions: [],
    };
  }

  private hasValue(value: string): boolean {
    return value.trim().length > 0;
  }

  private validateEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
  }

  private isPartnerTypeKey(value: string): value is PartnerTypeKey {
    return ['franchise', 'revendeur', 'commissionnaire'].includes(value);
  }

  private toApiPartnerType(type: PartnerTypeKey): PartnerRequestType {
    switch (type) {
      case 'franchise':
        return 'Franchise';
      case 'revendeur':
        return 'Revendeur';
      case 'commissionnaire':
        return 'Commissionnaire';
    }
  }

  private toApiPersonType(type: Exclude<PersonType, ''>): PartnerRequestPersonType {
    return type === 'physique' ? 'Physique' : 'Morale';
  }

  private clearFeedback(): void {
    this.submitSuccessMessage.set('');
    this.submitErrorMessage.set('');
  }

  private resetSubmitState(): void {
    this.isSubmitting.set(false);
    this.clearFeedback();
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

  private openExternalUrl(url: string, newTab = true): void {
    if (!this.isBrowser) {
      return;
    }

    if (newTab) {
      window.open(url, '_blank', 'noopener,noreferrer');
      return;
    }

    window.location.href = url;
  }
}
