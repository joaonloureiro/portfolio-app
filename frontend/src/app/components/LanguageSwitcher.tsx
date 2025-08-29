'use client';

import { useLocale } from 'next-intl';
import { usePathname, useRouter } from 'next/navigation';
import { useState, useTransition, useRef, useEffect } from 'react';
import ReactCountryFlag from 'react-country-flag';

export default function LanguageSwitcher() {
  const [isPending, startTransition] = useTransition();
  const router = useRouter();
  const pathname = usePathname().replaceAll(/^\/(pt|en)/g, '');
  const locale = useLocale();

  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  
  const onSelectChange = (nextLocale: string) => {
    startTransition(() => {
      router.replace(`/${nextLocale}${pathname}`);
    });
    setIsOpen(false);
  };

  useEffect(() => {
    const handleOutsideClick = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleOutsideClick);
    return () => document.removeEventListener('mousedown', handleOutsideClick);
  }, []);

  const languages: { [key: string]: { label: string; countryCode: string } } = {
    pt: { label: 'Português', countryCode: 'BR' },
    en: { label: 'English', countryCode: 'US' }
  };

  return (
    <div className="relative" ref={containerRef}>
      <button
        type="button"
        disabled={isPending}
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center justify-center w-10 h-10 text-sm font-medium text-[var(--color-text-secondary)] bg-[var(--color-card)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-border)]/50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-[var(--color-background)] focus:ring-[var(--color-primary)] transition-colors"
      >
        <ReactCountryFlag countryCode={languages[locale].countryCode} svg style={{ width: '24px', height: '24px', borderRadius: '20%' }} /> 
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-40 bg-[var(--color-card)] border border-[var(--color-border)] rounded-md shadow-lg z-10 lang-switcher-options">
          <ul className="py-1">
            {Object.keys(languages).map((langCode) => (
              <li key={langCode}>
                <button
                  onClick={() => onSelectChange(langCode)}
                  className="flex items-center w-full px-4 py-2 text-sm text-left text-[var(--color-text-secondary)] hover:bg-[var(--color-border)]/50"
                >
                  <ReactCountryFlag countryCode={languages[langCode].countryCode} svg style={{ width: '20px', height: '20px', marginRight: '8px', borderRadius: '20%' }} />
                  {languages[langCode].label}
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}