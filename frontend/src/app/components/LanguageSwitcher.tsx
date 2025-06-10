'use client';

import { useLocale } from 'next-intl';
import { usePathname, useRouter } from 'next/navigation';
import { useState, useTransition, useRef, useEffect } from 'react';
import { FaChevronDown } from 'react-icons/fa6';

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

  const languages: { [key: string]: { flag: string; label: string } } = {
    en: { flag: '🇺🇸', label: 'English' },
    pt: { flag: '🇧🇷', label: 'Português' },
  };

  return (
    <div className="relative" ref={containerRef}>
      <button
        type="button"
        disabled={isPending}
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center justify-between w-full px-3 py-2 text-sm font-medium text-[var(--color-text-secondary)] bg-[var(--color-card)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-border)]/50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-[var(--color-background)] focus:ring-[var(--color-primary)] transition-colors"
      >
        <span>{languages[locale].flag} {languages[locale].label}</span>
        <FaChevronDown className={`ml-2 h-4 w-4 transition-transform ${isOpen ? 'transform rotate-180' : ''}`} />
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
                  <span className="mr-2">{languages[langCode].flag}</span>
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