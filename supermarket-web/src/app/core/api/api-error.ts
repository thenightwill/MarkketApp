import { HttpErrorResponse } from '@angular/common/http';
import { Translations } from '../i18n/translations';

export interface ApiProblem {
  status: number;
  code: string;
  detail: string;
}

export function toProblem(error: unknown): ApiProblem {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return { status: 0, code: 'NETWORK_ERROR', detail: error.message };
    }

    const body = error.error as { code?: string; detail?: string } | null;
    return {
      status: error.status,
      code: body?.code ?? 'UNKNOWN',
      detail: body?.detail ?? error.message,
    };
  }

  return { status: 0, code: 'UNKNOWN', detail: '' };
}

export function friendlyMessage(error: unknown, errors: Translations['errors']): string {
  const problem = toProblem(error);
  return errors[problem.code as keyof Translations['errors']] ?? problem.detail ?? errors.UNKNOWN;
}
