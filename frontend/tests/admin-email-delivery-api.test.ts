import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {getAdminEmailDeliveryPolicy, sendAdminEmailDeliveryTest, updateAdminEmailDeliveryPolicy} from '@/lib/admin-email-delivery-api';
import {resolveAdminAccess} from '@/lib/admin-auth';

vi.mock('@/lib/admin-auth', () => ({resolveAdminAccess: vi.fn()}));

const mockedSession = vi.mocked(resolveAdminAccess);
const fetchMock = vi.fn<typeof fetch>();

describe('Admin email delivery server boundary', () => {
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

  it('returns a typed coarse policy and never forwards browser credentials', async () => {
    fetchMock.mockResolvedValue(Response.json({
      enabled: true,
      recipientAddress: 'recipient@example.test',
      senderDisplayName: 'Portfolio contact',
      subjectPrefix: 'New Portfolio Contact',
      revision: 4,
      transportConfigured: true,
      password: 'must-not-be-returned'
    }));

    await expect(getAdminEmailDeliveryPolicy()).resolves.toEqual({status: 'success', data: {
      enabled: true,
      recipientAddress: 'recipient@example.test',
      senderDisplayName: 'Portfolio contact',
      subjectPrefix: 'New Portfolio Contact',
      revision: 4,
      transportConfigured: true
    }});
    const [, init] = fetchMock.mock.calls[0] ?? [];
    expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer server-session-access-token');
  });

  it('maps conflict and throttling without exposing upstream text', async () => {
    fetchMock.mockResolvedValueOnce(Response.json({errorKey: 'email_delivery_stale_revision', detail: 'provider secret'}, {status: 409}));
    await expect(updateAdminEmailDeliveryPolicy(3, {
      enabled: true,
      recipientAddress: 'recipient@example.test',
      senderDisplayName: 'Portfolio contact',
      subjectPrefix: 'New Portfolio Contact'
    })).resolves.toMatchObject({status: 'conflict', errorKey: 'email_delivery_stale_revision'});

    fetchMock.mockResolvedValueOnce(Response.json({errorKey: 'email_delivery_test_throttled'}, {status: 429}));
    await expect(sendAdminEmailDeliveryTest()).resolves.toMatchObject({status: 'throttled', errorKey: 'email_delivery_test_throttled'});
  });
});
