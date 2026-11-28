import Link from 'next/link';
import {useLocale, useTranslations} from 'next-intl';
import {FaGithub, FaLinkedin} from 'react-icons/fa';
import {HiOutlineMail} from 'react-icons/hi';
import {contactEmail} from '@/lib/contact-config';

export default function Footer() {
  const t = useTranslations('footer');
  const locale = useLocale();
  const githubUrl = process.env.NEXT_PUBLIC_GITHUB_URL;
  const linkedinUrl = process.env.NEXT_PUBLIC_LINKEDIN_URL;

  return (
    <footer className="signal-footer">
      <div className="signal-wrap signal-footer-inner">
        <div className="signal-footer-left">
          <Link href={`/${locale}`} className="signal-footer-brand">
            <svg width="20" height="20" viewBox="-18 -6 138 122" fill="none" stroke="currentColor" strokeWidth="9" strokeLinecap="square" aria-hidden="true">
              <path d="M38 0v68c0 14-10 24-22 24C5 92 0 85 0 76c0-10 7-18 17-18h11M56 0v68c0 21-16 40-38 40-14 0-34-10-34-34 2-20 12-32 36-32h8M56 0v108h56V92M56 25h19v66h37" />
            </svg>
            <span>João Loureiro</span>
          </Link>
          <p>© {new Date().getFullYear()} João Loureiro</p>
        </div>

        <nav className="signal-social-icons" aria-label={t('socials_title')}>
          {githubUrl && (
            <Link
              href={githubUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="signal-icon-btn"
              aria-label="GitHub"
              title="GitHub"
            >
              <FaGithub aria-hidden="true" />
            </Link>
          )}
          {linkedinUrl && (
            <Link
              href={linkedinUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="signal-icon-btn"
              aria-label="LinkedIn"
              title="LinkedIn"
            >
              <FaLinkedin aria-hidden="true" />
            </Link>
          )}
          <a
            href={`mailto:${contactEmail}`}
            className="signal-icon-btn"
            aria-label="Email"
            title={contactEmail}
          >
            <HiOutlineMail aria-hidden="true" />
          </a>
        </nav>
      </div>
    </footer>
  );
}
