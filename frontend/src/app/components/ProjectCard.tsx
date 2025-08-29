import Link from 'next/link';
import {useTranslations, useLocale} from 'next-intl';
import { FaGithub, FaArrowUpRightFromSquare, FaLink } from 'react-icons/fa6';
import Image from 'next/image';

type ProjectCardProps = {
  title: string;
  description: string;
  tech: string[];
  imageUrl: string;
  liveUrl?: string;
  repoUrl?: string;
  id: number;
};

export default function ProjectCard({ title, description, tech, imageUrl, liveUrl, repoUrl, id }: ProjectCardProps) {
  const t = useTranslations('projects');
  const locale = useLocale();

  return (
    <div className="h-full bg-[var(--color-background)] rounded-lg overflow-hidden flex flex-col border border-[var(--color-border)] hover:border-[var(--color-primary)]/50 transition-all duration-300 hover:shadow-xl hover:-translate-y-1">
      <div className="relative w-full h-48">
        <Image src={imageUrl} alt={title} layout="fill" objectFit="cover" />
      </div>
      <div className="p-6 flex flex-col flex-grow">
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
          <Link href={`/${locale}/project/${id}`} className="flex gap-2 items-center text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] transition-colors">
            <FaLink size={20} />
            {t('page_link')}
          </Link>
          {repoUrl && (
            <Link href={repoUrl} target="_blank" className="flex gap-2 items-center text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] transition-colors">
              <FaGithub size={20} />
              {t('repo_link')}
            </Link>
          )}
          {liveUrl && (
            <Link href={liveUrl} target="_blank" className="flex gap-2 items-center text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] transition-colors">
              {t('live_link')} 
              <FaArrowUpRightFromSquare size={12} />
            </Link>
          )}
        </div>
      </div>
    </div>
  );
}