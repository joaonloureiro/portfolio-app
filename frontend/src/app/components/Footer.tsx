import { useTranslations } from 'next-intl';
import Link from 'next/link';
import { FaGithub, FaLinkedin, FaTwitter } from 'react-icons/fa';

export default function Footer() {
  const t = useTranslations('footer');

  return (
    <footer className="border-t border-[var(--color-border)] py-8 text-[var(--color-text-secondary)]">
      <div className="container mx-auto flex flex-col md:flex-row justify-between items-center gap-4 px-4 sm:px-6 lg:px-8">
        <p className="text-sm">&copy; {new Date().getFullYear()} João Loureiro. {t('copyright')}</p>
        <div className="flex items-center space-x-4">
          <Link href="#" aria-label="GitHub" target="_blank" className="hover:text-[var(--color-primary)] transition-colors">
            <FaGithub size={20} />
          </Link>
          <Link href="#" aria-label="LinkedIn" target="_blank" className="hover:text-[var(--color-primary)] transition-colors">
            <FaLinkedin size={20} />
          </Link>
          <Link href="#" aria-label="Twitter" target="_blank" className="hover:text-[var(--color-primary)] transition-colors">
            <FaTwitter size={20} />
          </Link>
        </div>
      </div>
    </footer>
  );
}