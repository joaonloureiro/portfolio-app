'use client';

import Link from 'next/link';
import {useLocale, useTranslations} from 'next-intl';
import {ArrowUpRightIcon, MoonIcon, SunIcon} from '@heroicons/react/24/outline';
import {useTheme} from '@/configuration/ThemeContext';
import LanguageSwitcher from '@/app/components/LanguageSwitcher';

export default function Header() {
  const locale = useLocale();
  const nav = useTranslations('navigation');
  const {theme, toggleTheme} = useTheme();
  const themeLabel = theme === 'dark' ? nav('switch_to_light') : nav('switch_to_dark');

  return (
    <header className="signal-hud">
      <Link href={`/${locale}`} className="signal-hud-pill signal-hud-brand">
        <svg width="22" height="22" viewBox="-18 -6 138 122" fill="none" stroke="currentColor" strokeWidth="9" strokeLinecap="square" aria-hidden="true">
          <path d="M38 0v68c0 14-10 24-22 24C5 92 0 85 0 76c0-10 7-18 17-18h11M56 0v68c0 21-16 40-38 40-14 0-34-10-34-34 2-20 12-32 36-32h8M56 0v108h56V92M56 25h19v66h37" />
        </svg>
        <span>João Loureiro</span>
      </Link>

      <div className="signal-hud-tools">
        <LanguageSwitcher idPrefix="public" />
        <button
          type="button"
          className="signal-hud-btn signal-theme-btn"
          onClick={toggleTheme}
          aria-label={themeLabel}
          title={themeLabel}
        >
          {theme === 'dark' ? <SunIcon aria-hidden="true" /> : <MoonIcon aria-hidden="true" />}
        </button>
        <Link href={`/${locale}#contact`} className="signal-hud-pill signal-hud-contact">
          <span>{nav('contact')}</span>
          <ArrowUpRightIcon aria-hidden="true" />
        </Link>
      </div>
    </header>
  );
}
