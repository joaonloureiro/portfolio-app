import Link from 'next/link';
import {redirect} from 'next/navigation';
import {getAdminIdentity} from '@/lib/admin-api';
import {getAdminPost} from '@/lib/admin-post-api';
import AdminShell from '../../AdminShell';
import AdminFrame from '../../AdminFrame';
import AdminPostEditor from '../../AdminPostEditor';

export const dynamic = 'force-dynamic';
export const revalidate = 0;

type Props = {params: Promise<{id: string}>};

function isUuid(value: string) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}

export default async function AdminPostPage({params}: Props) {
  const identity = await getAdminIdentity();
  if (identity.status === 'unauthorized') redirect('/auth/login');
  if (identity.status !== 'success') return <AdminShell result={identity} />;
  const {id} = await params;
  const result = isUuid(id) ? await getAdminPost(id) : {status: 'not-found' as const, message: 'The post is no longer available.'};

  return (
    <AdminFrame>
      {result.status === 'success'
        ? <AdminPostEditor post={result.data} />
        : <main className="admin-main admin-main-status"><section className="admin-status" role="alert"><p className="admin-status-code">POST / UNAVAILABLE</p><h1>Post could not load</h1><p>{result.message}</p><Link href="/admin/posts" className="admin-action">Back to posts</Link></section></main>}
    </AdminFrame>
  );
}
