import { DOCUMENT, isPlatformBrowser, NgFor, NgIf } from '@angular/common';
import {
  Component,
  DestroyRef,
  HostListener,
  inject,
  PLATFORM_ID,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs/operators';
import { MenuItem, MenuItemWithChildren } from '../../../core/models/menu-item.model';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, NgFor, NgIf],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss',
})
export class NavbarComponent {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);

  /** Drawer ouvert (≤991px). */
  mobileMenuOpen = false;

  constructor() {
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.closeMobileMenu());
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.mobileMenuOpen) {
      this.closeMobileMenu();
    }
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    if (window.innerWidth > 991 && this.mobileMenuOpen) {
      this.closeMobileMenu();
    }
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen = !this.mobileMenuOpen;
    this.syncBodyScroll();
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen = false;
    this.closeDropdown();
    this.syncBodyScroll();
  }

  private syncBodyScroll(): void {
    this.document.body.style.overflow = this.mobileMenuOpen ? 'hidden' : '';
  }

  /** Liens simples : Accueil, Nos Solutions, Nos Packs */
  readonly mainMenuItems: MenuItem[] = [
    { label: 'Accueil', path: '/home' },
    { label: 'Nos Solutions', path: '/solutions' },
    { label: 'Nos Packs', path: '/packs' },
  ];

  /** Actualité : menu déroulant (Blog, Event, Newsletters) */
  readonly actualiteMenu: MenuItemWithChildren = {
    label: 'Actualité',
    path: '/news',
    children: [
      { label: 'Blog', path: '/news/blog' },
      { label: 'Event', path: '/news/event' },
      { label: 'Newsletters', path: '/news/newsletters' },
    ],
  };

  /** Entreprise : menu déroulant (À propos, Carrière, Pôle Formation, Contact) */
  readonly entrepriseMenu: MenuItemWithChildren = {
    label: 'Entreprise',
    path: '/about',
    children: [
      { label: 'À propos TUNAV', path: '/about' },
      { label: 'Carrière', path: '/career' },
      { label: 'Pôle Formation', path: '/formation' },
      { label: 'Contactez-nous', path: '/contact' },
    ],
  };

  /** Lien à droite */
  readonly partnerLink: MenuItem = {
    label: 'Devenir Partenaire',
    path: '/partners',
  };

  readonly demoCta: MenuItem = {
    label: 'Demander une démo',
    path: '/demo-request',
  };

  readonly showTestRubrique = false;
  readonly testRubriqueItem: MenuItem = {
    label: 'Test Rubrique',
    path: '/test-rubrique',
  };

  /** Quel dropdown est ouvert (pour affichage mobile / clic) */
  openDropdown: 'actualite' | 'entreprise' | null = null;

  toggleDropdown(which: 'actualite' | 'entreprise'): void {
    this.openDropdown = this.openDropdown === which ? null : which;
  }

  closeDropdown(): void {
    this.openDropdown = null;
  }
}

