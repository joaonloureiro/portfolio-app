import Link from 'next/link';
import {getTranslations} from 'next-intl/server';
import type {Locale} from 'next-intl';
import type {PublicPostSummary} from '@/lib/public-post-types';

export default async function FieldNotes({locale, posts}: {locale: Locale; posts: readonly PublicPostSummary[]}) {
  const t = await getTranslations({locale, namespace: 'notes'});
  const hasPosts = posts.length > 0;

  return (
    <section id="notes" className="field-notes" aria-labelledby="notes-title">
      <div className="notes-heading">
        <h2 id="notes-title">{t('title')} <span>— {hasPosts ? t('published_status') : t('status')}</span></h2>
        <p>{hasPosts ? t('published_description') : t('description')}</p>
      </div>
      {hasPosts && (
        <div className="notes-post-list">
          {posts.slice(0, 3).map((post) => (
            <article key={post.slug} className="notes-post-item">
              <time dateTime={post.publishedAt}>{new Intl.DateTimeFormat(locale === 'pt' ? 'pt-BR' : 'en-US', {dateStyle: 'medium', timeZone: 'America/Sao_Paulo'}).format(new Date(post.publishedAt))}</time>
              <h3><Link href={`/${locale}/posts/${post.slug}`}>{post.title}</Link></h3>
              <p>{post.excerpt}</p>
            </article>
          ))}
          <Link href={`/${locale}/posts`} className="notes-posts-link">{t('published_link')} →</Link>
        </div>
      )}
    </section>
  );
}
