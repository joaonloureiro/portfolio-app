import {useTranslations} from 'next-intl';
import Link from 'next/link';

export default function Hero() {
  const t = useTranslations('hero');

  return (
    <section id="home" className="container mx-auto text-center py-24 md:py-32 lg:py-48">
      <h1 className="text-4xl md:text-5xl lg:text-6xl font-bold text-[var(--color-text-primary)]">
        {t('greeting')} <span className="block text-[var(--color-primary)]">{t('title')}</span>
      </h1>
      <p className="text-lg md:text-xl text-[var(--color-text-secondary)] max-w-2xl mx-auto mt-6 mb-8">
        {t('subtitle')}
      </p>
      <Link href="#contact" className="bg-[var(--color-primary)] hover:bg-[var(--color-primary)]/90 text-white font-bold py-3 px-8 rounded-full transition-colors">
        {t('cta_button')}
      </Link>
    </section>
  );
}