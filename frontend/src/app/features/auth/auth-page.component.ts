import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthActions } from '../../state/auth/auth.actions';
import { selectAuthError, selectAuthLoading } from '../../state/auth/auth.selectors';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatInputModule, MatIconModule],
  template: `
    <section class="auth-layout">
      <div class="auth-story">
        <span class="eyebrow">Built for real work</span>
        <h1>{{ isRegister ? 'Open your workshop account.' : 'Welcome back to the bench.' }}</h1>
        <p>Save your cart, track every order, and keep the right tools within reach.</p>
        <div class="proof"><mat-icon>verified_user</mat-icon><span>JWT-secured demo account<br><small>No real payment information is collected.</small></span></div>
      </div>
      <form class="auth-card fm-panel" [formGroup]="form" (ngSubmit)="submit()">
        <span class="eyebrow">{{ isRegister ? 'Create account' : 'Customer & admin access' }}</span>
        <h2>{{ isRegister ? 'Join ForgeMart' : 'Sign in' }}</h2>
        @if (isRegister) {
          <div class="two-columns">
            <mat-form-field appearance="outline"><mat-label>First name</mat-label><input matInput formControlName="firstName" autocomplete="given-name"></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Last name</mat-label><input matInput formControlName="lastName" autocomplete="family-name"></mat-form-field>
          </div>
        }
        <mat-form-field appearance="outline"><mat-label>Email</mat-label><input matInput formControlName="email" type="email" autocomplete="email"><mat-icon matSuffix>mail</mat-icon></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Password</mat-label><input matInput formControlName="password" type="password" [autocomplete]="isRegister ? 'new-password' : 'current-password'"><mat-icon matSuffix>lock</mat-icon></mat-form-field>
        @if (isRegister) { <p class="hint">At least 8 characters with upper/lower case, a number, and a symbol.</p> }
        @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }
        <button mat-flat-button class="submit" type="submit" [disabled]="form.invalid || loading()">
          {{ loading() ? 'Working…' : (isRegister ? 'Create account' : 'Sign in') }}
        </button>
        <p class="switch">{{ isRegister ? 'Already registered?' : 'New to ForgeMart?' }}
          <a [routerLink]="isRegister ? '/login' : '/register'">{{ isRegister ? 'Sign in' : 'Create an account' }}</a>
        </p>
      </form>
    </section>
  `,
  styles: [`
    .auth-layout { min-height: calc(100vh - 190px); display: grid; grid-template-columns: 1fr minmax(360px, 520px); align-items: center; gap: 8vw; padding: 60px max(22px, calc((100vw - 1160px) / 2)); background: radial-gradient(circle at 15% 20%, #384852 0, #1c252b 42%, #11171b 100%); }
    .auth-story { color: white; max-width: 570px; } .auth-story .eyebrow { color: #ff9c5c; } .auth-story h1 { margin: 12px 0 20px; max-width: 10ch; font-size: clamp(2.7rem, 6vw, 5rem); line-height: .94; letter-spacing: -.055em; } .auth-story > p { max-width: 520px; color: #bac4c9; font-size: 1.05rem; line-height: 1.7; }
    .proof { display: flex; align-items: center; gap: 13px; margin-top: 36px; color: #ecf0f2; font-weight: 700; } .proof mat-icon { color: #ff8a3d; } .proof small { color: #9ba8af; font-weight: 400; }
    .auth-card { padding: clamp(26px, 5vw, 48px); } .auth-card h2 { margin: 7px 0 28px; font-size: 2rem; letter-spacing: -.04em; } mat-form-field { width: 100%; } .two-columns { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .hint { margin: -10px 0 20px; color: var(--fm-steel); font-size: .78rem; } .form-error { padding: 10px 12px; border-radius: 8px; color: var(--fm-danger); background: #fff0ef; font-size: .85rem; }
    .submit { width: 100%; min-height: 48px; color: white !important; background: var(--fm-orange) !important; } .switch { margin: 22px 0 0; text-align: center; color: var(--fm-steel); font-size: .87rem; } .switch a { color: var(--fm-orange-deep); font-weight: 800; }
    @media (max-width: 820px) { .auth-layout { grid-template-columns: 1fr; padding-block: 40px; } .auth-story h1 { max-width: 14ch; font-size: 3rem; } .proof { display: none; } }
    @media (max-width: 480px) { .two-columns { grid-template-columns: 1fr; gap: 0; } }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuthPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly store = inject(Store);
  readonly isRegister = inject(ActivatedRoute).snapshot.data['mode'] === 'register';
  readonly loading = this.store.selectSignal(selectAuthLoading);
  readonly error = this.store.selectSignal(selectAuthError);
  readonly form = this.fb.nonNullable.group({
    firstName: ['', this.isRegister ? [Validators.required, Validators.minLength(2)] : []],
    lastName: ['', this.isRegister ? [Validators.required, Validators.minLength(2)] : []],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  submit(): void {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    this.store.dispatch(this.isRegister ? AuthActions.register(value) : AuthActions.login({ email: value.email, password: value.password }));
  }
}

