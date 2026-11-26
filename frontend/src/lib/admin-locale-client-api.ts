'use client';

import type {AdminLocaleCatalog, AdminLocaleChange, AdminLocaleFailureStatus, AdminLocaleResult} from '@/app/admin/admin-types';
import {parseAdminLocaleCatalog} from './locale-catalog-validation';

const ADMIN_LOCALES_PATH = '/admin/api/locales';

const messages: Readonly<Record<string, string>> = {
  locale_invalid_shape: 'Check the locale fields and try again.',
  locale_unknown_key: 'That locale key is not editable.',
  locale_invalid_value: 'One or more locale values exceed their limits.',
  locale_stale_revision: 'This catalog changed in another tab. Reload it before publishing again.',
  locale_storage_unavailable: 'The locale catalog is unavailable right now.',
  csrf_invalid: 'This form expired. Reload the page and try again.'
};

const fallback: Readonly<Record<AdminLocaleFailureStatus, string>> = {
  validation: 'Check the locale fields and try again.',
  unauthorized: 'Admin authentication could not be verified.',
  forbidden: 'This identity is not authorized for the Admin area.',
  conflict: 'This catalog changed in another tab. Reload it before publishing again.',
  timeout: 'The locale service took too long to respond.',
  'malformed-response': 'The locale service returned an invalid response.',
  unavailable: 'The locale service is unavailable right now.'
};

function csrfToken(): string | null {
  const item = document.cookie.split(';').map((part) => part.trim()).find((part) => part.startsWith('portfolio_admin_csrf='));
  if (!item) return null;
  try {
    return decodeURIComponent(item.slice('portfolio_admin_csrf='.length));
  } catch {
    return null;
  }
}

function readErrorKey(value: unknown): string | undefined {
  return typeof value === 'object' && value !== null && 'errorKey' in value && typeof value.errorKey === 'string'
    ? value.errorKey
    : undefined;
}

function failure<T>(status: Exclude<AdminLocaleResult<T>['status'], 'success'>, errorKey?: string): AdminLocaleResult<T> {
  return {status, errorKey, message: errorKey ? messages[errorKey] ?? fallback[status] : fallback[status]};
}

function statusForResponse(status: number): AdminLocaleFailureStatus | null {
  if (status === 400 || status === 415) return 'validation';
  if (status === 401) return 'unauthorized';
  if (status === 403) return 'forbidden';
  if (status === 409) return 'conflict';
  if (status === 504) return 'timeout';
  return status >= 500 ? 'unavailable' : null;
}

export async function updateAdminLocaleCatalog(
  expectedRevision: number,
  changes: Readonly<Record<string, AdminLocaleChange>>
): Promise<AdminLocaleResult<AdminLocaleCatalog>> {
  const csrf = csrfToken();
  if (!csrf) return failure('forbidden', 'csrf_invalid');

  try {
    const response = await fetch(ADMIN_LOCALES_PATH, {
      method: 'PUT',
      headers: {'Accept': 'application/json', 'Content-Type': 'application/json', 'X-CSRF-Token': csrf},
      body: JSON.stringify({expectedRevision, changes}),
      credentials: 'same-origin',
      cache: 'no-store'
    });
    let payload: unknown = null;
    try {
      payload = await response.json();
    } catch {
      payload = null;
    }

    const status = statusForResponse(response.status);
    if (status) return failure(status, readErrorKey(payload));
    if (!response.ok) return failure('unavailable', readErrorKey(payload));
    const catalog = parseAdminLocaleCatalog(payload, true);
    return catalog === null ? failure('malformed-response') : {status: 'success', data: catalog};
  } catch {
    return failure('unavailable');
  }
}
