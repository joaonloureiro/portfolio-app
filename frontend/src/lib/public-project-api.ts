import 'server-only';

import type {PublicProject, PublicProjectList, PublicProjectResult, PublicProjectSummary} from './public-project-types';

const PUBLIC_PROJECTS_PATH = '/api/projects';
const PUBLIC_API_TIMEOUT_MS = 5_000;
const MAX_RESPONSE_BYTES = 2 * 1024 * 1024;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function parseSummary(value: unknown): PublicProjectSummary | null {
  if (!isRecord(value) || typeof value.id !== 'number' || !Number.isInteger(value.id) || value.id < 1 ||
      typeof value.sortOrder !== 'number' || !Number.isInteger(value.sortOrder) || value.sortOrder < 1 ||
      typeof value.imageReference !== 'string' || value.imageReference.length === 0 ||
      (value.repositoryUrl !== null && typeof value.repositoryUrl !== 'string') ||
      (value.liveUrl !== null && typeof value.liveUrl !== 'string') || !Array.isArray(value.technologies) ||
      value.technologies.length < 1 || value.technologies.length > 20 || value.technologies.some((technology) => typeof technology !== 'string') ||
      typeof value.title !== 'string' || value.title.length > 120 || typeof value.summary !== 'string' || value.summary.length > 320) return null;

  return {
    id: value.id,
    sortOrder: value.sortOrder,
    imageReference: value.imageReference,
    repositoryUrl: value.repositoryUrl,
    liveUrl: value.liveUrl,
    technologies: value.technologies,
    title: value.title,
    summary: value.summary
  };
}

function parseList(value: unknown): PublicProjectList | null {
  if (!isRecord(value) || !Array.isArray(value.items) || typeof value.page !== 'number' || typeof value.pageSize !== 'number' ||
      !Number.isInteger(value.page) || !Number.isInteger(value.pageSize) || value.page < 1 || value.pageSize < 1 || value.pageSize > 50 || typeof value.hasNext !== 'boolean') return null;
  const items = value.items.map(parseSummary);
  return items.every((item): item is PublicProjectSummary => item !== null)
    ? {items, page: value.page, pageSize: value.pageSize, hasNext: value.hasNext}
    : null;
}

function parseProject(value: unknown): PublicProject | null {
  if (!isRecord(value) || typeof value.details !== 'string' || value.details.length > 10_000 ||
      typeof value.featureHeading !== 'string' || value.featureHeading.length > 120 || !Array.isArray(value.features) ||
      value.features.length < 1 || value.features.length > 20 || value.features.some((feature) => typeof feature !== 'string' || feature.length > 500)) return null;
  const summary = parseSummary(value);
  return summary ? {...summary, details: value.details, featureHeading: value.featureHeading, features: value.features} : null;
}

function getEndpoint(path: string): URL | null {
  const backendBaseUrl = process.env.BACKEND_INTERNAL_URL?.trim();
  if (!backendBaseUrl) return null;
  try {
    const endpoint = new URL(path, backendBaseUrl);
    return endpoint.protocol === 'http:' || endpoint.protocol === 'https:' ? endpoint : null;
  } catch {
    return null;
  }
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

function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === 'AbortError';
}

async function request<T>(path: string, parser: (value: unknown) => T | null, tags: string[], requestSignal?: AbortSignal): Promise<PublicProjectResult<T>> {
  const endpoint = getEndpoint(path);
  if (!endpoint) return {status: 'unavailable', message: 'Projects are unavailable right now.'};
  const timeoutController = new AbortController();
  const timeoutId = setTimeout(() => timeoutController.abort(), PUBLIC_API_TIMEOUT_MS);
  const handleRequestAbort = () => timeoutController.abort();
  requestSignal?.addEventListener('abort', handleRequestAbort, {once: true});
  if (requestSignal?.aborted) timeoutController.abort();

  try {
    const response = await fetch(endpoint, {
      method: 'GET',
      headers: {Accept: 'application/json'},
      next: {revalidate: 60, tags},
      signal: timeoutController.signal
    });
    if (response.status === 404) return {status: 'not-found', message: 'Project not found.'};
    if (!response.ok) return {status: 'unavailable', message: 'Projects are unavailable right now.'};
    const parsed = parser(await readJson(response));
    return parsed === null ? {status: 'malformed-response', message: 'Projects returned an invalid response.'} : {status: 'success', data: parsed};
  } catch (error: unknown) {
    return isAbortError(error) ? {status: 'timeout', message: 'Projects took too long to respond.'} : {status: 'unavailable', message: 'Projects are unavailable right now.'};
  } finally {
    clearTimeout(timeoutId);
    requestSignal?.removeEventListener('abort', handleRequestAbort);
  }
}

export function getPublicProjectList(locale: 'en' | 'pt', page = 1, pageSize = 20, requestSignal?: AbortSignal) {
  return request(`${PUBLIC_PROJECTS_PATH}?locale=${locale}&page=${page}&pageSize=${pageSize}`, parseList, ['public-projects', `public-projects-${locale}`], requestSignal);
}

export function getPublicProject(locale: 'en' | 'pt', id: number, requestSignal?: AbortSignal) {
  return request(`${PUBLIC_PROJECTS_PATH}/${encodeURIComponent(id)}?locale=${locale}`, parseProject, ['public-projects', `public-project-${locale}-${id}`], requestSignal);
}
