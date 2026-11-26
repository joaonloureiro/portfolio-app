import {timingSafeEqual} from 'node:crypto';
import {revalidatePath} from 'next/cache';
import {NextResponse} from 'next/server';
import {
  ADMIN_CSRF_COOKIE,
  clearAdminCsrfCookie,
  refreshAdminAccess,
  resolveAdminAccess
} from '@/lib/admin-auth';
import {
  getAdminBackendEndpoint,
  isUpstreamRedirect,
  logAdminBackendError,
  safeRedirectTarget
} from '@/lib/admin-backend';

export const dynamic = 'force-dynamic';

const MAX_REQUEST_BYTES = 256 * 1024;
const MAX_RESPONSE_BYTES = 2 * 1024 * 1024;
const BFF_TIMEOUT_MS = 5_000;
const EMAIL_TEST_TIMEOUT_MS = 20_000;

type RouteContext = {params: Promise<{path: string[]}>};
type ForwardResult =
  | {kind: 'response'; response: Response}
  | {kind: 'timeout'}
  | {kind: 'redirect'}
  | {kind: 'misconfigured'}
  | {kind: 'unavailable'};

function isUuid(value: string) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}

function isProjectId(value: string) {
  return /^(?:[1-9][0-9]{0,8})$/.test(value);
}

function allowedBackendPath(segments: string[], method: string): string | null {
  if (segments.length === 1 && segments[0] === 'me' && method === 'GET') return '/admin/api/me';
  if (segments.length === 1 && segments[0] === 'locales' && (method === 'GET' || method === 'PUT')) return '/admin/api/locales';
  if (segments[0] === 'posts') {
    if (segments.length === 1 && (method === 'GET' || method === 'POST')) return '/admin/api/posts';
    if (segments.length === 2 && isUuid(segments[1]) && ['GET', 'PUT', 'DELETE'].includes(method)) {
      return `/admin/api/posts/${segments[1]}`;
    }
    if (segments.length === 3 && isUuid(segments[1]) && ['publish', 'unpublish'].includes(segments[2]) && method === 'POST') {
      return `/admin/api/posts/${segments[1]}/${segments[2]}`;
    }
    return null;
  }

  if (segments[0] === 'email-delivery') {
    if (segments.length === 1 && (method === 'GET' || method === 'PUT')) return '/admin/api/email-delivery';
    if (segments.length === 2 && segments[1] === 'test' && method === 'POST') return '/admin/api/email-delivery/test';
    return null;
  }

  if (segments[0] !== 'projects') return null;
  if (segments.length === 1 && (method === 'GET' || method === 'POST')) return '/admin/api/projects';
  if (segments.length === 2 && isProjectId(segments[1]) && ['GET', 'PUT', 'DELETE'].includes(method)) {
    return `/admin/api/projects/${segments[1]}`;
  }
  if (segments.length === 3 && isProjectId(segments[1]) && ['publish', 'unpublish'].includes(segments[2]) && method === 'POST') {
    return `/admin/api/projects/${segments[1]}/${segments[2]}`;
  }
  if (segments.length === 2 && segments[1] === 'reorder' && method === 'POST') return '/admin/api/projects/reorder';
  return null;
}

function problem(status: number, errorKey: string, title: string) {
  return NextResponse.json({type: 'about:blank', title, status, errorKey}, {status, headers: {'Cache-Control': 'no-store'}});
}

function sameOrigin(request: Request): boolean {
  const origin = request.headers.get('origin');
  if (!origin) return true;
  const configuredOrigin = process.env.PUBLIC_BASE_URL?.trim() || process.env.BETTER_AUTH_URL?.trim();
  if (!configuredOrigin) return false;
  try {
    return origin === new URL(configuredOrigin).origin;
  } catch {
    return false;
  }
}

function readCookie(request: Request, name: string): string | null {
  const value = request.headers.get('cookie')?.split(';')
    .map((item) => item.trim())
    .find((item) => item.startsWith(`${name}=`))
    ?.slice(name.length + 1);
  if (!value) return null;
  try {
    return decodeURIComponent(value);
  } catch {
    return null;
  }
}

function stateChanging(method: string) {
  return method === 'POST' || method === 'PUT' || method === 'DELETE';
}

function safeTokenEquals(left: string, right: string): boolean {
  const leftBuffer = Buffer.from(left, 'utf8');
  const rightBuffer = Buffer.from(right, 'utf8');
  return leftBuffer.length === rightBuffer.length && timingSafeEqual(leftBuffer, rightBuffer);
}

function invalidatePublicContentPaths(path: string) {
  if (path.startsWith('/admin/api/locales')) {
    revalidatePath('/en');
    revalidatePath('/pt');
    revalidatePath('/[locale]', 'layout');
    return;
  }
  if (path.startsWith('/admin/api/posts')) {
    revalidatePath('/en');
    revalidatePath('/pt');
    revalidatePath('/en/posts');
    revalidatePath('/pt/posts');
    revalidatePath('/[locale]/posts/[slug]', 'page');
    return;
  }
  if (!path.startsWith('/admin/api/projects')) return;
  revalidatePath('/en');
  revalidatePath('/pt');
  revalidatePath('/[locale]/project/[id]', 'page');
}

function isAdminProjectPath(path: string) {
  return path.startsWith('/admin/api/projects');
}

async function readBody(request: Request): Promise<Uint8Array | null | 'too-large' | 'invalid-content-type'> {
  if (!stateChanging(request.method)) return null;
  const contentLength = request.headers.get('content-length');
  if (contentLength) {
    const length = Number(contentLength);
    if (!Number.isSafeInteger(length) || length < 0 || length > MAX_REQUEST_BYTES) return 'too-large';
  }
  if (request.headers.get('content-type')?.split(';', 1)[0].trim().toLowerCase() !== 'application/json') return 'invalid-content-type';
  const body = new Uint8Array(await request.arrayBuffer());
  return body.byteLength > MAX_REQUEST_BYTES ? 'too-large' : body;
}

function clearSession(response: NextResponse) {
  clearAdminCsrfCookie(response);
}

async function forward(request: Request, backendPath: string, accessToken: string, body: Uint8Array | null): Promise<ForwardResult> {
  const endpoint = getAdminBackendEndpoint(backendPath);
  if (!endpoint) {
    logAdminBackendError('invalid-backend-configuration', {path: backendPath});
    return {kind: 'misconfigured'};
  }

  const controller = new AbortController();
  const timeoutMs = backendPath === '/admin/api/email-delivery/test' ? EMAIL_TEST_TIMEOUT_MS : BFF_TIMEOUT_MS;
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);
  const abortRequest = () => controller.abort();
  request.signal.addEventListener('abort', abortRequest, {once: true});
  try {
    const headers = new Headers({Accept: 'application/json', Authorization: `Bearer ${accessToken}`});
    if (body !== null) headers.set('Content-Type', 'application/json');
    const response = await fetch(endpoint, {
      method: request.method,
      headers,
      body: body === null ? undefined : body,
      cache: 'no-store',
      redirect: 'manual',
      signal: controller.signal
    });
    if (isUpstreamRedirect(response.status)) {
      logAdminBackendError('unexpected-upstream-redirect', {
        path: backendPath,
        status: response.status,
        location: safeRedirectTarget(response, endpoint)
      });
      return {kind: 'redirect'};
    }
    return {kind: 'response', response};
  } catch (error: unknown) {
    const timeout = error instanceof Error && error.name === 'AbortError';
    logAdminBackendError(timeout ? 'upstream-timeout' : 'upstream-request-failed', {path: backendPath}, error);
    return {kind: timeout ? 'timeout' : 'unavailable'};
  } finally {
    clearTimeout(timeoutId);
    request.signal.removeEventListener('abort', abortRequest);
  }
}

async function handle(request: Request, context: RouteContext) {
  const method = request.method.toUpperCase();
  const segments = (await context.params).path;
  let backendPath = allowedBackendPath(segments, method);
  if (!backendPath) return problem(404, 'admin_route_not_found', 'Admin route not found.');

  if (backendPath === '/admin/api/locales' && new URL(request.url).search.length > 0) {
    return problem(404, 'admin_route_not_found', 'Admin route not found.');
  }
  if (backendPath.startsWith('/admin/api/email-delivery') && new URL(request.url).search.length > 0) {
    return problem(404, 'admin_route_not_found', 'Admin route not found.');
  }

  if (method === 'GET' && (backendPath === '/admin/api/posts' || backendPath === '/admin/api/projects')) {
    const query = new URL(request.url).searchParams;
    if ([...query.keys()].some((key) => key !== 'page' && key !== 'pageSize')) {
      return problem(404, 'admin_route_not_found', 'Admin route not found.');
    }
    for (const name of ['page', 'pageSize']) {
      const value = query.get(name);
      if (value !== null && (!/^\d+$/.test(value) || Number(value) < 1 || Number(value) > 50)) {
        return problem(400, isAdminProjectPath(backendPath) ? 'project_invalid_input' : 'post_invalid_input', 'Invalid content input.');
      }
    }
    const queryString = query.toString();
    if (queryString) backendPath = `${backendPath}?${queryString}`;
  }

  if (stateChanging(method)) {
    const csrfCookie = readCookie(request, ADMIN_CSRF_COOKIE);
    const csrfHeader = request.headers.get('x-csrf-token');
    if (!sameOrigin(request) || !csrfCookie || !csrfHeader || !safeTokenEquals(csrfCookie, csrfHeader)) {
      return problem(403, 'csrf_invalid', 'Request origin could not be verified.');
    }
  }

  const body = await readBody(request);
  if (body === 'too-large') {
    const errorKey = backendPath.startsWith('/admin/api/projects')
      ? 'project_invalid_input'
      : backendPath.startsWith('/admin/api/locales')
        ? 'locale_invalid_shape'
        : 'post_invalid_input';
    return problem(400, errorKey, 'Invalid content input.');
  }
  if (body === 'invalid-content-type') return problem(415, 'admin_invalid_content_type', 'JSON is required.');

  const authResult = await resolveAdminAccess(new Headers(request.headers));
  if (authResult.status === 'unavailable') {
    return problem(503, 'admin_auth_unavailable', 'Admin authentication is temporarily unavailable.');
  }
  if (authResult.status === 'unauthorized') {
    const response = problem(401, 'authentication_required', 'Admin authentication is required.');
    clearSession(response);
    return response;
  }

  let result = await forward(request, backendPath, authResult.access.accessToken, body);
  if (result.kind === 'response' && result.response.status === 401) {
    const refreshed = await refreshAdminAccess(authResult.access.accountId, new Headers(request.headers));
    if (refreshed.status === 'success') {
      result = await forward(request, backendPath, refreshed.access.accessToken, body);
    } else if (refreshed.status === 'unavailable') {
      return problem(503, 'admin_auth_unavailable', 'Admin authentication is temporarily unavailable.');
    }
  }

  if (result.kind === 'timeout') return problem(504, 'admin_upstream_timeout', 'The Admin service took too long to respond.');
  if (result.kind === 'redirect') return problem(502, 'admin_upstream_redirect', 'The Admin service returned an unexpected redirect.');
  if (result.kind === 'misconfigured') return problem(503, 'admin_backend_not_configured', 'The Admin service is not configured.');
  if (result.kind === 'unavailable') return problem(503, 'admin_service_unavailable', 'The Admin service is unavailable right now.');
  const upstreamResponse = result.response;
  if (upstreamResponse.status === 401) {
    const response = problem(401, 'authentication_required', 'Admin authentication is required.');
    clearSession(response);
    return response;
  }

  const declaredLength = Number(upstreamResponse.headers.get('content-length'));
  if (Number.isFinite(declaredLength) && declaredLength > MAX_RESPONSE_BYTES) {
    logAdminBackendError('upstream-response-too-large', {path: backendPath, bytes: declaredLength});
    return problem(502, 'admin_upstream_invalid_response', 'The Admin service returned an invalid response.');
  }
  const responseBody = await upstreamResponse.arrayBuffer();
  if (responseBody.byteLength > MAX_RESPONSE_BYTES) {
    logAdminBackendError('upstream-response-too-large', {path: backendPath, bytes: responseBody.byteLength});
    return problem(502, 'admin_upstream_invalid_response', 'The Admin service returned an invalid response.');
  }
  const response = new NextResponse(responseBody, {status: upstreamResponse.status});
  const contentType = upstreamResponse.headers.get('content-type');
  if (contentType) response.headers.set('Content-Type', contentType);
  for (const name of ['etag']) {
    const value = upstreamResponse.headers.get(name);
    if (value) response.headers.set(name, value);
  }
  response.headers.set('Cache-Control', 'no-store');
  response.headers.set('Pragma', 'no-cache');
  if (upstreamResponse.ok && stateChanging(method)) invalidatePublicContentPaths(backendPath);
  return response;
}

export async function GET(request: Request, context: RouteContext) { return handle(request, context); }
export async function POST(request: Request, context: RouteContext) { return handle(request, context); }
export async function PUT(request: Request, context: RouteContext) { return handle(request, context); }
export async function DELETE(request: Request, context: RouteContext) { return handle(request, context); }
