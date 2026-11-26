import 'server-only';

import englishMessages from '@/locales/en.json';
import portugueseMessages from '@/locales/pt.json';

const PUBLIC_LOCALES_PATH = '/api/locales';
const PUBLIC_API_TIMEOUT_MS = 5_000;
const MAX_RESPONSE_BYTES = 512 * 1024;
const REVALIDATE_SECONDS = 60;

export type PublicLocale = 'en' | 'pt';

const builtInMessages: Readonly<Record<PublicLocale, Record<string, unknown>>> = {
  en: englishMessages,
  pt: portugueseMessages
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isSafeMessageTree(value: unknown, expected: unknown): value is Record<string, unknown> {
  if (typeof expected === 'string') {
    return typeof value === 'string' && value.length <= 2_000 && !value.includes('<') && !value.includes('>');
  }

  if (Array.isArray(expected)) {
    if (!Array.isArray(value)) return false;
    if (expected.every((item) => typeof item === 'string')) {
      return value.length >= 1 && value.length <= 20 && value.every((item) => typeof item === 'string' && item.length <= 120 && !item.includes('<') && !item.includes('>'));
    }

    return value.length === expected.length && value.every((item, index) => isSafeMessageTree(item, expected[index]));
  }

  if (!isRecord(expected) || !isRecord(value)) return false;
  const expectedKeys = Object.keys(expected);
  const valueKeys = Object.keys(value);
  return expectedKeys.length === valueKeys.length &&
    expectedKeys.every((key) => Object.prototype.hasOwnProperty.call(value, key) && isSafeMessageTree(value[key], expected[key]));
}

function getEndpoint(locale: PublicLocale): URL | null {
  const base = process.env.BACKEND_INTERNAL_URL?.trim();
  if (!base) return null;
  try {
    const endpoint = new URL(`${PUBLIC_LOCALES_PATH}/${locale}`, base);
    return endpoint.protocol === 'http:' || endpoint.protocol === 'https:' ? endpoint : null;
  } catch {
    return null;
  }
}

function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === 'AbortError';
}

async function readJson(response: Response): Promise<unknown | null> {
  const contentLength = response.headers.get('content-length');
  if (contentLength && Number(contentLength) > MAX_RESPONSE_BYTES) return null;
  try {
    const text = await response.text();
    return new TextEncoder().encode(text).byteLength <= MAX_RESPONSE_BYTES ? JSON.parse(text) as unknown : null;
  } catch {
    return null;
  }
}

async function fetchMessages(locale: PublicLocale): Promise<Record<string, unknown> | null> {
  const endpoint = getEndpoint(locale);
  if (!endpoint) return null;

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), PUBLIC_API_TIMEOUT_MS);
  try {
    const response = await fetch(endpoint, {
      method: 'GET',
      headers: {Accept: 'application/json'},
      next: {revalidate: REVALIDATE_SECONDS, tags: ['public-locales', `public-locale-${locale}`]},
      signal: controller.signal
    });
    if (!response.ok) return null;

    const payload = await readJson(response);
    if (!isRecord(payload) || payload.locale !== locale || typeof payload.revision !== 'number' ||
        !Number.isInteger(payload.revision) || payload.revision < 1 || !isRecord(payload.messages)) {
      return null;
    }

    return isSafeMessageTree(payload.messages, builtInMessages[locale]) ? payload.messages : null;
  } catch (error: unknown) {
    return isAbortError(error) ? null : null;
  } finally {
    clearTimeout(timeoutId);
  }
}

export async function getPublicLocaleMessages(locale: PublicLocale): Promise<Record<string, unknown>> {
  const messages = await fetchMessages(locale);
  if (messages) return messages;

  // Stable operational event only: never log catalog bodies or upstream details.
  console.warn('public_locale_catalog_fallback', {locale});
  return builtInMessages[locale];
}

export function getBuiltInLocaleMessages(locale: PublicLocale): Record<string, unknown> {
  return builtInMessages[locale];
}
