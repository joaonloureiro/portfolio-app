import Link from 'next/link';
import {ArrowLeftIcon} from '@heroicons/react/24/outline';
import type {Locale} from 'next-intl';
import {getTranslations, setRequestLocale} from 'next-intl/server';
import PostCoverImage from '@/components/PostCoverImage';
import {getPublicPostList} from '@/lib/public-post-api';

type Props = {params: Promise<{locale: Locale}>};

export const revalidate = 60;

export async function generateMetadata({params}: Props) {
  const {locale} = await params;
  const t = await getTranslations({locale, namespace: 'posts'});
  return {
    title: t('title'),
    description: t('description'),
    alternates: {
      canonical: `/${locale}/posts`,
      languages: {en: '/en/posts', pt: '/pt/posts'}
    }
  };
}

function formatDate(locale: Locale, value: string) {
  return new Intl.DateTimeFormat(locale === 'pt' ? 'pt-BR' : 'en-US', {dateStyle: 'long', timeZone: 'America/Sao_Paulo'}).format(new Date(value));
}

export default async function PostsPage({params}: Props) {
  const {locale} = await params;
  setRequestLocale(locale);
  const t = await getTranslations({locale, namespace: 'posts'});
  const nav = await getTranslations({locale, namespace: 'navigation'});
  const result = await getPublicPostList(locale as 'en' | 'pt');

  return (
    <div className="post-page">
      <header className="post-page-heading">
        <Link href={`/${locale}`} className="signal-back-link">
          <ArrowLeftIcon aria-hidden="true" />
          <span>{nav('home')}</span>
        </Link>
        <h1>{t('title')}</h1>
        <p>{t('description')}</p>
      </header>
      {result.status === 'success' ? (
        result.data.items.length > 0 ? (
          <section className="post-list" aria-labelledby="posts-list-title">
            <h2 id="posts-list-title" className="sr-only">{t('list_label')}</h2>
            {result.data.items.map((post) => (
              <article className="post-list-item" key={post.slug}>
                <PostCoverImage src={post.coverImageUrl} alt={post.coverImageAlt} className="post-list-image" />
                <div className="post-list-copy">
                  <time dateTime={post.publishedAt}>{formatDate(locale, post.publishedAt)}</time>
                  <h2><Link href={`/${locale}/posts/${post.slug}`}>{post.title}</Link></h2>
                  <p>{post.excerpt}</p>
                </div>
              </article>
            ))}
          </section>
        ) : <p className="post-empty">{t('empty')}</p>
      ) : <p className="post-unavailable" role="status">{t('unavailable')}</p>}
    </div>
  );
}
