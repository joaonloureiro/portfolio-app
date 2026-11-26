'use client';

import {useRouter} from 'next/navigation';
import {useState, useTransition} from 'react';
import {deleteAdminPost, publishAdminPost, unpublishAdminPost} from '@/lib/admin-client-api';

type Props = {
  id: string;
  slug: string;
  status: 'draft' | 'published' | 'unpublished';
  revision: number;
};

export default function AdminPostActions({id, slug, status, revision}: Props) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [message, setMessage] = useState<string | null>(null);

  const run = (action: () => Promise<{status: string; message?: string}>) => {
    setMessage(null);
    startTransition(async () => {
      const result = await action();
      if (result.status === 'success') {
        router.refresh();
      } else {
        setMessage(result.message ?? 'The action could not be completed.');
      }
    });
  };

  const publish = () => {
    if (window.confirm(`Publish “${slug}” now?`)) run(() => publishAdminPost(id, revision));
  };

  const unpublish = () => {
    if (window.confirm(`Unpublish “${slug}” now?`)) run(() => unpublishAdminPost(id, revision));
  };

  const remove = () => {
    if (window.confirm(`Permanently delete post “${slug}” (${id})? This cannot be undone.`)) {
      run(async () => {
        const result = await deleteAdminPost(id, revision);
        if (result.status === 'success') router.push('/admin/posts');
        return result;
      });
    }
  };

  return (
    <div className="admin-row-actions">
      {status === 'published'
        ? <button type="button" className="admin-button admin-button-quiet" onClick={unpublish} disabled={isPending}>Unpublish</button>
        : <button type="button" className="admin-button admin-button-accent" onClick={publish} disabled={isPending}>Publish</button>}
      {status !== 'published' && (
        <button type="button" className="admin-button admin-button-danger" onClick={remove} disabled={isPending}>Delete</button>
      )}
      {message && <p className="admin-inline-error" role="alert">{message}</p>}
    </div>
  );
}
