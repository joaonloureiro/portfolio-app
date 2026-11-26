import "server-only";

import { randomBytes } from "node:crypto";
import { headers } from "next/headers";
import type { NextResponse } from "next/server";
import { APIError } from "better-auth/api";
import { AUTHENTIK_PROVIDER_ID, AuthConfigurationError } from "./auth-settings";
import { getAuth } from "./auth";

export const ADMIN_CSRF_COOKIE = "portfolio_admin_csrf";

export type AdminAccess = Readonly<{
  accessToken: string;
  accountId: string;
}>;

export type AdminAccessResult =
  | { status: "success"; access: AdminAccess }
  | { status: "unauthorized" }
  | { status: "unavailable" };

function safeError(error: unknown) {
  const details: Record<string, string | number> = {
    name: error instanceof Error ? error.name : "UnknownError",
  };
  if (error instanceof APIError) {
    details.status = error.status;
    if (typeof error.body?.code === "string") details.code = error.body.code;
  } else if (error instanceof AuthConfigurationError) {
    details.reason = error.message;
  }
  return details;
}

export function logAdminAuthError(event: string, error: unknown): void {
  console.error(`[admin-auth] ${event}`, safeError(error));
}

function requiresNewLogin(error: unknown): boolean {
  if (!(error instanceof APIError)) return false;
  return (
    error.status === "UNAUTHORIZED" ||
    error.body?.code === "ACCOUNT_NOT_FOUND" ||
    error.body?.code === "FAILED_TO_GET_ACCESS_TOKEN" ||
    error.body?.code === "FAILED_TO_REFRESH_ACCESS_TOKEN"
  );
}

async function requestHeaders(provided?: Headers): Promise<Headers> {
  return provided ?? new Headers(await headers());
}

export async function resolveAdminAccess(
  providedHeaders?: Headers,
): Promise<AdminAccessResult> {
  try {
    const requestHeadersValue = await requestHeaders(providedHeaders);
    const auth = await getAuth();
    const session = await auth.api.getSession({ headers: requestHeadersValue });

    if (!session) {
      return { status: "unauthorized" };
    }

    const accounts = await auth.api.listUserAccounts({
      headers: requestHeadersValue,
    });

    const account = accounts.find(
      (candidate) => candidate.providerId === AUTHENTIK_PROVIDER_ID,
    );

    if (!account) {
      return { status: "unauthorized" };
    }

    const tokens = await auth.api.getAccessToken({
      headers: requestHeadersValue,
      body: { accountId: account.id },
    });

    if (!tokens.accessToken) {
      return { status: "unauthorized" };
    }

    return {
      status: "success",
      access: { accessToken: tokens.accessToken, accountId: account.id },
    };
  } catch (error: unknown) {
    logAdminAuthError("access-token-resolution-failed", error);
    return { status: requiresNewLogin(error) ? "unauthorized" : "unavailable" };
  }
}

export async function refreshAdminAccess(
  accountId: string,
  providedHeaders?: Headers,
): Promise<AdminAccessResult> {
  try {
    const requestHeadersValue = await requestHeaders(providedHeaders);
    const auth = await getAuth();
    const tokens = await auth.api.refreshToken({
      headers: requestHeadersValue,
      body: { accountId },
    });
    return tokens.accessToken
      ? {
          status: "success",
          access: { accessToken: tokens.accessToken, accountId },
        }
      : { status: "unauthorized" };
  } catch (error: unknown) {
    logAdminAuthError("access-token-refresh-failed", error);
    return { status: requiresNewLogin(error) ? "unauthorized" : "unavailable" };
  }
}

export function createAdminCsrfToken(): string {
  return randomBytes(32).toString("base64url");
}

export function setAdminCsrfCookie(
  response: NextResponse,
  token: string,
): void {
  response.cookies.set(ADMIN_CSRF_COOKIE, token, {
    httpOnly: false,
    maxAge: 8 * 60 * 60,
    path: "/",
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
  });
}

export function clearAdminCsrfCookie(response: NextResponse): void {
  response.cookies.set(ADMIN_CSRF_COOKIE, "", {
    httpOnly: false,
    maxAge: 0,
    path: "/",
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
  });
}
