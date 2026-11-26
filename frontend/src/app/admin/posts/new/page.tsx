import {getAdminIdentity} from '@/lib/admin-api';
import {redirect} from 'next/navigation';
import AdminShell from '../../AdminShell';
import AdminFrame from '../../AdminFrame';
import AdminPostEditor from '../../AdminPostEditor';

export const dynamic = 'force-dynamic';
export const revalidate = 0;

export default async function NewAdminPostPage() {
  const identity = await getAdminIdentity();
  if (identity.status === 'unauthorized') redirect('/auth/login');
  if (identity.status !== 'success') return <AdminShell result={identity} />;
  return <AdminFrame><AdminPostEditor /></AdminFrame>;
}
