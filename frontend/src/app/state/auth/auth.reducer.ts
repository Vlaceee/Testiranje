import { createReducer, on } from '@ngrx/store';
import { AuthResponse, CurrentUser } from '../../models/api.models';
import { AuthActions } from './auth.actions';

export interface AuthState {
  token: string | null;
  expiresAt: string | null;
  user: CurrentUser | null;
  loading: boolean;
  error: string | null;
}

const emptyState: AuthState = { token: null, expiresAt: null, user: null, loading: false, error: null };

function fromAuth(auth: AuthResponse | null): AuthState {
  return auth
    ? { token: auth.accessToken, expiresAt: auth.expiresAt, user: auth.user, loading: false, error: null }
    : emptyState;
}

export const authReducer = createReducer(
  emptyState,
  on(AuthActions.hydrate, (_state, { auth }) => fromAuth(auth)),
  on(AuthActions.login, AuthActions.register, state => ({ ...state, loading: true, error: null })),
  on(AuthActions.authSuccess, (_state, { auth }) => fromAuth(auth)),
  on(AuthActions.authFailure, (state, { error }) => ({ ...state, loading: false, error })),
  on(AuthActions.logout, () => emptyState)
);

