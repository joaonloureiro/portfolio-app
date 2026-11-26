import Link from 'next/link';
import {notFound} from 'next/navigation';
import type {Locale} from 'next-intl';
import {getTranslations, setRequestLocale} from 'next-intl/server';
import PostCoverImage from '@/components/PostCoverImage';
import SafeMarkdown from '@/components/SafeMarkdown';
import {getPublicPost, getPublicPostList} from '@/lib/public-post-api';
import {getRelatedPosts} from '@/lib/related-posts';

type Props = {params: Promise<{locale: Locale; slug: string}>};

export const revalidate = 60;

export async function generateMetadata({params}: Props) {
  const {locale, slug} = await params;
  const t = await getTranslations({locale, namespace: 'posts'});
  const result = await getPublicPost(locale as 'en' | 'pt', slug);
  const title = result.status === 'success' ? result.data.title : t('title');
  const description = result.status === 'success' ? result.data.excerpt : t('description');
  return {
    title: `${title} | ${t('title')}`,
    description,
    alternates: {
      canonical: `/${locale}/posts/${encodeURIComponent(slug)}`,
      languages: {en: `/en/posts/${encodeURIComponent(slug)}`, pt: `/pt/posts/${encodeURIComponent(slug)}`}
    }
  };
}

export default async function PostDetailPage({params}: Props) {
  const {locale, slug} = await params;
  setRequestLocale(locale);
  const t = await getTranslations({locale, namespace: 'posts'});
  const result = await getPublicPost(locale as 'en' | 'pt', slug);
  if (result.status === 'not-found') notFound();
  if (result.status !== 'success') {
    return <div className="post-page"><p className="post-unavailable" role="status">{t('unavailable')}</p></div>;
  }

  const post = result.data;
  const relatedResult = post.tags.length > 0 ? await getPublicPostList(locale as 'en' | 'pt', 1, 20) : null;
  const relatedPosts = relatedResult?.status === 'success' ? getRelatedPosts(post, relatedResult.data.items) : [];
  return (
    <div className="post-page">
      <article className="post-article">
        <header className="post-article-header">
          <h1>{post.title}</h1>
          <p className="post-excerpt">{post.excerpt}</p>
          <time dateTime={post.publishedAt}>{new Intl.DateTimeFormat(locale === 'pt' ? 'pt-BR' : 'en-US', {dateStyle: 'long', timeZone: 'America/Sao_Paulo'}).format(new Date(post.publishedAt))}</time>
        </header>
        <PostCoverImage src={post.coverImageUrl} alt={post.coverImageAlt} className="post-hero-image" />
        <SafeMarkdown source={post.body} className="post-markdown" />
        <Link href={`/${locale}/posts`} className="post-back-link">← {t('back_to_posts')}</Link>
      </article>
      {relatedPosts.length > 0 && (
        <section className="related-posts" aria-labelledby="related-posts-title">
          <h2 id="related-posts-title">{t('related_title')}</h2>
          <div className="related-post-grid">
            {relatedPosts.map((related) => (
              <article className="related-post-card" key={related.slug}>
                <PostCoverImage src={related.coverImageUrl} alt={related.coverImageAlt} className="related-post-image" />
                <time dateTime={related.publishedAt}>{new Intl.DateTimeFormat(locale === 'pt' ? 'pt-BR' : 'en-US', {dateStyle: 'medium', timeZone: 'America/Sao_Paulo'}).format(new Date(related.publishedAt))}</time>
                <h3><Link href={`/${locale}/posts/${related.slug}`}>{related.title}</Link></h3>
                <p>{related.excerpt}</p>
              </article>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
