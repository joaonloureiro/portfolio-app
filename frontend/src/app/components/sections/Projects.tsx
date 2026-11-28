import Link from 'next/link';
import {useLocale, useTranslations} from 'next-intl';
import {ArrowTopRightOnSquareIcon, ArrowUpRightIcon} from '@heroicons/react/24/outline';
import {FaGithub} from 'react-icons/fa';
import SignalSpine from '@/app/components/SignalSpine';
import TiltFrame from '@/app/components/TiltFrame';
import ProjectImage from '@/components/ProjectImage';
import type {PublicProjectSummary} from '@/lib/public-project-types';

type ProjectsProps = {
  projects: readonly PublicProjectSummary[];
  unavailable: boolean;
};

export default function Projects({projects, unavailable}: ProjectsProps) {
  const t = useTranslations('projects');
  const engagement = useTranslations('engagement');
  const locale = useLocale();

  return (
    <section id="projects" className="signal-spine-section" aria-labelledby="projects-title">
      <div className="signal-wrap">
        <header className="signal-section-heading">
          <h2 id="projects-title">{t('title')}</h2>
          <p>{t('intro')}</p>
        </header>

        {unavailable && <p role="status" className="signal-status-note">{t('unavailable')}</p>}
        {!unavailable && projects.length === 0 && <p className="signal-status-note">{engagement('work_empty')}</p>}

        {projects.length > 0 && (
          <SignalSpine>
            {projects.map((project, index) => {
              const isFlipped = index % 2 === 1;
              const chapterIndex = String(index + 1).padStart(2, '0');
              const detailHref = `/${locale}/project/${project.id}`;

              return (
                <article className={`signal-chapter${isFlipped ? ' is-flipped' : ''}`} key={project.id}>
                  <div className="signal-chapter-node" aria-hidden="true" />

                  <div className="signal-chapter-copy">
                    <span className={`signal-chapter-index ${index % 2 === 0 ? 'is-emerald' : 'is-ice'}`}>
                      {chapterIndex} {`// ${t('entry_label').toUpperCase()}`}
                    </span>
                    <h3>
                      <Link href={detailHref}>{project.title}</Link>
                    </h3>
                    <p>{project.summary}</p>
                    <ul className="signal-tags" aria-label={t('tech_used')}>
                      {project.technologies.map((tech) => (
                        <li className="signal-tag" key={tech}>{tech}</li>
                      ))}
                    </ul>
                    <div className="signal-chapter-actions">
                      <Link href={detailHref} className="signal-link-detail">
                        <span>{t('page_link')}</span>
                        <ArrowUpRightIcon aria-hidden="true" />
                      </Link>
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
                  </div>

                  <TiltFrame>
                    <Link href={detailHref} className="signal-frame-link" aria-label={project.title}>
                      <ProjectImage
                        src={project.imageReference}
                        alt={project.title}
                        sizes="(min-width: 1200px) 620px, (min-width: 920px) 52vw, 100vw"
                        priority={index === 0}
                      />
                    </Link>
                  </TiltFrame>
                </article>
              );
            })}
          </SignalSpine>
        )}
      </div>
    </section>
  );
}
