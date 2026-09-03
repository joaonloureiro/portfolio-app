'use client';

import {useEffect, useRef, useState} from 'react';
import Link from 'next/link';
import {useLocale, useTranslations} from 'next-intl';
import {Bars3Icon, MoonIcon, SunIcon, XMarkIcon} from '@heroicons/react/24/outline';
import {FaGithub, FaLinkedin} from 'react-icons/fa';
import {HiOutlineMail} from 'react-icons/hi';
import {useTheme} from '@/configuration/ThemeContext';
import LanguageSwitcher from '@/app/components/LanguageSwitcher';

const primaryLinks = [
  {href: '#projects', key: 'projects'},
  {href: '#about', key: 'about'},
  {href: '#contact', key: 'contact'}
] as const;

export default function Header() {
  const t = useTranslations('navigation');
  const locale = useLocale();
  const {theme, toggleTheme} = useTheme();
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [activeSection, setActiveSection] = useState<string | null>(null);
  const headerRef = useRef<HTMLElement>(null);

  const closeMenu = () => setIsMenuOpen(false);

  useEffect(() => {
    const handlePointerDown = (event: MouseEvent) => {
      if (isMenuOpen && headerRef.current && !headerRef.current.contains(event.target as Node)) {
        closeMenu();
      }
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        closeMenu();
      }
    };

    document.addEventListener('mousedown', handlePointerDown);
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('mousedown', handlePointerDown);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isMenuOpen]);

  useEffect(() => {
    document.body.style.overflow = isMenuOpen ? 'hidden' : '';
    return () => {
      document.body.style.overflow = '';
    };
  }, [isMenuOpen]);

  useEffect(() => {
    const updateActiveSection = () => {
      const marker = window.innerHeight * 0.35;
      let nextSection: string | null = null;

      for (const item of primaryLinks) {
        const section = document.getElementById(item.key);
        if (!section) continue;

        const bounds = section.getBoundingClientRect();
        if (bounds.top <= marker && bounds.bottom > marker) {
          nextSection = item.key;
          break;
        }
      }

      setActiveSection((currentSection) => currentSection === nextSection ? currentSection : nextSection);
    };

    updateActiveSection();
    window.addEventListener('scroll', updateActiveSection, {passive: true});
    window.addEventListener('resize', updateActiveSection);

    return () => {
      window.removeEventListener('scroll', updateActiveSection);
      window.removeEventListener('resize', updateActiveSection);
    };
  }, []);

  const themeLabel = theme === 'dark' ? t('switch_to_light') : t('switch_to_dark');
  const githubUrl = process.env.NEXT_PUBLIC_GITHUB_URL || '#';
  const linkedinUrl = process.env.NEXT_PUBLIC_LINKEDIN_URL || '#';

  return (
    <header ref={headerRef} className="site-header">
      <div className="identity-rail">
        <Link href={`/${locale}`} className="identity-lockup" aria-label={t('home')}>
          <span className="identity-name">João Loureiro</span>
        </Link>

        <nav className="atlas-nav" aria-label={t('primary_label')}>
          <ol>
            {primaryLinks.map((item, index) => (
              <li key={item.key} data-active={activeSection === item.key}>
                <Link
                  href={`/${locale}${item.href}`}
                  aria-current={activeSection === item.key ? 'location' : undefined}
                  data-active={activeSection === item.key}
                >
                  <span aria-hidden="true">{String(index + 1).padStart(2, '0')}</span>
                  {t(item.key)}
                </Link>
              </li>
            ))}
          </ol>
        </nav>

        <Link href={`/${locale}`} className="identity-monogram" aria-hidden="true" tabIndex={-1}>
          <svg viewBox="0 0 112 132" aria-hidden="true">
            <path d="M 38 0 V 68 C 38 82, 28 92, 16 92 C 5 92, 0 85, 0 76 C 0 66, 7 58, 17 58 H 28" />
            <path d="M 56 0 V 68 C 56 89, 40 108, 18 108 C 4 108, -16 98, -16 74 C -14 54, -4 42, 20 42 H 28" />
            <path d="M 56 0 V 108 H 112 V 92" />
            <path d="M 56 25 H 75 V 91 H 112" />
          </svg>
        </Link>

        <div className="rail-footer">
          <div className="rail-controls">
            <LanguageSwitcher idPrefix="rail" />
            <button type="button" className="rail-icon-button" onClick={toggleTheme} aria-label={themeLabel} title={themeLabel}>
              {theme === 'dark' ? <SunIcon aria-hidden="true" /> : <MoonIcon aria-hidden="true" />}
            </button>
          </div>

          <div className="rail-socials" aria-label={t('social_label')}>
            <Link href={githubUrl} target="_blank" rel="noopener noreferrer" aria-label="GitHub"><FaGithub aria-hidden="true" /></Link>
            <Link href="mailto:me@joaoloureiro.dev.br" aria-label="Email"><HiOutlineMail aria-hidden="true" /></Link>
            <Link href={linkedinUrl} target="_blank" rel="noopener noreferrer" aria-label="LinkedIn"><FaLinkedin aria-hidden="true" /></Link>
          </div>
        </div>
      </div>

      <div className="mobile-bar">
        <Link href={`/${locale}`} className="mobile-brand" onClick={closeMenu}>JL / FULL STACK</Link>
        <div className="mobile-actions">
          <LanguageSwitcher compact idPrefix="mobile" />
          <button
            type="button"
            className="mobile-menu-button"
            onClick={() => setIsMenuOpen((open) => !open)}
            aria-expanded={isMenuOpen}
            aria-controls="mobile-navigation"
            aria-label={isMenuOpen ? t('close_menu') : t('open_menu')}
          >
            {isMenuOpen ? <XMarkIcon aria-hidden="true" /> : <Bars3Icon aria-hidden="true" />}
          </button>
        </div>
      </div>

      <div id="mobile-navigation" className="mobile-navigation" data-open={isMenuOpen} aria-hidden={!isMenuOpen}>
        <nav aria-label={t('primary_label')}>
          {primaryLinks.map((item, index) => (
            <Link
              key={item.key}
              href={`/${locale}${item.href}`}
              onClick={closeMenu}
              tabIndex={isMenuOpen ? 0 : -1}
              aria-current={activeSection === item.key ? 'location' : undefined}
              data-active={activeSection === item.key}
            >
              <span aria-hidden="true">{String(index + 1).padStart(2, '0')}</span>
              {t(item.key)}
            </Link>
          ))}
        </nav>
        <div className="mobile-navigation-footer">
          <button type="button" onClick={toggleTheme} tabIndex={isMenuOpen ? 0 : -1}>
            {themeLabel}
          </button>
          <a href="mailto:me@joaoloureiro.dev.br" tabIndex={isMenuOpen ? 0 : -1}>me@joaoloureiro.dev.br</a>
        </div>
      </div>
    </header>
  );
}
