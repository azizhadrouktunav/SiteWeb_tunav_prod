import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  OnInit,
  Output,
  SimpleChanges,
  signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { Solution } from '../../core/models/solution.model';
import { DemoRequestEntryPoint } from '../../core/models/demo-request.model';
import { DemoRequestsApiService } from '../../core/services/demo-requests-api.service';
import { SolutionsApiService } from '../../core/services/solutions-api.service';

type WhatsappPreference = 'yes' | 'no' | '';

interface DemoRequestFormValue {
  solutionId: number | null;
  firstName: string;
  lastName: string;
  email: string;
  hasWhatsapp: WhatsappPreference;
  whatsappNumber: string;
}

type DemoRequestField = keyof DemoRequestFormValue;

@Component({
  selector: 'app-demo-request-form',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './demo-request-form.component.html',
  styleUrl: './demo-request-form.component.scss',
})
export class DemoRequestFormComponent implements OnInit, OnChanges {
  @Input({ required: true }) entryPoint: DemoRequestEntryPoint = 'DemoPage';
  @Input() presetSolution: Solution | null = null;
  @Input() showSolutionSelector = false;
  @Input() showCancelButton = false;
  @Input() compact = false;
  @Input() submitLabel = 'Envoyer la demande';
  @Output() cancel = new EventEmitter<void>();

  readonly solutions = signal<Solution[]>([]);
  readonly loadingSolutions = signal(false);
  readonly solutionsError = signal<string | null>(null);
  readonly submitSuccess = signal<string | null>(null);
  readonly submitError = signal<string | null>(null);
  readonly submitting = signal(false);

  form: DemoRequestFormValue = this.createInitialForm();
  errors: Partial<Record<DemoRequestField, string>> = {};

  constructor(
    private readonly demoRequestsApi: DemoRequestsApiService,
    private readonly solutionsApi: SolutionsApiService
  ) {}

  ngOnInit(): void {
    this.syncPresetSolution();

    if (this.showSolutionSelector) {
      this.loadSolutions();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['presetSolution']) {
      this.syncPresetSolution();
    }
  }

  onWhatsappChange(choice: WhatsappPreference): void {
    this.form.hasWhatsapp = choice;
    delete this.errors.hasWhatsapp;

    if (choice !== 'yes') {
      this.form.whatsappNumber = '';
      delete this.errors.whatsappNumber;
    }
  }

  submit(): void {
    this.errors = {};
    this.submitError.set(null);
    this.submitSuccess.set(null);

    if (!this.form.solutionId || this.form.solutionId <= 0) {
      this.errors.solutionId = 'Veuillez selectionner une solution.';
    }

    if (!this.hasValue(this.form.firstName)) {
      this.errors.firstName = 'Le prenom est obligatoire.';
    }

    if (!this.hasValue(this.form.lastName)) {
      this.errors.lastName = 'Le nom est obligatoire.';
    }

    if (!this.validateEmail(this.form.email)) {
      this.errors.email = 'Veuillez saisir une adresse email valide.';
    }

    if (!this.form.hasWhatsapp) {
      this.errors.hasWhatsapp = 'Veuillez choisir une option.';
    }

    if (
      this.form.hasWhatsapp === 'yes' &&
      !this.hasValue(this.form.whatsappNumber)
    ) {
      this.errors.whatsappNumber = 'Le numero WhatsApp est obligatoire.';
    }

    if (Object.keys(this.errors).length > 0 || !this.form.solutionId) {
      return;
    }

    this.submitting.set(true);

    this.demoRequestsApi
      .submitRequest({
        solutionId: this.form.solutionId,
        firstName: this.form.firstName.trim(),
        lastName: this.form.lastName.trim(),
        email: this.form.email.trim(),
        hasWhatsapp: this.form.hasWhatsapp === 'yes',
        whatsappNumber:
          this.form.hasWhatsapp === 'yes'
            ? this.form.whatsappNumber.trim()
            : null,
        entryPoint: this.entryPoint,
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (response) => {
          this.submitSuccess.set(
            response.message ||
              'Votre demande a bien ete recue et sera examinee par notre equipe.'
          );
          this.form = this.createInitialForm();
          this.syncPresetSolution();
          this.errors = {};
        },
        error: (error: unknown) => {
          this.submitError.set(this.getErrorMessage(error));
        },
      });
  }

  retrySolutionsLoad(): void {
    this.loadSolutions();
  }

  private loadSolutions(): void {
    this.loadingSolutions.set(true);
    this.solutionsError.set(null);

    this.solutionsApi
      .getActiveSolutions()
      .pipe(finalize(() => this.loadingSolutions.set(false)))
      .subscribe({
        next: (solutions) => {
          const sortedSolutions = [...solutions].sort((left, right) =>
            left.title.localeCompare(right.title, 'fr', { sensitivity: 'base' })
          );

          this.solutions.set(sortedSolutions);

          if (
            this.form.solutionId &&
            !sortedSolutions.some((solution) => solution.id === this.form.solutionId)
          ) {
            this.form.solutionId = null;
          }
        },
        error: () => {
          this.solutions.set([]);
          this.solutionsError.set(
            'Impossible de charger les solutions pour le moment.'
          );
        },
      });
  }

  private syncPresetSolution(): void {
    this.form.solutionId = this.presetSolution?.id ?? this.form.solutionId;

    if (!this.presetSolution && !this.showSolutionSelector) {
      this.form.solutionId = null;
    }
  }

  private createInitialForm(): DemoRequestFormValue {
    return {
      solutionId: this.presetSolution?.id ?? null,
      firstName: '',
      lastName: '',
      email: '',
      hasWhatsapp: '',
      whatsappNumber: '',
    };
  }

  private hasValue(value: string): boolean {
    return value.trim().length > 0;
  }

  private validateEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const apiMessage =
        typeof error.error?.message === 'string' ? error.error.message : null;

      return apiMessage || 'Une erreur est survenue lors de l envoi de la demande.';
    }

    return 'Une erreur est survenue lors de l envoi de la demande.';
  }
}
