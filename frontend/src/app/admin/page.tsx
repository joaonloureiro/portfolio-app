import {getAdminIdentity} from '@/lib/admin-api';
import {redirect} from 'next/navigation';
import AdminShell from './AdminShell';

export const dynamic = 'force-dynamic';
export const revalidate = 0;

export default async function AdminPage({searchParams}: {searchParams: Promise<{auth?: string}>}) {
  const params = await searchParams;
  if (params.auth === 'failed' || params.auth === 'unavailable') {
    return <AdminShell result={{
      status: 'unavailable',
      message: params.auth === 'failed'
        ? 'Sign-in could not be completed. Try again.'
        : 'Admin sign-in is not configured or temporarily unavailable.'
    }} />;
  }

  const result = await getAdminIdentity();
  if (result.status === 'unauthorized') redirect('/auth/login');
  return <AdminShell result={result} />;
}
