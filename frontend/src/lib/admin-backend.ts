import 'server-only';

const LOCAL_BACKEND_URL = 'http://localhost:3000';

export function getAdminBackendEndpoint(path: string): URL | null {
  const configured = process.env.BACKEND_INTERNAL_URL?.trim();
  if (!configured && process.env.NODE_ENV === 'production') return null;

  try {
    const base = new URL(configured || LOCAL_BACKEND_URL);
    if ((base.protocol !== 'http:' && base.protocol !== 'https:') || base.username || base.password || base.search || base.hash) {
      return null;
    }
    return new URL(path, base);
  } catch {
    return null;
  }
}

export function isUpstreamRedirect(status: number): boolean {
  return status >= 300 && status < 400;
}

function safeError(error: unknown) {
  return {name: error instanceof Error ? error.name : 'UnknownError'};
}

export function logAdminBackendError(
  event: string,
  details: Readonly<Record<string, string | number | undefined>> = {},
  error?: unknown
): void {
  console.error(`[admin-backend] ${event}`, {
    ...details,
    ...(error === undefined ? {} : safeError(error))
  });
}

export function safeRedirectTarget(response: Response, endpoint: URL): string | undefined {
  const location = response.headers.get('location');
  if (!location) return undefined;
  try {
    const target = new URL(location, endpoint);
    return `${target.origin}${target.pathname}`;
  } catch {
    return 'invalid-location';
  }
}
