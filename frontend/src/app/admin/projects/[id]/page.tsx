import Link from 'next/link';
import {redirect} from 'next/navigation';
import {getAdminIdentity} from '@/lib/admin-api';
import {getAdminProject} from '@/lib/admin-project-api';
import AdminShell from '../../AdminShell';
import AdminFrame from '../../AdminFrame';
import AdminProjectEditor from '../../AdminProjectEditor';

export const dynamic = 'force-dynamic';
export const revalidate = 0;

type Props = {params: Promise<{id: string}>};

function isProjectId(value: string) {
  return /^(?:[1-9][0-9]{0,8})$/.test(value);
}

export default async function AdminProjectPage({params}: Props) {
  const identity = await getAdminIdentity();
  if (identity.status === 'unauthorized') redirect('/auth/login');
  if (identity.status !== 'success') return <AdminShell result={identity} />;
  const {id} = await params;
  const result = isProjectId(id) ? await getAdminProject(Number(id)) : {status: 'not-found' as const, message: 'The project is no longer available.'};

  return (
    <AdminFrame>
      {result.status === 'success'
        ? <AdminProjectEditor project={result.data} />
        : <main className="admin-main admin-main-status"><section className="admin-status" role="alert"><p className="admin-status-code">PROJECT / UNAVAILABLE</p><h1>Project could not load</h1><p>{result.message}</p><Link href="/admin/projects" className="admin-action">Back to projects</Link></section></main>}
    </AdminFrame>
  );
}
