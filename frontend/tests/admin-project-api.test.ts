import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {getAdminProject, listAdminProjects, updateAdminProject} from '@/lib/admin-project-api';
import {resolveAdminAccess} from '@/lib/admin-auth';

vi.mock('@/lib/admin-auth', () => ({resolveAdminAccess: vi.fn()}));

const mockedSession = vi.mocked(resolveAdminAccess);
const fetchMock = vi.fn<typeof fetch>();

const validProject = {
  id: 4,
  sortOrder: 4,
  status: 'draft',
  imageReference: '/project.png',
  titles: {en: 'Project', pt: 'Projeto'},
  repositoryUrl: null,
  liveUrl: null,
  publishedAt: null,
  updatedAt: '2026-09-09T12:00:00Z',
  revision: 1,
  technologies: ['.NET 8'],
  content: {
    en: {title: '', summary: '', details: '', featureHeading: '', features: ['']},
    pt: {title: '', summary: '', details: '', featureHeading: '', features: ['']}
  }
};

describe('Admin project server boundary', () => {
  beforeEach(() => {
    vi.stubEnv('BACKEND_INTERNAL_URL', 'http://localhost:3001');
    vi.stubGlobal('fetch', fetchMock);
    fetchMock.mockReset();
    mockedSession.mockReset();
    mockedSession.mockResolvedValue({status: 'success', access: {
      accessToken: 'server-session-access-token',
      accountId: 'authentik-account'
    }});
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  it('forwards only the server Bearer token and no browser credentials', async () => {
    fetchMock.mockResolvedValue(Response.json({items: [], page: 1, pageSize: 20, hasNext: false}));

    await expect(listAdminProjects()).resolves.toEqual({status: 'success', data: {items: [], page: 1, pageSize: 20, hasNext: false}});
    const [, requestInit] = fetchMock.mock.calls[0] ?? [];
    expect(requestInit?.headers).toEqual({Accept: 'application/json', Authorization: 'Bearer server-session-access-token'});
    expect(requestInit?.cache).toBe('no-store');
  });

  it('rejects malformed project payloads without exposing backend details', async () => {
    fetchMock.mockResolvedValue(Response.json({id: validProject.id, status: 'draft'}));

    await expect(getAdminProject(validProject.id)).resolves.toEqual({
      status: 'malformed-response',
      message: 'The project service returned an invalid response.'
    });
  });

  it('projects only the allowlisted project fields and drops token-shaped backend fields', async () => {
    fetchMock.mockResolvedValue(Response.json({...validProject, accessToken: 'must-not-leak'}));

    const result = await getAdminProject(validProject.id);

    expect(result.status).toBe('success');
    if (result.status === 'success') {
      expect('accessToken' in result.data).toBe(false);
    }
  });

  it('maps project conflicts to safe actionable messages', async () => {
    fetchMock.mockResolvedValue(Response.json({errorKey: 'project_stale_revision', detail: 'database internals'}, {status: 409}));

    await expect(updateAdminProject(validProject.id, 1, {
      sortOrder: validProject.sortOrder,
      imageReference: validProject.imageReference,
      technologies: validProject.technologies,
      content: validProject.content
    })).resolves.toEqual({
      status: 'conflict',
      errorKey: 'project_stale_revision',
      message: 'This project changed in another tab. Reload it before saving again.'
    });
  });
});
