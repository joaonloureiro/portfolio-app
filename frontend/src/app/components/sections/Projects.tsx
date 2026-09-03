import Image from 'next/image';
import Link from 'next/link';
import {useLocale, useTranslations} from 'next-intl';

const projects = [
  {
    id: 1,
    tech: ['Next.js', '.NET 8', 'TypeScript', 'Tailwind CSS'],
    imageUrl: '/Portfolio.png',
    repoUrl: 'https://github.com/joaonloureiro/portfolio-app'
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

const Arrow = ({external = false}: {external?: boolean}) => (
  <svg viewBox="0 0 20 20" aria-hidden="true">
    {external ? <path d="M 7 4 H 16 V 13 M 16 4 L 6 14 M 14 10 V 16 H 4 V 6 H 10" /> : <path d="M 2 10 H 17 M 11 4 L 17 10 L 11 16" />}
  </svg>
);

export default function Projects() {
  const t = useTranslations('projects');
  const locale = useLocale();

  return (
    <section id="projects" className="projects-index" aria-labelledby="projects-title">
      <header className="projects-heading">
        <div className="projects-title-line">
          <h2 id="projects-title">{t('title')}</h2>
          <span aria-hidden="true" />
        </div>
        <p>{t('intro')}</p>
      </header>

      <div className="project-ledger">
        {projects.map((project, index) => (
          <article className="project-entry" key={project.id}>
            <div className="project-entry-heading">
              <span>{String(index + 1).padStart(2, '0')}</span>
              <p>{t('entry_label')}</p>
            </div>

            <Link
              href={`/${locale}/project/${project.id}`}
              className="project-image"
              aria-label={`${t('page_link')}: ${t(`project_${project.id}_title`)}`}
            >
              <Image
                src={project.imageUrl}
                alt=""
                fill
                sizes="(min-width: 1100px) 26vw, (min-width: 700px) 44vw, 100vw"
              />
              <span className="project-image-index" aria-hidden="true">0{project.id}</span>
            </Link>

            <div className="project-copy">
              <h3><Link href={`/${locale}/project/${project.id}`}>{t(`project_${project.id}_title`)}</Link></h3>
              <p>{t(`project_${project.id}_description`)}</p>
              <ul aria-label={t('tech_used')}>
                {project.tech.map((tech) => <li key={tech}>{tech}</li>)}
              </ul>
            </div>

            <div className="project-links">
              <Link href={`/${locale}/project/${project.id}`}>{t('page_link')}<Arrow /></Link>
              {'repoUrl' in project && project.repoUrl && <Link href={project.repoUrl} target="_blank" rel="noopener noreferrer">{t('repo_link')}<Arrow external /></Link>}
              {'liveUrl' in project && project.liveUrl && <Link href={project.liveUrl} target="_blank" rel="noopener noreferrer">{t('live_link')}<Arrow external /></Link>}
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
