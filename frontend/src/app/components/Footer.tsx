import Link from 'next/link';
import {useTranslations} from 'next-intl';

export default function Footer() {
  const t = useTranslations('footer');
  const githubUrl = process.env.NEXT_PUBLIC_GITHUB_URL || '#';
  const linkedinUrl = process.env.NEXT_PUBLIC_LINKEDIN_URL || '#';

  return (
    <footer className="site-footer">
      <p>© {new Date().getFullYear()} João Loureiro. {t('copyright')}</p>
      <p>{t('credit')}</p>
      <nav aria-label={t('socials_title')}>
        <Link href={githubUrl} target="_blank" rel="noopener noreferrer">GitHub</Link>
        <Link href={linkedinUrl} target="_blank" rel="noopener noreferrer">LinkedIn</Link>
        <Link href="mailto:me@joaoloureiro.dev.br">Email</Link>
      </nav>
    </footer>
  );
}
