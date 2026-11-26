'use client';

import Link from 'next/link';
import {useRouter} from 'next/navigation';
import {useState, useTransition} from 'react';
import {deleteAdminProject, publishAdminProject, reorderAdminProjects, unpublishAdminProject} from '@/lib/admin-project-client-api';
import type {AdminProjectReorderItem, AdminProjectSummary} from './admin-types';

type Props = {projects: readonly AdminProjectSummary[]};

export default function AdminProjectList({projects}: Props) {
  const router = useRouter();
  const [items, setItems] = useState(() => [...projects]);
  const [isPending, startTransition] = useTransition();
  const [message, setMessage] = useState<string | null>(null);

  const runAction = (action: () => Promise<{status: string; message?: string}>) => {
    setMessage(null);
    startTransition(async () => {
      const result = await action();
      if (result.status === 'success') router.refresh();
      else setMessage(result.message ?? 'The project action could not be completed.');
    });
  };

  const move = (index: number, direction: -1 | 1) => {
    const target = index + direction;
    if (target < 0 || target >= items.length) return;
    const next = [...items];
    [next[index], next[target]] = [next[target], next[index]];
    const payload: AdminProjectReorderItem[] = next.map((item) => ({id: item.id, expectedRevision: item.revision}));
    setMessage(null);
    startTransition(async () => {
      const result = await reorderAdminProjects(payload);
      if (result.status === 'success') {
        setItems(next.map((item) => ({...item, revision: item.revision + 1})));
        router.refresh();
      } else {
        setMessage(result.message ?? 'The project order could not be saved.');
      }
    });
  };

  if (items.length === 0) {
    return (
      <section className="admin-empty-state admin-projects-empty" aria-labelledby="projects-empty-title">
        <div className="admin-empty-mark" aria-hidden="true"><span /></div>
        <h2 id="projects-empty-title">No projects yet</h2>
        <p>Create a draft when you have a real project record to maintain. Nothing is invented or published from this screen.</p>
        <Link href="/admin/projects/new" className="admin-button admin-button-accent">New project</Link>
      </section>
    );
  }

  return (
    <section className="admin-project-list" aria-labelledby="project-list-title">
      <h2 id="project-list-title" className="sr-only">All projects</h2>
      {message && <p className="admin-form-feedback admin-form-feedback-error" role="alert">{message}</p>}
      {items.map((project, index) => (
        <article className="admin-project-row" key={project.id}>
          <div className="admin-project-order" aria-label={`Order ${project.sortOrder}`}>
            <span>{String(index + 1).padStart(2, '0')}</span>
            <div className="admin-reorder-controls" aria-label={`Reorder ${project.titles.en}`}>
              <button type="button" className="admin-button admin-button-quiet" onClick={() => move(index, -1)} disabled={isPending || index === 0} aria-label={`Move ${project.titles.en} earlier`}>Move up</button>
              <button type="button" className="admin-button admin-button-quiet" onClick={() => move(index, 1)} disabled={isPending || index === items.length - 1} aria-label={`Move ${project.titles.en} later`}>Move down</button>
            </div>
          </div>
          <div className="admin-project-row-main">
            <Link href={`/admin/projects/${project.id}`} className="admin-post-title">{project.titles.en || project.titles.pt || `Project ${project.id}`}</Link>
            <p><span className={`admin-status-badge admin-status-${project.status}`}>{project.status}</span> <span>Project {project.id} · Revision {project.revision}</span></p>
          </div>
          <div className="admin-project-row-meta">
            <span>{project.titles.pt || 'Portuguese title pending'}</span>
            <div className="admin-row-actions">
              <Link href={`/admin/projects/${project.id}`} className="admin-button admin-button-quiet">Edit</Link>
              {project.status === 'published'
                ? <button type="button" className="admin-button admin-button-quiet" onClick={() => window.confirm(`Unpublish “${project.titles.en}” now?`) && runAction(() => unpublishAdminProject(project.id, project.revision))} disabled={isPending}>Unpublish</button>
                : <button type="button" className="admin-button admin-button-accent" onClick={() => window.confirm(`Publish “${project.titles.en}” now?`) && runAction(() => publishAdminProject(project.id, project.revision))} disabled={isPending}>Publish</button>}
              {project.status === 'unpublished' && <button type="button" className="admin-button admin-button-danger" onClick={() => window.confirm(`Permanently delete project “${project.titles.en}” (${project.id})? This cannot be undone.`) && runAction(() => deleteAdminProject(project.id, project.revision))} disabled={isPending}>Delete</button>}
            </div>
          </div>
        </article>
      ))}
    </section>
  );
}
