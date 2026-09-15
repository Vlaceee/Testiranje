import { HttpErrorResponse } from '@angular/common/http';
import { ApiProblem } from '../models/api.models';

export function apiErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as ApiProblem | undefined;
    if (problem?.detail) return problem.detail;
    if (problem?.errors) return Object.values(problem.errors).flat().join(' ');
    return error.status === 0 ? 'The ForgeMart API is unavailable.' : `Request failed (${error.status}).`;
  }
  return 'An unexpected error occurred.';
}

