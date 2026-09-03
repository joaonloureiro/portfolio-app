'use client';

import {useLocale, useTranslations} from 'next-intl';
import {usePathname, useRouter} from 'next/navigation';
import {useEffect, useRef, useState, useTransition} from 'react';

type LanguageSwitcherProps = {
  compact?: boolean;
  idPrefix: string;
};

const languages = [
  {code: 'pt', label: 'Português'},
  {code: 'en', label: 'English'}
] as const;

export default function LanguageSwitcher({compact = false, idPrefix}: LanguageSwitcherProps) {
  const [isPending, startTransition] = useTransition();
  const [isOpen, setIsOpen] = useState(false);
  const router = useRouter();
  const pathname = usePathname().replace(/^\/(pt|en)(?=\/|$)/, '') || '/';
  const locale = useLocale();
  const t = useTranslations('navigation');
  const containerRef = useRef<HTMLDivElement>(null);

  const selectLanguage = (nextLocale: string) => {
    startTransition(() => {
      router.replace(`/${nextLocale}${pathname === '/' ? '' : pathname}`);
    });
    setIsOpen(false);
  };

  useEffect(() => {
    const handleOutsideClick = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleOutsideClick);
    document.addEventListener('keydown', handleEscape);
    return () => {
      document.removeEventListener('mousedown', handleOutsideClick);
      document.removeEventListener('keydown', handleEscape);
    };
  }, []);

  return (
    <div className="language-switcher" data-compact={compact} ref={containerRef}>
      <button
        type="button"
        disabled={isPending}
        onClick={() => setIsOpen((open) => !open)}
        className="language-trigger"
        aria-label={t('language')}
        aria-expanded={isOpen}
        aria-controls={`${idPrefix}-language-options`}
      >
        {locale.toUpperCase()}
      </button>

      <div id={`${idPrefix}-language-options`} className="language-options" data-open={isOpen}>
        {languages.map((language) => (
          <button
            type="button"
            key={language.code}
            onClick={() => selectLanguage(language.code)}
            aria-current={locale === language.code ? 'true' : undefined}
            tabIndex={isOpen ? 0 : -1}
          >
            <span>{language.code.toUpperCase()}</span>
            {language.label}
          </button>
        ))}
      </div>
    </div>
  );
}
