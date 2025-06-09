import {useTranslations} from 'next-intl';
import ProjectCard from '../ProjectCard';

// Você pode popular isso com seus dados reais
const projectsData = [
  { id: 1, tech: ["Next.js", "Stripe", "Tailwind CSS", "Prisma"] },
  { id: 2, tech: ["Socket.IO", "Node.js", "React", "Express"] },
  { id: 3, tech: ["Next.js", "MDX", "Tailwind CSS", "Vercel"] }
];

export default function Projects() {
  const t = useTranslations('projects');

  return (
    <section id="projects" className="container mx-auto py-24 md:py-32 lg:py-48">
      <h2 className="text-3xl md:text-4xl font-bold text-center text-[var(--color-text-primary)] mb-12">{t('title')}</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
        {projectsData.map(project => (
          <ProjectCard
            key={project.id}
            title={t(`project_${project.id}_title`)}
            description={t(`project_${project.id}_description`)}
            tech={project.tech}
          />
        ))}
      </div>
    </section>
  );
}