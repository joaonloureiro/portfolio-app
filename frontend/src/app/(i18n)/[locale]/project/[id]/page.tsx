import Link from 'next/link';
import {Fragment} from 'react';
import {ArrowLeftIcon, ArrowTopRightOnSquareIcon, ArrowUpRightIcon} from '@heroicons/react/24/outline';
import {FaGithub} from 'react-icons/fa';
import {getTranslations, setRequestLocale} from 'next-intl/server';
import ProjectImage from '@/components/ProjectImage';
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
  const nav = await getTranslations({locale, namespace: 'navigation'});
  const projectId = /^\d+$/.test(id) ? Number(id) : NaN;
  const result = Number.isSafeInteger(projectId)
    ? await getPublicProject(locale, projectId)
    : {status: 'not-found' as const};
  const project = result.status === 'success' ? result.data : null;
  const backHref = `/${locale}#projects`;
  const backLink = (
    <Link href={backHref} className="signal-back-link">
      <ArrowLeftIcon aria-hidden="true" />
      <span>{t('back_to_projects')}</span>
    </Link>
  );

  if (!project) {
    return (
      <section className="signal-detail">
        <div className="signal-detail-hero">
          <div className="signal-wrap">
            {backLink}
            <h1>{result.status === 'not-found' ? t('not_found') : t('unavailable')}</h1>
          </div>
        </div>
      </section>
    );
  }

  return (
    <article className="signal-detail">
      <header className="signal-detail-hero">
        <div className="signal-wrap">
          {backLink}
          <h1>{project.title}</h1>
          <p>{project.summary}</p>
        </div>
      </header>

      <div className="signal-wrap">
        <div className="signal-detail-frame">
          <ProjectImage
            src={project.imageReference}
            alt={project.title}
            priority
            sizes="(min-width: 1200px) 1180px, 100vw"
          />
        </div>

        <div className="signal-detail-body">
          <div className="signal-detail-main">
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

          <aside className="signal-detail-aside">
            <h2>{t('tech_used')}</h2>
            <ul className="signal-tags" aria-label={t('tech_used')}>
              {project.technologies.map((tech) => (
                <li className="signal-tag" key={tech}>{tech}</li>
              ))}
            </ul>
            {(project.liveUrl || project.repositoryUrl) && (
              <div className="signal-detail-links">
                {project.liveUrl && (
                  <Link
                    href={project.liveUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="signal-icon-btn"
                    aria-label={`${t('live_link')} — ${project.title}`}
                    title={t('live_link')}
                  >
                    <ArrowTopRightOnSquareIcon aria-hidden="true" />
                  </Link>
                )}
                {project.repositoryUrl && (
                  <Link
                    href={project.repositoryUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="signal-icon-btn"
                    aria-label={`${t('repo_link')} — ${project.title}`}
                    title={t('repo_link')}
                  >
                    <FaGithub aria-hidden="true" />
                  </Link>
                )}
              </div>
            )}
            <Link href={`/${locale}#contact`} className="signal-btn-primary">
              <span>{nav('contact')}</span>
              <ArrowUpRightIcon aria-hidden="true" />
            </Link>
          </aside>
        </div>
      </div>
    </article>
  );
}
