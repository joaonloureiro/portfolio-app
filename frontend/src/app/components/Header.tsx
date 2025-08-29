'use client';
import { useState, useEffect, useRef } from 'react';
import Link from 'next/link';
import { useTranslations } from 'next-intl';
import { useTheme } from '@/configuration/ThemeContext';
import { SunIcon, MoonIcon, Bars3Icon, XMarkIcon } from '@heroicons/react/24/outline';
import Image from 'next/image';
import LanguageSwitcher from "@/app/components/LanguageSwitcher";

export default function Header() {
  const t = useTranslations('navigation');
  const { theme, toggleTheme } = useTheme();
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const headerRef = useRef<HTMLElement>(null);

  // Close the menu when a link is clicked
  const handleLinkClick = () => {
    setIsMenuOpen(false);
  };

  // Effect to handle closing menu on outside click
  useEffect(() => {
    const handleOutsideClick = (event: MouseEvent) => {
      if (isMenuOpen && headerRef.current && !headerRef.current.contains(event.target as Node)) {
        setIsMenuOpen(false);
      }
    };

    document.addEventListener('mousedown', handleOutsideClick);
    return () => {
      document.removeEventListener('mousedown', handleOutsideClick);
    };
  }, [isMenuOpen]);

  // Prevent background scrolling when the mobile menu is open
  useEffect(() => {
    if (isMenuOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = 'auto';
    }
  }, [isMenuOpen]);

  return (
    <header ref={headerRef} className="sticky top-0 z-50 bg-[var(--color-background)]/90 backdrop-blur-sm border-b border-[var(--color-border)]">
      <nav className="container mx-auto flex items-center justify-between py-4 px-4 sm:px-6 lg:px-8">
        <Link href="/" onClick={handleLinkClick}>
          <Image src="/logo.png" alt="João Loureiro Logo" width={40} height={40} priority />
        </Link>
        
        {/* Desktop Menu */}
        <div className="hidden md:flex items-center space-x-8">
          <Link href="#about" className="text-sm font-medium text-[var(--color-text-primary)] hover:text-[var(--color-primary)] transition-colors">{t('about')}</Link>
          <Link href="#projects" className="text-sm font-medium text-[var(--color-text-primary)] hover:text-[var(--color-primary)] transition-colors">{t('projects')}</Link>
          <Link href="#contact" className="text-sm font-medium text-[var(--color-text-primary)] hover:text-[var(--color-primary)] transition-colors">{t('contact')}</Link>
        </div>

        <div className="hidden md:flex items-center space-x-4">
          <LanguageSwitcher />
          <button onClick={toggleTheme} aria-label="Toggle theme" className="flex items-center justify-center w-10 h-10 text-sm font-medium text-[var(--color-text-secondary)] bg-[var(--color-card)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-border)]/50">
            {theme === 'dark' ? <SunIcon className="h-5 w-5 text-yellow-400" /> : <MoonIcon className="h-5 w-5 text-gray-600" />}
          </button>
        </div>

        {/* Mobile Menu Button */}
        <div className="md:hidden">
          <button onClick={() => setIsMenuOpen(!isMenuOpen)} className="p-2 rounded-md" aria-label="Toggle menu">
            {isMenuOpen ? <XMarkIcon className="h-6 w-6" /> : <Bars3Icon className="h-6 w-6" />}
          </button>
        </div>
      </nav>

      {/* Mobile Menu Panel */}
      {isMenuOpen && (
        <div className="md:hidden absolute top-full left-0 w-full bg-[var(--color-background)] shadow-lg border-t border-[var(--color-border)]">
          <div className="flex flex-col items-center space-y-4 p-6">
            <Link href="#about" className="text-lg" onClick={handleLinkClick}>{t('about')}</Link>
            <Link href="#projects" className="text-lg" onClick={handleLinkClick}>{t('projects')}</Link>
            <Link href="#contact" className="text-lg" onClick={handleLinkClick}>{t('contact')}</Link>
            
            <div className="flex items-center space-x-6 pt-6 mt-4 border-t border-[var(--color-border)] w-full justify-center">
              <LanguageSwitcher />
              <button onClick={toggleTheme} aria-label="Toggle theme" className="p-2 rounded-md">
                {theme === 'dark' ? <SunIcon className="h-6 w-6 text-yellow-400" /> : <MoonIcon className="h-6 w-6 text-gray-600" />}
              </button>
            </div>
          </div>
        </div>
      )}
    </header>
  );
}