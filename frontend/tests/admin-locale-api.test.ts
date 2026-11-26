import {describe, expect, it} from 'vitest';
import {parseAdminLocaleCatalog} from '@/lib/locale-catalog-validation';

describe('locale Admin contract parsing', () => {
  it('accepts bilingual scalar and ordered-list entries', () => {
    const result = parseAdminLocaleCatalog({
      revision: 4,
      entries: [
        {key: 'hero.title', type: 'string', en: 'English', pt: 'Português'},
        {key: 'atlas.interfaces.points', type: 'string-list', en: ['One'], pt: ['Um']}
      ]
    });

    expect(result).toEqual({
      revision: 4,
      entries: [
        {key: 'hero.title', type: 'string', en: 'English', pt: 'Português'},
        {key: 'atlas.interfaces.points', type: 'string-list', en: ['One'], pt: ['Um']}
      ]
    });
  });

  it.each([
    {revision: 0, entries: []},
    {revision: 1, entries: [{key: 'hero.title', type: 'string-list', en: 'English', pt: ['Português']}]},
    {revision: 1, entries: [{key: 'hero.title', type: 'string', en: 'English', pt: 'Português'}, {key: 'hero.title', type: 'string', en: 'Again', pt: 'Outra'}]},
    {revision: 1, entries: [{key: 'atlas.points', type: 'string-list', en: [], pt: ['Um']}]}
  ])('rejects unsafe catalog response %#', (value) => {
    expect(parseAdminLocaleCatalog(value)).toBeNull();
  });
});
