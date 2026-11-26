import english from '@/locales/en.json';
import portuguese from '@/locales/pt.json';
import {describe, expect, it} from 'vitest';

type LeafType = 'string' | 'string-list';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function editableShape(value: unknown, path = '', result = new Map<string, LeafType>()) {
  if (typeof value === 'string') {
    result.set(path, 'string');
  } else if (Array.isArray(value) && value.length > 0 && value.every((item) => typeof item === 'string')) {
    result.set(path, 'string-list');
  } else if (isRecord(value)) {
    for (const [key, child] of Object.entries(value)) {
      editableShape(child, path ? `${path}.${key}` : key, result);
    }
  }
  return result;
}

describe('editable locale catalog contract', () => {
  it('keeps English and Portuguese editable keys and leaf types identical', () => {
    expect([...editableShape(english)]).toEqual([...editableShape(portuguese)]);
  });

  it('keeps structured and feature-owned content outside the locale editor', () => {
    const shape = editableShape(english);
    expect(shape.has('about.principles')).toBe(false);
    expect([...shape.keys()].some((key) => key.startsWith('projects.project_'))).toBe(false);
    expect([...shape.values()].every((type) => type === 'string' || type === 'string-list')).toBe(true);
  });
});
