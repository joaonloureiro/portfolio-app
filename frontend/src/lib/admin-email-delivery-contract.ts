import type {AdminEmailDeliveryPolicy} from '@/app/admin/admin-types';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

export function parseAdminEmailDeliveryPolicy(value: unknown): AdminEmailDeliveryPolicy | null {
  if (!isRecord(value) || typeof value.enabled !== 'boolean' ||
      typeof value.recipientAddress !== 'string' || value.recipientAddress.length === 0 || value.recipientAddress.length > 320 ||
      typeof value.senderDisplayName !== 'string' || value.senderDisplayName.length === 0 || value.senderDisplayName.length > 120 ||
      typeof value.subjectPrefix !== 'string' || value.subjectPrefix.length === 0 || value.subjectPrefix.length > 120 ||
      typeof value.revision !== 'number' || !Number.isSafeInteger(value.revision) || value.revision < 1 ||
      typeof value.transportConfigured !== 'boolean') return null;

  return {
    enabled: value.enabled,
    recipientAddress: value.recipientAddress,
    senderDisplayName: value.senderDisplayName,
    subjectPrefix: value.subjectPrefix,
    revision: value.revision,
    transportConfigured: value.transportConfigured
  };
}

export function parseAdminEmailDeliveryTest(value: unknown): {message: string} | null {
  return isRecord(value) && typeof value.message === 'string' && value.message.length > 0 && value.message.length <= 160
    ? {message: value.message}
    : null;
}
