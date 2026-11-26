import {NextResponse} from 'next/server';
import {getAuth} from '@/lib/auth';
import {getAuthSettings} from '@/lib/auth-settings';
import {clearAdminCsrfCookie, logAdminAuthError} from '@/lib/admin-auth';
import {clearFallbackAuthCookies, copySetCookieHeaders, isSafeAuthRedirect} from '@/lib/auth-response';

export const dynamic = 'force-dynamic';

export async function GET(request: Request) {
  let destination = new URL('/', request.url);
  let authHeaders: Headers | undefined;

  try {
    const settings = getAuthSettings();
    destination = new URL(settings.postLogoutRedirectUrl);
    const auth = await getAuth();
    const result = await auth.api.signOut({
      headers: request.headers,
      body: {callbackURL: settings.postLogoutRedirectUrl, disableRedirect: true},
      returnHeaders: true
    });
    authHeaders = result.headers;
    if (result.response.url) {
      const providerDestination = new URL(result.response.url);
      if (!isSafeAuthRedirect(providerDestination)) throw new Error('OIDC provider returned an unsafe logout URL.');
      destination = providerDestination;
    }
  } catch (error: unknown) {
    logAdminAuthError('sign-out-failed', error);
  }

  const response = NextResponse.redirect(destination, 302);
  copySetCookieHeaders(authHeaders, response.headers);
  clearFallbackAuthCookies(response);
  clearAdminCsrfCookie(response);
  return response;
}
