'use client';

import Link from 'next/link';
import {useRouter} from 'next/navigation';
import {useState, useTransition} from 'react';
import PostCoverImage from '@/components/PostCoverImage';
import SafeMarkdown from '@/components/SafeMarkdown';
import {deleteAdminPost, createAdminPost, publishAdminPost, unpublishAdminPost, updateAdminPost} from '@/lib/admin-client-api';
import type {AdminPost, AdminPostContent, AdminPostContentInput, AdminPostEditorInput, PostLocale} from './admin-types';

const emptyContent: AdminPostContentInput = {
  en: {title: '', excerpt: '', body: '', coverImageAlt: ''},
  pt: {title: '', excerpt: '', body: '', coverImageAlt: ''}
};

type Props = {
  post?: AdminPost;
};

function initialInput(post?: AdminPost): AdminPostEditorInput {
  return post
    ? {slug: post.slug, content: {en: {...post.content.en}, pt: {...post.content.pt}}, coverImageUrl: post.coverImageUrl ?? '', tags: [...post.tags]}
    : {slug: '', content: {en: {...emptyContent.en}, pt: {...emptyContent.pt}}, coverImageUrl: '', tags: []};
}

function isComplete(content: {title: string; excerpt: string; body: string}) {
  return [content.title, content.excerpt, content.body].every((value) => value.trim().length > 0);
}

export default function AdminPostEditor({post}: Props) {
  const router = useRouter();
  const [input, setInput] = useState<AdminPostEditorInput>(() => initialInput(post));
  const [revision, setRevision] = useState(post?.revision ?? 1);
  const [status, setStatus] = useState(post?.status ?? 'draft');
  const [publishedAt, setPublishedAt] = useState(post?.publishedAt ?? null);
  const [previewLocale, setPreviewLocale] = useState<PostLocale>('en');
  const [feedback, setFeedback] = useState<{kind: 'success' | 'error'; text: string} | null>(null);
  const [isPending, startTransition] = useTransition();
  const isNew = !post;
  const bothLocalesComplete = isComplete(input.content.en) && isComplete(input.content.pt);

  const updateContent = (locale: PostLocale, field: keyof AdminPostContent, value: string) => {
    setInput((current) => ({
      ...current,
      content: {
        ...current.content,
        [locale]: {...current.content[locale], [field]: value}
      }
    }));
    setFeedback(null);
  };

  const handleSave = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFeedback(null);
    startTransition(async () => {
      const result = isNew
        ? await createAdminPost(input)
        : await updateAdminPost(post.id, revision, input.content, input.coverImageUrl || null, input.tags);
      if (result.status === 'success') {
        setFeedback({kind: 'success', text: isNew ? 'Draft created.' : 'Draft saved.'});
        if (isNew) {
          router.replace(`/admin/posts/${result.data.id}`);
        } else {
          setRevision(result.data.revision);
          setStatus(result.data.status);
          setPublishedAt(result.data.publishedAt);
        }
        router.refresh();
      } else {
        setFeedback({kind: 'error', text: result.message});
      }
    });
  };

  const handlePublish = () => {
    if (!bothLocalesComplete) {
      setFeedback({kind: 'error', text: 'Complete English and Portuguese title, excerpt, and body before publishing.'});
      return;
    }
    if (!window.confirm(`Publish “${input.slug}” now?`)) return;
    startTransition(async () => {
      const result = await publishAdminPost(post!.id, revision);
      if (result.status === 'success') {
        setStatus(result.data.status);
        setRevision(result.data.revision);
        setPublishedAt(result.data.publishedAt);
        setFeedback({kind: 'success', text: 'Post published.'});
      } else {
        setFeedback({kind: 'error', text: result.message});
      }
    });
  };

  const handleUnpublish = () => {
    if (!window.confirm(`Unpublish “${input.slug}” now?`)) return;
    startTransition(async () => {
      const result = await unpublishAdminPost(post!.id, revision);
      if (result.status === 'success') {
        setStatus(result.data.status);
        setRevision(result.data.revision);
        setPublishedAt(result.data.publishedAt);
        setFeedback({kind: 'success', text: 'Post unpublished.'});
      } else {
        setFeedback({kind: 'error', text: result.message});
      }
    });
  };

  const handleDelete = () => {
    if (!window.confirm(`Permanently delete post “${input.slug}” (${post!.id})? This cannot be undone.`)) return;
    startTransition(async () => {
      const result = await deleteAdminPost(post!.id, revision);
      if (result.status === 'success') router.push('/admin/posts');
      else setFeedback({kind: 'error', text: result.message});
    });
  };

  return (
    <main className="admin-main admin-post-editor-main">
      <div className="admin-page-heading">
        <div>
          <Link href="/admin/posts" className="admin-back-link">← All posts</Link>
          <h1>{isNew ? 'New post' : 'Edit post'}</h1>
          <p>Write once, keep both languages complete, and preview the supported Markdown safely.</p>
        </div>
        {!isNew && <span className={`admin-status-badge admin-status-${status}`}>{status}</span>}
      </div>

      <form className="admin-post-form" onSubmit={handleSave}>
        <fieldset disabled={isPending}>
          <legend>Shared details</legend>
          <label className="admin-field admin-field-slug">
            <span>Stable slug</span>
            <input
              value={input.slug}
              onChange={(event) => setInput((current) => ({...current, slug: event.target.value}))}
              readOnly={!isNew}
              maxLength={80}
              required
              aria-describedby="slug-help"
            />
            <small id="slug-help">Lowercase ASCII words separated by single hyphens, up to 80 characters. It cannot change after creation.</small>
          </label>
          <label className="admin-field">
            <span>Cover image URL <small>(optional, HTTPS)</small></span>
            <input type="url" value={input.coverImageUrl} onChange={(event) => setInput((current) => ({...current, coverImageUrl: event.target.value}))} maxLength={2048} placeholder="https://example.com/image.jpg" />
          </label>
          <label className="admin-field">
            <span>Topics / tags <small>(up to 8, comma-separated)</small></span>
            <input value={input.tags.join(', ')} onChange={(event) => setInput((current) => ({...current, tags: event.target.value.split(',').map((tag) => tag.trim()).filter(Boolean)}))} placeholder="next-js, accessibility" />
            <small>Tags connect related notes. Use short topic names; spaces become hyphens.</small>
          </label>
        </fieldset>

        <div className="admin-locale-grid">
          {(['en', 'pt'] as const).map((locale) => (
            <fieldset className="admin-locale-panel" key={locale} disabled={isPending}>
              <legend>{locale === 'en' ? 'English' : 'Português'}</legend>
              <label className="admin-field">
                <span>Title <small>(120 characters)</small></span>
                <input value={input.content[locale].title} onChange={(event) => updateContent(locale, 'title', event.target.value)} maxLength={120} />
              </label>
              <label className="admin-field">
                <span>Excerpt <small>(320 characters)</small></span>
                <textarea value={input.content[locale].excerpt} onChange={(event) => updateContent(locale, 'excerpt', event.target.value)} maxLength={320} rows={3} />
              </label>
              <label className="admin-field">
                <span>Cover image description <small>(optional, 200 characters)</small></span>
                <input value={input.content[locale].coverImageAlt ?? ''} onChange={(event) => updateContent(locale, 'coverImageAlt', event.target.value)} maxLength={200} />
              </label>
              <label className="admin-field">
                <span>Body <small>(50,000 characters)</small></span>
                <textarea value={input.content[locale].body} onChange={(event) => updateContent(locale, 'body', event.target.value)} maxLength={50_000} rows={18} aria-describedby={`markdown-help-${locale}`} />
              </label>
              <p id={`markdown-help-${locale}`} className="admin-markdown-help">Markdown: headings, emphasis, lists, quotes, inline code, fenced code, links, and inline images with HTTPS URLs. HTML, tables, and tasks are not supported.</p>
            </fieldset>
          ))}
        </div>

        {!bothLocalesComplete && <p className="admin-incomplete-warning" role="status">This draft is incomplete. Both locales need a title, excerpt, and body before publication.</p>}
        {feedback && <p className={`admin-form-feedback admin-form-feedback-${feedback.kind}`} role={feedback.kind === 'error' ? 'alert' : 'status'}>{feedback.text}</p>}

        <div className="admin-form-actions">
          <button type="submit" className="admin-button admin-button-accent" disabled={isPending}>{isPending ? 'Saving…' : isNew ? 'Create draft' : 'Save changes'}</button>
          {!isNew && status === 'published' && <button type="button" className="admin-button admin-button-quiet" onClick={handleUnpublish} disabled={isPending}>Unpublish</button>}
          {!isNew && status !== 'published' && <button type="button" className="admin-button admin-button-accent" onClick={handlePublish} disabled={isPending}>Publish</button>}
          {!isNew && status !== 'published' && <button type="button" className="admin-button admin-button-danger" onClick={handleDelete} disabled={isPending}>Delete permanently</button>}
        </div>
      </form>

      <section className="admin-preview" aria-labelledby="preview-title">
        <div className="admin-preview-heading">
          <div>
            <h2 id="preview-title">Safe preview</h2>
            <p>Preview uses the same raw-HTML-disabled renderer as public posts.</p>
          </div>
          <div className="admin-segmented" role="group" aria-label="Preview language">
            {(['en', 'pt'] as const).map((locale) => (
              <button type="button" key={locale} onClick={() => setPreviewLocale(locale)} aria-pressed={previewLocale === locale}>{locale}</button>
            ))}
          </div>
        </div>
        <PostCoverImage src={input.coverImageUrl || null} alt={input.content[previewLocale].coverImageAlt ?? ''} className="admin-post-cover-preview" />
        <SafeMarkdown source={input.content[previewLocale].body || '_Nothing written yet._'} className="admin-markdown-preview" />
      </section>

      {!isNew && publishedAt && <p className="admin-publication-note">First published {new Intl.DateTimeFormat('en-US', {dateStyle: 'medium', timeZone: 'America/Sao_Paulo'}).format(new Date(publishedAt))}. Revision {revision}.</p>}
    </main>
  );
}
