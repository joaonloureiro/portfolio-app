'use client';
import Link from 'next/link';
import { useTranslations } from 'next-intl';
import { useTheme } from '@/configuration/ThemeContext';
import { SunIcon, MoonIcon } from '@heroicons/react/24/outline'; // Install @heroicons/react
import Image from 'next/image';
import LanguageSwitcher from "@/app/components/LanguageSwitcher";

export default function Header() {
  const t = useTranslations('navigation');
  const { theme, toggleTheme } = useTheme();

  return (
    <header className="sticky top-0 z-50 border-b border-[var(--color-border)] bg-[var(--color-background)]/90 backdrop-blur-sm">
      <nav className="container mx-auto flex items-center justify-between py-4 px-4 sm:px-6 lg:px-8">
        <Link href="/">
          <Image src="/logo.png" alt="João Loureiro Logo" width={40} height={40} priority />
        </Link>
        <div className="hidden md:flex items-center space-x-6 text-[var(--color-text-secondary)]">
          <Link href="#about" className="hover:text-[var(--color-primary)] transition-colors">{t('about')}</Link>
          <Link href="#skills" className="hover:text-[var(--color-primary)] transition-colors">{t('tech')}</Link>
          <Link href="#projects" className="hover:text-[var(--color-primary)] transition-colors">{t('projects')}</Link>
          <Link href="#contact" className="hover:text-[var(--color-primary)] transition-colors">{t('contact')}</Link>
        </div>
        <div className="hidden md:flex items-center space-x-6 text-[var(--color-text-secondary)]">
          {/* Theme Toggle */}
          <button onClick={toggleTheme} className="p-2 rounded-md hover:bg-gray-200 dark:hover:bg-gray-700">
            {theme === 'dark'? <SunIcon className="h-6 w-6 text-yellow-400" /> : <MoonIcon className="h-6 w-6 text-gray-600" />}
          </button>

          <LanguageSwitcher />
        </div>
      </nav>
    </header>
  );
}
