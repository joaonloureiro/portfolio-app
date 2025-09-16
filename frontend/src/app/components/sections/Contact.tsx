'use client';
import { motion } from 'framer-motion';
import {useTranslations} from 'next-intl';
import { FormEvent, useState } from 'react';
import toast from 'react-hot-toast';

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
      try {
        const response = await fetch('/api/email/send', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(formData),
        });

        if (!response.ok) {
          const body = await response.json();
          throw new Error(body.errorKey || 'status_error_generic');
        }

        return await response.json();

      } catch (error: unknown) {
        if (error instanceof TypeError) {
          throw new Error('status_error_generic');
        }
        if (error instanceof Error) {
            throw error;
        }
        throw new Error('server_unexpected_error');
      }
    };

    toast.promise(submissionPromise(), {
      loading: t('status_sending'),
      success: () => {
        setFormData({ name: '', email: '', message: '' });
        setIsSubmitting(false);
        return t('status_success');
      },
      error: (err: unknown) => {
        setIsSubmitting(false);
        if (err instanceof Error) {
            return t(err.message as string) || t('status_error_generic');
        }
        return t('server_unexpected_error');
      },
    });
  };

  return (
    <motion.section 
      id="contact" 
      className="mx-auto py-12 md:py-20"
      initial={{ opacity: 0, y: 20 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ duration: 0.6, ease: "easeOut" }}
    >
      <div className="container mx-auto px-4">
        <div className="text-justify max-w-2xl mx-auto">
          <h2 className="text-3xl md:text-4xl font-bold text-[var(--color-text-primary)] heading-underline">{t('title')}</h2>
          <p className="text-[var(--color-text-secondary)] mb-10">{t('subtitle')}</p>
        </div>

        {/* Centered Form */}
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
          <button type="submit" className="bg-[var(--color-primary)] hover:bg-[var(--color-primary)]/90 text-white font-bold py-3 px-8 rounded-full transition-colors w-full" disabled={isSubmitting}>
            {isSubmitting ? t('status_sending') : t('submit_button')}
          </button>
        </form>
      </div>
    </motion.section>
  );
}