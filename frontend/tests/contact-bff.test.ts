import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {POST} from '@/app/api/email/send/route';

const fetchMock = vi.fn<typeof fetch>();

describe('Public contact BFF', () => {
  beforeEach(() => {
    vi.stubEnv('BACKEND_INTERNAL_URL', 'http://backend:80');
    vi.stubGlobal('fetch', fetchMock);
    fetchMock.mockReset();
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  it('forwards valid JSON and preserves the success contract', async () => {
    fetchMock.mockResolvedValue(Response.json({message: 'Message sent successfully!'}));
    const response = await POST(new Request('http://localhost/api/email/send', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify({name: 'Visitor', email: 'visitor@example.test', message: 'Hello'})
    }));

    expect(response.status).toBe(200);
    expect(await response.json()).toEqual({message: 'Message sent successfully!'});
    const [, init] = fetchMock.mock.calls[0] ?? [];
    expect(new Headers(init?.headers).get('Authorization')).toBeNull();
  });

  it('maps upstream failures to stable keys without provider text', async () => {
    fetchMock.mockResolvedValue(Response.json({errorKey: 'contact_delivery_unavailable', detail: 'smtp password'}, {status: 503}));
    const response = await POST(new Request('http://localhost/api/email/send', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: '{}'
    }));

    expect(response.status).toBe(503);
    const body = await response.text();
    expect(JSON.parse(body)).toEqual(expect.objectContaining({errorKey: 'contact_delivery_unavailable'}));
    expect(body).not.toContain('smtp password');
  });
});
