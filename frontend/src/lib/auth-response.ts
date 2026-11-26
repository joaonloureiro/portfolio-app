import 'server-only';

import type {NextResponse} from 'next/server';

export function isSafeAuthRedirect(url: URL): boolean {
  return url.protocol === 'https:' || (
    process.env.NODE_ENV !== 'production' &&
    url.protocol === 'http:' &&
    (url.hostname === 'localhost' || url.hostname === '127.0.0.1')
  );
}

export function applicationUrl(path: string, requestUrl: string): URL {
  const configured = process.env.PUBLIC_BASE_URL?.trim() || process.env.BETTER_AUTH_URL?.trim();
  if (configured) {
    try {
      const base = new URL(configured);
      if (isSafeAuthRedirect(base)) return new URL(path, base.origin);
    } catch {
      // The authentication configuration reports the actionable failure.
    }
  }
  return new URL(path, requestUrl);
}

export function copySetCookieHeaders(source: Headers | undefined, destination: Headers): void {
  if (!source) return;
  const values = source.getSetCookie();
  if (values.length > 0) {
    for (const value of values) destination.append('Set-Cookie', value);
    return;
  }

  const combined = source.get('set-cookie');
  if (combined) destination.append('Set-Cookie', combined);
}

export function clearFallbackAuthCookies(response: NextResponse): void {
  const secure = process.env.NODE_ENV === 'production';
  for (const name of [
    'portfolio_admin.session_token',
    'portfolio_admin.session_data',
    '__Secure-portfolio_admin.session_token',
    '__Secure-portfolio_admin.session_data'
  ]) {
    response.cookies.set(name, '', {httpOnly: true, maxAge: 0, path: '/', sameSite: 'lax', secure});
  }
}
