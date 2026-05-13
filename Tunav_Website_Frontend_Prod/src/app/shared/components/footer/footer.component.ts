import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss',
})
export class FooterComponent {
  readonly currentYear = new Date().getFullYear();
  emailInput = '';
  phoneInput = '';

  subscribe() {
    if (this.emailInput) {
      alert('Merci ! Vous êtes abonné à la newsletter TUNAV.');
      this.emailInput = '';
      this.phoneInput = '';
    }
  }
}
