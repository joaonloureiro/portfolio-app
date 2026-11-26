import Link from 'next/link';
import {redirect} from 'next/navigation';
import {getAdminIdentity} from '@/lib/admin-api';
import {getAdminEmailDeliveryPolicy} from '@/lib/admin-email-delivery-api';
import AdminShell from '../AdminShell';
import AdminFrame from '../AdminFrame';
import AdminEmailDeliveryEditor from '../AdminEmailDeliveryEditor';

export const dynamic = 'force-dynamic';
export const revalidate = 0;

export default async function AdminEmailDeliveryPage() {
  const identity = await getAdminIdentity();
  if (identity.status === 'unauthorized') redirect('/auth/login');
  if (identity.status !== 'success') return <AdminShell result={identity} />;

  const result = await getAdminEmailDeliveryPolicy();
  return (
    <AdminFrame>
      {result.status === 'success' ? (
        <AdminEmailDeliveryEditor policy={result.data} />
      ) : (
        <main className="admin-main admin-main-status">
          <section className="admin-status" role="alert">
            <p className="admin-status-code">EMAIL DELIVERY / UNAVAILABLE</p>
            <h1>Email delivery could not load</h1>
            <p>{result.message}</p>
            <Link href="/admin/email-delivery" className="admin-action">Try again</Link>
          </section>
        </main>
      )}
    </AdminFrame>
  );
}
