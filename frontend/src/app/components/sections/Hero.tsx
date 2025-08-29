'use client';
import {useTranslations} from 'next-intl';
import Link from 'next/link';

export default function Hero() {
  const t = useTranslations('hero');

  return (
    <section id="home" className="relative w-full flex flex-col items-center justify-center min-h-[calc(100vh-var(--header-height))]">
        <div className="container mx-auto px-4 text-center flex flex-col items-center">
            <h1 className="text-4xl md:text-5xl lg:text-6xl font-bold text-white">
                {t('greeting')} <span className="text-[var(--color-primary)]">João Loureiro</span>
            </h1>
            <p className="text-lg md:text-xl text-slate-200 max-w-2xl mx-auto mt-6 mb-8">
                {t('subtitle')}
            </p>
            <Link href="#contact" className="bg-[var(--color-primary)] hover:bg-[var(--color-primary)]/90 text-white font-bold py-3 px-8 rounded-full transition-colors">
                {t('cta_button')}
            </Link>
        </div>
      
      <div className="absolute bottom-12">
        <div className="mouse"></div>
      </div>
    </section>
  );
}