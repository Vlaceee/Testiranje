import { AuthResponse } from '../models/api.models';
import { AuthStorageService } from './auth-storage.service';

function session(expiresAt = '2099-01-01T00:00:00Z'): AuthResponse {
  return {
    accessToken: 'test-token', expiresAt,
    user: { id: 'user-1', email: 'customer@forgemart.test', firstName: 'Test', lastName: 'Customer', role: 'Customer', isActive: true }
  };
}

describe('AuthStorageService', () => {
  let service: AuthStorageService;

  beforeEach(() => {
    localStorage.clear();
    service = new AuthStorageService();
  });

  afterEach(() => localStorage.clear());

  it('returns null when no session is stored', () => expect(service.read()).toBeNull());
  it('round-trips a valid session', () => {
    service.write(session());
    expect(service.read()).toEqual(session());
  });
  it('returns the current token', () => {
    service.write(session());
    expect(service.token()).toBe('test-token');
  });
  it('clears an explicitly removed session', () => {
    service.write(session());
    service.clear();
    expect(service.read()).toBeNull();
  });
  it('removes expired sessions', () => {
    service.write(session('2000-01-01T00:00:00Z'));
    expect(service.read()).toBeNull();
    expect(localStorage.length).toBe(0);
  });
  it('removes malformed JSON', () => {
    localStorage.setItem('forgemart.auth.v1', '{not-json');
    expect(service.read()).toBeNull();
    expect(localStorage.length).toBe(0);
  });
  it('returns no token for an expired session', () => {
    service.write(session('2000-01-01T00:00:00Z'));
    expect(service.token()).toBeNull();
  });
});
