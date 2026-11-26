import {fireEvent, render, screen} from '@testing-library/react';
import {describe, expect, it} from 'vitest';
import AdminShell from '@/app/admin/AdminShell';
import {ADMIN_DOCUMENT_LANGUAGE, ADMIN_LOGOUT_PATH, ADMIN_PATH} from '@/app/admin/admin-constants';
import type {AdminIdentityResult} from '@/app/admin/admin-types';

const authenticatedResult = (subject: string): AdminIdentityResult => ({
  status: 'success',
  identity: {subject}
});

describe('Admin shell', () => {
  it('keeps navigation unprefixed, identifies English, and exposes keyboard-safe links', () => {
    render(<AdminShell result={authenticatedResult('administrator')} />);

    expect(ADMIN_DOCUMENT_LANGUAGE).toBe('en');
    const homeLink = screen.getByRole('link', {name: 'Admin home'});
    const logoutLink = screen.getByRole('link', {name: 'Log out'});

    expect(homeLink).toHaveAttribute('href', ADMIN_PATH);
    expect(logoutLink).toHaveAttribute('href', ADMIN_LOGOUT_PATH);
    expect(logoutLink).not.toHaveAttribute('tabindex', '-1');
    fireEvent.keyDown(logoutLink, {key: 'Enter', code: 'Enter'});
    expect(logoutLink).toHaveAttribute('href', ADMIN_LOGOUT_PATH);
  });

  it('renders only escaped, backend-validated identity data', () => {
    const token = 'header.payload.secret';
    const subject = '<script>window.leak = true</script>';
    const {container} = render(<AdminShell result={authenticatedResult(subject)} />);

    expect(screen.getByText(subject)).toBeInTheDocument();
    expect(container.innerHTML).not.toContain(token);
    expect(container.innerHTML).not.toContain('<script>');
  });

  it('renders the available Posts module in English', () => {
    render(<AdminShell result={authenticatedResult('administrator')} />);

    expect(screen.getByRole('heading', {name: 'Portfolio operations'})).toBeInTheDocument();
    expect(screen.getByRole('link', {name: 'Open Posts'})).toHaveAttribute('href', '/admin/posts');
    expect(screen.getByRole('link', {name: 'Open Locales'})).toHaveAttribute('href', '/admin/locales');
  });

  it.each([
    ['unauthorized', 'Admin access required'],
    ['forbidden', 'Access denied'],
    ['timeout', 'Admin service timed out'],
    ['unavailable', 'Admin service unavailable'],
    ['malformed-response', 'Admin service returned an invalid response']
  ] as const)('renders the %s recovery state', (status, title) => {
    const result: AdminIdentityResult = {
      status,
      message: 'A safe recovery message.'
    };
    render(<AdminShell result={result} />);

    expect(screen.getByRole('heading', {name: title})).toBeInTheDocument();
    expect(screen.getByRole('link', {name: 'Try again'})).toHaveAttribute('href', ADMIN_PATH);
  });
});
