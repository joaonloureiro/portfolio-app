import Link from 'next/link';
import type {AdminFailureStatus, AdminIdentityResult} from './admin-types';
import {ADMIN_PATH} from './admin-constants';
import AdminFrame from './AdminFrame';

const failureTitles: Readonly<Record<AdminFailureStatus, string>> = {
  unauthorized: 'Admin access required',
  forbidden: 'Access denied',
  timeout: 'Admin service timed out',
  'malformed-response': 'Admin service returned an invalid response',
  unavailable: 'Admin service unavailable'
};

function FailureState({result}: {result: Exclude<AdminIdentityResult, {status: 'success'}>}) {
  return (
    <main className="admin-main admin-main-status">
      <section className="admin-status" data-state={result.status} role="alert" aria-labelledby="admin-status-title">
        <p className="admin-status-code" aria-hidden="true">{result.status.replace('-', ' / ').toUpperCase()}</p>
        <h1 id="admin-status-title">{failureTitles[result.status]}</h1>
        <p>{result.message}</p>
        <a href={ADMIN_PATH} className="admin-action">Try again</a>
      </section>
    </main>
  );
}

function AuthenticatedDashboard({subject}: {subject: string}) {
  return (
    <main className="admin-main">
      <section className="admin-intro" aria-labelledby="admin-title">
        <div>
          <span className="admin-kicker">CONTROL PLANE</span>
          <h1 id="admin-title">Admin workspace</h1>
        </div>
        <p>A protected place to operate João Loureiro’s portfolio.</p>
      </section>

      <section className="admin-identity" aria-labelledby="identity-title">
        <div>
          <h2 id="identity-title">Signed-in identity</h2>
          <p>Authenticated administrator</p>
        </div>
        <code>{subject}</code>
      </section>

      <section className="admin-empty-state" aria-labelledby="empty-title">
        <span className="admin-kicker">OPERATIONS</span>
        <h2 id="empty-title">Portfolio operations</h2>
        <p>Operate the portfolio’s bilingual content without exposing drafts, tokens, or deployment details to the public surface.</p>
        <div className="admin-ops-grid">
          <Link href="/admin/projects" className="admin-button admin-button-accent">Open Projects</Link>
          <Link href="/admin/posts" className="admin-button admin-button-quiet">Open Posts</Link>
          <Link href="/admin/locales" className="admin-button admin-button-quiet">Open Locales</Link>
          <Link href="/admin/email-delivery" className="admin-button admin-button-quiet">Email delivery</Link>
        </div>
      </section>
    </main>
  );
}

export default function AdminShell({result}: {result: AdminIdentityResult}) {
  return (
    <AdminFrame>
      {result.status === 'success'
        ? <AuthenticatedDashboard subject={result.identity.subject} />
        : <FailureState result={result} />}
    </AdminFrame>
  );
}
