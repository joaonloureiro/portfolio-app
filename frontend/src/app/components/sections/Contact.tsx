'use client';

import {FormEvent, useState} from 'react';
import {useTranslations} from 'next-intl';
import toast from 'react-hot-toast';

type FormData = {
  name: string;
  email: string;
  message: string;
};

type FieldErrors = Partial<Record<keyof FormData, string>>;

const emptyForm: FormData = {name: '', email: '', message: ''};

export default function Contact() {
  const t = useTranslations('contact');
  const [formData, setFormData] = useState<FormData>(emptyForm);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleChange = (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
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
    <section id="contact" className="contact-section" aria-labelledby="contact-title">
      <div className="contact-intro">
        <h2 id="contact-title">{t('title')}</h2>
        <p>{t('subtitle')}</p>
        <a href="mailto:me@joaoloureiro.dev.br" className="direct-email">
          <span>{t('direct_label')}</span>
          me@joaoloureiro.dev.br
        </a>
      </div>

      <form onSubmit={handleSubmit} className="contact-form" noValidate>
        <div className="form-field" data-invalid={Boolean(fieldErrors.name)}>
          <label htmlFor="name">{t('form_name')}</label>
          <input
            type="text"
            name="name"
            id="name"
            autoComplete="name"
            maxLength={120}
            value={formData.name}
            onChange={handleChange}
            aria-invalid={Boolean(fieldErrors.name)}
            aria-describedby={fieldErrors.name ? 'name-error' : undefined}
          />
          {fieldErrors.name && <p id="name-error" className="field-error">{fieldErrors.name}</p>}
        </div>

        <div className="form-field" data-invalid={Boolean(fieldErrors.email)}>
          <label htmlFor="email">{t('form_email')}</label>
          <input
            type="email"
            name="email"
            id="email"
            autoComplete="email"
            maxLength={254}
            value={formData.email}
            onChange={handleChange}
            aria-invalid={Boolean(fieldErrors.email)}
            aria-describedby={fieldErrors.email ? 'email-error' : undefined}
          />
          {fieldErrors.email && <p id="email-error" className="field-error">{fieldErrors.email}</p>}
        </div>

        <div className="form-field form-field-message" data-invalid={Boolean(fieldErrors.message)}>
          <label htmlFor="message">{t('form_message')}</label>
          <textarea
            name="message"
            id="message"
            rows={5}
            maxLength={4000}
            value={formData.message}
            onChange={handleChange}
            aria-invalid={Boolean(fieldErrors.message)}
            aria-describedby={fieldErrors.message ? 'message-error' : undefined}
          />
          {fieldErrors.message && <p id="message-error" className="field-error">{fieldErrors.message}</p>}
        </div>

        <div className="form-submit-row">
          <p>{t('privacy_note')}</p>
          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? t('status_sending') : t('submit_button')}
            <svg viewBox="0 0 24 16" aria-hidden="true"><path d="M 0 8 H 21 M 15 2 L 21 8 L 15 14" /></svg>
          </button>
        </div>
      </form>
    </section>
  );
}
