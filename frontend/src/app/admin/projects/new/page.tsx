import {redirect} from 'next/navigation';
import {getAdminIdentity} from '@/lib/admin-api';
import AdminShell from '../../AdminShell';
import AdminFrame from '../../AdminFrame';
import AdminProjectEditor from '../../AdminProjectEditor';

export const dynamic = 'force-dynamic';
export const revalidate = 0;

export default async function NewAdminProjectPage() {
  const identity = await getAdminIdentity();
  if (identity.status === 'unauthorized') redirect('/auth/login');
  if (identity.status !== 'success') return <AdminShell result={identity} />;
  return <AdminFrame><AdminProjectEditor /></AdminFrame>;
}
