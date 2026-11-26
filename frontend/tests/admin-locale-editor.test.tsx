import {fireEvent, render, screen, waitFor} from '@testing-library/react';
import {beforeEach, describe, expect, it, vi} from 'vitest';
import AdminLocaleEditor from '@/app/admin/AdminLocaleEditor';
import {updateAdminLocaleCatalog} from '@/lib/admin-locale-client-api';

vi.mock('@/lib/admin-locale-client-api', () => ({updateAdminLocaleCatalog: vi.fn()}));

const mockedUpdate = vi.mocked(updateAdminLocaleCatalog);
const catalog = {
  revision: 7,
  entries: [
    {key: 'hero.title', type: 'string' as const, en: 'English title', pt: 'Título'},
    {key: 'atlas.interfaces.points', type: 'string-list' as const, en: ['Routes', 'States'], pt: ['Rotas', 'Estados']}
  ]
};

describe('Admin locale editor', () => {
  beforeEach(() => {
    mockedUpdate.mockReset();
    vi.stubGlobal('confirm', vi.fn(() => true));
    Object.defineProperty(document, 'cookie', {configurable: true, value: 'portfolio_admin_csrf=csrf'});
  });

  it('renders labelled bilingual fields and repeatable list controls', () => {
    render(<AdminLocaleEditor catalog={catalog} />);

    expect(screen.getByRole('textbox', {name: /^English.*2,000 characters/})).toHaveValue('English title');
    expect(screen.getByRole('textbox', {name: /^Português.*2,000 characters/})).toHaveValue('Título');
    expect(screen.getByRole('button', {name: 'Move English atlas.interfaces.points item 2 up'})).toBeInTheDocument();
    expect(screen.getByRole('button', {name: 'Remove Português atlas.interfaces.points item 1'})).toBeInTheDocument();
  });

  it('requires confirmation and preserves unsaved values on a stale revision', async () => {
    mockedUpdate.mockResolvedValue({
      status: 'conflict',
      errorKey: 'locale_stale_revision',
      message: 'This catalog changed in another tab. Reload it before publishing again.'
    });
    render(<AdminLocaleEditor catalog={catalog} />);

    const title = screen.getByRole('textbox', {name: /^English.*2,000 characters/});
    fireEvent.change(title, {target: {value: 'Unsaved title'}});
    fireEvent.click(screen.getByRole('button', {name: 'Publish catalog'}));

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('another tab'));
    expect(title).toHaveValue('Unsaved title');
    expect(screen.getByRole('button', {name: 'Reload latest catalog'})).toBeInTheDocument();
    expect(vi.mocked(window.confirm)).toHaveBeenCalledWith('Publish this complete English and Portuguese catalog now?');
  });
});
