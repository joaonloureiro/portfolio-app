import 'server-only';

import {join} from 'node:path';

const LOCAL_FRONTEND_URL = 'http://localhost:3000';

export const AUTHENTIK_PROVIDER_ID = 'authentik';

export type AuthSettings = Readonly<{
  baseUrl: string;
  clientId: string;
  clientSecret: string;
  databasePath: string;
  discoveryUrl: string;
  postLogoutRedirectUrl: string;
  scopes: string[];
  secret: string;
}>;

export class AuthConfigurationError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'AuthConfigurationError';
  }
}

function required(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) throw new AuthConfigurationError(`${name} is required.`);
  return value;
}

function parseUrl(name: string, value: string, allowPath: boolean): URL {
  let parsed: URL;
  try {
    parsed = new URL(value);
  } catch {
    throw new AuthConfigurationError(`${name} must be an absolute URL.`);
  }

  const localHttp = process.env.NODE_ENV !== 'production' &&
    (parsed.hostname === 'localhost' || parsed.hostname === '127.0.0.1');
  if (parsed.protocol !== 'https:' && !(parsed.protocol === 'http:' && localHttp)) {
    throw new AuthConfigurationError(`${name} must use HTTPS outside local development.`);
  }
  if (parsed.username || parsed.password || parsed.search || parsed.hash) {
    throw new AuthConfigurationError(`${name} must not contain credentials, a query, or a fragment.`);
  }
  if (!allowPath && parsed.pathname !== '/') {
    throw new AuthConfigurationError(`${name} must contain only an origin.`);
  }

  return parsed;
}

function issuerDiscoveryUrl(issuer: URL): string {
  const issuerWithSlash = issuer.toString().endsWith('/') ? issuer.toString() : `${issuer.toString()}/`;
  return new URL('.well-known/openid-configuration', issuerWithSlash).toString();
}

export function getAuthSettings(): AuthSettings {
  const baseUrl = parseUrl(
    'PUBLIC_BASE_URL',
    process.env.PUBLIC_BASE_URL?.trim() || process.env.BETTER_AUTH_URL?.trim() || LOCAL_FRONTEND_URL,
    false
  );
  const issuer = parseUrl('OIDC_ISSUER', required('OIDC_ISSUER'), true);
  const configuredDiscovery = process.env.OIDC_DISCOVERY_ADDRESS?.trim();
  const discoveryUrl = configuredDiscovery
    ? parseUrl('OIDC_DISCOVERY_ADDRESS', configuredDiscovery, true).toString()
    : issuerDiscoveryUrl(issuer);
  const postLogoutRedirectUrl = parseUrl(
    'OIDC_POST_LOGOUT_REDIRECT_URI',
    process.env.OIDC_POST_LOGOUT_REDIRECT_URI?.trim() || baseUrl.toString(),
    true
  );
  if (postLogoutRedirectUrl.origin !== baseUrl.origin) {
    throw new AuthConfigurationError('OIDC_POST_LOGOUT_REDIRECT_URI must use PUBLIC_BASE_URL origin.');
  }

  const secret = required('BETTER_AUTH_SECRET');
  if (secret.length < 32) throw new AuthConfigurationError('BETTER_AUTH_SECRET must contain at least 32 characters.');

  const scopes = (process.env.OIDC_SCOPE?.trim() || 'openid profile email offline_access')
    .split(/\s+/)
    .filter((scope, index, all) => scope.length > 0 && all.indexOf(scope) === index);
  if (!scopes.includes('openid')) throw new AuthConfigurationError('OIDC_SCOPE must include openid.');

  const configuredDatabasePath = process.env.AUTH_DATABASE_PATH?.trim();
  if (process.env.NODE_ENV === 'production' && !configuredDatabasePath) {
    throw new AuthConfigurationError('AUTH_DATABASE_PATH is required in production.');
  }

  return {
    baseUrl: baseUrl.origin,
    clientId: required('OIDC_CLIENT_ID'),
    clientSecret: required('OIDC_CLIENT_SECRET'),
    databasePath: configuredDatabasePath || join(process.cwd(), '.data', 'auth.db'),
    discoveryUrl,
    postLogoutRedirectUrl: postLogoutRedirectUrl.toString(),
    scopes,
    secret
  };
}

export function expectedOidcCallbackUrl(baseUrl: string): string {
  return new URL(`/api/auth/callback/${AUTHENTIK_PROVIDER_ID}`, baseUrl).toString();
}
