'use client';

import {useTranslations} from 'next-intl';
import { FormEvent, useState } from 'react';
import toast from 'react-hot-toast'; // <-- Import toast

export default function Contact() {
  const t = useTranslations('contact');
  const [formData, setFormData] = useState<{ name: string; email: string; message: string }>({ name: '', email: '', message: '' });
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setFormData({...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);

    if (!formData.name || !formData.email || !formData.message) {
      toast.error(t('status_error_all_fields'));
      setIsSubmitting(false);
      return;
    }
    if (!/\S+@\S+\.\S+/.test(formData.email)) {
      toast.error(t('status_error_invalid_email'));
      setIsSubmitting(false);
      return;
    }

    const submissionPromise = async () => {
      //const backendUrl = process.env.NEXT_PUBLIC_BACKEND_URL;
      try {
        const response = await fetch(`http://localhost:3001/api/email/send`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(formData),
        });

        if (!response.ok) {
          const body = await response.json();
          throw new Error(body.errorKey || 'status_error_generic');
        }

        return await response.json();

      } catch (error: any) {
        if (error instanceof TypeError) {
          throw new Error('status_error_generic');
        }
        throw error;
      }
    };

    toast.promise(submissionPromise(), {
      loading: t('status_sending'),
      success: () => {
        setFormData({ name: '', email: '', message: '' });
        setIsSubmitting(false);
        return t('status_success');
      },
      error: (err: Error) => {
        setIsSubmitting(false);
        return t(err.message as any);
      },
    });
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
          <input type="text" name="name" id="name" value={formData.name} onChange={handleChange} required
           className="bg-[var(--color-card)] border border-[var(--color-border)] text-[var(--color-text-primary)] text-sm rounded-lg focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)] block w-full p-2.5" />
        </div>
        <div className="mb-4">
          <label htmlFor="email" className="block mb-2 text-sm font-medium text-[var(--color-text-secondary)]">{t('form_email')}</label>
          <input type="email" name="email" id="email" value={formData.email} onChange={handleChange} required
           className="bg-[var(--color-card)] border border-[var(--color-border)] text-[var(--color-text-primary)] text-sm rounded-lg focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)] block w-full p-2.5" />
        </div>
        <div className="mb-6">
          <label htmlFor="message" className="block mb-2 text-sm font-medium text-[var(--color-text-secondary)]">{t('form_message')}</label>
          <textarea name="message" id="message" rows={5} value={formData.message} onChange={handleChange} required
            className="bg-[var(--color-card)] border border-[var(--color-border)] text-[var(--color-text-primary)] text-sm rounded-lg focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)] block w-full p-2.5"></textarea>
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