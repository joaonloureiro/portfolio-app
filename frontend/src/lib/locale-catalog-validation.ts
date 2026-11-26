import type {AdminLocaleCatalog, AdminLocaleEntry, LocaleCatalogValue} from '@/app/admin/admin-types';
import englishMessages from '@/locales/en.json';

export const LOCALE_CATALOG_MAX_SCALAR_LENGTH = 2_000;
export const LOCALE_CATALOG_MAX_LIST_ITEMS = 20;
export const LOCALE_CATALOG_MAX_LIST_ITEM_LENGTH = 120;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isStringList(value: unknown): value is readonly string[] {
  return Array.isArray(value) && value.length >= 1 && value.length <= LOCALE_CATALOG_MAX_LIST_ITEMS &&
    value.every((item) => typeof item === 'string' && item.length <= LOCALE_CATALOG_MAX_LIST_ITEM_LENGTH && !item.includes('<') && !item.includes('>'));
}

function isCatalogValue(value: unknown, type: AdminLocaleEntry['type']): value is LocaleCatalogValue {
  return type === 'string'
    ? typeof value === 'string' && value.length <= LOCALE_CATALOG_MAX_SCALAR_LENGTH && !value.includes('<') && !value.includes('>')
    : isStringList(value);
}

function isKey(value: unknown): value is string {
  if (typeof value !== 'string' || value.length === 0 || value.length > 96) return false;
  const parts = value.split('.');
  return parts.length <= 4 && parts.every((part) => /^[A-Za-z0-9_]+$/.test(part));
}

function editableDefinitions(value: unknown, path = '', definitions = new Map<string, AdminLocaleEntry['type']>()) {
  if (typeof value === 'string') {
    definitions.set(path, 'string');
  } else if (Array.isArray(value) && value.length > 0 && value.every((item) => typeof item === 'string')) {
    definitions.set(path, 'string-list');
  } else if (isRecord(value)) {
    for (const [key, child] of Object.entries(value)) {
      editableDefinitions(child, path ? `${path}.${key}` : key, definitions);
    }
  }
  return definitions;
}

const knownEditableDefinitions = editableDefinitions(englishMessages);

function parseEntry(value: unknown): AdminLocaleEntry | null {
  if (!isRecord(value) || !isKey(value.key) || (value.type !== 'string' && value.type !== 'string-list') ||
      knownEditableDefinitions.get(value.key) !== value.type ||
      !isCatalogValue(value.en, value.type) || !isCatalogValue(value.pt, value.type)) {
    return null;
  }

  return {
    key: value.key,
    type: value.type,
    en: value.en,
    pt: value.pt
  };
}

export function parseAdminLocaleCatalog(value: unknown, requireComplete = false): AdminLocaleCatalog | null {
  if (!isRecord(value) || typeof value.revision !== 'number' || !Number.isInteger(value.revision) ||
      value.revision < 1 || !Array.isArray(value.entries) || value.entries.length < 1 || value.entries.length > knownEditableDefinitions.size) {
    return null;
  }

  const entries = value.entries.map(parseEntry);
  if (!entries.every((entry): entry is AdminLocaleEntry => entry !== null)) return null;
  if (new Set(entries.map((entry) => entry.key)).size !== entries.length) return null;
  if (requireComplete && entries.length !== knownEditableDefinitions.size) return null;

  return {revision: value.revision, entries};
}

export function cloneLocaleValue(value: LocaleCatalogValue): LocaleCatalogValue {
  return Array.isArray(value) ? [...value] : value;
}
