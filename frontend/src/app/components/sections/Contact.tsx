'use client';

import {useState} from 'react';
import type {FormEvent} from 'react';
import {ArrowUpRightIcon} from '@heroicons/react/24/outline';
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
type Engagement = 'monthly' | 'task' | '';

export default function Contact({initialEngagement}: {initialEngagement: Engagement}) {
  const t = useTranslations('contact');
  const offer = useTranslations('engagement');
  const [selection, setSelection] = useState({source: initialEngagement, value: initialEngagement});
  if (selection.source !== initialEngagement) {
    setSelection({source: initialEngagement, value: initialEngagement});
  }
  const engagement = selection.source === initialEngagement ? selection.value : initialEngagement;
  const [formData, setFormData] = useState<FormData>(emptyForm);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const prefix = engagement ? `${offer('form_engagement')}: ${offer(engagement)}\n\n` : '';

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
    else if (prefix.length + formData.message.length > 4000) errors.message = t('contact_payload_too_large');
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
          body: JSON.stringify({...formData, message: prefix + formData.message})
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
    <section id="contact" className="contract-contact" aria-labelledby="contact-title">
      <div className="contract-wrap contract-contact-grid">
        <div className="contact-intro">
          <h2 id="contact-title">{offer('contact_heading')}</h2>
          <p>{offer('contact_intro')}</p>
          <a href={`mailto:${contactEmail}`} className="direct-email">
            <span>{t('direct_label')}</span>
            {contactEmail}
          </a>
        </div>

        <form onSubmit={handleSubmit} className="contract-form" noValidate>
          <div className="form-field">
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
            {fieldErrors.name && <p id="name-error" className="field-error">{fieldErrors.name}</p>}
          </div>

          <div className="form-field">
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
            {fieldErrors.email && <p id="email-error" className="field-error">{fieldErrors.email}</p>}
          </div>

          <div className="form-field form-field-wide">
            <label htmlFor="engagement">{offer('form_engagement')}</label>
            <select
              id="engagement"
              value={engagement}
              onChange={(event) => {
                const {value} = event.target;
                setSelection({
                  source: initialEngagement,
                  value: value === 'monthly' || value === 'task' ? value : ''
                });
                setFieldErrors((current) => ({...current, message: undefined}));
              }}
            >
              <option value="">{offer('form_unsure')}</option>
              <option value="monthly">{offer('monthly')}</option>
              <option value="task">{offer('task')}</option>
            </select>
          </div>
          <div className="form-field form-field-wide">
            <label htmlFor="message">{t('form_message')}</label>
            <textarea
              name="message"
              id="message"
              required
              rows={5}
              maxLength={4000 - prefix.length}
              value={formData.message}
              onChange={handleChange}
              aria-invalid={Boolean(fieldErrors.message)}
              aria-describedby={fieldErrors.message ? 'message-error' : undefined}
            />
            {fieldErrors.message && <p id="message-error" className="field-error">{fieldErrors.message}</p>}
          </div>

          <div className="contract-submit">
            <p>{t('privacy_note')}</p>
            <button className="contract-button" type="submit" disabled={isSubmitting}>
              {isSubmitting ? t('status_sending') : t('submit_button')}
              <ArrowUpRightIcon aria-hidden="true" />
            </button>
          </div>
        </form>
      </div>
    </section>
  );
}
