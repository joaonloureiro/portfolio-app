import {useTranslations} from 'next-intl';

export default function About() {
  const t = useTranslations('about');
  
  return (
    <section id="about" className="container mx-auto py-24 md:py-32 lg:py-48">
      <h2 className="text-3xl md:text-4xl font-bold text-center text-[var(--color-text-primary)] mb-12">{t('title')}</h2>
      <div className="max-w-3xl mx-auto text-[var(--color-text-secondary)] space-y-6 text-lg text-left md:text-justify">
        <p>{t('paragraph1')}</p>
        <p>{t('paragraph2')}</p>
        <p>{t('paragraph3')}</p>
      </div>
    </section>
  );
}