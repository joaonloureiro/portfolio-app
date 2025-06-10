'use client';
import {useTranslations} from 'next-intl';
import ProjectCard from '../ProjectCard';
import { motion } from 'framer-motion';

const projectsData = [
  { id: 1, tech: ["Next.js", "Stripe", "Tailwind CSS", "Prisma"], imageUrl: "/project1.jpg" },
  { id: 2, tech: ["Socket.IO", "Node.js", "React", "Express"], imageUrl: "/project2.jpg" },
  { id: 3, tech: ["Next.js", "MDX", "Tailwind CSS", "Vercel"], imageUrl: "/project3.jpg" }
];

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { staggerChildren: 0.2, delayChildren: 0.1 }
  }
};

const cardVariants = {
  hidden: { y: 20, opacity: 0 },
  visible: { y: 0, opacity: 1 }
};

export default function Projects() {
  const t = useTranslations('projects');

  return (
    <motion.section 
      id="projects" 
      className="bg-[var(--color-card)] py-12 md:py-20"
      initial={{ opacity: 0 }}
      whileInView={{ opacity: 1 }}
      viewport={{ once: true }}
      transition={{ duration: 0.5 }}
    >
      <div className="container mx-auto px-4">
        <h2 className="text-3xl md:text-4xl font-bold text-center text-[var(--color-text-primary)] heading-underline">{t('title')}</h2>
        <motion.div 
          className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8"
          variants={containerVariants}
          initial="hidden"
          whileInView="visible"
          viewport={{ once: true }}
        >
          {projectsData.map(project => (
            <motion.div key={project.id} variants={cardVariants}>
              <ProjectCard
                title={t(`project_${project.id}_title`)}
                description={t(`project_${project.id}_description`)}
                tech={project.tech}
                imageUrl={project.imageUrl}
              />
            </motion.div>
          ))}
        </motion.div>
      </div>
    </motion.section>
  );
}