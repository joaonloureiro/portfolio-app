'use client';

import Link from 'next/link';
import {useState} from 'react';
import type {ChangeEvent, FormEvent} from 'react';
import {ArrowUpRightIcon} from '@heroicons/react/24/outline';
import {FaGithub, FaLinkedin} from 'react-icons/fa';
import {useTranslations} from 'next-intl';
import toast from 'react-hot-toast';
import {contactEmail} from '@/lib/contact-config';

type FormData = {
  name: string;
  email: string;
  message: string;
};

type FieldErrors = Partial<Record<keyof FormData, string>>;

const emptyForm: FormData = {name: '', email: '', message: ''};

export default function Contact() {
  const t = useTranslations('contact');
  const footerT = useTranslations('footer');
  const [formData, setFormData] = useState<FormData>(emptyForm);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const githubUrl = process.env.NEXT_PUBLIC_GITHUB_URL;
  const linkedinUrl = process.env.NEXT_PUBLIC_LINKEDIN_URL;

  const handleChange = (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const field = event.target.name as keyof FormData;
    setFormData((current) => ({...current, [field]: event.target.value}));
    setFieldErrors((current) => ({...current, [field]: undefined}));
  };

  const validate = () => {
    const errors: FieldErrors = {};
    if (!formData.name.trim()) errors.name = t('field_required');
    if (!formData.email.trim()) errors.email = t('field_required');
    else if (!/^\S+@\S+\.\S+$/.test(formData.email)) errors.email = t('status_error_invalid_email');
    if (!formData.message.trim()) errors.message = t('field_required');
    else if (formData.message.length > 4000) errors.message = t('contact_payload_too_large');
    return errors;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const errors = validate();
    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      toast.error(t('status_error_all_fields'));
      return;
    }

    setIsSubmitting(true);

    const submission = async () => {
      try {
        const response = await fetch('/api/email/send', {
          method: 'POST',
          headers: {'Content-Type': 'application/json'},
          body: JSON.stringify(formData)
        });

        if (!response.ok) {
          const body = await response.json().catch(() => ({})) as {errorKey?: string};
          throw new Error(body.errorKey || 'status_error_generic');
        }

        return response.json();
      } catch (error: unknown) {
        if (error instanceof TypeError) throw new Error('status_error_generic');
        if (error instanceof Error) throw error;
        throw new Error('server_unexpected_error');
      }
    };

    toast.promise(submission(), {
      loading: t('status_sending'),
      success: () => {
        setFormData(emptyForm);
        setFieldErrors({});
        setIsSubmitting(false);
        return t('status_success');
      },
      error: (error: unknown) => {
        setIsSubmitting(false);
        const key = error instanceof Error ? error.message : 'server_unexpected_error';
        return t.has(key) ? t(key) : t('status_error_generic');
      }
    });
  };

  return (
    <section id="contact" className="signal-contact" aria-labelledby="contact-title">
      <div className="signal-wrap signal-contact-grid">
        <div className="signal-contact-intro">
          <h2 id="contact-title">{t('title')}</h2>
          <p>{t('subtitle')}</p>

          <div className="signal-contact-channels">
            <span className="signal-contact-kicker">{t('direct_label')}</span>
            <a href={`mailto:${contactEmail}`} className="signal-email-inline">
              <span>{contactEmail}</span>
              <ArrowUpRightIcon aria-hidden="true" />
            </a>

            {(githubUrl || linkedinUrl) && (
              <div className="signal-social-icons" aria-label={footerT('socials_title')}>
                {githubUrl && (
                  <Link
                    href={githubUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="signal-icon-btn"
                    aria-label="GitHub"
                    title="GitHub"
                  >
                    <FaGithub aria-hidden="true" />
                  </Link>
                )}
                {linkedinUrl && (
                  <Link
                    href={linkedinUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="signal-icon-btn"
                    aria-label="LinkedIn"
                    title="LinkedIn"
                  >
                    <FaLinkedin aria-hidden="true" />
                  </Link>
                )}
              </div>
            )}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="signal-form" noValidate>
          <div className="signal-form-row">
            <div className="signal-field">
              <label htmlFor="name">{t('form_name')}</label>
              <input
                type="text"
                name="name"
                id="name"
                required
                autoComplete="name"
                maxLength={120}
                value={formData.name}
                onChange={handleChange}
                aria-invalid={Boolean(fieldErrors.name)}
                aria-describedby={fieldErrors.name ? 'name-error' : undefined}
              />
              {fieldErrors.name && <p id="name-error" className="signal-field-error">{fieldErrors.name}</p>}
            </div>

            <div className="signal-field">
              <label htmlFor="email">{t('form_email')}</label>
              <input
                type="email"
                name="email"
                id="email"
                required
                autoComplete="email"
                maxLength={320}
                value={formData.email}
                onChange={handleChange}
                aria-invalid={Boolean(fieldErrors.email)}
                aria-describedby={fieldErrors.email ? 'email-error' : undefined}
              />
              {fieldErrors.email && <p id="email-error" className="signal-field-error">{fieldErrors.email}</p>}
            </div>
          </div>

          <div className="signal-field">
            <label htmlFor="message">{t('form_message')}</label>
            <textarea
              name="message"
              id="message"
              required
              rows={5}
              maxLength={4000}
              value={formData.message}
              onChange={handleChange}
              aria-invalid={Boolean(fieldErrors.message)}
              aria-describedby={fieldErrors.message ? 'message-error' : undefined}
            />
            {fieldErrors.message && <p id="message-error" className="signal-field-error">{fieldErrors.message}</p>}
          </div>

          <div className="signal-submit-row">
            <p>{t('privacy_note')}</p>
            <button className="signal-btn-primary" type="submit" disabled={isSubmitting}>
              <span>{isSubmitting ? t('status_sending') : t('submit_button')}</span>
              <ArrowUpRightIcon aria-hidden="true" />
            </button>
          </div>
        </form>
      </div>
    </section>
  );
}
