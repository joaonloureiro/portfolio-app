import Link from 'next/link';
import {useTranslations} from 'next-intl';
import { FaGithub, FaArrowUpRightFromSquare } from 'react-icons/fa6';

type ProjectCardProps = {
  title: string;
  description: string;
  tech: string[];
};

export default function ProjectCard({ title, description, tech }: ProjectCardProps) {
  const t = useTranslations('projects');

  return (
    <div className="bg-[var(--color-card)] rounded-lg p-6 flex flex-col border border-[var(--color-border)] hover:border-[var(--color-primary)]/50 transition-colors duration-300">
      <h3 className="text-[var(--color-text-primary)] mb-2">{title}</h3>
      <p className="text-[var(--color-text-secondary)] mb-4 flex-grow">{description}</p>
      <div className="flex flex-wrap gap-2 mb-4">
        {tech.map(t => (
          <span key={t} className="bg-[var(--color-border)]/50 text-[var(--color-text-secondary)] text-xs font-semibold px-2.5 py-1 rounded-full">
            {t}
          </span>
        ))}
      </div>
      <div className="flex items-center space-x-8 mt-auto pt-4 border-t border-[var(--color-border)]">
        <Link href="#" target="_blank" className="flex gap-2 items-center text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] transition-colors">
          {t('live_link')} 
          <FaArrowUpRightFromSquare size={12} />
        </Link>
        <Link href="#" target="_blank" className="flex gap-2 items-center text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] transition-colors">
          <FaGithub size={20} />
          {t('repo_link')}
        </Link>
      </div>
    </div>
  );
}