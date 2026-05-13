import { Injectable, OnDestroy, computed, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { BaseViewModel } from '../../core/viewmodels/base-viewmodel';
import { SolutionsApiService } from '../../core/services/solutions-api.service';
import { Solution } from '../../core/models/solution.model';

@Injectable()
export class SolutionsViewModel extends BaseViewModel implements OnDestroy {
  readonly solutions = signal<Solution[]>([]);
  readonly sectors = signal<string[]>([]);
  readonly searchQuery = signal('');
  readonly selectedSectors = signal<string[]>([]);

  readonly filteredSolutions = computed(() => {
    const allSolutions = this.solutions();
    const selectedSectors = this.selectedSectors();
    const normalizedSearch = this.searchQuery().trim().toLowerCase();
    const sectorialSolutions = allSolutions.filter(
      (solution) => solution.type === 'Sectorial'
    );
    const restrictToSectorialSolutions = sectorialSolutions.length > 0;
    const filterSourceSolutions =
      restrictToSectorialSolutions ? sectorialSolutions : allSolutions;

    const displayedSolutions = selectedSectors.length === 0
      ? allSolutions.filter((solution) => solution.type === 'General')
      : filterSourceSolutions.filter((solution) =>
          this.matchesSectorFilter(
            solution,
            selectedSectors,
            restrictToSectorialSolutions
          )
        );

    if (!normalizedSearch) {
      return displayedSolutions;
    }

    return displayedSolutions.filter((solution) =>
      solution.title.toLowerCase().includes(normalizedSearch) ||
      solution.description.toLowerCase().includes(normalizedSearch)
    );
  });

  readonly hasFilters = computed(() =>
    this.searchQuery().trim().length > 0 ||
    this.selectedSectors().length > 0
  );

  private loadSubscription?: Subscription;

  constructor(private readonly api: SolutionsApiService) {
    super();
  }

  initialize(): void {
    this.reloadSolutions();
  }

  reloadSolutions(): void {
    this.startLoading();
    this.loadSubscription?.unsubscribe();
    this.loadSubscription = this.api.getActiveSolutions().subscribe({
      next: (solutions) => {
        this.solutions.set(solutions);
        this.sectors.set(this.buildSectorOptions(solutions));
        this.endLoading();
      },
      error: () => {
        this.sectors.set([]);
        this.setError('Erreur lors du chargement des solutions.');
      },
    });
  }

  setSearch(query: string): void {
    this.searchQuery.set(query);
  }

  toggleSector(sector: string): void {
    this.selectedSectors.update((current) =>
      current.includes(sector)
        ? current.filter((value) => value !== sector)
        : [...current, sector]
    );
  }

  clearFilters(): void {
    if (!this.hasFilters()) {
      return;
    }

    this.searchQuery.set('');
    this.selectedSectors.set([]);
  }

  clearSectorFilters(): void {
    if (this.selectedSectors().length === 0) {
      return;
    }

    this.selectedSectors.set([]);
  }

  ngOnDestroy(): void {
    this.loadSubscription?.unsubscribe();
  }

  private buildSectorOptions(solutions: Solution[]): string[] {
    const sectors = new Set<string>();
    const sectorialSolutions = solutions.filter(
      (solution) => solution.type === 'Sectorial'
    );
    const filterSourceSolutions =
      sectorialSolutions.length > 0 ? sectorialSolutions : solutions;

    filterSourceSolutions
      .forEach((solution) => {
        const sectorName = solution.sectorName?.trim();

        if (sectorName) {
          sectors.add(sectorName);
          return;
        }

        solution.sectors
          .map((sector) => sector.trim())
          .filter((sector) => sector.length > 0)
          .forEach((sector) => sectors.add(sector));
      });

    return Array.from(sectors).sort((left, right) => left.localeCompare(right));
  }

  private matchesSectorFilter(
    solution: Solution,
    selectedSectors: string[],
    restrictToSectorialSolutions: boolean
  ): boolean {
    if (restrictToSectorialSolutions && solution.type !== 'Sectorial') {
      return false;
    }

    const solutionTerms = this.getComparableSectorTerms(solution);

    return selectedSectors.some((selectedSector) =>
      this.expandSectorTerms(selectedSector).some((selectedTerm) =>
        solutionTerms.has(selectedTerm)
      )
    );
  }

  private getComparableSectorTerms(solution: Solution): Set<string> {
    const comparableTerms = new Set<string>();
    const sectorLabels = [
      solution.sectorName?.trim() ?? '',
      ...solution.sectors.map((sector) => sector.trim()),
    ].filter((sector) => sector.length > 0);

    sectorLabels.forEach((sector) => {
      this.expandSectorTerms(sector).forEach((term) => comparableTerms.add(term));
    });

    return comparableTerms;
  }

  private expandSectorTerms(value: string): string[] {
    const rawValue = value.trim();

    if (!rawValue) {
      return [];
    }

    const terms = new Set<string>();
    terms.add(this.normalizeSectorValue(rawValue));

    rawValue
      .split(/[\/,|&]+/)
      .map((segment) => this.normalizeSectorValue(segment))
      .filter((segment) => segment.length > 0)
      .forEach((segment) => terms.add(segment));

    return Array.from(terms);
  }

  private normalizeSectorValue(value: string): string {
    return value
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .replace(/\s+/g, ' ')
      .trim();
  }
}
