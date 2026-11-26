import Link from 'next/link';
import {useLocale, useTranslations} from 'next-intl';
import {contactEmail} from '@/lib/contact-config';

export default function Footer() {
  const t = useTranslations('footer');
  const locale = useLocale();
  const githubUrl = process.env.NEXT_PUBLIC_GITHUB_URL;
  const linkedinUrl = process.env.NEXT_PUBLIC_LINKEDIN_URL;

  return (
    <footer className="contract-footer">
      <div className="contract-wrap">
        <Link href={`/${locale}`} className="contract-brand">João Loureiro</Link>
        <p>© {new Date().getFullYear()} João Loureiro</p>
        <nav aria-label={t('socials_title')}>
          {githubUrl && <Link href={githubUrl} target="_blank" rel="noopener noreferrer">GitHub</Link>}
          {linkedinUrl && <Link href={linkedinUrl} target="_blank" rel="noopener noreferrer">LinkedIn</Link>}
          <a href={`mailto:${contactEmail}`}>Email</a>
        </nav>
      </div>
    </footer>
  );
}
