import 'server-only';

import {mkdirSync} from 'node:fs';
import {dirname} from 'node:path';
import type {DatabaseSync} from 'node:sqlite';
import {betterAuth} from 'better-auth';
import {getMigrations} from 'better-auth/db/migration';
import {nextCookies} from 'better-auth/next-js';
import {genericOAuth} from 'better-auth/plugins/generic-oauth';
import {AUTHENTIK_PROVIDER_ID, getAuthSettings} from './auth-settings';

async function createDatabase(path: string): Promise<DatabaseSync> {
  mkdirSync(dirname(path), {recursive: true});
  const {DatabaseSync} = await import('node:sqlite');
  const database = new DatabaseSync(path);
  database.exec('PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000;');
  return database;
}

async function createAuth() {
  const settings = getAuthSettings();
  const database = await createDatabase(settings.databasePath);

  // Generic OAuth does not add schema fields, so core migrations can run
  // before provider discovery. This keeps a temporary IdP outage from
  // preventing the local session database from being initialized.
  const migrations = await getMigrations({database, secret: settings.secret});
  await migrations.runMigrations();

  return betterAuth({
    appName: 'Portfolio Admin',
    baseURL: settings.baseUrl,
    database,
    secret: settings.secret,
    trustedOrigins: [settings.baseUrl],
    session: {
      expiresIn: 8 * 60 * 60,
      updateAge: 60 * 60
    },
    account: {
      encryptOAuthTokens: true
    },
    advanced: {
      cookiePrefix: 'portfolio_admin',
      useSecureCookies: new URL(settings.baseUrl).protocol === 'https:'
    },
    plugins: [
      genericOAuth({
        config: [{
          providerId: AUTHENTIK_PROVIDER_ID,
          name: 'Authentik',
          discoveryUrl: settings.discoveryUrl,
          requireIdTokenVerification: true,
          clientId: settings.clientId,
          clientSecret: settings.clientSecret,
          tokenEndpointAuth: {method: 'client_secret_basic'},
          scopes: settings.scopes,
          pkce: true,
          postLogoutRedirectURI: settings.postLogoutRedirectUrl
        }]
      }),
      nextCookies()
    ]
  });
}

let authPromise: ReturnType<typeof createAuth> | undefined;

export function getAuth() {
  if (!authPromise) {
    authPromise = createAuth().catch((error: unknown) => {
      authPromise = undefined;
      throw error;
    });
  }
  return authPromise;
}

export async function handleAuthRequest(request: Request): Promise<Response> {
  return (await getAuth()).handler(request);
}
