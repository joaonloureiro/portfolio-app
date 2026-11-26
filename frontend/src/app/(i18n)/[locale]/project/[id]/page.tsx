import Image from 'next/image';
import Link from 'next/link';
import {Fragment} from 'react';
import {ArrowLeftIcon, ArrowUpRightIcon} from '@heroicons/react/24/outline';
import {getTranslations, setRequestLocale} from 'next-intl/server';
import {getPublicProject} from '@/lib/public-project-api';

function formatFeature(feature: string) {
  return feature.split(/(\*\*.*?\*\*)/g).filter(Boolean).map((part, index) =>
    part.startsWith('**') && part.endsWith('**')
      ? <strong key={index}>{part.slice(2, -2)}</strong>
      : <Fragment key={index}>{part.replaceAll('`', '')}</Fragment>
  );
}

type Props = {params: Promise<{locale: 'en' | 'pt'; id: string}>};

export default async function ProjectPage({params}: Props) {
  const {locale, id} = await params;
  setRequestLocale(locale);
  const t = await getTranslations({locale, namespace: 'projects'});
  const engagement = await getTranslations({locale, namespace: 'engagement'});
  const projectId = /^\d+$/.test(id) ? Number(id) : NaN;
  const result = Number.isSafeInteger(projectId)
    ? await getPublicProject(locale, projectId)
    : {status: 'not-found' as const};
  const project = result.status === 'success' ? result.data : null;
  const backHref = `/${locale}#projects`;
  const backLink = (
    <Link href={backHref} className="contract-text-link">
      <ArrowLeftIcon aria-hidden="true" />{t('back_to_projects')}
    </Link>
  );

  if (!project) {
    return (
      <section className="contract-detail">
        <div className="contract-detail-hero">
          <div className="contract-wrap">
            {backLink}
            <h1>{result.status === 'not-found' ? t('not_found') : t('unavailable')}</h1>
          </div>
        </div>
      </section>
    );
  }

  return (
    <article className="contract-detail">
      <header className="contract-detail-hero">
        <div className="contract-wrap">
          {backLink}
          <h1>{project.title}</h1>
          <p>{project.summary}</p>
        </div>
      </header>

      <div className="contract-wrap">
        <div className="contract-detail-image">
          <Image
            src={project.imageReference}
            alt={project.title}
            fill
            preload
            sizes="(min-width: 1200px) 1200px, 100vw"
          />
        </div>

        <div className="contract-detail-body">
          <div>
            <section>
              <h2>{t('about_project')}</h2>
              <p>{project.details}</p>
            </section>
            <section>
              <h2>{project.featureHeading}</h2>
              <ul>
                {project.features.map((feature, index) => (
                  <li key={`${feature}-${index}`}>{formatFeature(feature)}</li>
                ))}
              </ul>
            </section>
          </div>
          <aside>
            <h2>{t('tech_used')}</h2>
            <p className="contract-tech">{project.technologies.join(' · ')}</p>
            <div className="contract-project-links">
              {project.liveUrl && <Link href={project.liveUrl} target="_blank" rel="noopener noreferrer">{t('live_link')}</Link>}
              {project.repositoryUrl && <Link href={project.repositoryUrl} target="_blank" rel="noopener noreferrer">{t('repo_link')}</Link>}
            </div>
            <Link href={`/${locale}#contact`} className="contract-button">
              {engagement('start')}<ArrowUpRightIcon aria-hidden="true" />
            </Link>
          </aside>
        </div>
      </div>
    </article>
  );
}
