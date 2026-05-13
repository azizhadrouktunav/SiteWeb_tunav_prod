import { Injectable, signal } from '@angular/core';
import { TestRubrique } from '../../core/models/test-rubrique.model';
import { BaseViewModel } from '../../core/viewmodels/base-viewmodel';
import { TestRubriqueApiService } from '../../core/services/test-rubrique-api.service';

@Injectable()
export class TestRubriqueViewModel extends BaseViewModel {
  readonly rubriques = signal<TestRubrique[]>([]);
  readonly current = signal<TestRubrique | null>(null);

  constructor(private readonly api: TestRubriqueApiService) {
    super();
  }

  loadAll(): void {
    this.startLoading();
    this.api.getAll().subscribe({
      next: (items) => {
        this.rubriques.set(items);
        this.endLoading();
      },
      error: () => this.setError('Erreur lors du chargement des rubriques.'),
    });
  }

  edit(item: TestRubrique | null): void {
    this.current.set(item);
  }

  save(formValue: Omit<TestRubrique, 'id'> & Partial<Pick<TestRubrique, 'id'>>): void {
    this.startLoading();

    const isNew = !formValue.id || formValue.id === 0;

    const toSave: TestRubrique = {
      id: formValue.id ?? 0,
      titre: formValue.titre,
      description: formValue.description,
      actif: formValue.actif,
    };

    const request$ = isNew
      ? this.api.create({ titre: toSave.titre, description: toSave.description, actif: toSave.actif })
      : this.api.update(toSave.id, toSave);

    request$.subscribe({
      next: () => {
        this.loadAll();
        this.current.set(null);
      },
      error: () => this.setError('Erreur lors de la sauvegarde.'),
    });
  }

  delete(id: number): void {
    if (!confirm('Supprimer cet enregistrement ?')) return;

    this.startLoading();
    this.api.delete(id).subscribe({
      next: () => {
        this.rubriques.update((list) => list.filter((x) => x.id !== id));
        this.endLoading();
      },
      error: () => this.setError('Erreur lors de la suppression.'),
    });
  }
}

