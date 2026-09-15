import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { AuthResponse } from '../../models/api.models';

export const AuthActions = createActionGroup({
  source: 'Auth',
  events: {
    Hydrate: props<{ auth: AuthResponse | null }>(),
    Login: props<{ email: string; password: string }>(),
    Register: props<{ email: string; password: string; firstName: string; lastName: string }>(),
    'Auth success': props<{ auth: AuthResponse }>(),
    'Auth failure': props<{ error: string }>(),
    Logout: emptyProps()
  }
});

