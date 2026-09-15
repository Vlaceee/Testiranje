import { Injectable } from '@angular/core';
import { AuthResponse } from '../models/api.models';

const AUTH_KEY = 'forgemart.auth.v1';

@Injectable({ providedIn: 'root' })
export class AuthStorageService {
  read(): AuthResponse | null {
    try {
      const raw = localStorage.getItem(AUTH_KEY);
      if (!raw) return null;
      const auth = JSON.parse(raw) as AuthResponse;
      if (Date.parse(auth.expiresAt) <= Date.now()) {
        this.clear();
        return null;
      }
      return auth;
    } catch {
      this.clear();
      return null;
    }
  }

  write(auth: AuthResponse): void { localStorage.setItem(AUTH_KEY, JSON.stringify(auth)); }
  clear(): void { localStorage.removeItem(AUTH_KEY); }
  token(): string | null { return this.read()?.accessToken ?? null; }
}

