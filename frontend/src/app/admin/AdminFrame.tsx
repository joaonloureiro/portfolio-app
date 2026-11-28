import Link from 'next/link';
import {ADMIN_LOGOUT_PATH, ADMIN_PATH} from './admin-constants';

export default function AdminFrame({children}: {children: React.ReactNode}) {
  return (
    <div className="admin-surface">
      <header className="admin-header">
        <Link href={ADMIN_PATH} className="admin-wordmark" aria-label="Admin home">
          <span className="admin-wordmark-mark" aria-hidden="true">
            <svg width="20" height="20" viewBox="-18 -6 138 122" fill="none" stroke="currentColor" strokeWidth="9" strokeLinecap="square">
              <path d="M38 0v68c0 14-10 24-22 24C5 92 0 85 0 76c0-10 7-18 17-18h11M56 0v68c0 21-16 40-38 40-14 0-34-10-34-34 2-20 12-32 36-32h8M56 0v108h56V92M56 25h19v66h37" />
            </svg>
          </span>
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
