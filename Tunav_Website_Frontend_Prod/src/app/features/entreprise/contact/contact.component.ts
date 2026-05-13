import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './contact.component.html',
  styleUrl: './contact.component.scss',
})
export class ContactComponent {
  private readonly api = environment.apiOrigin;

  // ── Formulaire contact ────────────────────────────────────────────────────
  contactForm = {
    firstName: '', lastName: '', email: '', phone: '',
    company: '', subject: '', otherSubject: '', message: '', consent: false,
  };
  contactSubmitting = signal(false);
  contactSuccess    = signal(false);
  contactError      = signal('');

  // ── Modal Rendez-vous ─────────────────────────────────────────────────────
  showDemoModal  = signal(false);
  demoForm = { firstName: '', lastName: '', email: '', hasWhatsApp: '', whatsAppNumber: '' };
  demoSubmitting = signal(false);
  demoSuccess    = signal(false);
  demoError      = signal('');

  constructor(private http: HttpClient) {}

  // ── Soumettre le formulaire de contact ────────────────────────────────────
  submitContact() {
    const f = this.contactForm;
    if (!f.firstName || !f.lastName || !f.email || !f.subject || !f.message) {
      this.contactError.set('Veuillez remplir tous les champs obligatoires.');
      return;
    }
    if (f.subject === 'Autre' && !f.otherSubject?.trim()) {
      this.contactError.set('Veuillez préciser votre sujet.');
      return;
    }
    if (!f.consent) {
      this.contactError.set('Vous devez accepter la politique de confidentialité.');
      return;
    }
    this.contactSubmitting.set(true);
    this.contactError.set('');

    const payload = {
      firstName:    f.firstName,
      lastName:     f.lastName,
      email:        f.email,
      phone:        f.phone,
      company:      f.company,
      subject:      f.subject,
      otherSubject: f.subject === 'Autre' ? f.otherSubject.trim() : null,
      message:      f.message,
      consent:      f.consent,
    };

    this.http.post(`${this.api}/api/contact`, payload).subscribe({
      next: () => {
        this.contactSubmitting.set(false);
        this.contactSuccess.set(true);
        this.contactForm = { firstName: '', lastName: '', email: '', phone: '', company: '', subject: '', otherSubject: '', message: '', consent: false };
      },
      error: (err) => {
        this.contactSubmitting.set(false);
        this.contactError.set(err?.error?.message || 'Une erreur est survenue.');
      }
    });
  }

  // ── Ouvrir/fermer modal démo ──────────────────────────────────────────────
  openDemoModal() {
    this.demoForm = { firstName: '', lastName: '', email: '', hasWhatsApp: '', whatsAppNumber: '' };
    this.demoSuccess.set(false);
    this.demoError.set('');
    this.showDemoModal.set(true);
    document.body.style.overflow = 'hidden';
  }

  closeDemoModal() {
    this.showDemoModal.set(false);
    document.body.style.overflow = '';
  }

  // ── Soumettre la demande de démo ──────────────────────────────────────────
  submitDemo() {
    const f = this.demoForm;
    if (!f.firstName || !f.lastName || !f.email || !f.hasWhatsApp) {
      this.demoError.set('Veuillez remplir tous les champs obligatoires.');
      return;
    }
    if (f.hasWhatsApp === 'true' && !f.whatsAppNumber) {
      this.demoError.set('Veuillez saisir votre numéro WhatsApp.');
      return;
    }
    this.demoSubmitting.set(true);
    this.demoError.set('');

    const payload = {
      ...f,
      hasWhatsApp: f.hasWhatsApp === 'true',
      whatsAppNumber: f.hasWhatsApp === 'true' ? f.whatsAppNumber : null,
    };
    this.http.post(`${this.api}/api/contact/demo`, payload).subscribe({
      next: () => {
        this.demoSubmitting.set(false);
        this.demoSuccess.set(true);
      },
      error: (err) => {
        this.demoSubmitting.set(false);
        this.demoError.set(err?.error?.message || 'Une erreur est survenue.');
      }
    });
  }
}
