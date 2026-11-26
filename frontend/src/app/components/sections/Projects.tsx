import Image from 'next/image';
import Link from 'next/link';
import {useLocale, useTranslations} from 'next-intl';
import {ArrowUpRightIcon} from '@heroicons/react/24/outline';
import type {PublicProjectSummary} from '@/lib/public-project-types';

export default function Projects({projects, unavailable}: {projects: readonly PublicProjectSummary[]; unavailable: boolean}) {
  const t = useTranslations('projects');
  const engagement = useTranslations('engagement');
  const locale = useLocale();

  return (
    <section id="projects" className="contract-work" aria-labelledby="projects-title">
      <div className="contract-wrap">
        <header className="contract-section-heading">
          <h2 id="projects-title">{engagement('work_heading')}</h2>
          <p>{engagement('work_intro')}</p>
        </header>

        {unavailable && <p role="status">{t('unavailable')}</p>}
        {!unavailable && projects.length === 0 && <p>{engagement('work_empty')}</p>}
        <div className="contract-gallery">
          {projects.map((project, index) => (
            <article className="contract-project" key={project.id}>
              <Link href={`/${locale}/project/${project.id}`} className="contract-project-image" aria-label={project.title}>
                <Image
                  src={project.imageReference}
                  alt=""
                  fill
                  sizes={index === 0 ? "(min-width: 1300px) 850px, (min-width: 760px) 62vw, 100vw" : "(min-width: 1300px) 625px, (min-width: 760px) 47vw, 100vw"}
                />
              </Link>
              <div className="contract-project-copy">
                <h3><Link href={`/${locale}/project/${project.id}`}>{project.title}</Link></h3>
                <p>{project.summary}</p>
                <p className="contract-tech">{project.technologies.join(' · ')}</p>
                <div className="contract-project-links">
                  <Link href={`/${locale}/project/${project.id}`} className="contract-text-link">{t('page_link')}<ArrowUpRightIcon aria-hidden="true" /></Link>
                  {project.liveUrl && <Link href={project.liveUrl} target="_blank" rel="noopener noreferrer">{t('live_link')}</Link>}
                  {project.repositoryUrl && <Link href={project.repositoryUrl} target="_blank" rel="noopener noreferrer">{t('repo_link')}</Link>}
                </div>
              </div>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}
