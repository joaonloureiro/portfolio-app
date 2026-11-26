import {NextResponse} from 'next/server';
import {getAuth} from '@/lib/auth';
import {AUTHENTIK_PROVIDER_ID, getAuthSettings} from '@/lib/auth-settings';
import {createAdminCsrfToken, logAdminAuthError, setAdminCsrfCookie} from '@/lib/admin-auth';
import {applicationUrl, copySetCookieHeaders, isSafeAuthRedirect} from '@/lib/auth-response';

export const dynamic = 'force-dynamic';

export async function GET(request: Request) {
  try {
    const settings = getAuthSettings();
    const auth = await getAuth();
    const result = await auth.api.signInSocial({
      headers: request.headers,
      body: {
        provider: AUTHENTIK_PROVIDER_ID,
        callbackURL: new URL('/admin', settings.baseUrl).toString(),
        errorCallbackURL: new URL('/admin?auth=failed', settings.baseUrl).toString(),
        disableRedirect: true
      },
      returnHeaders: true
    });
    if (!result.response.url) throw new Error('OIDC provider did not return an authorization URL.');

    const destination = new URL(result.response.url);
    if (!isSafeAuthRedirect(destination)) {
      throw new Error('OIDC provider returned an unsafe authorization URL.');
    }

    const response = NextResponse.redirect(destination, 302);
    copySetCookieHeaders(result.headers, response.headers);
    setAdminCsrfCookie(response, createAdminCsrfToken());
    return response;
  } catch (error: unknown) {
    logAdminAuthError('sign-in-start-failed', error);
    return NextResponse.redirect(applicationUrl('/admin?auth=unavailable', request.url), 302);
  }
}
