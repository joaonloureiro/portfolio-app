import {getTranslations} from 'next-intl/server';

export default async function PostsLoading() {
  const t = await getTranslations('posts');
  return (
    <div className="post-page" aria-busy="true" aria-live="polite">
      <div className="post-page-heading"><h1>{t('title')}</h1><p>{t('loading')}</p></div>
      <div className="post-list" aria-hidden="true"><div className="post-list-item"><div><h2>{t('loading')}</h2><p>{t('loading')}</p></div></div></div>
    </div>
  );
}
