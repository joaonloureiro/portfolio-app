import Link from 'next/link';
import {redirect} from 'next/navigation';
import {getAdminIdentity} from '@/lib/admin-api';
import {listAdminProjects} from '@/lib/admin-project-api';
import AdminShell from '../AdminShell';
import AdminFrame from '../AdminFrame';
import AdminProjectList from '../AdminProjectList';

export const dynamic = 'force-dynamic';
export const revalidate = 0;

function ListFailure({message}: {message: string}) {
  return <main className="admin-main admin-main-status"><section className="admin-status" role="alert"><p className="admin-status-code">PROJECTS / UNAVAILABLE</p><h1>Projects could not load</h1><p>{message}</p><Link href="/admin/projects" className="admin-action">Try again</Link></section></main>;
}

export default async function AdminProjectsPage() {
  const identity = await getAdminIdentity();
  if (identity.status === 'unauthorized') redirect('/auth/login');
  if (identity.status !== 'success') return <AdminShell result={identity} />;

  const result = await listAdminProjects();
  return (
    <AdminFrame>
      {result.status === 'success' ? (
        <main className="admin-main admin-projects-main">
          <div className="admin-page-heading">
            <div><h1>Projects</h1><p>Maintain the published project ledger without losing language parity, order, or public identifiers.</p></div>
            <Link href="/admin/projects/new" className="admin-button admin-button-accent">New project</Link>
          </div>
          <AdminProjectList projects={result.data.items} />
        </main>
      ) : <ListFailure message={result.message} />}
    </AdminFrame>
  );
}
