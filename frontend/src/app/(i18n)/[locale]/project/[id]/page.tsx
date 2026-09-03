'use client';

import Image from 'next/image';
import Link from 'next/link';
import {Fragment, ReactNode} from 'react';
import {useLocale, useTranslations} from 'next-intl';
import {useParams} from 'next/navigation';

const projectsData = [
  {
    id: 1,
    tech: ['Next.js', '.NET 8', 'TypeScript', 'Tailwind CSS'],
    imageUrl: '/Portfolio.png',
    repoUrl: 'https://github.com/joaonloureiro/portfolio-app',
    liveUrl: 'https://joaoloureiro.dev.br'
  },
  {
    id: 2,
    tech: ['Proxmox', 'Docker', 'Traefik', 'Observability'],
    imageUrl: '/ProxmoxServer.png'
  },
  {
    id: 3,
    tech: ['React', 'Node.js', 'TypeScript', 'MariaDB'],
    imageUrl: '/Happy.png',
    repoUrl: 'https://github.com/joaonloureiro/happy-app',
    liveUrl: 'https://happy.joaoloureiro.dev.br/'
  }
] as const;

const Arrow = ({direction = 'right'}: {direction?: 'left' | 'right' | 'external'}) => (
  <svg viewBox="0 0 24 18" aria-hidden="true">
    {direction === 'left' && <path d="M 22 9 H 3 M 9 2 L 2 9 L 9 16" />}
    {direction === 'right' && <path d="M 2 9 H 21 M 15 2 L 22 9 L 15 16" />}
    {direction === 'external' && <path d="M 8 4 H 20 V 16 M 20 4 L 6 18 M 17 12 V 20 H 4 V 7 H 12" />}
  </svg>
);

function formatFeature(feature: string): ReactNode[] {
  return feature.split(/(\*\*.*?\*\*)/g).filter(Boolean).map((part, index) => {
    if (part.startsWith('**') && part.endsWith('**')) {
      return <strong key={`${part}-${index}`}>{part.slice(2, -2)}</strong>;
    }

    return <Fragment key={`${part}-${index}`}>{part.replaceAll('`', '')}</Fragment>;
  });
}

export default function ProjectPage() {
  const t = useTranslations('projects');
  const locale = useLocale();
  const {id} = useParams<{id: string}>();
  const projectId = Number.parseInt(id, 10);
  const project = projectsData.find((item) => item.id === projectId);
  const backHref = `/${locale}#projects`;

  if (!project) {
    return (
      <section className="project-missing" aria-labelledby="project-missing-title">
        <span aria-hidden="true">404 / 03</span>
        <h1 id="project-missing-title">{t('not_found')}</h1>
        <Link href={backHref} className="project-back-link"><Arrow direction="left" />{t('back_to_projects')}</Link>
      </section>
    );
  }

  const rawFeatures = t.raw(`project_${project.id}_features`);
  const projectFeatures = Array.isArray(rawFeatures) ? rawFeatures.filter((feature): feature is string => typeof feature === 'string') : [];
  const index = String(project.id).padStart(2, '0');

  return (
    <article className="project-detail">
      <header className="project-detail-hero">
        <div className="project-detail-copy">
          <Link href={backHref} className="project-back-link"><Arrow direction="left" />{t('back_to_projects')}</Link>
          <span className="project-detail-index" aria-hidden="true">{index} / 03</span>
          <h1>{t(`project_${project.id}_title`)}</h1>
          <p>{t(`project_${project.id}_description`)}</p>
        </div>

        <figure className="project-detail-visual">
          <Image
            src={project.imageUrl}
            alt={t(`project_${project.id}_title`)}
            fill
            priority
            sizes="(min-width: 981px) 50vw, 100vw"
          />
          <figcaption aria-hidden="true">PROJECT / {index}</figcaption>
        </figure>
      </header>

      <div className="project-detail-body">
        <section className="project-detail-story" aria-labelledby="project-about-title">
          <h2 id="project-about-title">{t('about_project')}</h2>
          <p>{t(`project_${project.id}_details`)}</p>
        </section>

        <aside className="project-detail-meta" aria-labelledby="project-tech-title">
          <h2 id="project-tech-title">{t('tech_used')}</h2>
          <ul>
            {project.tech.map((tech) => <li key={tech}>{tech}</li>)}
          </ul>
          <div className="project-detail-links">
            {'liveUrl' in project && project.liveUrl && (
              <Link href={project.liveUrl} target="_blank" rel="noopener noreferrer">{t('live_link')}<Arrow direction="external" /></Link>
            )}
            {'repoUrl' in project && project.repoUrl && (
              <Link href={project.repoUrl} target="_blank" rel="noopener noreferrer">{t('repo_link')}<Arrow direction="external" /></Link>
            )}
          </div>
        </aside>
      </div>

      <section className="project-features" aria-labelledby="project-features-title">
        <header>
          <span aria-hidden="true">{index} / SYSTEM NOTES</span>
          <h2 id="project-features-title">{t(`project_${project.id}_features_title`)}</h2>
        </header>
        <ol>
          {projectFeatures.map((feature, featureIndex) => (
            <li key={`${feature}-${featureIndex}`}>
              <span aria-hidden="true">{String(featureIndex + 1).padStart(2, '0')}</span>
              <p>{formatFeature(feature)}</p>
            </li>
          ))}
        </ol>
      </section>
    </article>
  );
}
