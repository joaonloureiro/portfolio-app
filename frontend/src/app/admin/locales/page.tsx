import Link from 'next/link';
import {redirect} from 'next/navigation';
import {getAdminIdentity} from '@/lib/admin-api';
import {getAdminLocaleCatalog} from '@/lib/admin-locale-api';
import AdminShell from '../AdminShell';
import AdminFrame from '../AdminFrame';
import AdminLocaleEditor from '../AdminLocaleEditor';

export const dynamic = 'force-dynamic';
export const revalidate = 0;

export default async function AdminLocalesPage() {
  const identity = await getAdminIdentity();
  if (identity.status === 'unauthorized') redirect('/auth/login');
  if (identity.status !== 'success') return <AdminShell result={identity} />;

  const result = await getAdminLocaleCatalog();
  return (
    <AdminFrame>
      {result.status === 'success'
        ? <AdminLocaleEditor catalog={result.data} />
        : <main className="admin-main admin-main-status"><section className="admin-status" role="alert"><p className="admin-status-code">LOCALES / UNAVAILABLE</p><h1>Locales could not load</h1><p>{result.message}</p><Link href="/admin/locales" className="admin-action">Try again</Link></section></main>}
    </AdminFrame>
  );
}
