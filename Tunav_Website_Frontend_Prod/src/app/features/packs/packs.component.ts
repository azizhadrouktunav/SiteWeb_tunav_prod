import {
  Component,
  DestroyRef,
  HostListener,
  OnInit,
  PLATFORM_ID,
  computed,
  inject,
  signal,
} from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';
import { CreateDemoRequestPayload } from '../../core/models/demo-request.model';
import {
  PackCatalogPack,
  PackCatalogSolution,
  PackThemeKey,
} from '../../core/models/pack.model';
import {
  PackSolutionThemeKey,
} from '../../core/models/solution.model';
import { DemoRequestsApiService } from '../../core/services/demo-requests-api.service';
import { PacksApiService } from '../../core/services/packs-api.service';
import { PackIconComponent } from './pack-icon.component';

type WhatsappChoice = 'yes' | 'no' | '';

interface DemoFormValue {
  lastName: string;
  firstName: string;
  email: string;
  hasWhatsapp: WhatsappChoice;
  whatsappNumber: string;
}

interface CustomPackFormValue {
  contactName: string;
  company: string;
  email: string;
  phone: string;
  message: string;
}

type DemoFormField = keyof DemoFormValue;
type CustomPackFormField = keyof CustomPackFormValue;

interface FlashMessage {
  type: 'success' | 'error';
  text: string;
}

interface GradientTheme {
  start: string;
  end: string;
}

@Component({
  selector: 'app-packs',
  standalone: true,
  imports: [FormsModule, PackIconComponent],
  templateUrl: './packs.component.html',
  styleUrl: './packs.component.scss',
})
export class PacksComponent implements OnInit {
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly catalog = signal<PackCatalogSolution[]>([]);
  readonly selectedSolution = signal<PackCatalogSolution | null>(null);
  readonly flashMessage = signal<FlashMessage | null>(null);

  readonly demoModalOpen = signal(false);
  readonly customModalOpen = signal(false);
  readonly selectedPack = signal<PackCatalogPack | null>(null);
  readonly demoSubmitError = signal<string | null>(null);
  readonly customSubmitError = signal<string | null>(null);
  readonly submittingDemo = signal(false);
  readonly submittingCustom = signal(false);
  readonly selectedCustomFeatures = signal<string[]>([]);

  readonly selectedPacks = computed(() => this.selectedSolution()?.packs ?? []);
  readonly comparisonFeatures = computed(() => {
    const features = new Set<string>();

    for (const pack of this.selectedPacks()) {
      for (const feature of pack.features) {
        features.add(feature);
      }
    }

    return Array.from(features);
  });

  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private routeSlug: string | null = null;

  demoForm: DemoFormValue = this.createEmptyDemoForm();
  customPackForm: CustomPackFormValue = this.createEmptyCustomPackForm();

  demoErrors: Partial<Record<DemoFormField, string>> = {};
  customPackErrors: Partial<Record<CustomPackFormField, string>> = {};
  customFeatureError: string | null = null;

  private readonly solutionThemes: Record<PackSolutionThemeKey, GradientTheme> = {
    'blue-cyan': { start: '#2b59ff', end: '#17c6e5' },
    'yellow-orange': { start: '#f4b400', end: '#f97316' },
    'teal-green': { start: '#0fb9b1', end: '#16a34a' },
    'pink-rose': { start: '#ec4899', end: '#f43f5e' },
    'sky-cyan': { start: '#2e6cff', end: '#12b9df' },
    'red-pink': { start: '#ef4444', end: '#ec4899' },
  };

  private readonly packThemes: Record<PackThemeKey, GradientTheme> = {
    green: { start: '#08c955', end: '#16a34a' },
    orange: { start: '#ff7a00', end: '#f59e0b' },
    rose: { start: '#ff0f4f', end: '#e11d48' },
  };

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly packsApi: PacksApiService,
    private readonly demoRequestsApi: DemoRequestsApiService
  ) {
    this.destroyRef.onDestroy(() => this.unlockBodyScroll());
  }

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        this.routeSlug = this.normalizeSlug(params.get('solution'));
        this.syncSelectedSolution();
      });

    this.loadCatalog();
  }

  @HostListener('document:keydown.escape')
  handleEscapeKey(): void {
    if (this.demoModalOpen()) {
      this.closeDemoModal();
      return;
    }

    if (this.customModalOpen()) {
      this.closeCustomPackModal();
    }
  }

  selectSolution(solution: PackCatalogSolution): void {
    const currentSlug = this.selectedSolution()?.solutionSlug;

    if (currentSlug === solution.solutionSlug) {
      return;
    }

    this.flashMessage.set(null);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { solution: solution.solutionSlug },
      queryParamsHandling: 'merge',
    });
  }

  isSolutionSelected(solution: PackCatalogSolution): boolean {
    return this.selectedSolution()?.solutionSlug === solution.solutionSlug;
  }

  getSolutionTheme(themeKey: string | null | undefined): GradientTheme {
    return (
      this.solutionThemes[themeKey as PackSolutionThemeKey] ??
      this.solutionThemes['blue-cyan']
    );
  }

  getPackTheme(themeKey: string | null | undefined): GradientTheme {
    return this.packThemes[themeKey as PackThemeKey] ?? this.packThemes.green;
  }

  formatPackName(name: string): string {
    const words = name.trim().split(/\s+/);

    if (words.length <= 1) {
      return name;
    }

    return `${words[0]}\n${words.slice(1).join(' ')}`;
  }

  hasFeature(pack: PackCatalogPack, feature: string): boolean {
    return pack.features.includes(feature);
  }

  openDemoModal(pack: PackCatalogPack): void {
    this.selectedPack.set(pack);
    this.demoForm = this.createEmptyDemoForm();
    this.demoErrors = {};
    this.demoSubmitError.set(null);
    this.demoModalOpen.set(true);
    this.customModalOpen.set(false);
    this.syncBodyScroll();
  }

  closeDemoModal(): void {
    this.demoModalOpen.set(false);
    this.selectedPack.set(null);
    this.demoErrors = {};
    this.demoSubmitError.set(null);
    this.demoForm = this.createEmptyDemoForm();
    this.syncBodyScroll();
  }

  openCustomPackModal(): void {
    this.customPackForm = this.createEmptyCustomPackForm();
    this.customPackErrors = {};
    this.customFeatureError = null;
    this.customSubmitError.set(null);
    this.selectedCustomFeatures.set([]);
    this.customModalOpen.set(true);
    this.demoModalOpen.set(false);
    this.selectedPack.set(null);
    this.syncBodyScroll();
  }

  closeCustomPackModal(): void {
    this.customModalOpen.set(false);
    this.customPackErrors = {};
    this.customFeatureError = null;
    this.customSubmitError.set(null);
    this.selectedCustomFeatures.set([]);
    this.customPackForm = this.createEmptyCustomPackForm();
    this.syncBodyScroll();
  }

  closeFlashMessage(): void {
    this.flashMessage.set(null);
  }

  setDemoWhatsapp(choice: WhatsappChoice): void {
    this.demoForm.hasWhatsapp = choice;
    delete this.demoErrors.hasWhatsapp;

    if (choice !== 'yes') {
      this.demoForm.whatsappNumber = '';
      delete this.demoErrors.whatsappNumber;
    }
  }

  toggleCustomFeature(feature: string): void {
    const selectedFeatures = this.selectedCustomFeatures();

    if (selectedFeatures.includes(feature)) {
      this.selectedCustomFeatures.set(
        selectedFeatures.filter((value) => value !== feature)
      );
    } else {
      this.selectedCustomFeatures.set([...selectedFeatures, feature]);
    }

    if (this.selectedCustomFeatures().length > 0) {
      this.customFeatureError = null;
    }
  }

  isCustomFeatureSelected(feature: string): boolean {
    return this.selectedCustomFeatures().includes(feature);
  }

  submitDemoRequest(): void {
    const solution = this.selectedSolution();
    const pack = this.selectedPack();

    if (!solution || !pack) {
      return;
    }

    this.demoErrors = {};
    this.demoSubmitError.set(null);

    if (!this.hasValue(this.demoForm.lastName)) {
      this.demoErrors.lastName = 'Le nom est obligatoire.';
    }

    if (!this.hasValue(this.demoForm.firstName)) {
      this.demoErrors.firstName = 'Le prénom est obligatoire.';
    }

    if (!this.validateEmail(this.demoForm.email)) {
      this.demoErrors.email = 'Veuillez saisir une adresse email valide.';
    }

    if (!this.demoForm.hasWhatsapp) {
      this.demoErrors.hasWhatsapp = 'Veuillez choisir une option.';
    }

    if (
      this.demoForm.hasWhatsapp === 'yes' &&
      !this.hasValue(this.demoForm.whatsappNumber)
    ) {
      this.demoErrors.whatsappNumber = 'Le numéro WhatsApp est obligatoire.';
    }

    if (Object.keys(this.demoErrors).length > 0) {
      return;
    }

    const payload: CreateDemoRequestPayload = {
      solutionId: solution.solutionId,
      packId: pack.id,
      firstName: this.demoForm.firstName.trim(),
      lastName: this.demoForm.lastName.trim(),
      email: this.demoForm.email.trim(),
      hasWhatsapp: this.demoForm.hasWhatsapp === 'yes',
      whatsappNumber:
        this.demoForm.hasWhatsapp === 'yes'
          ? this.demoForm.whatsappNumber.trim()
          : null,
      entryPoint: 'PacksPage',
    };

    this.submittingDemo.set(true);

    this.demoRequestsApi
      .submitRequest(payload)
      .pipe(finalize(() => this.submittingDemo.set(false)))
      .subscribe({
        next: (response) => {
          this.flashMessage.set({
            type: 'success',
            text:
              response.message ||
              'Votre demande a bien ete recue et sera examinee par notre equipe.',
          });
          this.closeDemoModal();
          this.scrollToTop();
        },
        error: (error: unknown) => {
          this.demoSubmitError.set(
            this.getErrorMessage(
              error,
              "Une erreur est survenue lors de l'envoi de la demande."
            )
          );
        },
      });
  }

  submitCustomPackRequest(): void {
    const solution = this.selectedSolution();

    if (!solution) {
      return;
    }

    this.customPackErrors = {};
    this.customFeatureError = null;
    this.customSubmitError.set(null);

    if (this.selectedCustomFeatures().length === 0) {
      this.customFeatureError =
        'Veuillez sélectionner au moins une fonctionnalité.';
    }

    if (!this.hasValue(this.customPackForm.contactName)) {
      this.customPackErrors.contactName = 'Le nom est obligatoire.';
    }

    if (!this.hasValue(this.customPackForm.company)) {
      this.customPackErrors.company = "Le nom de l'entreprise est obligatoire.";
    }

    if (!this.validateEmail(this.customPackForm.email)) {
      this.customPackErrors.email =
        'Veuillez saisir une adresse email valide.';
    }

    if (!this.hasValue(this.customPackForm.phone)) {
      this.customPackErrors.phone = 'Le téléphone est obligatoire.';
    }

    if (
      this.hasValue(this.customPackForm.message) &&
      this.customPackForm.message.trim().length > 2000
    ) {
      this.customPackErrors.message =
        'Le message ne peut pas dépasser 2000 caractères.';
    }

    if (
      Object.keys(this.customPackErrors).length > 0 ||
      this.selectedCustomFeatures().length === 0
    ) {
      return;
    }

    this.submittingCustom.set(true);

    this.packsApi
      .submitCustomPackRequest({
        solutionId: solution.solutionId,
        contactName: this.customPackForm.contactName.trim(),
        company: this.customPackForm.company.trim(),
        email: this.customPackForm.email.trim(),
        phone: this.customPackForm.phone.trim(),
        message: this.hasValue(this.customPackForm.message)
          ? this.customPackForm.message.trim()
          : null,
        selectedFeatures: this.selectedCustomFeatures(),
      })
      .pipe(finalize(() => this.submittingCustom.set(false)))
      .subscribe({
        next: (response) => {
          this.flashMessage.set({
            type: 'success',
            text:
              response.message ||
              'Votre demande de pack personnalisé a bien été envoyée.',
          });
          this.closeCustomPackModal();
          this.scrollToTop();
        },
        error: (error: unknown) => {
          this.customSubmitError.set(
            this.getErrorMessage(
              error,
              "Une erreur est survenue lors de l'envoi de la demande."
            )
          );
        },
      });
  }

  private loadCatalog(): void {
    this.loading.set(true);
    this.error.set(null);

    this.packsApi
      .getCatalog()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (catalog) => {
          const normalizedCatalog = [...catalog].map((solution) => ({
            ...solution,
            packs: [...solution.packs].sort((left, right) => {
              if (left.displayOrder !== right.displayOrder) {
                return left.displayOrder - right.displayOrder;
              }

              return left.name.localeCompare(right.name, 'fr', {
                sensitivity: 'base',
              });
            }),
          }));

          this.catalog.set(normalizedCatalog);

          if (normalizedCatalog.length === 0) {
            this.error.set('Aucun pack actif n’est disponible pour le moment.');
            this.selectedSolution.set(null);
            return;
          }

          this.syncSelectedSolution();
        },
        error: () => {
          this.catalog.set([]);
          this.selectedSolution.set(null);
          this.error.set(
            'Impossible de charger le catalogue des packs pour le moment.'
          );
        },
      });
  }

  private syncSelectedSolution(): void {
    const catalog = this.catalog();

    if (catalog.length === 0) {
      return;
    }

    const matchedSolution = this.routeSlug
      ? catalog.find(
          (solution) =>
            solution.solutionSlug.toLowerCase() === this.routeSlug?.toLowerCase()
        )
      : null;

    const nextSolution = matchedSolution ?? catalog[0];
    const previousSlug = this.selectedSolution()?.solutionSlug ?? null;
    const nextSlug = nextSolution.solutionSlug.toLowerCase();

    this.selectedSolution.set(nextSolution);

    if (previousSlug && previousSlug !== nextSolution.solutionSlug) {
      this.resetTransientState();
    }

    if (this.routeSlug !== nextSlug) {
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { solution: nextSolution.solutionSlug },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });
    }
  }

  private resetTransientState(): void {
    if (this.demoModalOpen()) {
      this.closeDemoModal();
    }

    if (this.customModalOpen()) {
      this.closeCustomPackModal();
    }

    this.demoForm = this.createEmptyDemoForm();
    this.customPackForm = this.createEmptyCustomPackForm();
    this.demoErrors = {};
    this.customPackErrors = {};
    this.customFeatureError = null;
    this.selectedCustomFeatures.set([]);
    this.selectedPack.set(null);
    this.demoSubmitError.set(null);
    this.customSubmitError.set(null);
  }

  private createEmptyDemoForm(): DemoFormValue {
    return {
      lastName: '',
      firstName: '',
      email: '',
      hasWhatsapp: '',
      whatsappNumber: '',
    };
  }

  private createEmptyCustomPackForm(): CustomPackFormValue {
    return {
      contactName: '',
      company: '',
      email: '',
      phone: '',
      message: '',
    };
  }

  private normalizeSlug(value: string | null): string | null {
    const normalized = (value ?? '').trim().toLowerCase();
    return normalized.length > 0 ? normalized : null;
  }

  private hasValue(value: string): boolean {
    return value.trim().length > 0;
  }

  private validateEmail(value: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
  }

  private getErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const apiMessage =
        typeof error.error?.message === 'string' ? error.error.message : null;
      return apiMessage || fallback;
    }

    return fallback;
  }

  private syncBodyScroll(): void {
    if (this.demoModalOpen() || this.customModalOpen()) {
      this.lockBodyScroll();
      return;
    }

    this.unlockBodyScroll();
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

  private scrollToTop(): void {
    if (!this.isBrowser) {
      return;
    }

    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}
