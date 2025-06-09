'use client';

import {useTranslations} from 'next-intl';

export default function Contact() {
  const t = useTranslations('contact');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    alert('Contact form submission logic needs to be implemented!');
  };

  return (
    <section id="contact" className="container mx-auto py-24 md:py-32 lg:py-48">
      <div className="text-center max-w-2xl mx-auto">
        <h2 className="text-3xl md:text-4xl font-bold text-[var(--color-text-primary)] mb-4">{t('title')}</h2>
        <p className="text-[var(--color-text-secondary)] mb-10">{t('subtitle')}</p>
      </div>
      <form onSubmit={handleSubmit} className="max-w-xl mx-auto">
        <div className="mb-4">
          <label htmlFor="name" className="block mb-2 text-sm font-medium text-[var(--color-text-secondary)]">{t('form_name')}</label>
          <input type="text" id="name" className="bg-[var(--color-card)] border border-[var(--color-border)] text-[var(--color-text-primary)] text-sm rounded-lg focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)] block w-full p-2.5" required />
        </div>
        <div className="mb-4">
          <label htmlFor="email" className="block mb-2 text-sm font-medium text-[var(--color-text-secondary)]">{t('form_email')}</label>
          <input type="email" id="email" className="bg-[var(--color-card)] border border-[var(--color-border)] text-[var(--color-text-primary)] text-sm rounded-lg focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)] block w-full p-2.5" required />
        </div>
        <div className="mb-6">
          <label htmlFor="message" className="block mb-2 text-sm font-medium text-[var(--color-text-secondary)]">{t('form_message')}</label>
          <textarea id="message" rows={5} className="bg-[var(--color-card)] border border-[var(--color-border)] text-[var(--color-text-primary)] text-sm rounded-lg focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)] block w-full p-2.5" required></textarea>
        </div>
        <div className="text-center">
          <button type="submit" className="bg-[var(--color-primary)] hover:bg-[var(--color-primary)]/90 text-white font-bold py-3 px-8 rounded-full transition-colors">
            {t('submit_button')}
          </button>
        </div>
      </form>
    </section>
  );
}