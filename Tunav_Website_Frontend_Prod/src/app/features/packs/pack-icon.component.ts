import { Component, Input } from '@angular/core';
import { PackIconKey } from '../../core/models/solution.model';

@Component({
  selector: 'app-pack-icon',
  standalone: true,
  template: `
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-linecap="round"
      stroke-linejoin="round"
      [attr.aria-hidden]="ariaHidden"
      [attr.role]="ariaHidden ? 'presentation' : 'img'"
    >
      @switch (iconKey) {
        @case ('zap') {
          <path stroke-width="2" d="M13 2 4 14h6l-1 8 9-12h-6l1-8Z" />
        }
        @case ('building') {
          <path
            stroke-width="1.9"
            d="M4 21V7.5A1.5 1.5 0 0 1 5.5 6H10v15M10 3h8.5A1.5 1.5 0 0 1 20 4.5V21M8 10h1M8 14h1M14 8h1M14 12h1M14 16h1M17 8h1M17 12h1M17 16h1M3 21h18"
          />
        }
        @case ('tag') {
          <path
            stroke-width="1.9"
            d="M20 13.5 13.5 20 4 10.5V4h6.5L20 13.5ZM7.75 7.75h.5"
          />
        }
        @case ('droplet') {
          <path
            stroke-width="1.9"
            d="M12 3s6 6.1 6 10.2A6 6 0 1 1 6 13.2C6 9.1 12 3 12 3Z"
          />
        }
        @case ('camera') {
          <path
            stroke-width="1.9"
            d="M4.5 8.5h3l1.6-2.1a1 1 0 0 1 .8-.4h4.2a1 1 0 0 1 .8.4L16.5 8.5h3A1.5 1.5 0 0 1 21 10v8.5A1.5 1.5 0 0 1 19.5 20h-15A1.5 1.5 0 0 1 3 18.5V10A1.5 1.5 0 0 1 4.5 8.5Z"
          />
          <circle stroke-width="1.9" cx="12" cy="14" r="3.25" />
        }
        @default {
          <path
            stroke-width="1.9"
            d="M12 20s-6-4.4-6-10a6 6 0 1 1 12 0c0 5.6-6 10-6 10Z"
          />
          <circle stroke-width="1.9" cx="12" cy="10" r="2.5" />
        }
      }
    </svg>
  `,
  styles: [
    `
      :host {
        display: inline-flex;
        inline-size: 1em;
        block-size: 1em;
        flex: 0 0 auto;
      }

      svg {
        inline-size: 100%;
        block-size: 100%;
      }
    `,
  ],
})
export class PackIconComponent {
  @Input() iconKey: PackIconKey = 'map-pin';
  @Input() ariaHidden = true;
}
