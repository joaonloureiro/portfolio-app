'use client';

import Link from 'next/link';
import Image from 'next/image';
import {useRouter} from 'next/navigation';
import {useState, useTransition} from 'react';
import {
  createAdminProject,
  deleteAdminProject,
  publishAdminProject,
  unpublishAdminProject,
  updateAdminProject
} from '@/lib/admin-project-client-api';
import type {
  AdminProject,
  AdminProjectContentInput,
  AdminProjectEditorInput,
  AdminProjectLocalizedContent,
  PostLocale
} from './admin-types';

type Props = {project?: AdminProject};
type ProjectEditorInput = AdminProjectEditorInput & {content: AdminProjectContentInput};

const emptyLocale = (): AdminProjectLocalizedContent => ({title: '', summary: '', details: '', featureHeading: '', features: ['']});

function initialInput(project?: AdminProject): ProjectEditorInput {
  return project
    ? {
        sortOrder: project.sortOrder,
        imageReference: project.imageReference,
        repositoryUrl: project.repositoryUrl ?? '',
        liveUrl: project.liveUrl ?? '',
        technologies: [...project.technologies],
        content: {en: {...project.content.en, features: [...project.content.en.features]}, pt: {...project.content.pt, features: [...project.content.pt.features]}}
      }
    : {sortOrder: 4, imageReference: '', repositoryUrl: '', liveUrl: '', technologies: [''], content: {en: emptyLocale(), pt: emptyLocale()}};
}

function complete(locale: AdminProjectLocalizedContent) {
  return [locale.title, locale.summary, locale.details, locale.featureHeading].every((value) => value.trim().length > 0) &&
    locale.features.length > 0 && locale.features.every((feature) => feature.trim().length > 0);
}

export default function AdminProjectEditor({project}: Props) {
  const router = useRouter();
  const [input, setInput] = useState<ProjectEditorInput>(() => initialInput(project));
  const [revision, setRevision] = useState(project?.revision ?? 1);
  const [status, setStatus] = useState(project?.status ?? 'draft');
  const [publishedAt, setPublishedAt] = useState(project?.publishedAt ?? null);
  const [previewLocale, setPreviewLocale] = useState<PostLocale>('en');
  const [feedback, setFeedback] = useState<{kind: 'success' | 'error'; text: string} | null>(null);
  const [isPending, startTransition] = useTransition();
  const isNew = !project;
  const previewableImage = (input.imageReference.trim().startsWith('/') && !input.imageReference.trim().startsWith('//')) || input.imageReference.trim().startsWith('https://');
  const readyToPublish = complete(input.content.en) && complete(input.content.pt) &&
    input.imageReference.trim().length > 0 && input.technologies.length > 0 && input.technologies.every((value) => value.trim().length > 0);

  const setShared = <K extends keyof ProjectEditorInput>(field: K, value: ProjectEditorInput[K]) => {
    setInput((current) => ({...current, [field]: value}));
    setFeedback(null);
  };

  const updateLocale = (locale: PostLocale, field: keyof Omit<AdminProjectLocalizedContent, 'features'>, value: string) => {
    setInput((current) => ({...current, content: {...current.content, [locale]: {...current.content[locale], [field]: value}}}));
    setFeedback(null);
  };

  const updateFeature = (locale: PostLocale, index: number, value: string) => {
    setInput((current) => {
      const features = [...current.content[locale].features];
      features[index] = value;
      return {...current, content: {...current.content, [locale]: {...current.content[locale], features}}};
    });
    setFeedback(null);
  };

  const addFeature = (locale: PostLocale) => setInput((current) => ({...current, content: {...current.content, [locale]: {...current.content[locale], features: [...current.content[locale].features, '']}}}));
  const removeFeature = (locale: PostLocale, index: number) => setInput((current) => ({...current, content: {...current.content, [locale]: {...current.content[locale], features: current.content[locale].features.filter((_, itemIndex) => itemIndex !== index)}}}));
  const updateTechnology = (index: number, value: string) => setShared('technologies', input.technologies.map((technology, itemIndex) => itemIndex === index ? value : technology));
  const addTechnology = () => setShared('technologies', [...input.technologies, '']);
  const removeTechnology = (index: number) => setShared('technologies', input.technologies.filter((_, itemIndex) => itemIndex !== index));

  const handleSave = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFeedback(null);
    startTransition(async () => {
      const result = isNew
        ? await createAdminProject(input)
        : await updateAdminProject(project.id, revision, input);
      if (result.status === 'success') {
        setFeedback({kind: 'success', text: isNew ? 'Draft created.' : 'Project saved.'});
        setRevision(result.data.revision);
        setStatus(result.data.status);
        setPublishedAt(result.data.publishedAt);
        if (isNew) router.replace(`/admin/projects/${result.data.id}`);
        router.refresh();
      } else {
        setFeedback({kind: 'error', text: result.message});
      }
    });
  };

  const handlePublish = () => {
    if (!readyToPublish) {
      setFeedback({kind: 'error', text: 'Complete both language panels, technologies, image reference, and links before publishing.'});
      return;
    }
    if (!window.confirm(`Publish “${input.content.en.title || `Project ${project?.id ?? ''}`}” now?`)) return;
    startTransition(async () => {
      const result = await publishAdminProject(project!.id, revision);
      if (result.status === 'success') {
        setStatus(result.data.status);
        setRevision(result.data.revision);
        setPublishedAt(result.data.publishedAt);
        setFeedback({kind: 'success', text: 'Project published.'});
      } else setFeedback({kind: 'error', text: result.message});
    });
  };

  const handleUnpublish = () => {
    if (!window.confirm(`Unpublish “${input.content.en.title || `Project ${project?.id ?? ''}`}” now?`)) return;
    startTransition(async () => {
      const result = await unpublishAdminProject(project!.id, revision);
      if (result.status === 'success') {
        setStatus(result.data.status);
        setRevision(result.data.revision);
        setPublishedAt(result.data.publishedAt);
        setFeedback({kind: 'success', text: 'Project unpublished.'});
      } else setFeedback({kind: 'error', text: result.message});
    });
  };

  const handleDelete = () => {
    if (status !== 'unpublished' || !window.confirm(`Permanently delete project “${input.content.en.title || `Project ${project?.id ?? ''}`}” (${project!.id})? This cannot be undone.`)) return;
    startTransition(async () => {
      const result = await deleteAdminProject(project!.id, revision);
      if (result.status === 'success') router.push('/admin/projects');
      else setFeedback({kind: 'error', text: result.message});
    });
  };

  return (
    <main className="admin-main admin-project-editor-main">
      <div className="admin-page-heading">
        <div>
          <Link href="/admin/projects" className="admin-back-link">← All projects</Link>
          <h1>{isNew ? 'New project' : `Project ${project.id}`}</h1>
          <p>Keep the source record honest: shared project facts first, then complete English and Portuguese content.</p>
        </div>
        {!isNew && <span className={`admin-status-badge admin-status-${status}`}>{status} · revision {revision}</span>}
      </div>

      <form className="admin-project-form" onSubmit={handleSave}>
        <fieldset disabled={isPending}>
          <legend>Shared details</legend>
          <div className="admin-project-shared-grid">
            <label className="admin-field"><span>Sort order</span><input type="number" min={1} max={1000} value={input.sortOrder} onChange={(event) => setShared('sortOrder', Number(event.target.value))} required /></label>
            <label className="admin-field"><span>Image reference</span><input value={input.imageReference} onChange={(event) => setShared('imageReference', event.target.value)} maxLength={512} required aria-describedby="project-image-help" /><small id="project-image-help">Use a root-relative deployed asset path or an approved HTTPS host.</small></label>
            <label className="admin-field"><span>Repository URL <small>(optional)</small></span><input type="url" value={input.repositoryUrl} onChange={(event) => setShared('repositoryUrl', event.target.value)} maxLength={2048} /></label>
            <label className="admin-field"><span>Live URL <small>(optional)</small></span><input type="url" value={input.liveUrl} onChange={(event) => setShared('liveUrl', event.target.value)} maxLength={2048} /></label>
          </div>
          <figure className="admin-image-preview">
            {previewableImage
              ? <Image src={input.imageReference.trim()} alt="" width={640} height={360} unoptimized />
              : <p>{input.imageReference.trim() ? 'Preview is available after a valid root-relative or HTTPS reference is entered.' : 'Image preview appears after an asset reference is entered.'}</p>}
            <figcaption>{input.imageReference.trim() || 'No image reference selected'}</figcaption>
          </figure>
        </fieldset>

        <fieldset disabled={isPending}>
          <legend>Technologies</legend>
          <div className="admin-repeat-list">
            {input.technologies.map((technology, index) => (
              <div className="admin-repeat-row" key={`technology-${index}`}>
                <label className="admin-field"><span className="sr-only">Technology {index + 1}</span><input value={technology} onChange={(event) => updateTechnology(index, event.target.value)} maxLength={60} placeholder="e.g. Next.js" /></label>
                <button type="button" className="admin-button admin-button-danger" onClick={() => removeTechnology(index)} disabled={input.technologies.length === 1}>Remove</button>
              </div>
            ))}
          </div>
          <button type="button" className="admin-button admin-button-quiet" onClick={addTechnology} disabled={input.technologies.length >= 20}>Add technology</button>
        </fieldset>

        <div className="admin-locale-grid">
          {(['en', 'pt'] as const).map((locale) => {
            const content = input.content[locale];
            return (
              <fieldset className="admin-locale-panel admin-project-locale-panel" key={locale} disabled={isPending}>
                <legend>{locale === 'en' ? 'English' : 'Português'}</legend>
                <label className="admin-field"><span>Title <small>(120 characters)</small></span><input value={content.title} onChange={(event) => updateLocale(locale, 'title', event.target.value)} maxLength={120} /></label>
                <label className="admin-field"><span>Summary <small>(320 characters)</small></span><textarea value={content.summary} onChange={(event) => updateLocale(locale, 'summary', event.target.value)} maxLength={320} rows={4} /></label>
                <label className="admin-field"><span>Details <small>(10,000 characters)</small></span><textarea value={content.details} onChange={(event) => updateLocale(locale, 'details', event.target.value)} maxLength={10_000} rows={10} /></label>
                <label className="admin-field"><span>Feature heading <small>(120 characters)</small></span><input value={content.featureHeading} onChange={(event) => updateLocale(locale, 'featureHeading', event.target.value)} maxLength={120} /></label>
                <div className="admin-field"><span>Ordered features</span>
                  <div className="admin-repeat-list">
                    {content.features.map((feature, index) => (
                      <div className="admin-repeat-row" key={`${locale}-feature-${index}`}>
                        <label className="admin-field"><span className="sr-only">Feature {index + 1}</span><textarea value={feature} onChange={(event) => updateFeature(locale, index, event.target.value)} maxLength={500} rows={3} /></label>
                        <button type="button" className="admin-button admin-button-danger" onClick={() => removeFeature(locale, index)} disabled={content.features.length === 1}>Remove</button>
                      </div>
                    ))}
                  </div>
                  <button type="button" className="admin-button admin-button-quiet" onClick={() => addFeature(locale)} disabled={content.features.length >= 20}>Add feature</button>
                </div>
              </fieldset>
            );
          })}
        </div>

        {!readyToPublish && <p className="admin-incomplete-warning" role="status">Publication stays locked until both locales, technologies, and the image reference are complete.</p>}
        {feedback && <p className={`admin-form-feedback admin-form-feedback-${feedback.kind}`} role={feedback.kind === 'error' ? 'alert' : 'status'}>{feedback.text}</p>}

        <div className="admin-form-actions">
          <button type="submit" className="admin-button admin-button-accent" disabled={isPending}>{isPending ? 'Saving…' : isNew ? 'Create draft' : 'Save changes'}</button>
          {!isNew && status === 'published' && <button type="button" className="admin-button admin-button-quiet" onClick={handleUnpublish} disabled={isPending}>Unpublish</button>}
          {!isNew && status !== 'published' && <button type="button" className="admin-button admin-button-accent" onClick={handlePublish} disabled={isPending}>Publish</button>}
          {!isNew && status === 'unpublished' && <button type="button" className="admin-button admin-button-danger" onClick={handleDelete} disabled={isPending}>Delete permanently</button>}
        </div>
      </form>

      <section className="admin-preview" aria-labelledby="project-preview-title">
        <div className="admin-preview-heading">
          <div><h2 id="project-preview-title">Content preview</h2><p>Review each language before publication. No live changes occur until you save.</p></div>
          <div className="admin-segmented" role="group" aria-label="Preview language">
            {(['en', 'pt'] as const).map((locale) => <button type="button" key={locale} onClick={() => setPreviewLocale(locale)} aria-pressed={previewLocale === locale}>{locale}</button>)}
          </div>
        </div>
        <div className="admin-project-preview">
          <h3>{input.content[previewLocale].title || 'Untitled project'}</h3>
          <p>{input.content[previewLocale].summary || 'No summary yet.'}</p>
          <p>{input.content[previewLocale].details || 'No details yet.'}</p>
          <h4>{input.content[previewLocale].featureHeading || 'Features'}</h4>
          <ol>{input.content[previewLocale].features.filter(Boolean).map((feature, index) => <li key={`${feature}-${index}`}>{feature}</li>)}</ol>
        </div>
      </section>

      {!isNew && publishedAt && <p className="admin-publication-note">First published {new Intl.DateTimeFormat('en-US', {dateStyle: 'medium', timeZone: 'America/Sao_Paulo'}).format(new Date(publishedAt))}. Revision {revision}.</p>}
    </main>
  );
}
