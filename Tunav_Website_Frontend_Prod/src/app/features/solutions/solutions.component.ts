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
import { RouterLink } from '@angular/router';
import { Solution } from '../../core/models/solution.model';
import { SolutionsApiService } from '../../core/services/solutions-api.service';
import { DemoRequestFormComponent } from '../../shared/demo-request-form/demo-request-form.component';
import { SolutionsViewModel } from './solutions.viewmodel';

const FALLBACK_IMAGE_MAP: Record<string, string> = {
  'tagit rfid': '/assets/solutions/tagit-rfid.png',
  'easytrace 360': '/assets/solutions/easytrace-360.png',
  'disjoncteur intelligent': '/assets/solutions/disjoncteur.png',
  'fuel rescue': '/assets/solutions/fuel-rescue.png',
  'easy trace v3': '/assets/solutions/easy-trace-v3.png',
  dashcam: '/assets/solutions/dashcam.png',
};

const DEFAULT_IMAGE = '/assets/solutions/tagit-rfid.png';

@Component({
  selector: 'app-solutions',
  standalone: true,
  imports: [FormsModule, RouterLink, DemoRequestFormComponent],
  templateUrl: './solutions.component.html',
  styleUrl: './solutions.component.scss',
  providers: [SolutionsViewModel],
})
export class SolutionsComponent implements OnInit {
  readonly activeDemoSolution = signal<Solution | null>(null);

  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  constructor(
    public readonly vm: SolutionsViewModel,
    private readonly solutionsApi: SolutionsApiService
  ) {
    this.destroyRef.onDestroy(() => this.unlockBodyScroll());
  }

  ngOnInit(): void {
    this.vm.initialize();
  }

  @HostListener('document:keydown.escape')
  handleEscapeKey(): void {
    if (this.activeDemoSolution()) {
      this.closeDemoModal();
    }
  }

  getImageUrl(solution: Solution): string {
    const backendImage = this.solutionsApi.resolveMediaUrl(solution.coverImageUrl);
    if (backendImage) {
      return backendImage;
    }

    const fallback = FALLBACK_IMAGE_MAP[solution.title.toLowerCase().trim()];
    return fallback ?? DEFAULT_IMAGE;
  }

  getVisibleSectors(solution: Solution): string[] {
    if (solution.sectors.length > 0) {
      return solution.sectors.slice(0, 4);
    }

    return solution.sectorName ? [solution.sectorName] : [];
  }

  getVisibleTopClients(solution: Solution): string[] {
    return solution.topClients.slice(0, 3);
  }

  isSectorSelected(sector: string): boolean {
    return this.vm.selectedSectors().includes(sector);
  }

  openDemoModal(solution: Solution): void {
    this.activeDemoSolution.set(solution);
    this.lockBodyScroll();
  }

  closeDemoModal(): void {
    this.activeDemoSolution.set(null);
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
}
