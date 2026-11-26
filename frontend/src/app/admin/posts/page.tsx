import Link from 'next/link';
import {redirect} from 'next/navigation';
import {getAdminIdentity} from '@/lib/admin-api';
import {listAdminPosts} from '@/lib/admin-post-api';
import AdminShell from '../AdminShell';
import AdminFrame from '../AdminFrame';
import AdminPostActions from '../AdminPostActions';

export const dynamic = 'force-dynamic';
export const revalidate = 0;

function formatAdminDate(value: string | null) {
  if (!value) return 'Not published';
  return new Intl.DateTimeFormat('en-US', {dateStyle: 'medium', timeZone: 'America/Sao_Paulo'}).format(new Date(value));
}

function ListFailure({message}: {message: string}) {
  return <main className="admin-main admin-main-status"><section className="admin-status" role="alert"><p className="admin-status-code">POSTS / UNAVAILABLE</p><h1>Posts could not load</h1><p>{message}</p><Link href="/admin/posts" className="admin-action">Try again</Link></section></main>;
}

export default async function AdminPostsPage() {
  const identity = await getAdminIdentity();
  if (identity.status === 'unauthorized') redirect('/auth/login');
  if (identity.status !== 'success') return <AdminShell result={identity} />;

  const result = await listAdminPosts();
  return (
    <AdminFrame>
      {result.status === 'success' ? (
        <main className="admin-main admin-posts-main">
          <div className="admin-page-heading">
            <div>
              <h1>Posts</h1>
              <p>Draft, review, and publish bilingual Field Notes without leaving the protected workspace.</p>
            </div>
            <Link href="/admin/posts/new" className="admin-button admin-button-accent">New post</Link>
          </div>
          {result.data.items.length === 0 ? (
            <section className="admin-empty-state admin-posts-empty" aria-labelledby="posts-empty-title">
              <div className="admin-empty-mark" aria-hidden="true"><span /></div>
              <h2 id="posts-empty-title">No posts yet</h2>
              <p>Create the first real Field Note when you have something worth publishing. Nothing is seeded or fabricated here.</p>
              <Link href="/admin/posts/new" className="admin-button admin-button-accent">New post</Link>
            </section>
          ) : (
            <section className="admin-post-list" aria-labelledby="post-list-title">
              <h2 id="post-list-title" className="sr-only">All posts</h2>
              {result.data.items.map((post) => (
                <article className="admin-post-row" key={post.id}>
                  <div className="admin-post-row-main">
                    <Link href={`/admin/posts/${post.id}`} className="admin-post-title">{post.slug}</Link>
                    <p><span className={`admin-status-badge admin-status-${post.status}`}>{post.status}</span> <span>Revision {post.revision} · Updated {formatAdminDate(post.updatedAt)}</span></p>
                  </div>
                  <div className="admin-post-row-meta"><span>{formatAdminDate(post.publishedAt)}</span><AdminPostActions id={post.id} slug={post.slug} status={post.status} revision={post.revision} /></div>
                </article>
              ))}
            </section>
          )}
        </main>
      ) : <ListFailure message={result.message} />}
    </AdminFrame>
  );
}
