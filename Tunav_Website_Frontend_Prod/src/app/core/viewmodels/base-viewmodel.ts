import { signal } from '@angular/core';

export abstract class BaseViewModel {
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  protected startLoading(): void {
    this.loading.set(true);
    this.error.set(null);
  }

  protected endLoading(): void {
    this.loading.set(false);
  }

  protected setError(message: string): void {
    this.error.set(message);
    this.loading.set(false);
  }
}

