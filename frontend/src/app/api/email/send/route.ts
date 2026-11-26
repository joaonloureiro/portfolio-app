import {NextResponse} from 'next/server';

export const dynamic = 'force-dynamic';

const MAX_REQUEST_BYTES = 16 * 1024;
const UPSTREAM_TIMEOUT_MS = 20_000;
const SAFE_ERROR_KEYS = new Set([
  'contact_invalid_input',
  'contact_payload_too_large',
  'contact_throttled',
  'contact_delivery_disabled',
  'contact_delivery_timeout',
  'contact_delivery_unavailable',
  'status_error_all_fields',
  'status_error_invalid_email'
]);

function problem(status: number, errorKey: string, title: string) {
  return NextResponse.json({type: 'about:blank', title, status, errorKey}, {status});
}

async function readBoundedBody(request: Request): Promise<Uint8Array | 'too-large'> {
  if (!request.body) return new Uint8Array();
  const reader = request.body.getReader();
  const chunks: Uint8Array[] = [];
  let total = 0;
  try {
    while (true) {
      const {done, value} = await reader.read();
      if (done) break;
      if (!value) continue;
      total += value.byteLength;
      if (total > MAX_REQUEST_BYTES) return 'too-large';
      chunks.push(value);
    }
  } finally {
    reader.releaseLock();
  }

  const body = new Uint8Array(total);
  let offset = 0;
  for (const chunk of chunks) {
    body.set(chunk, offset);
    offset += chunk.byteLength;
  }
  return body;
}

export async function POST(request: Request) {
  const base = process.env.BACKEND_INTERNAL_URL?.trim();
  if (!base) return problem(503, 'contact_delivery_unavailable', 'Contact delivery is unavailable.');
  if (request.headers.get('content-type')?.split(';', 1)[0].trim().toLowerCase() !== 'application/json') {
    return problem(415, 'contact_invalid_input', 'JSON is required.');
  }

  const contentLength = request.headers.get('content-length');
  if (contentLength && Number(contentLength) > MAX_REQUEST_BYTES) {
    return problem(413, 'contact_payload_too_large', 'Contact message is too large.');
  }

  const body = await readBoundedBody(request);
  if (body === 'too-large') return problem(413, 'contact_payload_too_large', 'Contact message is too large.');

  let endpoint: URL;
  try {
    endpoint = new URL('/api/email/send', base);
    if (endpoint.protocol !== 'http:' && endpoint.protocol !== 'https:') throw new Error('Unsupported backend URL.');
  } catch {
    return problem(503, 'contact_delivery_unavailable', 'Contact delivery is unavailable.');
  }

  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), UPSTREAM_TIMEOUT_MS);
  try {
    const response = await fetch(endpoint, {
      method: 'POST',
      headers: {'Accept': 'application/json', 'Content-Type': 'application/json'},
      body,
      cache: 'no-store',
      signal: controller.signal
    });
    const payload = await response.json().catch(() => null) as unknown;
    if (response.ok) return NextResponse.json(payload ?? {message: 'Message sent successfully!'}, {status: response.status});

    const errorKey = typeof payload === 'object' && payload !== null && 'errorKey' in payload && typeof payload.errorKey === 'string' && SAFE_ERROR_KEYS.has(payload.errorKey)
      ? payload.errorKey
      : response.status === 429
        ? 'contact_throttled'
        : response.status === 504
          ? 'contact_delivery_timeout'
          : response.status >= 500
            ? 'contact_delivery_unavailable'
            : 'contact_invalid_input';
    return problem(response.status, errorKey, 'Contact delivery could not be completed.');
  } catch (error: unknown) {
    return problem(error instanceof Error && error.name === 'AbortError' ? 504 : 503,
      error instanceof Error && error.name === 'AbortError' ? 'contact_delivery_timeout' : 'contact_delivery_unavailable',
      'Contact delivery could not be completed.');
  } finally {
    clearTimeout(timeout);
  }
}
