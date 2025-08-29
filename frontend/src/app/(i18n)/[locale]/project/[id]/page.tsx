'use client';

import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { FaArrowLeft } from 'react-icons/fa6';
import Link from 'next/link';
import Image from 'next/image';

const projectsData = [
  { id: 1, tech: ["Next.js", "Tailwind CSS", "TypeScript", "Framer Motion"], imageUrl: "/project1.jpg", repoUrl: "https://github.com/joaoloureiro/portfolio-app" },
  { id: 2, tech: ["Traefik", "Docker", "Linux", "Homelab"], imageUrl: "/project2.jpg" },
  { id: 3, tech: ["React", "TypeScript", "Styled-Components"], imageUrl: "/project3.jpg", liveUrl: "https://happy.joaoloureiro.dev.br/" }
];

export default function ProjectPage() {
  const t = useTranslations('projects');
  const { id } = useParams();
  const projectId = parseInt(id as string, 10);
  const project = projectsData.find(p => p.id === projectId);

  if (!project) {
    return <div>Project not found</div>;
  }

  return (
    <section className="py-12 md:py-20">
      <div className="container mx-auto px-4">
        <Link href="/#projects" className="flex items-center gap-2 text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] transition-colors mb-8">
          <FaArrowLeft />
          {t('back_to_projects')}
        </Link>
        <h1 className="text-3xl md:text-4xl font-bold text-center text-[var(--color-text-primary)] heading-underline">{t(`project_${project.id}_title`)}</h1>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8 mt-12">
          <div>
            <Image src={project.imageUrl} alt={t(`project_${project.id}_title`)} className="rounded-lg" />
          </div>
          <div>
            <h2 className="text-2xl font-bold text-[var(--color-text-primary)] mb-4">{t('about_project')}</h2>
            <p className="text-[var(--color-text-secondary)] mb-4">{t(`project_${project.id}_description`)}</p>
            <h3 className="text-xl font-bold text-[var(--color-text-primary)] mb-2">{t('tech_used')}</h3>
            <div className="flex flex-wrap gap-2 mb-4">
              {project.tech.map(t => (
                <span key={t} className="bg-[var(--color-border)]/50 text-[var(--color-text-secondary)] text-xs font-semibold px-2.5 py-1 rounded-full">
                  {t}
                </span>
              ))}
            </div>
            <div className="flex items-center space-x-8 mt-auto pt-4 border-t border-[var(--color-border)]">
              {project.liveUrl && (
                <Link href={project.liveUrl} target="_blank" className="flex gap-2 items-center text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] transition-colors">
                  {t('live_link')}
                </Link>
              )}
              {project.repoUrl && (
                <Link href={project.repoUrl} target="_blank" className="flex gap-2 items-center text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] transition-colors">
                  {t('repo_link')}
                </Link>
              )}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}