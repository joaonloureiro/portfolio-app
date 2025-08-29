'use client';

import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { FaArrowLeft, FaGithub, FaExternalLinkAlt } from 'react-icons/fa';
import Link from 'next/link';
import Image from 'next/image';

const projectsData = [
  {
    id: 1,
    tech: ["Next.js", "React", "TypeScript", "Tailwind CSS", "Node.js", "Express"],
    imageUrl: "/Portfolio.png",
    repoUrl: "https://github.com/joaonloureiro/portfolio-app",
    liveUrl: "https://joaoloureiro.dev.br"
  },
  {
    id: 2,
    tech: ["Docker", "Proxmox", "Traefik", "Gitea", "MariaDB", "RabbitMQ", "Prometheus", "Grafana", "N8N"],
    imageUrl: "/ProxmoxServer.png",
  },
  {
    id: 3,
    tech: ["React", "TypeScript", "Styled-Components", "Node.js", "Express", "MariaDB"],
    imageUrl: "/Happy.png",
    repoUrl: "https://github.com/joaonloureiro/happy-app",
    liveUrl: "https://happy.joaoloureiro.dev.br/"
  }
];

export default function ProjectPage() {
  const t = useTranslations('projects');
  const { id } = useParams();
  const projectId = parseInt(id as string, 10);
  const project = projectsData.find(p => p.id === projectId);

  if (!project) {
    return (
      <div className="container mx-auto px-4 py-12 md:py-20 text-center">
        <h1 className="text-2xl font-bold text-[var(--color-text-primary)]">Project Not Found</h1>
        <Link href="/#projects" className="mt-4 inline-flex items-center gap-2 text-[var(--color-primary)] hover:underline">
          <FaArrowLeft />
          {t('back_to_projects')}
        </Link>
      </div>
    );
  }

  // Helper function to safely get the features array from translations
  const getProjectFeatures = (id: number): string[] => {
    try {
      const features = t.raw(`project_${id}_features`);
      return Array.isArray(features) ? features : [];
    } catch {
      return [];
    }
  };

  const projectFeatures = getProjectFeatures(project.id);

  return (
    <section className="bg-[var(--color-card)] py-12 md:py-20">
      <div className="container mx-auto px-4">
        <Link href="/#projects" className="flex items-center gap-2 text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] transition-colors mb-8 text-sm font-medium">
          <FaArrowLeft />
          {t('back_to_projects')}
        </Link>
        
        <div className="bg-[var(--color-background)] rounded-lg shadow-xl overflow-hidden">
          <div className="relative w-full h-64 md:h-96">
            <Image 
              src={project.imageUrl} 
              alt={t(`project_${project.id}_title`)} 
              layout="fill" 
              objectFit="cover" 
              className="opacity-90"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent" />
            <h1 className="absolute bottom-6 left-6 text-3xl md:text-5xl font-bold text-white tracking-tight">
              {t(`project_${project.id}_title`)}
            </h1>
          </div>
          
          <div className="p-6 md:p-10">
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
              <div className="lg:col-span-2 text-[var(--color-text-secondary)]">
                <h2 className="text-2xl font-bold text-[var(--color-text-primary)] mb-4 border-b border-[var(--color-border)] pb-2">{t('about_project')}</h2>
                <p className="text-lg mb-4">{t(`project_${project.id}_details`)}</p>

                <h3 className="text-xl font-semibold text-[var(--color-text-primary)] mb-2">{t(`project_${project.id}_features_title`)}</h3>
                <ul className="list-disc list-inside mb-4 space-y-2">
                  {projectFeatures.map((feature, index) => (
                    <li key={index} dangerouslySetInnerHTML={{ __html: feature.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>') }} />
                  ))}
                </ul>
              </div>
              
              <aside className="space-y-6">
                <div>
                  <h3 className="text-xl font-bold text-[var(--color-text-primary)] mb-3">{t('tech_used')}</h3>
                  <div className="flex flex-wrap gap-2">
                    {project.tech.map(techItem => (
                      <span key={techItem} className="bg-[var(--color-border)]/50 text-[var(--color-text-secondary)] text-xs font-semibold px-3 py-1.5 rounded-full">
                        {techItem}
                      </span>
                    ))}
                  </div>
                </div>

                <div className="flex flex-col space-y-3 pt-4 border-t border-[var(--color-border)]">
                  {project.liveUrl && (
                    <Link href={project.liveUrl} target="_blank" className="flex items-center gap-3 text-sm text-[var(--color-text-primary)] hover:text-[var(--color-primary)] transition-colors font-semibold">
                      <FaExternalLinkAlt />
                      {t('live_link')}
                    </Link>
                  )}
                  {project.repoUrl && (
                    <Link href={project.repoUrl} target="_blank" className="flex items-center gap-3 text-sm text-[var(--color-text-primary)] hover:text-[var(--color-primary)] transition-colors font-semibold">
                      <FaGithub />
                      {t('repo_link')}
                    </Link>
                  )}
                </div>
              </aside>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}