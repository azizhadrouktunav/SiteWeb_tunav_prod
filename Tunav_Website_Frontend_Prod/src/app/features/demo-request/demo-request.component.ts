import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DemoRequestFormComponent } from '../../shared/demo-request-form/demo-request-form.component';

@Component({
  selector: 'app-demo-request',
  standalone: true,
  imports: [RouterLink, DemoRequestFormComponent],
  templateUrl: './demo-request.component.html',
  styleUrl: './demo-request.component.scss',
})
export class DemoRequestComponent {}
