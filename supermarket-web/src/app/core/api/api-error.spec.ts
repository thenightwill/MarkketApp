import { HttpErrorResponse } from '@angular/common/http';
import { TRANSLATIONS } from '../i18n/translations';
import { friendlyMessage, toProblem } from './api-error';

function httpError(status: number, body: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: body, statusText: 'x' });
}

describe('api-error', () => {
  it('extracts the code and detail from a problem response', () => {
    const problem = toProblem(httpError(409, { code: 'DUPLICATE_PRODUCT', detail: 'already exists' }));

    expect(problem).toEqual({ status: 409, code: 'DUPLICATE_PRODUCT', detail: 'already exists' });
  });

  it('translates known codes into friendly Spanish messages', () => {
    const errors = TRANSLATIONS.es.errors;

    expect(friendlyMessage(httpError(409, { code: 'INSUFFICIENT_STOCK' }), errors)).toContain('stock');
    expect(friendlyMessage(httpError(409, { code: 'INVALID_TASK_TRANSITION' }), errors)).toContain('Pendiente');
    expect(friendlyMessage(httpError(401, { code: 'INVALID_CREDENTIALS' }), errors)).toContain('incorrectos');
  });

  it('translates known codes into friendly English messages', () => {
    const errors = TRANSLATIONS.en.errors;

    expect(friendlyMessage(httpError(409, { code: 'INSUFFICIENT_STOCK' }), errors)).toContain('stock');
    expect(friendlyMessage(httpError(409, { code: 'INVALID_TASK_TRANSITION' }), errors)).toContain('Pending');
    expect(friendlyMessage(httpError(401, { code: 'INVALID_CREDENTIALS' }), errors)).toContain('Incorrect');
  });

  it('falls back to the server detail for unknown codes', () => {
    expect(friendlyMessage(httpError(400, { code: 'SOMETHING_NEW', detail: 'custom detail' }), TRANSLATIONS.es.errors)).toBe(
      'custom detail',
    );
  });

  it('reports network failures', () => {
    const problem = toProblem(new HttpErrorResponse({ status: 0 }));

    expect(problem.code).toBe('NETWORK_ERROR');
  });

  it('handles non-http errors with the generic unknown-error message', () => {
    expect(friendlyMessage(new Error('boom'), TRANSLATIONS.es.errors)).toBe(TRANSLATIONS.es.errors.UNKNOWN);
    expect(friendlyMessage(new Error('boom'), TRANSLATIONS.en.errors)).toBe(TRANSLATIONS.en.errors.UNKNOWN);
  });

  it('handles responses without a body', () => {
    expect(toProblem(httpError(500, null)).code).toBe('UNKNOWN');
  });
});
