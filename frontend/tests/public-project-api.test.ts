import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {getPublicProject, getPublicProjectList} from '@/lib/public-project-api';

const fetchMock = vi.fn<typeof fetch>();

const project = {
  id: 1,
  sortOrder: 1,
  imageReference: '/Portfolio.png',
  repositoryUrl: 'https://github.com/joaonloureiro/portfolio-app',
  liveUrl: 'https://joaoloureiro.dev.br',
  technologies: ['Next.js', '.NET 8'],
  title: 'Personal Portfolio',
  summary: 'A portfolio.',
  details: 'Details.',
  featureHeading: 'Key Features:',
  features: ['**Feature:** Honest content.']
};

describe('Public project reader', () => {
  beforeEach(() => {
    vi.stubEnv('BACKEND_INTERNAL_URL', 'http://backend:80');
    vi.stubGlobal('fetch', fetchMock);
    fetchMock.mockReset();
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  it('reads bounded localized list data with public revalidation', async () => {
    fetchMock.mockResolvedValue(Response.json({items: [project], page: 1, pageSize: 20, hasNext: false}));

    await expect(getPublicProjectList('en')).resolves.toEqual({status: 'success', data: {items: [{
      id: 1,
      sortOrder: 1,
      imageReference: '/Portfolio.png',
      repositoryUrl: 'https://github.com/joaonloureiro/portfolio-app',
      liveUrl: 'https://joaoloureiro.dev.br',
      technologies: ['Next.js', '.NET 8'],
      title: 'Personal Portfolio',
      summary: 'A portfolio.'
    }], page: 1, pageSize: 20, hasNext: false}});
    const [, requestInit] = fetchMock.mock.calls[0] ?? [];
    expect(requestInit?.next).toEqual({revalidate: 60, tags: ['public-projects', 'public-projects-en']});
  });

  it('fails closed for malformed content and maps missing details safely', async () => {
    fetchMock.mockResolvedValueOnce(Response.json({id: 1, title: 'missing fields'}));
    await expect(getPublicProject('pt', 1)).resolves.toEqual({status: 'malformed-response', message: 'Projects returned an invalid response.'});

    fetchMock.mockResolvedValueOnce(Response.json({}, {status: 404}));
    await expect(getPublicProject('pt', 999)).resolves.toEqual({status: 'not-found', message: 'Project not found.'});
  });
});
