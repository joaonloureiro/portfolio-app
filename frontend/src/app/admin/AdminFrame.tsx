import Link from 'next/link';
import {ADMIN_LOGOUT_PATH, ADMIN_PATH} from './admin-constants';

export default function AdminFrame({children}: {children: React.ReactNode}) {
  return (
    <div className="admin-surface">
      <header className="admin-header">
        <Link href={ADMIN_PATH} className="admin-wordmark" aria-label="Admin home">
          <span className="admin-wordmark-mark" aria-hidden="true">JL</span>
          <span>Admin workspace</span>
        </Link>
        <nav className="admin-header-nav" aria-label="Admin navigation">
          <Link href="/admin/posts">Posts</Link>
          <Link href="/admin/projects">Projects</Link>
          <Link href="/admin/locales">Locales</Link>
          <Link href="/admin/email-delivery">Email</Link>
          <a href={ADMIN_LOGOUT_PATH} className="admin-logout">Log out</a>
        </nav>
      </header>
      {children}
      <footer className="admin-footer">Protected portfolio operations</footer>
    </div>
  );
}
